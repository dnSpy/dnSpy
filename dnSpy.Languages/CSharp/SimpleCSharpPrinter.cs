/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Languages;
using dnSpy.Decompiler.Shared;
using dnSpy.Languages.Properties;
using dnSpy.Shared.MVVM;

namespace dnSpy.Languages.CSharp {
	public struct SimpleCSharpPrinter {
		const int MAX_RECURSION = 200;
		const int MAX_OUTPUT_LEN = 1024 * 4;
		int recursionCounter;
		int lineLength;
		bool outputLengthExceeded;
		bool forceWrite;

		readonly ISyntaxHighlightOutput output;
		SimplePrinterFlags flags;

		static readonly Dictionary<string, string[]> nameToOperatorName = new Dictionary<string, string[]> {
			{ "op_Addition", "operator +".Split(' ') },
			{ "op_BitwiseAnd", "operator &".Split(' ') },
			{ "op_BitwiseOr", "operator |".Split(' ') },
			{ "op_Decrement", "operator --".Split(' ') },
			{ "op_Division", "operator /".Split(' ') },
			{ "op_Equality", "operator ==".Split(' ') },
			{ "op_ExclusiveOr", "operator ^".Split(' ') },
			{ "op_Explicit", "explicit operator".Split(' ') },
			{ "op_False", "operator false".Split(' ') },
			{ "op_GreaterThan", "operator >".Split(' ') },
			{ "op_GreaterThanOrEqual", "operator >=".Split(' ') },
			{ "op_Implicit", "implicit operator".Split(' ') },
			{ "op_Increment", "operator ++".Split(' ') },
			{ "op_Inequality", "operator !=".Split(' ') },
			{ "op_LeftShift", "operator <<".Split(' ') },
			{ "op_LessThan", "operator <".Split(' ') },
			{ "op_LessThanOrEqual", "operator <=".Split(' ') },
			{ "op_LogicalNot", "operator !".Split(' ') },
			{ "op_Modulus", "operator %".Split(' ') },
			{ "op_Multiply", "operator *".Split(' ') },
			{ "op_OnesComplement", "operator ~".Split(' ') },
			{ "op_RightShift", "operator >>".Split(' ') },
			{ "op_Subtraction", "operator -".Split(' ') },
			{ "op_True", "operator true".Split(' ') },
			{ "op_UnaryNegation", "operator -".Split(' ') },
			{ "op_UnaryPlus", "operator +".Split(' ') },
		};

		bool ShowModuleNames {
			get { return (flags & SimplePrinterFlags.ShowModuleNames) != 0; }
		}

		bool ShowParameterTypes {
			get { return (flags & SimplePrinterFlags.ShowParameterTypes) != 0; }
		}

		bool ShowParameterNames {
			get { return (flags & SimplePrinterFlags.ShowParameterNames) != 0; }
		}

		bool ShowOwnerTypes {
			get { return (flags & SimplePrinterFlags.ShowOwnerTypes) != 0; }
		}

		bool ShowReturnTypes {
			get { return (flags & SimplePrinterFlags.ShowReturnTypes) != 0; }
		}

		bool ShowNamespaces {
			get { return (flags & SimplePrinterFlags.ShowNamespaces) != 0; }
		}

		bool ShowTypeKeywords {
			get { return (flags & SimplePrinterFlags.ShowTypeKeywords) != 0; }
		}

		bool UseDecimal {
			get { return (flags & SimplePrinterFlags.UseDecimal) != 0; }
		}

		bool ShowTokens {
			get { return (flags & SimplePrinterFlags.ShowTokens) != 0; }
		}

		bool ShowArrayValueSizes {
			get { return (flags & SimplePrinterFlags.ShowArrayValueSizes) != 0; }
		}

		bool ShowFieldLiteralValues {
			get { return (flags & SimplePrinterFlags.ShowFieldLiteralValues) != 0; }
		}

		bool ShowParameterLiteralValues {
			get { return (flags & SimplePrinterFlags.ShowParameterLiteralValues) != 0; }
		}

		public SimpleCSharpPrinter(ISyntaxHighlightOutput output, SimplePrinterFlags flags) {
			this.output = output;
			this.flags = flags;
			this.recursionCounter = 0;
			this.lineLength = 0;
			this.outputLengthExceeded = false;
			this.forceWrite = false;
		}

		static string FilterName(string s) {
			const int MAX_NAME_LEN = 0x100;
			if (s == null)
				return "<<NULL>>";

			var sb = new StringBuilder(s.Length);

			foreach (var c in s) {
				if (sb.Length >= MAX_NAME_LEN)
					break;
				if (c >= ' ')
					sb.Append(c);
				else
					sb.Append(string.Format("\\u{0:X4}", (ushort)c));
			}

			if (sb.Length > MAX_NAME_LEN)
				sb.Length = MAX_NAME_LEN;
			return sb.ToString();
		}

		static readonly HashSet<string> isKeyword = new HashSet<string>(StringComparer.Ordinal) {
			"abstract", "as", "base", "bool", "break", "byte", "case", "catch",
			"char", "checked", "class", "const", "continue", "decimal", "default", "delegate",
			"do", "double", "else", "enum", "event", "explicit", "extern", "false",
			"finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
			"in", "int", "interface", "internal", "is", "lock", "long", "namespace",
			"new", "null", "object", "operator", "out", "override", "params", "private",
			"protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
			"sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
			"true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
			"using", "virtual", "void", "volatile", "while",
		};

		void WriteIdentifier(string id, TextTokenKind tokenKind) {
			if (isKeyword.Contains(id))
				OutputWrite("@", TextTokenKind.Operator);
			OutputWrite(IdentifierEscaper.Escape(id), tokenKind);
		}

		static string RemoveGenericTick(string s) {
			int index = s.LastIndexOf('`');
			if (index < 0)
				return s;
			if (s[0] == '<')	// check if compiler generated name
				return s;
			return s.Substring(0, index);
		}

		static string GetFileName(string s) {
			// Don't use Path.GetFileName() since it can throw if input contains invalid chars
			int index = Math.Max(s.LastIndexOf('/'), s.LastIndexOf('\\'));
			if (index < 0)
				return s;
			return s.Substring(index + 1);
		}

		void OutputWrite(string s, TextTokenKind color) {
			if (!forceWrite) {
				if (outputLengthExceeded)
					return;
				if (lineLength + s.Length > MAX_OUTPUT_LEN) {
					s = s.Substring(0, MAX_OUTPUT_LEN - lineLength);
					s += "[…]";
					outputLengthExceeded = true;
				}
			}
			output.Write(s, color);
			lineLength += s.Length;
		}

		void WriteSpace() {
			OutputWrite(" ", TextTokenKind.Text);
		}

		void WriteCommaSpace() {
			OutputWrite(",", TextTokenKind.Operator);
			WriteSpace();
		}

		void WritePeriod() {
			OutputWrite(".", TextTokenKind.Operator);
		}

		void WriteError() {
			OutputWrite("???", TextTokenKind.Error);
		}

		void WriteNumber(object value) {
			OutputWrite(ConvertNumberToString(value), TextTokenKind.Number);
		}

		void WriteSystemTypeKeyword(string name, string keyword) {
			if (ShowTypeKeywords)
				OutputWrite(keyword, TextTokenKind.Keyword);
			else
				WriteSystemType(name);
		}

		void WriteSystemType(string name) {
			if (ShowNamespaces) {
				OutputWrite("System", TextTokenKind.NamespacePart);
				OutputWrite(".", TextTokenKind.Operator);
			}
			OutputWrite(name, TextTokenKind.Type);
		}

		string ConvertNumberToString(object value) {
			if (value == null)
				return string.Empty;
			if (!UseDecimal) {
				switch (Type.GetTypeCode(value.GetType())) {
				case TypeCode.SByte:	return string.Format("0x{0:X2}", value);
				case TypeCode.Int16:	return string.Format("0x{0:X4}", value);
				case TypeCode.Int32:	return string.Format("0x{0:X8}", value);
				case TypeCode.Int64:	return string.Format("0x{0:X16}", value);
				case TypeCode.Byte:		return string.Format("0x{0:X2}", value);
				case TypeCode.UInt16:	return string.Format("0x{0:X4}", value);
				case TypeCode.UInt32:	return string.Format("0x{0:X8}", value);
				case TypeCode.UInt64:	return string.Format("0x{0:X16}", value);
				}
				if (value is IntPtr)
					return string.Format("0x{0:X}", ((IntPtr)value).ToInt64());
				if (value is UIntPtr)
					return string.Format("0x{0:X}", ((UIntPtr)value).ToUInt64());
			}
			return value.ToString();
		}

		void WriteToken(IMDTokenProvider tok) {
			if (!ShowTokens)
				return;
			Debug.Assert(tok != null);
			if (tok == null)
				return;
			OutputWrite(string.Format("/*0x{0:X8}*/", tok.MDToken.Raw), TextTokenKind.Comment);
		}

		public void WriteToolTip(IMemberRef member) {
			if (member == null) {
				WriteError();
				return;
			}

			var method = member as IMethod;
			if (method != null && method.MethodSig != null) {
				WriteToolTip(method);
				return;
			}

			var field = member as IField;
			if (field != null && field.FieldSig != null) {
				WriteToolTip(field);
				return;
			}

			var prop = member as PropertyDef;
			if (prop != null && prop.PropertySig != null) {
				WriteToolTip(prop);
				return;
			}

			var evt = member as EventDef;
			if (evt != null && evt.EventType != null) {
				WriteToolTip(evt);
				return;
			}

			var tdr = member as ITypeDefOrRef;
			if (tdr != null) {
				WriteToolTip(tdr);
				return;
			}

			var gp = member as GenericParam;
			if (gp != null) {
				WriteToolTip(gp);
				return;
			}

			Debug.Fail("Unknown reference");
		}

		public void Write(IMemberRef member) {
			if (member == null) {
				WriteError();
				return;
			}

			var method = member as IMethod;
			if (method != null && method.MethodSig != null) {
				Write(method);
				return;
			}

			var field = member as IField;
			if (field != null && field.FieldSig != null) {
				Write(field);
				return;
			}

			var prop = member as PropertyDef;
			if (prop != null && prop.PropertySig != null) {
				Write(prop);
				return;
			}

			var evt = member as EventDef;
			if (evt != null && evt.EventType != null) {
				Write(evt);
				return;
			}

			var tdr = member as ITypeDefOrRef;
			if (tdr != null) {
				Write(tdr);
				return;
			}

			var gp = member as GenericParam;
			if (gp != null) {
				Write(gp);
				return;
			}

			Debug.Fail("Unknown reference");
		}

		void WriteToolTip(IMethod method) {
			if (method == null) {
				WriteError();
				return;
			}

			Write(method);

			var td = method.DeclaringType.ResolveTypeDef();
			if (td != null) {
				int overloads = GetNumberOfOverloads(td, method.Name);
				if (overloads == 1)
					OutputWrite(string.Format(" (+ {0})", Languages_Resources.ToolTip_OneMethodOverload), TextTokenKind.Text);
				else if (overloads > 1)
					OutputWrite(string.Format(" (+ {0})", string.Format(Languages_Resources.ToolTip_NMethodOverloads, overloads)), TextTokenKind.Text);
			}
		}

		static int GetNumberOfOverloads(TypeDef type, string name) {
			var hash = new HashSet<MethodDef>(MethodEqualityComparer.DontCompareDeclaringTypes);
			while (type != null) {
				foreach (var m in type.Methods) {
					if (m.Name == name)
						hash.Add(m);
				}
				type = type.BaseType.ResolveTypeDef();
			}
			return hash.Count - 1;
		}

		void WriteType(ITypeDefOrRef type, bool useNamespaces, bool useTypeKeywords) {
			var td = type as TypeDef;
			if (td == null && type is TypeRef)
				td = ((TypeRef)type).Resolve();
			if (td == null ||
				td.GenericParameters.Count == 0 ||
				(td.DeclaringType != null && td.DeclaringType.GenericParameters.Count >= td.GenericParameters.Count)) {
				var oldFlags = this.flags;
				this.flags &= ~(SimplePrinterFlags.ShowNamespaces | SimplePrinterFlags.ShowTypeKeywords);
				if (useNamespaces)
					this.flags |= SimplePrinterFlags.ShowNamespaces;
				if (useTypeKeywords)
					this.flags |= SimplePrinterFlags.ShowTypeKeywords;
				Write(type);
				this.flags = oldFlags;
				return;
			}

			var typeSig = type.ToTypeSig();

			int numGenParams = td.GenericParameters.Count;
			if (type.DeclaringType != null) {
				var oldFlags = this.flags;
				this.flags &= ~(SimplePrinterFlags.ShowNamespaces | SimplePrinterFlags.ShowTypeKeywords);
				if (useNamespaces)
					this.flags |= SimplePrinterFlags.ShowNamespaces;
				Write(type.DeclaringType);
				this.flags = oldFlags;
				OutputWrite(".", TextTokenKind.Operator);
				numGenParams = numGenParams - td.DeclaringType.GenericParameters.Count;
				if (numGenParams < 0)
					numGenParams = 0;
			}
			else if (useNamespaces && !UTF8String.IsNullOrEmpty(td.Namespace)) {
				foreach (var ns in td.Namespace.String.Split('.')) {
					WriteIdentifier(ns, TextTokenKind.NamespacePart);
					OutputWrite(".", TextTokenKind.Operator);
				}
			}

			WriteIdentifier(RemoveGenericTick(td.Name), TextTokenKindUtils.GetTextTokenType(td));
			WriteToken(type);
			var genParams = td.GenericParameters.Skip(td.GenericParameters.Count - numGenParams).ToArray();
			WriteGenerics(genParams, TextTokenKind.TypeGenericParameter);
		}

		bool WriteRefIfByRef(TypeSig typeSig, ParamDef pd) {
			if (typeSig.RemovePinnedAndModifiers() is ByRefSig) {
				if (pd != null && (!pd.IsIn && pd.IsOut)) {
					OutputWrite("out", TextTokenKind.Keyword);
					WriteSpace();
				}
				else {
					OutputWrite("ref", TextTokenKind.Keyword);
					WriteSpace();
				}
				return true;
			}
			return false;
		}

		void Write(IMethod method) {
			if (method == null) {
				WriteError();
				return;
			}

			var info = new MethodInfo(method);
			WriteModuleName(info);
			WriteReturnType(info);

			if (ShowOwnerTypes) {
				Write(method.DeclaringType);
				OutputWrite(".", TextTokenKind.Operator);
			}
			if (info.MethodDef != null && info.MethodDef.IsConstructor && method.DeclaringType != null)
				WriteIdentifier(RemoveGenericTick(method.DeclaringType.Name), TextTokenKindUtils.GetTextTokenType(method));
			else if (info.MethodDef != null && info.MethodDef.Overrides.Count > 0) {
				var ovrMeth = (IMemberRef)info.MethodDef.Overrides[0].MethodDeclaration;
				WriteType(ovrMeth.DeclaringType, false, ShowTypeKeywords);
				OutputWrite(".", TextTokenKind.Operator);
				WriteMethodName(method, ovrMeth.Name);
			}
			else
				WriteMethodName(method, method.Name);
			WriteToken(method);

			WriteGenericArguments(info);
			WriteMethodParameterList(info, "(", ")");
		}

		void WriteMethodName(IMethod method, string name) {
			string[] list;
			if (nameToOperatorName.TryGetValue(name, out list)) {
				for (int i = 0; i < list.Length; i++) {
					if (i > 0)
						WriteSpace();
					var s = list[i];
					OutputWrite(s, 'a' <= s[0] && s[0] <= 'z' ? TextTokenKind.Keyword : TextTokenKind.Operator);
				}
			}
			else
				WriteIdentifier(name, TextTokenKindUtils.GetTextTokenType(method));
		}

		void WriteToolTip(IField field) {
			Write(field, true);
		}

		void Write(IField field) {
			Write(field, false);
		}

		void Write(IField field, bool isToolTip) {
			if (field == null) {
				WriteError();
				return;
			}

			var sig = field.FieldSig;
			var td = field.DeclaringType.ResolveTypeDef();
			bool isEnumOwner = td != null && td.IsEnum;

			var fd = field.ResolveFieldDef();
			if (!isEnumOwner || (fd != null && !fd.IsLiteral)) {
				if (isToolTip)
					OutputWrite(string.Format("({0})", fd != null && fd.IsLiteral ? Languages_Resources.ToolTip_Constant : Languages_Resources.ToolTip_Field), TextTokenKind.Text);
				WriteSpace();
				Write(sig.Type, null, null, null);
				WriteSpace();
			}
			if (ShowOwnerTypes) {
				Write(field.DeclaringType);
				OutputWrite(".", TextTokenKind.Operator);
			}
			WriteIdentifier(field.Name, TextTokenKindUtils.GetTextTokenType(field));
			WriteToken(field);
			if (ShowFieldLiteralValues && fd != null && fd.IsLiteral && fd.Constant != null) {
				WriteSpace();
				OutputWrite("=", TextTokenKind.Operator);
				WriteSpace();
				WriteConstant(fd.Constant.Value);
			}
		}

		void WriteConstant(object obj) {
			if (obj == null) {
				OutputWrite("null", TextTokenKind.Keyword);
				return;
			}

			switch (Type.GetTypeCode(obj.GetType())) {
			case TypeCode.Boolean:
				OutputWrite((bool)obj ? "true" : "false", TextTokenKind.Keyword);
				break;

			case TypeCode.Char:
				OutputWrite(NumberVMUtils.ToString((char)obj), TextTokenKind.Char);
				break;

			case TypeCode.SByte:
				OutputWrite(NumberVMUtils.ToString((sbyte)obj, sbyte.MinValue, sbyte.MaxValue, UseDecimal), TextTokenKind.Number);
				break;

			case TypeCode.Byte:
				OutputWrite(NumberVMUtils.ToString((byte)obj, byte.MinValue, byte.MaxValue, UseDecimal), TextTokenKind.Number);
				break;

			case TypeCode.Int16:
				OutputWrite(NumberVMUtils.ToString((short)obj, short.MinValue, short.MaxValue, UseDecimal), TextTokenKind.Number);
				break;

			case TypeCode.UInt16:
				OutputWrite(NumberVMUtils.ToString((ushort)obj, ushort.MinValue, ushort.MaxValue, UseDecimal), TextTokenKind.Number);
				break;

			case TypeCode.Int32:
				OutputWrite(NumberVMUtils.ToString((int)obj, int.MinValue, int.MaxValue, UseDecimal), TextTokenKind.Number);
				break;

			case TypeCode.UInt32:
				OutputWrite(NumberVMUtils.ToString((uint)obj, uint.MinValue, uint.MaxValue, UseDecimal), TextTokenKind.Number);
				break;

			case TypeCode.Int64:
				OutputWrite(NumberVMUtils.ToString((long)obj, long.MinValue, long.MaxValue, UseDecimal), TextTokenKind.Number);
				break;

			case TypeCode.UInt64:
				OutputWrite(NumberVMUtils.ToString((ulong)obj, ulong.MinValue, ulong.MaxValue, UseDecimal), TextTokenKind.Number);
				break;

			case TypeCode.Single:
				OutputWrite(NumberVMUtils.ToString((float)obj), TextTokenKind.Number);
				break;

			case TypeCode.Double:
				OutputWrite(NumberVMUtils.ToString((double)obj), TextTokenKind.Number);
				break;

			case TypeCode.String:
				OutputWrite(NumberVMUtils.ToString((string)obj, true), TextTokenKind.String);
				break;

			default:
				Debug.Fail(string.Format("Unknown constant: '{0}'", obj));
				OutputWrite(obj.ToString(), TextTokenKind.Text);
				break;
			}
		}

		void WriteToolTip(PropertyDef prop) {
			Write(prop);
		}

		void Write(PropertyDef prop) {
			if (prop == null) {
				WriteError();
				return;
			}

			var getMethod = prop.GetMethods.FirstOrDefault();
			var setMethod = prop.SetMethods.FirstOrDefault();
			var md = getMethod ?? setMethod;
			if (md == null) {
				WriteError();
				return;
			}

			var info = new MethodInfo(md, md == setMethod);
			WriteModuleName(info);
			WriteReturnType(info);
			if (ShowOwnerTypes) {
				Write(prop.DeclaringType);
				OutputWrite(".", TextTokenKind.Operator);
			}
			var ovrMeth = md == null || md.Overrides.Count == 0 ? null : md.Overrides[0].MethodDeclaration;
			if (prop.IsIndexer()) {
				if (ovrMeth != null) {
					WriteType(ovrMeth.DeclaringType, false, ShowTypeKeywords);
					OutputWrite(".", TextTokenKind.Operator);
				}
				OutputWrite("this", TextTokenKind.Keyword);
				WriteGenericArguments(info);
				WriteMethodParameterList(info, "[", "]");
			}
			else if (ovrMeth != null && GetPropName(ovrMeth) != null) {
				WriteType(ovrMeth.DeclaringType, false, ShowTypeKeywords);
				OutputWrite(".", TextTokenKind.Operator);
				WriteIdentifier(GetPropName(ovrMeth), TextTokenKindUtils.GetTextTokenType(prop));
			}
			else
				WriteIdentifier(prop.Name, TextTokenKindUtils.GetTextTokenType(prop));
			WriteToken(prop);

			WriteSpace();
			OutputWrite("{", TextTokenKind.Operator);
			if (prop.GetMethods.Count > 0) {
				WriteSpace();
				OutputWrite("get", TextTokenKind.Keyword);
				OutputWrite(";", TextTokenKind.Operator);
			}
			if (prop.SetMethods.Count > 0) {
				WriteSpace();
				OutputWrite("set", TextTokenKind.Keyword);
				OutputWrite(";", TextTokenKind.Operator);
			}
			WriteSpace();
			OutputWrite("}", TextTokenKind.Operator);
		}

		static string GetPropName(IMethod method) {
			if (method == null)
				return null;
			var name = method.Name;
			if (name.StartsWith("get_", StringComparison.Ordinal) || name.StartsWith("set_", StringComparison.Ordinal))
				return name.Substring(4);
			return null;
		}

		void WriteToolTip(EventDef evt) {
			Write(evt);
		}

		void Write(EventDef evt) {
			if (evt == null) {
				WriteError();
				return;
			}

			Write(evt.EventType);
			WriteSpace();
			if (ShowOwnerTypes) {
				Write(evt.DeclaringType);
				OutputWrite(".", TextTokenKind.Operator);
			}
			WriteIdentifier(evt.Name, TextTokenKindUtils.GetTextTokenType(evt));
			WriteToken(evt);
		}

		void WriteToolTip(GenericParam gp) {
			if (gp == null) {
				WriteError();
				return;
			}

			Write(gp);
			WriteSpace();
			OutputWrite(Languages_Resources.ToolTip_GenericParameterInTypeOrMethod, TextTokenKind.Text);
			WriteSpace();

			var td = gp.Owner as TypeDef;
			if (td != null)
				WriteType(td, ShowNamespaces, ShowTypeKeywords);
			else
				Write(gp.Owner as MethodDef);
		}

		void Write(GenericParam gp) {
			if (gp == null) {
				WriteError();
				return;
			}

			WriteIdentifier(gp.Name, TextTokenKindUtils.GetTextTokenType(gp));
			WriteToken(gp);
		}

		void WriteToolTip(ITypeDefOrRef type) {
			var td = type.ResolveTypeDef();

			MethodDef invoke;
			if (IsDelegate(td) && (invoke = td.FindMethod("Invoke")) != null && invoke.MethodSig != null) {
				OutputWrite("delegate", TextTokenKind.Keyword);
				WriteSpace();

				var info = new MethodInfo(invoke);
				WriteModuleName(info);
				WriteReturnType(info);

				// Always print the namespace here because that's what VS does
				WriteType(td, true, ShowTypeKeywords);

				WriteGenericArguments(info);
				WriteMethodParameterList(info, "(", ")");
				return;
			}

			if (td == null) {
				Write(type);
				return;
			}

			string keyword;
			if (td.IsEnum)
				keyword = "enum";
			else if (td.IsValueType)
				keyword = "struct";
			else if (td.IsInterface)
				keyword = "interface";
			else
				keyword = "class";
			OutputWrite(keyword, TextTokenKind.Keyword);
			WriteSpace();

			// Always print the namespace here because that's what VS does
			WriteType(type, true, false);
		}

		void Write(ITypeDefOrRef type) {
			if (type == null) {
				WriteError();
				return;
			}

			if (recursionCounter >= MAX_RECURSION)
				return;
			recursionCounter++;
			try {
				var ts = type as TypeSpec;
				if (ts != null) {
					Write(ts.TypeSig, null, null, null);
					return;
				}

				if (type.DeclaringType != null) {
					Write(type.DeclaringType);
					WritePeriod();
				}

				string keyword = GetTypeKeyword(type);
				if (keyword != null)
					OutputWrite(keyword, TextTokenKind.Keyword);
				else {
					WriteNamespace(type.Namespace);
					WriteIdentifier(RemoveGenericTick(type.Name), TextTokenKindUtils.GetTextTokenType(type));
				}
				WriteToken(type);
			}
			finally {
				recursionCounter--;
			}
		}

		void WriteNamespace(string ns) {
			if (!ShowNamespaces || string.IsNullOrEmpty(ns))
				return;
			var namespaces = ns.Split(nsSep);
			for (int i = 0; i < namespaces.Length; i++) {
				OutputWrite(namespaces[i], TextTokenKind.NamespacePart);
				WritePeriod();
			}
		}
		static readonly char[] nsSep = new char[] { '.' };

		string GetTypeKeyword(ITypeDefOrRef type) {
			if (!ShowTypeKeywords)
				return null;
			if (type == null || type.DeclaringType != null || type.Namespace != "System" || !type.DefinitionAssembly.IsCorLib())
				return null;
			switch (type.TypeName) {
			case "Void":	return "void";
			case "Boolean":	return "bool";
			case "Byte":	return "byte";
			case "Char":	return "char";
			case "Decimal":	return "decimal";
			case "Double":	return "double";
			case "Int16":	return "short";
			case "Int32":	return "int";
			case "Int64":	return "long";
			case "Object":	return "object";
			case "SByte":	return "sbyte";
			case "Single":	return "float";
			case "String":	return "string";
			case "UInt16":	return "ushort";
			case "UInt32":	return "uint";
			case "UInt64":	return "ulong";
			default:		return null;
			}
		}

		void Write(TypeSig type, ParamDef ownerParam, IList<TypeSig> typeGenArgs, IList<TypeSig> methGenArgs) {
			WriteRefIfByRef(type, ownerParam);
			var byRef = type.RemovePinnedAndModifiers() as ByRefSig;
			if (byRef != null)
				type = byRef.Next;
			Write(type, typeGenArgs, methGenArgs);
		}

		static readonly TypeSig[] emptyTypeSigArray = new TypeSig[0];
		void Write(TypeSig type, IList<TypeSig> typeGenArgs, IList<TypeSig> methGenArgs) {
			if (type == null) {
				WriteError();
				return;
			}

			if (recursionCounter >= MAX_RECURSION)
				return;
			recursionCounter++;
			try {
				if (typeGenArgs == null)
					typeGenArgs = emptyTypeSigArray;
				if (methGenArgs == null)
					methGenArgs = emptyTypeSigArray;

				List<ArraySigBase> list = null;
				while (type != null && (type.ElementType == ElementType.SZArray || type.ElementType == ElementType.Array)) {
					if (list == null)
						list = new List<ArraySigBase>();
					list.Add((ArraySigBase)type);
					type = type.Next;
				}
				if (list != null) {
					Write(list[list.Count - 1].Next, typeGenArgs, methGenArgs);
					foreach (var aryType in list) {
						if (aryType.ElementType == ElementType.Array) {
							OutputWrite("[", TextTokenKind.Operator);
							uint rank = aryType.Rank;
							if (rank == 0)
								OutputWrite("<RANK0>", TextTokenKind.Error);
							else {
								if (rank == 1)
									OutputWrite("*", TextTokenKind.Operator);
								var indexes = aryType.GetLowerBounds();
								var dims = aryType.GetSizes();
								if (ShowArrayValueSizes) {
									for (int i = 0; (uint)i < rank; i++) {
										if (i > 0)
											WriteCommaSpace();
										if (i < indexes.Count && indexes[i] == 0)
											WriteNumber(dims[i]);
										else if (i < indexes.Count && i < dims.Count) {
											//TODO: How does VS print these arrays?
											WriteNumber((int)indexes[i]);
											OutputWrite("..", TextTokenKind.Operator);
											WriteNumber((int)(indexes[i] + dims[i]));
										}
									}
								}
								else {
									for (uint i = 1; i < rank; i++)
										OutputWrite(",", TextTokenKind.Operator);
								}
								for (uint i = 1; i < rank; i++)
									OutputWrite(",", TextTokenKind.Operator);
							}
							OutputWrite("]", TextTokenKind.Operator);
						}
						else {
							Debug.Assert(aryType.ElementType == ElementType.SZArray);
							OutputWrite("[]", TextTokenKind.Operator);
						}
					}
					return;
				}

				switch (type.ElementType) {
				case ElementType.Void:			WriteSystemTypeKeyword("Void", "void"); break;
				case ElementType.Boolean:		WriteSystemTypeKeyword("Boolean", "bool"); break;
				case ElementType.Char:			WriteSystemTypeKeyword("Char", "char"); break;
				case ElementType.I1:			WriteSystemTypeKeyword("SByte", "sbyte"); break;
				case ElementType.U1:			WriteSystemTypeKeyword("Byte", "byte"); break;
				case ElementType.I2:			WriteSystemTypeKeyword("Int16", "short"); break;
				case ElementType.U2:			WriteSystemTypeKeyword("UInt16", "ushort"); break;
				case ElementType.I4:			WriteSystemTypeKeyword("Int32", "int"); break;
				case ElementType.U4:			WriteSystemTypeKeyword("UInt32", "uint"); break;
				case ElementType.I8:			WriteSystemTypeKeyword("Int64", "long"); break;
				case ElementType.U8:			WriteSystemTypeKeyword("UInt64", "ulong"); break;
				case ElementType.R4:			WriteSystemTypeKeyword("Single", "float"); break;
				case ElementType.R8:			WriteSystemTypeKeyword("Double", "double"); break;
				case ElementType.String:		WriteSystemTypeKeyword("String", "string"); break;
				case ElementType.Object:		WriteSystemTypeKeyword("Object", "object"); break;

				case ElementType.TypedByRef:	WriteSystemType("TypedReference"); break;
				case ElementType.I:				WriteSystemType("IntPtr"); break;
				case ElementType.U:				WriteSystemType("UIntPtr"); break;

				case ElementType.Ptr:
					Write(type.Next, typeGenArgs, methGenArgs);
					OutputWrite("*", TextTokenKind.Operator);
					break;

				case ElementType.ByRef:
					Write(type.Next, typeGenArgs, methGenArgs);
					OutputWrite("&", TextTokenKind.Operator);
					break;

				case ElementType.ValueType:
				case ElementType.Class:
					var cvt = (TypeDefOrRefSig)type;
					Write(cvt.TypeDefOrRef);
					break;

				case ElementType.Var:
				case ElementType.MVar:
					var gsType = Read(type.ElementType == ElementType.Var ? typeGenArgs : methGenArgs, (int)((GenericSig)type).Number);
					if (gsType != null)
						Write(gsType, typeGenArgs, methGenArgs);
					else {
						var gp = ((GenericSig)type).GenericParam;
						Write(gp);
					}
					break;

				case ElementType.GenericInst:
					var gis = (GenericInstSig)type;
					if (IsSystemNullable(gis)) {
						Write(gis.GenericArguments[0], typeGenArgs, methGenArgs);
						OutputWrite("?", TextTokenKind.Operator);
					}
					else {
						Write(gis.GenericType, typeGenArgs, methGenArgs);
						OutputWrite("<", TextTokenKind.Operator);
						for (int i = 0; i < gis.GenericArguments.Count; i++) {
							if (i > 0)
								WriteCommaSpace();
							Write(gis.GenericArguments[i], typeGenArgs, methGenArgs);
						}
						OutputWrite(">", TextTokenKind.Operator);
					}
					break;

				case ElementType.FnPtr:
					OutputWrite("fnptr", TextTokenKind.Keyword);
					break;

				case ElementType.CModReqd:
				case ElementType.CModOpt:
				case ElementType.Pinned:
					Write(type.Next, typeGenArgs, methGenArgs);
					break;

				case ElementType.End:
				case ElementType.Array:		// handled above
				case ElementType.ValueArray:
				case ElementType.R:
				case ElementType.SZArray:	// handled above
				case ElementType.Internal:
				case ElementType.Module:
				case ElementType.Sentinel:
				default:
					break;
				}
			}
			finally {
				recursionCounter--;
			}
		}

		TypeSig Read(IList<TypeSig> list, int index) {
			if ((uint)index < (uint)list.Count)
				return list[index];
			return null;
		}

		public void WriteToolTip(IVariable variable, string name) {
			if (variable == null) {
				WriteError();
				return;
			}

			var isLocal = variable is Local;
			OutputWrite(string.Format("({0}) ", isLocal ? Languages_Resources.ToolTip_Local : Languages_Resources.ToolTip_Parameter), TextTokenKind.Text);
			Write(variable.Type, !isLocal ? ((Parameter)variable).ParamDef : null, null, null);
			WriteSpace();
			WriteIdentifier(GetName(variable, name), isLocal ? TextTokenKind.Local : TextTokenKind.Parameter);
			var p = variable as Parameter;
			var pd = p == null ? null : p.ParamDef;
			if (pd != null)
				WriteToken(pd);
		}

		struct MethodInfo {
			public readonly ModuleDef ModuleDef;
			public readonly IList<TypeSig> TypeGenericParams;
			public readonly IList<TypeSig> MethodGenericParams;
			public readonly MethodDef MethodDef;
			public readonly MethodSig MethodSig;
			public readonly bool RetTypeIsLastArgType;

			public MethodInfo(IMethod method, bool retTypeIsLastArgType = false) {
				this.ModuleDef = method.Module;
				this.TypeGenericParams = null;
				this.MethodGenericParams = null;
				this.MethodSig = method.MethodSig ?? new MethodSig(CallingConvention.Default);
				this.RetTypeIsLastArgType = retTypeIsLastArgType;

				this.MethodDef = method as MethodDef;
				var ms = method as MethodSpec;
				var mr = method as MemberRef;
				if (ms != null) {
					var ts = ms.Method == null ? null : ms.Method.DeclaringType as TypeSpec;
					if (ts != null) {
						var gp = ts.TypeSig.RemovePinnedAndModifiers() as GenericInstSig;
						if (gp != null)
							this.TypeGenericParams = gp.GenericArguments;
					}

					var gsSig = ms.GenericInstMethodSig;
					if (gsSig != null)
						this.MethodGenericParams = gsSig.GenericArguments;

					this.MethodDef = ms.Method.ResolveMethodDef();
				}
				else if (mr != null) {
					var ts = mr.DeclaringType as TypeSpec;
					if (ts != null) {
						var gp = ts.TypeSig.RemovePinnedAndModifiers() as GenericInstSig;
						if (gp != null)
							this.TypeGenericParams = gp.GenericArguments;
					}

					this.MethodDef = mr.ResolveMethod();
				}
			}
		}

		void Write(ModuleDef module) {
			try {
				if (recursionCounter++ >= MAX_RECURSION)
					return;
				if (module == null) {
					OutputWrite("null module", TextTokenKind.Error);
					return;
				}

				var name = GetFileName(module.Location);
				OutputWrite(FilterName(name), TextTokenKind.Module);
			}
			finally {
				recursionCounter--;
			}
		}

		void WriteModuleName(MethodInfo info) {
			if (!ShowModuleNames)
				return;

			Write(info.ModuleDef);
			OutputWrite("!", TextTokenKind.Operator);
			return;
		}

		void WriteReturnType(MethodInfo info) {
			if (!ShowReturnTypes)
				return;
			if (!(info.MethodDef != null && info.MethodDef.IsConstructor)) {
				TypeSig retType;
				ParamDef retParamDef;
				if (info.RetTypeIsLastArgType) {
					retType = info.MethodSig.Params.LastOrDefault();
					if (info.MethodDef == null)
						retParamDef = null;
					else {
						var l = info.MethodDef.Parameters.LastOrDefault();
						retParamDef = l == null ? null : l.ParamDef;
					}
				}
				else {
					retType = info.MethodSig.RetType;
					retParamDef = info.MethodDef == null ? null : info.MethodDef.Parameters.ReturnParameter.ParamDef;
				}
				Write(retType, retParamDef, info.TypeGenericParams, info.MethodGenericParams);
				WriteSpace();
			}
		}

		void WriteGenericArguments(MethodInfo info) {
			if (info.MethodSig.GenParamCount > 0) {
				if (info.MethodGenericParams != null)
					WriteGenerics(info.MethodGenericParams, TextTokenKind.MethodGenericParameter, GenericParamContext.Create(info.MethodDef));
				else if (info.MethodDef != null)
					WriteGenerics(info.MethodDef.GenericParameters, TextTokenKind.MethodGenericParameter);
			}
		}

		void WriteMethodParameterList(MethodInfo info, string lparen, string rparen) {
			if (!ShowParameterTypes && !ShowParameterNames)
				return;

			OutputWrite(lparen, TextTokenKind.Operator);
			int baseIndex = info.MethodSig.HasThis ? 1 : 0;
			int count = info.MethodSig.Params.Count;
			if (info.RetTypeIsLastArgType)
				count--;
			for (int i = 0; i < count; i++) {
				if (i > 0)
					WriteCommaSpace();
				ParamDef pd;
				if (info.MethodDef != null && baseIndex + i < info.MethodDef.Parameters.Count)
					pd = info.MethodDef.Parameters[baseIndex + i].ParamDef;
				else
					pd = null;
				bool needSpace = false;
				if (ShowParameterTypes) {
					needSpace = true;

					if (pd != null && pd.CustomAttributes.IsDefined("System.ParamArrayAttribute")) {
						OutputWrite("params", TextTokenKind.Keyword);
						WriteSpace();
					}
					var paramType = info.MethodSig.Params[i];
					Write(paramType, pd, info.TypeGenericParams, info.MethodGenericParams);
				}
				if (ShowParameterNames) {
					if (needSpace)
						WriteSpace();
					needSpace = true;

					if (pd != null) {
						WriteIdentifier(pd.Name, TextTokenKind.Parameter);
						WriteToken(pd);
					}
					else
						WriteIdentifier(string.Format("A_{0}", i), TextTokenKind.Parameter);
				}
				if (ShowParameterLiteralValues && pd != null && pd.Constant != null) {
					if (needSpace)
						WriteSpace();
					needSpace = true;

					var c = pd.Constant.Value;
					WriteSpace();
					OutputWrite("=", TextTokenKind.Operator);
					WriteSpace();

					var t = info.MethodSig.Params[i].RemovePinnedAndModifiers();
					if (t.GetElementType() == ElementType.ByRef)
						t = t.Next;
					if (c == null && t != null && t.IsValueType) {
						OutputWrite("default", TextTokenKind.Keyword);
						OutputWrite("(", TextTokenKind.Operator);
						Write(t, pd, info.TypeGenericParams, info.MethodGenericParams);
						OutputWrite(")", TextTokenKind.Operator);
					}
					else
						WriteConstant(c);
				}
			}
			OutputWrite(rparen, TextTokenKind.Operator);
		}

		void WriteGenerics(IList<GenericParam> gps, TextTokenKind gpTokenType) {
			if (gps == null || gps.Count == 0)
				return;
			OutputWrite("<", TextTokenKind.Operator);
			for (int i = 0; i < gps.Count; i++) {
				if (i > 0)
					WriteCommaSpace();
				var gp = gps[i];
				if (gp.IsCovariant) {
					OutputWrite("out", TextTokenKind.Keyword);
					WriteSpace();
				}
				else if (gp.IsContravariant) {
					OutputWrite(Languages_Resources.ToolTip_GenericParameterInTypeOrMethod, TextTokenKind.Keyword);
					WriteSpace();
				}
				WriteIdentifier(gp.Name, gpTokenType);
				WriteToken(gp);
			}
			OutputWrite(">", TextTokenKind.Operator);
		}

		void WriteGenerics(IList<TypeSig> gps, TextTokenKind gpTokenType, GenericParamContext gpContext) {
			if (gps == null || gps.Count == 0)
				return;
			OutputWrite("<", TextTokenKind.Operator);
			for (int i = 0; i < gps.Count; i++) {
				if (i > 0)
					WriteCommaSpace();
				Write(gps[i], null, null, null);
			}
			OutputWrite(">", TextTokenKind.Operator);
		}

		static string GetName(IVariable variable, string name) {
			if (!string.IsNullOrWhiteSpace(name))
				return name;
			var n = variable.Name;
			if (!string.IsNullOrWhiteSpace(n))
				return n;
			return string.Format("#{0}", variable.Index);
		}

		static bool IsSystemNullable(GenericInstSig gis) {
			var gt = gis.GenericType as ValueTypeSig;
			return gt != null &&
				gt.TypeDefOrRef != null &&
				gt.TypeDefOrRef.DefinitionAssembly.IsCorLib() &&
				gt.TypeDefOrRef.FullName == "System.Nullable`1";
		}

		static bool IsDelegate(TypeDef td) {
			return td != null &&
				new SigComparer().Equals(td.BaseType, td.Module.CorLibTypes.GetTypeRef("System", "MulticastDelegate")) &&
				td.BaseType.DefinitionAssembly.IsCorLib();
		}
	}
}

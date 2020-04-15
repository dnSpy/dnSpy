/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Globalization;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.Properties;

namespace dnSpy.Decompiler.CSharp {
	public struct CSharpFormatter {
		const string Keyword_true = "true";
		const string Keyword_false = "false";
		const string Keyword_null = "null";
		const string Keyword_out = "out";
		const string Keyword_in = "in";
		const string Keyword_ref = "ref";
		const string Keyword_readonly = "readonly";
		const string Keyword_this = "this";
		const string Keyword_get = "get";
		const string Keyword_set = "set";
		const string Keyword_add = "add";
		const string Keyword_remove = "remove";
		const string Keyword_enum = "enum";
		const string Keyword_struct = "struct";
		const string Keyword_interface = "interface";
		const string Keyword_class = "class";
		const string Keyword_namespace = "namespace";
		const string Keyword_params = "params";
		const string Keyword_default = "default";
		const string Keyword_delegate = "delegate";
		const string HexPrefix = "0x";
		const string VerbatimStringPrefix = "@";
		const string IdentifierEscapeBegin = "@";
		const string ModuleNameSeparator = "!";
		const string CommentBegin = "/*";
		const string CommentEnd = "*/";
		const string DeprecatedParenOpen = "[";
		const string DeprecatedParenClose = "]";
		const string MemberSpecialParenOpen = "(";
		const string MemberSpecialParenClose = ")";
		const string MethodParenOpen = "(";
		const string MethodParenClose = ")";
		const string DescriptionParenOpen = "(";
		const string DescriptionParenClose = ")";
		const string IndexerParenOpen = "[";
		const string IndexerParenClose = "]";
		const string PropertyParenOpen = "[";
		const string PropertyParenClose = "]";
		const string ArrayParenOpen = "[";
		const string ArrayParenClose = "]";
		const string TupleParenOpen = "(";
		const string TupleParenClose = ")";
		const string GenericParenOpen = "<";
		const string GenericParenClose = ">";
		const string DefaultParamValueParenOpen = "[";
		const string DefaultParamValueParenClose = "]";

		int recursionCounter;
		int lineLength;
		bool outputLengthExceeded;
		bool forceWrite;

		readonly ITextColorWriter output;
		FormatterOptions options;
		readonly CultureInfo cultureInfo;

		static readonly Dictionary<string, string[]> nameToOperatorName = new Dictionary<string, string[]>(StringComparer.Ordinal) {
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

		bool ShowModuleNames => (options & FormatterOptions.ShowModuleNames) != 0;
		bool ShowParameterTypes => (options & FormatterOptions.ShowParameterTypes) != 0;
		bool ShowParameterNames => (options & FormatterOptions.ShowParameterNames) != 0;
		bool ShowDeclaringTypes => (options & FormatterOptions.ShowDeclaringTypes) != 0;
		bool ShowReturnTypes => (options & FormatterOptions.ShowReturnTypes) != 0;
		bool ShowNamespaces => (options & FormatterOptions.ShowNamespaces) != 0;
		bool ShowIntrinsicTypeKeywords => (options & FormatterOptions.ShowIntrinsicTypeKeywords) != 0;
		bool UseDecimal => (options & FormatterOptions.UseDecimal) != 0;
		bool ShowTokens => (options & FormatterOptions.ShowTokens) != 0;
		bool ShowArrayValueSizes => (options & FormatterOptions.ShowArrayValueSizes) != 0;
		bool ShowFieldLiteralValues => (options & FormatterOptions.ShowFieldLiteralValues) != 0;
		bool ShowParameterLiteralValues => (options & FormatterOptions.ShowParameterLiteralValues) != 0;
		bool DigitSeparators => (options & FormatterOptions.DigitSeparators) != 0;

		public CSharpFormatter(ITextColorWriter output, FormatterOptions options, CultureInfo? cultureInfo) {
			this.output = output;
			this.options = options;
			this.cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
			recursionCounter = 0;
			lineLength = 0;
			outputLengthExceeded = false;
			forceWrite = false;
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

		void WriteIdentifier(string id, object data) {
			if (isKeyword.Contains(id))
				OutputWrite(IdentifierEscapeBegin + IdentifierEscaper.Escape(id), data);
			else
				OutputWrite(IdentifierEscaper.Escape(id), data);
		}

		void OutputWrite(string s, object data) {
			if (!forceWrite) {
				if (outputLengthExceeded)
					return;
				if (lineLength + s.Length > TypeFormatterUtils.MAX_OUTPUT_LEN) {
					s = s.Substring(0, TypeFormatterUtils.MAX_OUTPUT_LEN - lineLength);
					s += "[...]";
					outputLengthExceeded = true;
				}
			}
			output.Write(data, s);
			lineLength += s.Length;
		}

		void WriteSpace() => OutputWrite(" ", BoxedTextColor.Text);

		void WriteCommaSpace() {
			OutputWrite(",", BoxedTextColor.Punctuation);
			WriteSpace();
		}

		void WritePeriod() => OutputWrite(".", BoxedTextColor.Operator);

		void WriteError() => OutputWrite("???", BoxedTextColor.Error);

		void WriteSystemTypeKeyword(string name, string keyword, bool isValueType) {
			if (ShowIntrinsicTypeKeywords)
				OutputWrite(keyword, BoxedTextColor.Keyword);
			else
				WriteSystemType(name, isValueType);
		}

		void WriteSystemType(string name, bool isValueType) {
			if (ShowNamespaces) {
				OutputWrite("System", BoxedTextColor.Namespace);
				WritePeriod();
			}
			OutputWrite(name, BoxedTextColor.Type);
		}

		void WriteToken(IMDTokenProvider tok) {
			if (!ShowTokens)
				return;
			Debug2.Assert(!(tok is null));
			if (tok is null)
				return;
			OutputWrite(CommentBegin + ToFormattedUInt32(tok.MDToken.Raw) + CommentEnd, BoxedTextColor.Comment);
		}

		public void WriteToolTip(IMemberRef? member) {
			if (member is null) {
				WriteError();
				return;
			}

			if (member is IMethod method && !(method.MethodSig is null)) {
				WriteToolTip(method);
				return;
			}

			if (member is IField field && !(field.FieldSig is null)) {
				WriteToolTip(field);
				return;
			}

			if (member is PropertyDef prop && !(prop.PropertySig is null)) {
				WriteToolTip(prop);
				return;
			}

			if (member is EventDef evt && !(evt.EventType is null)) {
				WriteToolTip(evt);
				return;
			}

			if (member is ITypeDefOrRef tdr) {
				WriteToolTip(tdr);
				return;
			}

			if (member is GenericParam gp) {
				WriteToolTip(gp);
				return;
			}

			Debug.Fail("Unknown reference");
		}

		public void Write(IMemberRef? member) {
			if (member is null) {
				WriteError();
				return;
			}

			if (member is IMethod method && !(method.MethodSig is null)) {
				Write(method);
				return;
			}

			if (member is IField field && !(field.FieldSig is null)) {
				Write(field);
				return;
			}

			if (member is PropertyDef prop && !(prop.PropertySig is null)) {
				Write(prop);
				return;
			}

			if (member is EventDef evt && !(evt.EventType is null)) {
				Write(evt);
				return;
			}

			if (member is ITypeDefOrRef tdr) {
				Write(tdr, ShowModuleNames);
				return;
			}

			if (member is GenericParam gp) {
				Write(gp);
				return;
			}

			Debug.Fail("Unknown reference");
		}

		void WriteDeprecated(bool isDeprecated) {
			if (isDeprecated) {
				OutputWrite(DeprecatedParenOpen, BoxedTextColor.Punctuation);
				OutputWrite(dnSpy_Decompiler_Resources.CSharp_Deprecated_Member, BoxedTextColor.Text);
				OutputWrite(DeprecatedParenClose, BoxedTextColor.Punctuation);
				WriteSpace();
			}
		}

		void Write(MemberSpecialFlags flags) {
			if (flags == MemberSpecialFlags.None)
				return;
			OutputWrite(MemberSpecialParenOpen, BoxedTextColor.Punctuation);
			bool comma = false;
			if ((flags & MemberSpecialFlags.Awaitable) != 0) {
				comma = true;
				OutputWrite(dnSpy_Decompiler_Resources.CSharp_Awaitable_Method, BoxedTextColor.Text);
			}
			if ((flags & MemberSpecialFlags.Extension) != 0) {
				if (comma)
					WriteCommaSpace();
				OutputWrite(dnSpy_Decompiler_Resources.CSharp_Extension_Method, BoxedTextColor.Text);
			}
			OutputWrite(MemberSpecialParenClose, BoxedTextColor.Punctuation);
			WriteSpace();
		}

		void WriteToolTip(IMethod? method) {
			if (method is null) {
				WriteError();
				return;
			}

			WriteDeprecated(TypeFormatterUtils.IsDeprecated(method));
			Write(TypeFormatterUtils.GetMemberSpecialFlags(method));
			Write(method);

			var td = method.DeclaringType.ResolveTypeDef();
			if (!(td is null)) {
				var s = TypeFormatterUtils.GetNumberOfOverloadsString(td, method);
				if (!(s is null))
					OutputWrite(s, BoxedTextColor.Text);
			}
		}

		void WriteType(ITypeDefOrRef type, bool useNamespaces, bool useTypeKeywords) {
			var td = type as TypeDef;
			if (td is null && type is TypeRef)
				td = ((TypeRef)type).Resolve();
			if (td is null ||
				td.GenericParameters.Count == 0 ||
				(!(td.DeclaringType is null) && td.DeclaringType.GenericParameters.Count >= td.GenericParameters.Count)) {
				var oldFlags = options;
				options &= ~(FormatterOptions.ShowNamespaces | FormatterOptions.ShowIntrinsicTypeKeywords);
				if (useNamespaces)
					options |= FormatterOptions.ShowNamespaces;
				if (useTypeKeywords)
					options |= FormatterOptions.ShowIntrinsicTypeKeywords;
				Write(type);
				options = oldFlags;
				return;
			}

			int numGenParams = td.GenericParameters.Count;
			if (!(type.DeclaringType is null)) {
				var oldFlags = options;
				options &= ~(FormatterOptions.ShowNamespaces | FormatterOptions.ShowIntrinsicTypeKeywords);
				if (useNamespaces)
					options |= FormatterOptions.ShowNamespaces;
				Write(type.DeclaringType);
				options = oldFlags;
				WritePeriod();
				numGenParams = numGenParams - td.DeclaringType!.GenericParameters.Count;
				if (numGenParams < 0)
					numGenParams = 0;
			}
			else if (useNamespaces && !UTF8String.IsNullOrEmpty(td.Namespace)) {
				foreach (var ns in td.Namespace.String.Split('.')) {
					WriteIdentifier(ns, BoxedTextColor.Namespace);
					WritePeriod();
				}
			}

			WriteIdentifier(TypeFormatterUtils.RemoveGenericTick(td.Name), CSharpMetadataTextColorProvider.Instance.GetColor(td));
			WriteToken(type);
			var genParams = td.GenericParameters.Skip(td.GenericParameters.Count - numGenParams).ToArray();
			WriteGenerics(genParams, BoxedTextColor.TypeGenericParameter);
		}

		bool WriteRefIfByRef(TypeSig? typeSig, ParamDef? pd, bool forceReadOnly) {
			if (typeSig.RemovePinnedAndModifiers() is ByRefSig) {
				if (!(pd is null) && (!pd.IsIn && pd.IsOut)) {
					OutputWrite(Keyword_out, BoxedTextColor.Keyword);
					WriteSpace();
				}
				else if (!(pd is null) && (pd.IsIn && !pd.IsOut && TypeFormatterUtils.IsReadOnlyParameter(pd))) {
					OutputWrite(Keyword_in, BoxedTextColor.Keyword);
					WriteSpace();
				}
				else {
					OutputWrite(Keyword_ref, BoxedTextColor.Keyword);
					WriteSpace();
					if (forceReadOnly) {
						OutputWrite(Keyword_readonly, BoxedTextColor.Keyword);
						WriteSpace();
					}
				}
				return true;
			}
			return false;
		}

		void WriteAccessor(AccessorKind kind) {
			string keyword;
			switch (kind) {
			case AccessorKind.None:
			default:
				throw new InvalidOperationException();
			case AccessorKind.Getter:
				keyword = Keyword_get;
				break;
			case AccessorKind.Setter:
				keyword = Keyword_set;
				break;
			case AccessorKind.Adder:
				keyword = Keyword_add;
				break;
			case AccessorKind.Remover:
				keyword = Keyword_remove;
				break;
			}
			OutputWrite(".", BoxedTextColor.Operator);
			OutputWrite(keyword, BoxedTextColor.Keyword);
		}

		void Write(IMethod? method) {
			if (method is null) {
				WriteError();
				return;
			}

			var propInfo = TypeFormatterUtils.TryGetProperty(method as MethodDef);
			if (propInfo.kind != AccessorKind.None) {
				Write(propInfo.property, writeAccessors: false);
				WriteAccessor(propInfo.kind);
				return;
			}

			var eventInfo = TypeFormatterUtils.TryGetEvent(method as MethodDef);
			if (eventInfo.kind != AccessorKind.None) {
				Write(eventInfo.@event, writeAccessors: false);
				WriteAccessor(eventInfo.kind);
				return;
			}

			var info = new FormatterMethodInfo(method);
			WriteModuleName(info);

			string[]? operatorInfo;
			if (!(info.MethodDef is null) && info.MethodDef.IsConstructor && !(method.DeclaringType is null))
				operatorInfo = null;
			else if (!(info.MethodDef is null) && info.MethodDef.Overrides.Count > 0) {
				var ovrMeth = (IMemberRef)info.MethodDef.Overrides[0].MethodDeclaration;
				operatorInfo = TryGetOperatorInfo(ovrMeth.Name);
			}
			else
				operatorInfo = TryGetOperatorInfo(method.Name);
			bool isExplicitOrImplicit = !(operatorInfo is null) && (operatorInfo[0] == "explicit" || operatorInfo[0] == "implicit");

			if (!isExplicitOrImplicit)
				WriteReturnType(info, writeSpace: true, isReadOnly: TypeFormatterUtils.IsReadOnlyMethod(info.MethodDef));

			if (ShowDeclaringTypes) {
				Write(method.DeclaringType);
				WritePeriod();
			}
			if (!(info.MethodDef is null) && info.MethodDef.IsConstructor && !(method.DeclaringType is null))
				WriteIdentifier(TypeFormatterUtils.RemoveGenericTick(method.DeclaringType.Name), CSharpMetadataTextColorProvider.Instance.GetColor(method));
			else if (!(info.MethodDef is null) && info.MethodDef.Overrides.Count > 0) {
				var ovrMeth = (IMemberRef)info.MethodDef.Overrides[0].MethodDeclaration;
				WriteMethodName(method, ovrMeth.Name, operatorInfo);
			}
			else
				WriteMethodName(method, method.Name, operatorInfo);
			if (isExplicitOrImplicit) {
				WriteToken(method);
				WriteSpace();
				ForceWriteReturnType(info, writeSpace: false, isReadOnly: TypeFormatterUtils.IsReadOnlyMethod(info.MethodDef));
			}
			else
				WriteToken(method);

			WriteGenericArguments(info);
			WriteMethodParameterList(info, MethodParenOpen, MethodParenClose);
		}

		static string[]? TryGetOperatorInfo(string name) {
			nameToOperatorName.TryGetValue(name, out var list);
			return list;
		}

		void WriteOperatorInfoString(string s) => OutputWrite(s, 'a' <= s[0] && s[0] <= 'z' ? BoxedTextColor.Keyword : BoxedTextColor.Operator);

		void WriteMethodName(IMethod method, string name, string[]? operatorInfo) {
			if (!(operatorInfo is null)) {
				for (int i = 0; i < operatorInfo.Length; i++) {
					if (i > 0)
						WriteSpace();
					var s = operatorInfo[i];
					WriteOperatorInfoString(s);
				}
			}
			else
				WriteIdentifier(name, CSharpMetadataTextColorProvider.Instance.GetColor(method));
		}

		void WriteToolTip(IField field) {
			WriteDeprecated(TypeFormatterUtils.IsDeprecated(field));
			Write(field, true);
		}

		void Write(IField field) => Write(field, false);

		void Write(IField? field, bool isToolTip) {
			if (field is null) {
				WriteError();
				return;
			}

			var sig = field.FieldSig;
			var td = field.DeclaringType.ResolveTypeDef();
			bool isEnumOwner = !(td is null) && td.IsEnum;

			var fd = field.ResolveFieldDef();
			object? constant = null;
			bool isConstant = !(fd is null) && (fd.IsLiteral || (fd.IsStatic && fd.IsInitOnly)) && TypeFormatterUtils.HasConstant(fd, out var constantAttribute) && TypeFormatterUtils.TryGetConstant(fd, constantAttribute, out constant);
			if (!isEnumOwner || (!(fd is null) && !fd.IsLiteral)) {
				if (isToolTip) {
					OutputWrite(DescriptionParenOpen, BoxedTextColor.Punctuation);
					OutputWrite(isConstant ? dnSpy_Decompiler_Resources.ToolTip_Constant : dnSpy_Decompiler_Resources.ToolTip_Field, BoxedTextColor.Text);
					OutputWrite(DescriptionParenClose, BoxedTextColor.Punctuation);
					WriteSpace();
				}
				WriteModuleName(fd?.Module);
				Write(sig.Type, null, null, null);
				WriteSpace();
			}
			else
				WriteModuleName(fd?.Module);
			if (ShowDeclaringTypes) {
				Write(field.DeclaringType);
				WritePeriod();
			}
			WriteIdentifier(field.Name, CSharpMetadataTextColorProvider.Instance.GetColor(field));
			WriteToken(field);
			if (ShowFieldLiteralValues && isConstant) {
				WriteSpace();
				OutputWrite("=", BoxedTextColor.Operator);
				WriteSpace();
				WriteConstant(constant);
			}
		}

		void WriteConstant(object? obj) {
			if (obj is null) {
				OutputWrite(Keyword_null, BoxedTextColor.Keyword);
				return;
			}

			switch (Type.GetTypeCode(obj.GetType())) {
			case TypeCode.Boolean:
				FormatBoolean((bool)obj);
				break;

			case TypeCode.Char:
				FormatChar((char)obj);
				break;

			case TypeCode.SByte:
				FormatSByte((sbyte)obj);
				break;

			case TypeCode.Byte:
				FormatByte((byte)obj);
				break;

			case TypeCode.Int16:
				FormatInt16((short)obj);
				break;

			case TypeCode.UInt16:
				FormatUInt16((ushort)obj);
				break;

			case TypeCode.Int32:
				FormatInt32((int)obj);
				break;

			case TypeCode.UInt32:
				FormatUInt32((uint)obj);
				break;

			case TypeCode.Int64:
				FormatInt64((long)obj);
				break;

			case TypeCode.UInt64:
				FormatUInt64((ulong)obj);
				break;

			case TypeCode.Single:
				FormatSingle((float)obj);
				break;

			case TypeCode.Double:
				FormatDouble((double)obj);
				break;

			case TypeCode.Decimal:
				FormatDecimal((decimal)obj);
				break;

			case TypeCode.String:
				FormatString((string)obj);
				break;

			default:
				Debug.Fail($"Unknown constant: '{obj}'");
				OutputWrite(obj.ToString() ?? "???", BoxedTextColor.Text);
				break;
			}
		}

		void WriteToolTip(PropertyDef prop) {
			WriteDeprecated(TypeFormatterUtils.IsDeprecated(prop));
			Write(prop);
		}

		void Write(PropertyDef? prop) => Write(prop, writeAccessors: true);
		void Write(PropertyDef? prop, bool writeAccessors) {
			if (prop is null) {
				WriteError();
				return;
			}

			var getMethod = prop.GetMethods.FirstOrDefault();
			var setMethod = prop.SetMethods.FirstOrDefault();
			var md = getMethod ?? setMethod;
			if (md is null) {
				WriteError();
				return;
			}

			var info = new FormatterMethodInfo(md, md == setMethod);
			WriteModuleName(info);
			WriteReturnType(info, writeSpace: true, isReadOnly: TypeFormatterUtils.IsReadOnlyProperty(prop));
			if (ShowDeclaringTypes) {
				Write(prop.DeclaringType);
				WritePeriod();
			}
			var ovrMeth = md is null || md.Overrides.Count == 0 ? null : md.Overrides[0].MethodDeclaration;
			if (prop.IsIndexer()) {
				OutputWrite(Keyword_this, BoxedTextColor.Keyword);
				WriteGenericArguments(info);
				WriteMethodParameterList(info, IndexerParenOpen, IndexerParenClose);
			}
			else if (!(ovrMeth is null) && TypeFormatterUtils.GetPropertyName(ovrMeth) is string ovrMethPropName)
				WriteIdentifier(ovrMethPropName, CSharpMetadataTextColorProvider.Instance.GetColor(prop));
			else
				WriteIdentifier(prop.Name, CSharpMetadataTextColorProvider.Instance.GetColor(prop));
			WriteToken(prop);

			if (writeAccessors) {
				WriteSpace();
				OutputWrite("{", BoxedTextColor.Punctuation);
				if (prop.GetMethods.Count > 0) {
					WriteSpace();
					OutputWrite(Keyword_get, BoxedTextColor.Keyword);
					OutputWrite(";", BoxedTextColor.Punctuation);
				}
				if (prop.SetMethods.Count > 0) {
					WriteSpace();
					OutputWrite(Keyword_set, BoxedTextColor.Keyword);
					OutputWrite(";", BoxedTextColor.Punctuation);
				}
				WriteSpace();
				OutputWrite("}", BoxedTextColor.Punctuation);
			}
		}

		void WriteToolTip(EventDef? evt) {
			WriteDeprecated(TypeFormatterUtils.IsDeprecated(evt));
			Write(evt);
		}

		void Write(EventDef? evt) => Write(evt, writeAccessors: true);
		void Write(EventDef? evt, bool writeAccessors) {
			if (evt is null) {
				WriteError();
				return;
			}

			WriteModuleName(evt.Module);
			Write(evt.EventType);
			WriteSpace();
			if (ShowDeclaringTypes) {
				Write(evt.DeclaringType);
				WritePeriod();
			}
			WriteIdentifier(evt.Name, CSharpMetadataTextColorProvider.Instance.GetColor(evt));
			WriteToken(evt);
		}

		void WriteToolTip(GenericParam? gp) {
			if (gp is null) {
				WriteError();
				return;
			}

			Write(gp);
			WriteSpace();
			OutputWrite(dnSpy_Decompiler_Resources.ToolTip_GenericParameterInTypeOrMethod, BoxedTextColor.Text);
			WriteSpace();

			if (gp.Owner is TypeDef td)
				WriteType(td, ShowNamespaces, ShowIntrinsicTypeKeywords);
			else
				Write(gp.Owner as MethodDef);
		}

		void Write(GenericParam? gp) {
			if (gp is null) {
				WriteError();
				return;
			}

			WriteIdentifier(gp.Name, CSharpMetadataTextColorProvider.Instance.GetColor(gp));
			WriteToken(gp);
		}

		void WriteToolTip(ITypeDefOrRef type) {
			var td = type.ResolveTypeDef();

			WriteDeprecated(TypeFormatterUtils.IsDeprecated(type));
			Write(TypeFormatterUtils.GetMemberSpecialFlags(type));

			MethodDef invoke;
			if (TypeFormatterUtils.IsDelegate(td) && !((invoke = td.FindMethod("Invoke")) is null) && !(invoke.MethodSig is null)) {
				OutputWrite(Keyword_delegate, BoxedTextColor.Keyword);
				WriteSpace();

				var info = new FormatterMethodInfo(invoke);
				WriteModuleName(info);
				WriteReturnType(info, writeSpace: true, isReadOnly: TypeFormatterUtils.IsReadOnlyMethod(info.MethodDef));

				// Always print the namespace here because that's what VS does
				WriteType(td, true, ShowIntrinsicTypeKeywords);

				WriteGenericArguments(info);
				WriteMethodParameterList(info, MethodParenOpen, MethodParenClose);
				return;
			}
			else
				WriteModuleName(td?.Module);

			if (td is null) {
				Write(type);
				return;
			}

			string keyword;
			if (td.IsEnum)
				keyword = Keyword_enum;
			else if (td.IsValueType) {
				if (TypeFormatterUtils.IsReadOnlyType(td)) {
					OutputWrite(Keyword_readonly, BoxedTextColor.Keyword);
					WriteSpace();
				}
				if (TypeFormatterUtils.IsByRefLike(td)) {
					OutputWrite(Keyword_ref, BoxedTextColor.Keyword);
					WriteSpace();
				}
				keyword = Keyword_struct;
			}
			else if (td.IsInterface)
				keyword = Keyword_interface;
			else
				keyword = Keyword_class;
			OutputWrite(keyword, BoxedTextColor.Keyword);
			WriteSpace();

			// Always print the namespace here because that's what VS does
			WriteType(type, true, false);
		}

		void Write(ITypeDefOrRef? type, bool showModuleNames = false) {
			if (type is null) {
				WriteError();
				return;
			}

			if (recursionCounter >= TypeFormatterUtils.MAX_RECURSION)
				return;
			recursionCounter++;
			try {
				if (type is TypeSpec ts) {
					Write(ts.TypeSig, null, null, null);
					return;
				}

				if (!(type.DeclaringType is null)) {
					Write(type.DeclaringType);
					WritePeriod();
				}

				string? keyword = GetTypeKeyword(type);
				if (!(keyword is null))
					OutputWrite(keyword, BoxedTextColor.Keyword);
				else {
					if (showModuleNames)
						WriteModuleName(type.ResolveTypeDef()?.Module);
					WriteNamespace(type.Namespace);
					WriteIdentifier(TypeFormatterUtils.RemoveGenericTick(type.Name), CSharpMetadataTextColorProvider.Instance.GetColor(type));
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
				OutputWrite(namespaces[i], BoxedTextColor.Namespace);
				WritePeriod();
			}
		}
		static readonly char[] nsSep = new char[] { '.' };

		string? GetTypeKeyword(ITypeDefOrRef? type) {
			if (!ShowIntrinsicTypeKeywords)
				return null;
			if (type is null || !(type.DeclaringType is null) || type.Namespace != "System" || !type.DefinitionAssembly.IsCorLib())
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

		void Write(TypeSig? type, ParamDef? ownerParam, IList<TypeSig>? typeGenArgs, IList<TypeSig>? methGenArgs, bool forceReadOnly = false) {
			WriteRefIfByRef(type, ownerParam, forceReadOnly);
			if (type.RemovePinnedAndModifiers() is ByRefSig byRef)
				type = byRef.Next;
			Write(type, typeGenArgs, methGenArgs);
		}

		void Write(TypeSig? type, IList<TypeSig>? typeGenArgs, IList<TypeSig>? methGenArgs) {
			if (type is null) {
				WriteError();
				return;
			}

			if (recursionCounter >= TypeFormatterUtils.MAX_RECURSION)
				return;
			recursionCounter++;
			try {
				if (typeGenArgs is null)
					typeGenArgs = Array.Empty<TypeSig>();
				if (methGenArgs is null)
					methGenArgs = Array.Empty<TypeSig>();

				List<ArraySigBase>? list = null;
				while (!(type is null) && (type.ElementType == ElementType.SZArray || type.ElementType == ElementType.Array)) {
					if (list is null)
						list = new List<ArraySigBase>();
					list.Add((ArraySigBase)type);
					type = type.Next;
				}
				if (!(list is null)) {
					Write(list[list.Count - 1].Next, typeGenArgs, Array.Empty<TypeSig>());
					foreach (var aryType in list) {
						if (aryType.ElementType == ElementType.Array) {
							OutputWrite(ArrayParenOpen, BoxedTextColor.Punctuation);
							uint rank = aryType.Rank;
							if (rank == 0)
								OutputWrite("<RANK0>", BoxedTextColor.Error);
							else {
								var indexes = aryType.GetLowerBounds();
								var dims = aryType.GetSizes();
								if (ShowArrayValueSizes && (uint)indexes.Count == rank && (uint)dims.Count == rank) {
									for (int i = 0; (uint)i < rank; i++) {
										if (i > 0)
											WriteCommaSpace();
										if (i < indexes.Count && indexes[i] == 0)
											FormatInt32((int)dims[i]);
										else if (i < indexes.Count && i < dims.Count) {
											FormatInt32((int)indexes[i]);
											OutputWrite("..", BoxedTextColor.Operator);
											FormatInt32((int)(indexes[i] + dims[i] - 1));
										}
									}
								}
								else {
									if (rank == 1)
										OutputWrite("*", BoxedTextColor.Operator);
									for (uint i = 1; i < rank; i++)
										OutputWrite(",", BoxedTextColor.Punctuation);
								}
							}
							OutputWrite(ArrayParenClose, BoxedTextColor.Punctuation);
						}
						else {
							Debug.Assert(aryType.ElementType == ElementType.SZArray);
							OutputWrite(ArrayParenOpen, BoxedTextColor.Punctuation);
							OutputWrite(ArrayParenClose, BoxedTextColor.Punctuation);
						}
					}
					return;
				}

				if (type is null)
					return;
				switch (type.ElementType) {
				case ElementType.Void:			WriteSystemTypeKeyword("Void", "void", true); break;
				case ElementType.Boolean:		WriteSystemTypeKeyword("Boolean", "bool", true); break;
				case ElementType.Char:			WriteSystemTypeKeyword("Char", "char", true); break;
				case ElementType.I1:			WriteSystemTypeKeyword("SByte", "sbyte", true); break;
				case ElementType.U1:			WriteSystemTypeKeyword("Byte", "byte", true); break;
				case ElementType.I2:			WriteSystemTypeKeyword("Int16", "short", true); break;
				case ElementType.U2:			WriteSystemTypeKeyword("UInt16", "ushort", true); break;
				case ElementType.I4:			WriteSystemTypeKeyword("Int32", "int", true); break;
				case ElementType.U4:			WriteSystemTypeKeyword("UInt32", "uint", true); break;
				case ElementType.I8:			WriteSystemTypeKeyword("Int64", "long", true); break;
				case ElementType.U8:			WriteSystemTypeKeyword("UInt64", "ulong", true); break;
				case ElementType.R4:			WriteSystemTypeKeyword("Single", "float", true); break;
				case ElementType.R8:			WriteSystemTypeKeyword("Double", "double", true); break;
				case ElementType.String:		WriteSystemTypeKeyword("String", "string", false); break;
				case ElementType.Object:		WriteSystemTypeKeyword("Object", "object", false); break;

				case ElementType.TypedByRef:	WriteSystemType("TypedReference", true); break;
				case ElementType.I:				WriteSystemType("IntPtr", true); break;
				case ElementType.U:				WriteSystemType("UIntPtr", true); break;

				case ElementType.Ptr:
					Write(type.Next, typeGenArgs, methGenArgs);
					OutputWrite("*", BoxedTextColor.Operator);
					break;

				case ElementType.ByRef:
					Write(type.Next, typeGenArgs, methGenArgs);
					OutputWrite("&", BoxedTextColor.Operator);
					break;

				case ElementType.ValueType:
				case ElementType.Class:
					var cvt = (TypeDefOrRefSig)type;
					Write(cvt.TypeDefOrRef);
					break;

				case ElementType.Var:
				case ElementType.MVar:
					var gsType = Read(type.ElementType == ElementType.Var ? typeGenArgs : methGenArgs, (int)((GenericSig)type).Number);
					if (!(gsType is null))
						Write(gsType, typeGenArgs, methGenArgs);
					else {
						var gp = ((GenericSig)type).GenericParam;
						if (!(gp is null))
							Write(gp);
						else {
							if (type.ElementType == ElementType.MVar) {
								OutputWrite("!!", BoxedTextColor.MethodGenericParameter);
								OutputWrite(((GenericSig)type).Number.ToString(), BoxedTextColor.MethodGenericParameter);
							}
							else {
								OutputWrite("!", BoxedTextColor.TypeGenericParameter);
								OutputWrite(((GenericSig)type).Number.ToString(), BoxedTextColor.TypeGenericParameter);
							}
						}
					}
					break;

				case ElementType.GenericInst:
					var gis = (GenericInstSig?)type;
					Debug2.Assert(!(gis is null));
					if (TypeFormatterUtils.IsSystemNullable(gis)) {
						Write(GenericArgumentResolver.Resolve(gis.GenericArguments[0], typeGenArgs, methGenArgs), null, null);
						OutputWrite("?", BoxedTextColor.Operator);
					}
					else if (TypeFormatterUtils.IsSystemValueTuple(gis)) {
						OutputWrite(TupleParenOpen, BoxedTextColor.Punctuation);
						bool needComma = false;
						for (int i = 0; i < 1000; i++) {
							for (int j = 0; j < gis.GenericArguments.Count && j < 7; j++) {
								if (needComma)
									WriteCommaSpace();
								needComma = true;
								Write(GenericArgumentResolver.Resolve(gis.GenericArguments[j], typeGenArgs, methGenArgs), null, null);
							}
							if (gis.GenericArguments.Count != 8)
								break;
							gis = gis.GenericArguments[gis.GenericArguments.Count - 1] as GenericInstSig;
							if (gis is null) {
								WriteError();
								break;
							}
						}
						OutputWrite(TupleParenClose, BoxedTextColor.Punctuation);
					}
					else {
						Write(gis.GenericType, null, null);
						OutputWrite(GenericParenOpen, BoxedTextColor.Punctuation);
						for (int i = 0; i < gis.GenericArguments.Count; i++) {
							if (i > 0)
								WriteCommaSpace();
							Write(GenericArgumentResolver.Resolve(gis.GenericArguments[i], typeGenArgs, methGenArgs), null, null);
						}
						OutputWrite(GenericParenClose, BoxedTextColor.Punctuation);
					}
					break;

				case ElementType.FnPtr:
					var sig = ((FnPtrSig)type).MethodSig;
					Write(sig.RetType, typeGenArgs, methGenArgs);
					WriteSpace();
					OutputWrite(MethodParenOpen, BoxedTextColor.Punctuation);
					for (int i = 0; i < sig.Params.Count; i++) {
						if (i > 0)
							WriteCommaSpace();
						Write(sig.Params[i], typeGenArgs, methGenArgs);
					}
					if (!(sig.ParamsAfterSentinel is null)) {
						if (sig.Params.Count > 0)
							WriteCommaSpace();
						OutputWrite("...", BoxedTextColor.Punctuation);
						for (int i = 0; i < sig.ParamsAfterSentinel.Count; i++) {
							WriteCommaSpace();
							Write(sig.ParamsAfterSentinel[i], typeGenArgs, methGenArgs);
						}
					}
					OutputWrite(MethodParenClose, BoxedTextColor.Punctuation);
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

		TypeSig? Read(IList<TypeSig> list, int index) {
			if ((uint)index < (uint)list.Count)
				return list[index];
			return null;
		}

		public void WriteToolTip(ISourceVariable? variable) {
			if (variable is null) {
				WriteError();
				return;
			}

			var isLocal = variable.IsLocal;
			var pd = (variable.Variable as Parameter)?.ParamDef;
			OutputWrite(DescriptionParenOpen, BoxedTextColor.Punctuation);
			OutputWrite(isLocal ? dnSpy_Decompiler_Resources.ToolTip_Local : dnSpy_Decompiler_Resources.ToolTip_Parameter, BoxedTextColor.Text);
			OutputWrite(DescriptionParenClose, BoxedTextColor.Punctuation);
			WriteSpace();
			Write(variable.Type, !isLocal ? ((Parameter)variable.Variable!).ParamDef : null, null, null, forceReadOnly: (variable.Flags & SourceVariableFlags.ReadOnlyReference) != 0);
			WriteSpace();
			WriteIdentifier(TypeFormatterUtils.GetName(variable), isLocal ? BoxedTextColor.Local : BoxedTextColor.Parameter);
			if (!(pd is null))
				WriteToken(pd);
		}

		public void WriteNamespaceToolTip(string? @namespace) {
			if (@namespace is null) {
				WriteError();
				return;
			}

			OutputWrite(Keyword_namespace, BoxedTextColor.Keyword);
			WriteSpace();
			var parts = @namespace.Split(namespaceSeparators);
			for (int i = 0; i < parts.Length; i++) {
				if (i > 0)
					OutputWrite(".", BoxedTextColor.Operator);
				OutputWrite(parts[i], BoxedTextColor.Namespace);
			}
		}
		static readonly char[] namespaceSeparators = new char[] { '.' };

		void Write(ModuleDef? module) {
			try {
				if (recursionCounter++ >= TypeFormatterUtils.MAX_RECURSION)
					return;
				if (module is null) {
					OutputWrite("null module", BoxedTextColor.Error);
					return;
				}

				var name = TypeFormatterUtils.GetFileName(module.Location);
				OutputWrite(TypeFormatterUtils.FilterName(name), BoxedTextColor.AssemblyModule);
			}
			finally {
				recursionCounter--;
			}
		}

		void WriteModuleName(in FormatterMethodInfo info) {
			if (!ShowModuleNames)
				return;

			Write(info.ModuleDef);
			OutputWrite(ModuleNameSeparator, BoxedTextColor.Operator);
			return;
		}

		void WriteModuleName(ModuleDef? module) {
			if (module is null)
				return;
			if (!ShowModuleNames)
				return;

			Write(module);
			OutputWrite(ModuleNameSeparator, BoxedTextColor.Operator);
			return;
		}

		void WriteReturnType(in FormatterMethodInfo info, bool writeSpace, bool isReadOnly) {
			if (!ShowReturnTypes)
				return;
			if (info.MethodDef?.IsConstructor == true)
				return;
			ForceWriteReturnType(info, writeSpace, isReadOnly);
		}

		void ForceWriteReturnType(in FormatterMethodInfo info, bool writeSpace, bool isReadOnly) {
			if (!(!(info.MethodDef is null) && info.MethodDef.IsConstructor)) {
				TypeSig? retType;
				ParamDef? retParamDef;
				if (info.RetTypeIsLastArgType) {
					retType = info.MethodSig.Params.LastOrDefault();
					if (info.MethodDef is null)
						retParamDef = null;
					else {
						var l = info.MethodDef.Parameters.LastOrDefault();
						retParamDef = l is null ? null : l.ParamDef;
					}
				}
				else {
					retType = info.MethodSig.RetType;
					retParamDef = info.MethodDef is null ? null : info.MethodDef.Parameters.ReturnParameter.ParamDef;
				}
				if (retType.RemovePinnedAndModifiers() is ByRefSig && isReadOnly) {
					retType = retType.RemovePinnedAndModifiers().Next;
					OutputWrite(Keyword_ref, BoxedTextColor.Keyword);
					WriteSpace();
					OutputWrite(Keyword_readonly, BoxedTextColor.Keyword);
					WriteSpace();
				}
				Write(retType, retParamDef, info.TypeGenericParams, info.MethodGenericParams);
				if (writeSpace)
					WriteSpace();
			}
		}

		void WriteGenericArguments(in FormatterMethodInfo info) {
			if (info.MethodSig.GenParamCount > 0) {
				if (!(info.MethodGenericParams is null))
					WriteGenerics(info.MethodGenericParams, BoxedTextColor.MethodGenericParameter, GenericParamContext.Create(info.MethodDef));
				else if (!(info.MethodDef is null))
					WriteGenerics(info.MethodDef.GenericParameters, BoxedTextColor.MethodGenericParameter);
			}
		}

		void WriteMethodParameterList(in FormatterMethodInfo info, string lparen, string rparen) {
			if (!ShowParameterTypes && !ShowParameterNames)
				return;

			OutputWrite(lparen, BoxedTextColor.Punctuation);
			int baseIndex = info.MethodSig.HasThis ? 1 : 0;
			int count = info.MethodSig.Params.Count;
			if (info.RetTypeIsLastArgType)
				count--;
			for (int i = 0; i < count; i++) {
				if (i > 0)
					WriteCommaSpace();
				ParamDef? pd;
				if (!(info.MethodDef is null) && baseIndex + i < info.MethodDef.Parameters.Count)
					pd = info.MethodDef.Parameters[baseIndex + i].ParamDef;
				else
					pd = null;

				bool isDefault = TypeFormatterUtils.HasConstant(pd, out var constantAttribute);
				if (isDefault)
					OutputWrite(DefaultParamValueParenOpen, BoxedTextColor.Punctuation);

				bool needSpace = false;
				if (ShowParameterTypes) {
					needSpace = true;

					if (!(pd is null) && pd.CustomAttributes.IsDefined("System.ParamArrayAttribute")) {
						OutputWrite(Keyword_params, BoxedTextColor.Keyword);
						WriteSpace();
					}
					var paramType = info.MethodSig.Params[i];
					Write(paramType, pd, info.TypeGenericParams, info.MethodGenericParams);
				}
				if (ShowParameterNames) {
					if (needSpace)
						WriteSpace();
					needSpace = true;

					if (!(pd is null)) {
						WriteIdentifier(pd.Name, BoxedTextColor.Parameter);
						WriteToken(pd);
					}
					else
						WriteIdentifier("A_" + (baseIndex + i).ToString(), BoxedTextColor.Parameter);
				}
				if (ShowParameterLiteralValues && isDefault && TypeFormatterUtils.TryGetConstant(pd, constantAttribute, out var constant)) {
					if (needSpace)
						WriteSpace();
					needSpace = true;

					WriteSpace();
					OutputWrite("=", BoxedTextColor.Operator);
					WriteSpace();

					var t = info.MethodSig.Params[i].RemovePinnedAndModifiers();
					if (t.GetElementType() == ElementType.ByRef)
						t = t.Next;
					if (constant is null && !(t is null) && t.IsValueType)
						OutputWrite(Keyword_default, BoxedTextColor.Keyword);
					else
						WriteConstant(constant);
				}

				if (isDefault)
					OutputWrite(DefaultParamValueParenClose, BoxedTextColor.Punctuation);
			}
			OutputWrite(rparen, BoxedTextColor.Punctuation);
		}

		void WriteGenerics(IList<GenericParam>? gps, object gpTokenType) {
			if (gps is null || gps.Count == 0)
				return;
			OutputWrite(GenericParenOpen, BoxedTextColor.Punctuation);
			for (int i = 0; i < gps.Count; i++) {
				if (i > 0)
					WriteCommaSpace();
				var gp = gps[i];
				if (gp.IsCovariant) {
					OutputWrite(Keyword_out, BoxedTextColor.Keyword);
					WriteSpace();
				}
				else if (gp.IsContravariant) {
					OutputWrite(Keyword_in, BoxedTextColor.Keyword);
					WriteSpace();
				}
				WriteIdentifier(gp.Name, gpTokenType);
				WriteToken(gp);
			}
			OutputWrite(GenericParenClose, BoxedTextColor.Punctuation);
		}

		void WriteGenerics(IList<TypeSig>? gps, object gpTokenType, GenericParamContext gpContext) {
			if (gps is null || gps.Count == 0)
				return;
			OutputWrite(GenericParenOpen, BoxedTextColor.Punctuation);
			for (int i = 0; i < gps.Count; i++) {
				if (i > 0)
					WriteCommaSpace();
				Write(gps[i], null, null, null);
			}
			OutputWrite(GenericParenClose, BoxedTextColor.Punctuation);
		}

		void FormatBoolean(bool value) {
			if (value)
				OutputWrite(Keyword_true, BoxedTextColor.Keyword);
			else
				OutputWrite(Keyword_false, BoxedTextColor.Keyword);
		}

		void FormatChar(char value) => OutputWrite(ToFormattedChar(value), BoxedTextColor.Char);

		string ToFormattedChar(char value) {
			var sb = new StringBuilder();

			sb.Append('\'');
			switch (value) {
			case '\a': sb.Append(@"\a"); break;
			case '\b': sb.Append(@"\b"); break;
			case '\f': sb.Append(@"\f"); break;
			case '\n': sb.Append(@"\n"); break;
			case '\r': sb.Append(@"\r"); break;
			case '\t': sb.Append(@"\t"); break;
			case '\v': sb.Append(@"\v"); break;
			case '\\': sb.Append(@"\\"); break;
			case '\0': sb.Append(@"\0"); break;
			case '\'': sb.Append(@"\'"); break;
			default:
				if (char.IsControl(value)) {
					sb.Append(@"\u");
					sb.Append(((ushort)value).ToString("X4"));
				}
				else
					sb.Append(value);
				break;
			}
			sb.Append('\'');

			return sb.ToString();
		}

		static bool CanUseVerbatimString(string s) {
			bool foundBackslash = false;
			foreach (var c in s) {
				switch (c) {
				case '"':
					break;

				case '\\':
					foundBackslash = true;
					break;

				case '\a':
				case '\b':
				case '\f':
				case '\n':
				case '\r':
				case '\t':
				case '\v':
				case '\0':
				// More newline chars
				case '\u0085':
				case '\u2028':
				case '\u2029':
					return false;

				default:
					if (char.IsControl(c))
						return false;
					break;
				}
			}
			return foundBackslash;
		}

		void FormatString(string value) {
			var s = ToFormattedString(value, out bool isVerbatim);
			OutputWrite(s, isVerbatim ? BoxedTextColor.VerbatimString : BoxedTextColor.String);
		}

		string ToFormattedString(string value, out bool isVerbatim) {
			if (CanUseVerbatimString(value)) {
				isVerbatim = true;
				return GetFormattedVerbatimString(value);
			}
			else {
				isVerbatim = false;
				return GetFormattedString(value);
			}
		}

		string GetFormattedString(string value) {
			var sb = new StringBuilder();

			sb.Append('"');
			foreach (var c in value) {
				switch (c) {
				case '\a': sb.Append(@"\a"); break;
				case '\b': sb.Append(@"\b"); break;
				case '\f': sb.Append(@"\f"); break;
				case '\n': sb.Append(@"\n"); break;
				case '\r': sb.Append(@"\r"); break;
				case '\t': sb.Append(@"\t"); break;
				case '\v': sb.Append(@"\v"); break;
				case '\\': sb.Append(@"\\"); break;
				case '\0': sb.Append(@"\0"); break;
				case '"': sb.Append("\\\""); break;
				default:
					if (char.IsControl(c)) {
						sb.Append(@"\u");
						sb.Append(((ushort)c).ToString("X4"));
					}
					else
						sb.Append(c);
					break;
				}
			}
			sb.Append('"');

			return sb.ToString();
		}

		string GetFormattedVerbatimString(string value) {
			var sb = new StringBuilder();

			sb.Append(VerbatimStringPrefix + "\"");
			foreach (var c in value) {
				if (c == '"')
					sb.Append("\"\"");
				else
					sb.Append(c);
			}
			sb.Append('"');

			return sb.ToString();
		}

		string ToFormattedDecimalNumber(string number) => ToFormattedNumber(string.Empty, number, TypeFormatterUtils.DigitGroupSizeDecimal);
		string ToFormattedHexNumber(string number) => ToFormattedNumber(HexPrefix, number, TypeFormatterUtils.DigitGroupSizeHex);
		string ToFormattedNumber(string prefix, string number, int digitGroupSize) => TypeFormatterUtils.ToFormattedNumber(DigitSeparators, prefix, number, digitGroupSize);
		void WriteNumber(string number) => OutputWrite(number, BoxedTextColor.Number);

		string ToFormattedSByte(sbyte value) {
			if (UseDecimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X2"));
		}

		string ToFormattedByte(byte value) {
			if (UseDecimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X2"));
		}

		string ToFormattedInt16(short value) {
			if (UseDecimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X4"));
		}

		string ToFormattedUInt16(ushort value) {
			if (UseDecimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X4"));
		}

		string ToFormattedInt32(int value) {
			if (UseDecimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X8"));
		}

		string ToFormattedUInt32(uint value) {
			if (UseDecimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X8"));
		}

		string ToFormattedInt64(long value) {
			if (UseDecimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X16"));
		}

		string ToFormattedUInt64(ulong value) {
			if (UseDecimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X16"));
		}

		void FormatSingle(float value) {
			if (float.IsNaN(value))
				OutputWrite(TypeFormatterUtils.NaN, BoxedTextColor.Number);
			else if (float.IsNegativeInfinity(value))
				OutputWrite(TypeFormatterUtils.NegativeInfinity, BoxedTextColor.Number);
			else if (float.IsPositiveInfinity(value))
				OutputWrite(TypeFormatterUtils.PositiveInfinity, BoxedTextColor.Number);
			else
				OutputWrite(value.ToString(cultureInfo), BoxedTextColor.Number);
		}

		void FormatDouble(double value) {
			if (double.IsNaN(value))
				OutputWrite(TypeFormatterUtils.NaN, BoxedTextColor.Number);
			else if (double.IsNegativeInfinity(value))
				OutputWrite(TypeFormatterUtils.NegativeInfinity, BoxedTextColor.Number);
			else if (double.IsPositiveInfinity(value))
				OutputWrite(TypeFormatterUtils.PositiveInfinity, BoxedTextColor.Number);
			else
				OutputWrite(value.ToString(cultureInfo), BoxedTextColor.Number);
		}

		void FormatSByte(sbyte value) => WriteNumber(ToFormattedSByte(value));
		void FormatByte(byte value) => WriteNumber(ToFormattedByte(value));
		void FormatInt16(short value) => WriteNumber(ToFormattedInt16(value));
		void FormatUInt16(ushort value) => WriteNumber(ToFormattedUInt16(value));
		void FormatInt32(int value) => WriteNumber(ToFormattedInt32(value));
		void FormatUInt32(uint value) => WriteNumber(ToFormattedUInt32(value));
		void FormatInt64(long value) => WriteNumber(ToFormattedInt64(value));
		void FormatUInt64(ulong value) => WriteNumber(ToFormattedUInt64(value));
		void FormatDecimal(decimal value) => OutputWrite(value.ToString(cultureInfo), BoxedTextColor.Number);
	}
}

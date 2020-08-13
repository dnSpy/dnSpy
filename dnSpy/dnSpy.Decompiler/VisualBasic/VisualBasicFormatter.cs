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

namespace dnSpy.Decompiler.VisualBasic {
	public struct VisualBasicFormatter {
		const string Keyword_true = "True";
		const string Keyword_false = "False";
		const string Keyword_null = "Nothing";
		const string Keyword_As = "As";
		const string Keyword_out = "Out";
		const string Keyword_in = "In";
		const string Keyword_get = "Get";
		const string Keyword_set = "Set";
		const string Keyword_add = "Add";
		const string Keyword_remove = "Remove";
		const string Keyword_module = "Module";
		const string Keyword_enum = "Enum";
		const string Keyword_struct = "Structure";
		const string Keyword_interface = "Interface";
		const string Keyword_class = "Class";
		const string Keyword_namespace = "Namespace";
		const string Keyword_params = "ParamArray";
		const string Keyword_delegate = "Delegate";
		const string Keyword_ByRef = "ByRef";
		const string Keyword_New = "New";
		const string Keyword_Sub = "Sub";
		const string Keyword_Function = "Function";
		const string Keyword_ReadOnly = "ReadOnly";
		const string Keyword_Property = "Property";
		const string Keyword_Event = "Event";
		const string HexPrefix = "&H";
		const string IdentifierEscapeBegin = "[";
		const string IdentifierEscapeEnd = "]";
		const string ModuleNameSeparator = "!";
		const string CommentBegin = "/*";
		const string CommentEnd = "*/";
		const string DeprecatedParenOpen = "(";
		const string DeprecatedParenClose = ")";
		const string MemberSpecialParenOpen = "<";
		const string MemberSpecialParenClose = ">";
		const string MethodParenOpen = "(";
		const string MethodParenClose = ")";
		const string DescriptionParenOpen = "(";
		const string DescriptionParenClose = ")";
		const string PropertyParenOpen = "(";
		const string PropertyParenClose = ")";
		const string ArrayParenOpen = "(";
		const string ArrayParenClose = ")";
		const string TupleParenOpen = "(";
		const string TupleParenClose = ")";
		const string GenericParenOpen = "(";
		const string GenericParenClose = ")";
		const string Keyword_Of = "Of";
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
			{ "op_UnaryPlus", "Operator +".Split(' ') },
			{ "op_UnaryNegation", "Operator -".Split(' ') },
			{ "op_False", "Operator IsFalse".Split(' ') },
			{ "op_True", "Operator IsTrue".Split(' ') },
			{ "op_OnesComplement", "Operator Not".Split(' ') },
			{ "op_Addition", "Operator +".Split(' ') },
			{ "op_Subtraction", "Operator -".Split(' ') },
			{ "op_Multiply", "Operator *".Split(' ') },
			{ "op_Division", "Operator /".Split(' ') },
			{ "op_IntegerDivision", @"Operator \".Split(' ') },
			{ "op_Concatenate", "Operator &".Split(' ') },
			{ "op_Exponent", "Operator ^".Split(' ') },
			{ "op_RightShift", "Operator >>".Split(' ') },
			{ "op_LeftShift", "Operator <<".Split(' ') },
			{ "op_Equality", "Operator =".Split(' ') },
			{ "op_Inequality", "Operator <>".Split(' ') },
			{ "op_GreaterThan", "Operator >".Split(' ') },
			{ "op_GreaterThanOrEqual", "Operator >=".Split(' ') },
			{ "op_LessThan", "Operator <".Split(' ') },
			{ "op_LessThanOrEqual", "Operator <=".Split(' ') },
			{ "op_BitwiseAnd", "Operator And".Split(' ') },
			{ "op_Like", "Operator Like".Split(' ') },
			{ "op_Modulus", "Operator Mod".Split(' ') },
			{ "op_BitwiseOr", "Operator Or".Split(' ') },
			{ "op_ExclusiveOr", "Operator Xor".Split(' ') },
			{ "op_Implicit", "Widening Operator CType".Split(' ') },
			{ "op_Explicit", "Narrowing Operator CType".Split(' ') },
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

		public VisualBasicFormatter(ITextColorWriter output, FormatterOptions options, CultureInfo? cultureInfo) {
			this.output = output;
			this.options = options;
			this.cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
			recursionCounter = 0;
			lineLength = 0;
			outputLengthExceeded = false;
			forceWrite = false;
		}

		static readonly HashSet<string> isKeyword = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"#Const", "#Else", "#ElseIf", "#End", "#If", "AddHandler", "AddressOf",
			"Alias", "And", "AndAlso", "As", "Boolean", "ByRef", "Byte", "ByVal",
			"Call", "Case", "Catch", "CBool", "CByte", "CChar", "CDate", "CDbl",
			"CDec", "Char", "CInt", "Class", "CLng", "CObj", "Const", "Continue",
			"CSByte", "CShort", "CSng", "CStr", "CType", "CUInt", "CULng", "CUShort",
			"Date", "Decimal", "Declare", "Default", "Delegate", "Dim", "DirectCast",
			"Do", "Double", "Each", "Else", "ElseIf", "End", "EndIf", "Enum", "Erase",
			"Error", "Event", "Exit", "False", "Finally", "For", "Friend", "Function",
			"Get", "GetType", "GetXMLNamespace", "Global", "GoSub", "GoTo", "Handles",
			"If", "Implements", "Imports", "In", "Inherits", "Integer", "Interface",
			"Is", "IsNot", "Let", "Lib", "Like", "Long", "Loop", "Me", "Mod", "Module",
			"MustInherit", "MustOverride", "MyBase", "MyClass", "Namespace", "Narrowing",
			"New", "Next", "Not", "Nothing", "NotInheritable", "NotOverridable", "Object",
			"Of", "On", "Operator", "Option", "Optional", "Or", "OrElse", "Out",
			"Overloads", "Overridable", "Overrides", "ParamArray", "Partial", "Private",
			"Property", "Protected", "Public", "RaiseEvent", "ReadOnly", "ReDim", "REM",
			"RemoveHandler", "Resume", "Return", "SByte", "Select", "Set", "Shadows",
			"Shared", "Short", "Single", "Static", "Step", "Stop", "String", "Structure",
			"Sub", "SyncLock", "Then", "Throw", "To", "True", "Try", "TryCast", "TypeOf",
			"UInteger", "ULong", "UShort", "Using", "Variant", "Wend", "When", "While",
			"Widening", "With", "WithEvents", "WriteOnly", "Xor",
		};

		void WriteIdentifier(string id, object data) {
			if (isKeyword.Contains(id))
				OutputWrite(IdentifierEscapeBegin + IdentifierEscaper.Escape(id) + IdentifierEscapeEnd, data);
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
			OutputWrite(name, isValueType ? BoxedTextColor.ValueType : BoxedTextColor.Type);
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
				OutputWrite(dnSpy_Decompiler_Resources.VisualBasic_Deprecated_Member, BoxedTextColor.Text);
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
				OutputWrite(dnSpy_Decompiler_Resources.VisualBasic_Awaitable_Method, BoxedTextColor.Text);
			}
			if ((flags & MemberSpecialFlags.Extension) != 0) {
				if (comma)
					WriteCommaSpace();
				OutputWrite(dnSpy_Decompiler_Resources.VisualBasic_Extension_Method, BoxedTextColor.Text);
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

			WriteIdentifier(TypeFormatterUtils.RemoveGenericTick(td.Name), VisualBasicMetadataTextColorProvider.Instance.GetColor(td));
			WriteToken(type);
			var genParams = td.GenericParameters.Skip(td.GenericParameters.Count - numGenParams).ToArray();
			WriteGenerics(genParams, BoxedTextColor.TypeGenericParameter);
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
			OutputWrite(keyword, BoxedTextColor.Keyword);
			WriteSpace();
		}

		void Write(IMethod? method) {
			if (method is null) {
				WriteError();
				return;
			}

			var propInfo = TypeFormatterUtils.TryGetProperty(method as MethodDef);
			if (propInfo.kind != AccessorKind.None) {
				Write(propInfo.property, propInfo.kind);
				return;
			}

			var eventInfo = TypeFormatterUtils.TryGetEvent(method as MethodDef);
			if (eventInfo.kind != AccessorKind.None) {
				Write(eventInfo.@event, eventInfo.kind);
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

			if (!(operatorInfo is null)) {
				for (int i = 0; i < operatorInfo.Length - 1; i++) {
					WriteOperatorInfoString(operatorInfo[i]);
					WriteSpace();
				}
			}
			else {
				bool isSub = IsSub(info);
				OutputWrite(isSub ? Keyword_Sub : Keyword_Function, BoxedTextColor.Keyword);
				WriteSpace();
			}

			if (ShowDeclaringTypes) {
				Write(method.DeclaringType);
				WritePeriod();
			}
			if (!(info.MethodDef is null) && info.MethodDef.IsConstructor && !(method.DeclaringType is null))
				OutputWrite(Keyword_New, BoxedTextColor.Keyword);
			else if (!(info.MethodDef is null) && info.MethodDef.Overrides.Count > 0) {
				var ovrMeth = (IMemberRef)info.MethodDef.Overrides[0].MethodDeclaration;
				WriteMethodName(method, ovrMeth.Name, operatorInfo);
			}
			else
				WriteMethodName(method, method.Name, operatorInfo);
			WriteToken(method);

			WriteGenericArguments(info);
			WriteMethodParameterList(info, MethodParenOpen, MethodParenClose);
			WriteReturnType(info);
		}

		static string[]? TryGetOperatorInfo(string name) {
			nameToOperatorName.TryGetValue(name, out var list);
			return list;
		}

		void WriteOperatorInfoString(string s) => OutputWrite(s, 'A' <= s[0] && s[0] <= 'Z' ? BoxedTextColor.Keyword : BoxedTextColor.Operator);

		void WriteMethodName(IMethod method, string name, string[]? operatorInfo) {
			if (!(operatorInfo is null))
				WriteOperatorInfoString(operatorInfo[operatorInfo.Length - 1]);
			else
				WriteIdentifier(name, VisualBasicMetadataTextColorProvider.Instance.GetColor(method));
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
			}
			WriteModuleName(fd?.Module);
			if (ShowDeclaringTypes) {
				Write(field.DeclaringType);
				WritePeriod();
			}
			WriteIdentifier(field.Name, VisualBasicMetadataTextColorProvider.Instance.GetColor(field));
			WriteToken(field);
			if (!isEnumOwner) {
				WriteSpace();
				OutputWrite(Keyword_As, BoxedTextColor.Keyword);
				WriteSpace();
				Write(sig.Type, null, null, null);
			}
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

		void Write(PropertyDef prop) => Write(prop, AccessorKind.None);

		void Write(PropertyDef? prop, AccessorKind accessorKind) {
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

			if (setMethod is null) {
				OutputWrite(Keyword_ReadOnly, BoxedTextColor.Keyword);
				WriteSpace();
			}

			OutputWrite(Keyword_Property, BoxedTextColor.Keyword);
			WriteSpace();

			var sigMethod = md;
			if (accessorKind != AccessorKind.None) {
				if (accessorKind == AccessorKind.Getter)
					sigMethod = getMethod ?? md;
				else if (accessorKind == AccessorKind.Setter)
					sigMethod = setMethod ?? md;
				else
					throw new InvalidOperationException();
				WriteAccessor(accessorKind);
			}

			var info = new FormatterMethodInfo(sigMethod, sigMethod == setMethod, accessorKind == AccessorKind.Setter);
			WriteModuleName(info);
			if (ShowDeclaringTypes) {
				Write(prop.DeclaringType);
				WritePeriod();
			}
			var ovrMeth = md is null || md.Overrides.Count == 0 ? null : md.Overrides[0].MethodDeclaration;
			if (!(ovrMeth is null) && TypeFormatterUtils.GetPropertyName(ovrMeth) is string ovrMethPropName)
				WriteIdentifier(ovrMethPropName, VisualBasicMetadataTextColorProvider.Instance.GetColor(prop));
			else
				WriteIdentifier(prop.Name, VisualBasicMetadataTextColorProvider.Instance.GetColor(prop));
			WriteToken(prop);
			WriteGenericArguments(info);
			if (accessorKind != AccessorKind.None || prop.PropertySig.GetParamCount() != 0)
				WriteMethodParameterList(info, PropertyParenOpen, PropertyParenClose);

			WriteReturnType(info);
		}

		void WriteToolTip(EventDef evt) {
			WriteDeprecated(TypeFormatterUtils.IsDeprecated(evt));
			Write(evt);
		}

		void Write(EventDef? evt) => Write(evt, AccessorKind.None);
		void Write(EventDef? evt, AccessorKind accessorKind) {
			if (evt is null) {
				WriteError();
				return;
			}

			OutputWrite(Keyword_Event, BoxedTextColor.Keyword);
			WriteSpace();

			if (accessorKind != AccessorKind.None)
				WriteAccessor(accessorKind);

			WriteModuleName(evt.Module);
			if (ShowDeclaringTypes) {
				Write(evt.DeclaringType);
				WritePeriod();
			}
			WriteIdentifier(evt.Name, VisualBasicMetadataTextColorProvider.Instance.GetColor(evt));
			WriteToken(evt);
			WriteSpace();
			OutputWrite(Keyword_As, BoxedTextColor.Keyword);
			WriteSpace();
			Write(evt.EventType);
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

			WriteIdentifier(gp.Name, VisualBasicMetadataTextColorProvider.Instance.GetColor(gp));
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

				bool isSub = IsSub(info);
				OutputWrite(isSub ? Keyword_Sub : Keyword_Function, BoxedTextColor.Keyword);
				WriteSpace();

				// Always print the namespace here because that's what VS does
				WriteType(td, true, ShowIntrinsicTypeKeywords);

				WriteGenericArguments(info);
				WriteMethodParameterList(info, MethodParenOpen, MethodParenClose);
				WriteReturnType(info);
				return;
			}
			else
				WriteModuleName(td?.Module);

			if (td is null) {
				Write(type);
				return;
			}

			string keyword;
			if (IsModule(td))
				keyword = Keyword_module;
			else if (td.IsEnum)
				keyword = Keyword_enum;
			else if (td.IsValueType)
				keyword = Keyword_struct;
			else if (td.IsInterface)
				keyword = Keyword_interface;
			else
				keyword = Keyword_class;
			OutputWrite(keyword, BoxedTextColor.Keyword);
			WriteSpace();

			// Always print the namespace here because that's what VS does
			WriteType(type, true, false);
		}

		static bool IsModule(TypeDef type) =>
			!(type is null) && type.DeclaringType is null && type.IsSealed && type.IsDefined(stringMicrosoftVisualBasicCompilerServices, stringStandardModuleAttribute);
		static readonly UTF8String stringMicrosoftVisualBasicCompilerServices = new UTF8String("Microsoft.VisualBasic.CompilerServices");
		static readonly UTF8String stringStandardModuleAttribute = new UTF8String("StandardModuleAttribute");

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
					WriteIdentifier(TypeFormatterUtils.RemoveGenericTick(type.Name), VisualBasicMetadataTextColorProvider.Instance.GetColor(type));
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

		string? GetTypeKeyword(ITypeDefOrRef type) {
			if (!ShowIntrinsicTypeKeywords)
				return null;
			if (type is null || !(type.DeclaringType is null) || type.Namespace != "System" || !type.DefinitionAssembly.IsCorLib())
				return null;
			switch (type.TypeName) {
			case "Boolean":	return "Boolean";
			case "Byte":	return "Byte";
			case "Char":	return "Char";
			case "DateTime":return "Date";
			case "Decimal":	return "Decimal";
			case "Double":	return "Double";
			case "Int16":	return "Short";
			case "Int32":	return "Integer";
			case "Int64":	return "Long";
			case "Object":	return "Object";
			case "SByte":	return "SByte";
			case "Single":	return "Single";
			case "String":	return "String";
			case "UInt16":	return "UShort";
			case "UInt32":	return "UInteger";
			case "UInt64":	return "ULong";
			default:		return null;
			}
		}

		void Write(TypeSig? type, ParamDef? ownerParam, IList<TypeSig>? typeGenArgs, IList<TypeSig>? methGenArgs) => Write(type, typeGenArgs, methGenArgs);

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
				case ElementType.Void:			WriteSystemType("Void", true); break;
				case ElementType.Boolean:		WriteSystemTypeKeyword("Boolean", "Boolean", true); break;
				case ElementType.Char:			WriteSystemTypeKeyword("Char", "Char", true); break;
				case ElementType.I1:			WriteSystemTypeKeyword("SByte", "SByte", true); break;
				case ElementType.U1:			WriteSystemTypeKeyword("Byte", "Byte", true); break;
				case ElementType.I2:			WriteSystemTypeKeyword("Int16", "Short", true); break;
				case ElementType.U2:			WriteSystemTypeKeyword("UInt16", "UShort", true); break;
				case ElementType.I4:			WriteSystemTypeKeyword("Int32", "Integer", true); break;
				case ElementType.U4:			WriteSystemTypeKeyword("UInt32", "UInteger", true); break;
				case ElementType.I8:			WriteSystemTypeKeyword("Int64", "Long", true); break;
				case ElementType.U8:			WriteSystemTypeKeyword("UInt64", "ULong", true); break;
				case ElementType.R4:			WriteSystemTypeKeyword("Single", "Single", true); break;
				case ElementType.R8:			WriteSystemTypeKeyword("Double", "Double", true); break;
				case ElementType.String:		WriteSystemTypeKeyword("String", "String", false); break;
				case ElementType.Object:		WriteSystemTypeKeyword("Object", "Object", false); break;

				case ElementType.TypedByRef:	WriteSystemType("TypedReference", true); break;
				case ElementType.I:				WriteSystemType("IntPtr", true); break;
				case ElementType.U:				WriteSystemType("UIntPtr", true); break;

				case ElementType.Ptr:
					Write(type.Next, typeGenArgs, methGenArgs);
					OutputWrite("*", BoxedTextColor.Operator);
					break;

				case ElementType.ByRef:
					OutputWrite(Keyword_ByRef, BoxedTextColor.Keyword);
					WriteSpace();
					Write(type.Next, typeGenArgs, methGenArgs);
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
						OutputWrite(Keyword_Of, BoxedTextColor.Keyword);
						WriteSpace();
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
			var type = variable.Type;
			OutputWrite(DescriptionParenOpen, BoxedTextColor.Punctuation);
			OutputWrite(isLocal ? dnSpy_Decompiler_Resources.ToolTip_Local : dnSpy_Decompiler_Resources.ToolTip_Parameter, BoxedTextColor.Text);
			OutputWrite(DescriptionParenClose, BoxedTextColor.Punctuation);
			WriteSpace();
			if (type.GetElementType() == ElementType.ByRef) {
				type = type.Next;
				OutputWrite(Keyword_ByRef, BoxedTextColor.Keyword);
				WriteSpace();
			}
			WriteIdentifier(TypeFormatterUtils.GetName(variable), isLocal ? BoxedTextColor.Local : BoxedTextColor.Parameter);
			if (!(pd is null))
				WriteToken(pd);
			WriteSpace();
			OutputWrite(Keyword_As, BoxedTextColor.Keyword);
			WriteSpace();
			Write(type, !isLocal ? ((Parameter)variable.Variable!).ParamDef : null, null, null);
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

		void WriteReturnType(in FormatterMethodInfo info) {
			if (!ShowReturnTypes)
				return;
			if (IsSub(info))
				return;
			if (!(!(info.MethodDef is null) && info.MethodDef.IsConstructor)) {
				var retInfo = GetReturnTypeInfo(info);
				WriteSpace();
				OutputWrite(Keyword_As, BoxedTextColor.Keyword);
				WriteSpace();
				Write(retInfo.returnType, retInfo.paramDef, info.TypeGenericParams, info.MethodGenericParams);
			}
		}

		static bool IsSub(in FormatterMethodInfo info) => GetReturnTypeInfo(info).returnType.RemovePinnedAndModifiers().GetElementType() == ElementType.Void;

		static (TypeSig? returnType, ParamDef? paramDef) GetReturnTypeInfo(in FormatterMethodInfo info) {
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
			return (retType, retParamDef);
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
			if (info.RetTypeIsLastArgType && !info.IncludeReturnTypeInArgsList)
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
				var paramType = info.MethodSig.Params[i];
				if (ShowParameterNames || ShowParameterTypes) {
					if (paramType.GetElementType() == ElementType.ByRef) {
						paramType = paramType.Next;
						OutputWrite(Keyword_ByRef, BoxedTextColor.Keyword);
						WriteSpace();
					}

					if (!(pd is null) && pd.CustomAttributes.IsDefined("System.ParamArrayAttribute")) {
						OutputWrite(Keyword_params, BoxedTextColor.Keyword);
						needSpace = true;
					}
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
				if (ShowParameterTypes) {
					if (ShowParameterNames) {
						WriteSpace();
						OutputWrite(Keyword_As, BoxedTextColor.Keyword);
					}
					if (needSpace)
						WriteSpace();
					needSpace = true;

					Write(paramType, pd, info.TypeGenericParams, info.MethodGenericParams);
				}
				if (ShowParameterLiteralValues && isDefault && TypeFormatterUtils.TryGetConstant(pd, constantAttribute, out var constant)) {
					if (needSpace)
						WriteSpace();
					needSpace = true;

					WriteSpace();
					OutputWrite("=", BoxedTextColor.Operator);
					WriteSpace();
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
			OutputWrite(Keyword_Of, BoxedTextColor.Keyword);
			WriteSpace();
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
			OutputWrite(Keyword_Of, BoxedTextColor.Keyword);
			WriteSpace();
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

		void FormatChar(char value) {
			switch (value) {
			case '\r':	OutputWrite("vbCr", BoxedTextColor.LiteralField); break;
			case '\n':	OutputWrite("vbLf", BoxedTextColor.LiteralField); break;
			case '\b':	OutputWrite("vbBack", BoxedTextColor.LiteralField); break;
			case '\f':	OutputWrite("vbFormFeed", BoxedTextColor.LiteralField); break;
			case '\t':	OutputWrite("vbTab", BoxedTextColor.LiteralField); break;
			case '\v':	OutputWrite("vbVerticalTab", BoxedTextColor.LiteralField); break;
			case '\0':	OutputWrite("vbNullChar", BoxedTextColor.LiteralField); break;
			case '"':	OutputWrite("\"\"\"\"c", BoxedTextColor.Char); break;
			default:
				if (char.IsControl(value))
					WriteCharW(value);
				else
					OutputWrite("\"" + value.ToString() + "\"c", BoxedTextColor.Char);
				break;
			}
		}

		void WriteCharW(char value) {
			OutputWrite("ChrW", BoxedTextColor.StaticMethod);
			OutputWrite("(", BoxedTextColor.Punctuation);
			FormatUInt16(value);
			OutputWrite(")", BoxedTextColor.Punctuation);
		}

		void FormatString(string value) {
			if (value == string.Empty) {
				OutputWrite("\"\"", BoxedTextColor.String);
				return;
			}

			int index = 0;
			bool needSep = false;
			while (index < value.Length) {
				var s = GetSubString(value, ref index);
				if (s.Length != 0) {
					if (needSep)
						WriteStringConcatOperator();
					OutputWrite("\"" + s + "\"", BoxedTextColor.String);
					needSep = true;
				}
				if (index < value.Length) {
					var c = value[index];
					switch (c) {
					case '\r':
						if (index + 1 < value.Length && value[index + 1] == '\n') {
							WriteSpecialConstantString("vbCrLf", ref needSep);
							index++;
						}
						else
							WriteSpecialConstantString("vbCr", ref needSep);
						break;

					case '\n':
						WriteSpecialConstantString("vbLf", ref needSep);
						break;

					case '\b':
						WriteSpecialConstantString("vbBack", ref needSep);
						break;

					case '\f':
						WriteSpecialConstantString("vbFormFeed", ref needSep);
						break;

					case '\t':
						WriteSpecialConstantString("vbTab", ref needSep);
						break;

					case '\v':
						WriteSpecialConstantString("vbVerticalTab", ref needSep);
						break;

					case '\0':
						WriteSpecialConstantString("vbNullChar", ref needSep);
						break;

					default:
						if (needSep)
							WriteStringConcatOperator();
						WriteCharW(c);
						break;
					}
					index++;
					needSep = true;
				}
			}
		}

		void WriteStringConcatOperator() {
			WriteSpace();
			OutputWrite("&", BoxedTextColor.Operator);
			WriteSpace();
		}

		void WriteSpecialConstantString(string s, ref bool needSep) {
			if (needSep)
				WriteStringConcatOperator();
			OutputWrite(s, BoxedTextColor.LiteralField);
			needSep = true;
		}

		string GetSubString(string value, ref int index) {
			var sb = new StringBuilder();

			while (index < value.Length) {
				var c = value[index];
				bool isSpecial;
				switch (c) {
				case '"':
					sb.Append(c);
					isSpecial = false;
					break;
				case '\r':
				case '\n':
				case '\b':
				case '\f':
				case '\t':
				case '\v':
				case '\0':
				// More newline chars
				case '\u0085':
				case '\u2028':
				case '\u2029':
					isSpecial = true;
					break;
				default:
					isSpecial = char.IsControl(c);
					break;
				}
				if (isSpecial)
					break;
				sb.Append(c);
				index++;
			}

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

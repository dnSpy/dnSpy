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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Decompiler;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.Formatters.VisualBasic {
	struct VisualBasicTypeFormatter {
		readonly IDbgTextWriter output;
		readonly TypeFormatterOptions options;
		readonly CultureInfo cultureInfo;
		const int MAX_RECURSION = 200;
		int recursionCounter;

		const string ARRAY_OPEN_PAREN = "(";
		const string ARRAY_CLOSE_PAREN = ")";
		const string GENERICS_OPEN_PAREN = "(";
		const string GENERICS_CLOSE_PAREN = ")";
		const string GENERICS_OF_KEYWORD = "Of";
		const string TUPLE_OPEN_PAREN = "(";
		const string TUPLE_CLOSE_PAREN = ")";
		const string METHOD_OPEN_PAREN = "(";
		const string METHOD_CLOSE_PAREN = ")";
		const string HEX_PREFIX = "&H";
		const string IDENTIFIER_ESCAPE_BEGIN = "[";
		const string IDENTIFIER_ESCAPE_END = "]";
		const string BYREF_KEYWORD = "ByRef";
		const int MAX_ARRAY_RANK = 100;

		bool ShowArrayValueSizes => (options & TypeFormatterOptions.ShowArrayValueSizes) != 0;
		bool UseDecimal => (options & TypeFormatterOptions.UseDecimal) != 0;
		bool DigitSeparators => (options & TypeFormatterOptions.DigitSeparators) != 0;
		bool ShowIntrinsicTypeKeywords => (options & TypeFormatterOptions.IntrinsicTypeKeywords) != 0;
		bool ShowNamespaces => (options & TypeFormatterOptions.Namespaces) != 0;

		public VisualBasicTypeFormatter(IDbgTextWriter output, TypeFormatterOptions options, CultureInfo? cultureInfo) {
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.options = options;
			this.cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
			recursionCounter = 0;
		}

		void OutputWrite(string s, DbgTextColor color) => output.Write(color, s);

		void WriteSpace() => OutputWrite(" ", DbgTextColor.Text);

		void WriteCommaSpace() {
			OutputWrite(",", DbgTextColor.Punctuation);
			WriteSpace();
		}

		string ToFormattedDecimalNumber(string number) => ToFormattedNumber(string.Empty, number, ValueFormatterUtils.DigitGroupSizeDecimal);
		string ToFormattedHexNumber(string number) => ToFormattedNumber(HEX_PREFIX, number, ValueFormatterUtils.DigitGroupSizeHex);
		string ToFormattedNumber(string prefix, string number, int digitGroupSize) => ValueFormatterUtils.ToFormattedNumber(DigitSeparators, prefix, number, digitGroupSize);

		string FormatUInt32(uint value) {
			if (UseDecimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X8"));
		}

		string FormatInt32(int value) {
			if (UseDecimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X8"));
		}

		void WriteUInt32(uint value) => OutputWrite(FormatUInt32(value), DbgTextColor.Number);
		void WriteInt32(int value) => OutputWrite(FormatInt32(value), DbgTextColor.Number);

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

		internal static string GetFormattedIdentifier(string? id) {
			if (isKeyword.Contains(id!))
				return IDENTIFIER_ESCAPE_BEGIN + IdentifierEscaper.Escape(id) + IDENTIFIER_ESCAPE_END;
			return IdentifierEscaper.Escape(id);
		}

		void WriteIdentifier(string? id, DbgTextColor color) => OutputWrite(GetFormattedIdentifier(id), color);

		public void Format(DmdType type, DbgDotNetValue? value) {
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			List<(DmdType type, DbgDotNetValue? value)>? arrayTypesList = null;
			DbgDotNetValue? disposeThisValue = null;
			try {
				if (recursionCounter++ >= MAX_RECURSION)
					return;

				switch (type.TypeSignatureKind) {
				case DmdTypeSignatureKind.SZArray:
				case DmdTypeSignatureKind.MDArray:
					// Array types are shown in reverse order
					arrayTypesList = new List<(DmdType type, DbgDotNetValue? value)>();
					do {
						arrayTypesList.Add((type, arrayTypesList.Count == 0 ? value : null));
						type = type.GetElementType()!;
					} while (type.IsArray);
					var t = arrayTypesList[arrayTypesList.Count - 1];
					Format(t.type.GetElementType()!, null);
					foreach (var tuple in arrayTypesList) {
						var aryType = tuple.type;
						var aryValue = tuple.value;
						uint elementCount;
						if (aryType.IsVariableBoundArray) {
							OutputWrite(ARRAY_OPEN_PAREN, DbgTextColor.Punctuation);
							int rank = Math.Min(aryType.GetArrayRank(), MAX_ARRAY_RANK);
							if (rank <= 0)
								OutputWrite("???", DbgTextColor.Error);
							else {
								if (aryValue is null || aryValue.IsNull || !aryValue.GetArrayInfo(out elementCount, out var dimensionInfos))
									dimensionInfos = null;
								if (ShowArrayValueSizes && dimensionInfos is not null && dimensionInfos.Length == rank) {
									for (int i = 0; i < rank; i++) {
										if (i > 0) {
											OutputWrite(",", DbgTextColor.Punctuation);
											WriteSpace();
										}
										if (dimensionInfos[i].BaseIndex == 0)
											WriteUInt32(dimensionInfos[i].Length);
										else {
											WriteInt32(dimensionInfos[i].BaseIndex);
											OutputWrite("..", DbgTextColor.Operator);
											WriteInt32(dimensionInfos[i].BaseIndex + (int)dimensionInfos[i].Length - 1);
										}
									}
								}
								else {
									if (rank == 1)
										OutputWrite("*", DbgTextColor.Operator);
									OutputWrite(TypeFormatterUtils.GetArrayCommas(rank), DbgTextColor.Punctuation);
								}
							}
							OutputWrite(ARRAY_CLOSE_PAREN, DbgTextColor.Punctuation);
						}
						else {
							Debug.Assert(aryType.IsSZArray);
							OutputWrite(ARRAY_OPEN_PAREN, DbgTextColor.Punctuation);
							if (ShowArrayValueSizes && aryValue is not null && !aryValue.IsNull) {
								if (aryValue.GetArrayCount(out elementCount))
									WriteUInt32(elementCount);
							}
							OutputWrite(ARRAY_CLOSE_PAREN, DbgTextColor.Punctuation);
						}
					}
					break;

				case DmdTypeSignatureKind.Pointer:
					Format(type.GetElementType()!, null);
					OutputWrite("*", DbgTextColor.Operator);
					break;

				case DmdTypeSignatureKind.ByRef:
					OutputWrite(BYREF_KEYWORD, DbgTextColor.Keyword);
					WriteSpace();
					Format(type.GetElementType()!, disposeThisValue = value?.LoadIndirect().Value);
					break;

				case DmdTypeSignatureKind.TypeGenericParameter:
					WriteIdentifier(type.MetadataName, DbgTextColor.TypeGenericParameter);
					break;

				case DmdTypeSignatureKind.MethodGenericParameter:
					WriteIdentifier(type.MetadataName, DbgTextColor.MethodGenericParameter);
					break;

				case DmdTypeSignatureKind.Type:
				case DmdTypeSignatureKind.GenericInstance:
					if (type.IsNullable) {
						Format(type.GetNullableElementType(), null);
						OutputWrite("?", DbgTextColor.Operator);
					}
					else if (TypeFormatterUtils.IsTupleType(type)) {
						OutputWrite(TUPLE_OPEN_PAREN, DbgTextColor.Punctuation);
						DmdType? tupleType = type;
						int tupleIndex = 0;
						for (;;) {
							tupleType = WriteTupleFields(tupleType, ref tupleIndex);
							if (tupleType is not null)
								WriteCommaSpace();
							else
								break;
						}
						OutputWrite(TUPLE_CLOSE_PAREN, DbgTextColor.Punctuation);
					}
					else {
						var genericArgs = type.GetGenericArguments();
						int genericArgsIndex = 0;
						KeywordType keywordType;
						if (type.DeclaringType is null) {
							keywordType = GetKeywordType(type);
							if (keywordType == KeywordType.NoKeyword)
								WriteNamespace(type);
							WriteTypeName(type, keywordType);
							WriteGenericArguments(type, genericArgs, ref genericArgsIndex);
						}
						else {
							var typesList = new List<DmdType>();
							typesList.Add(type);
							while (type.DeclaringType is not null) {
								type = type.DeclaringType;
								typesList.Add(type);
							}
							keywordType = GetKeywordType(type);
							if (keywordType == KeywordType.NoKeyword)
								WriteNamespace(type);
							for (int i = typesList.Count - 1; i >= 0; i--) {
								WriteTypeName(typesList[i], i == 0 ? keywordType : KeywordType.NoKeyword);
								WriteGenericArguments(typesList[i], genericArgs, ref genericArgsIndex);
								if (i != 0)
									OutputWrite(".", DbgTextColor.Operator);
							}
						}
					}
					break;

				case DmdTypeSignatureKind.FunctionPointer:
					var sig = type.GetFunctionPointerMethodSignature();
					Format(sig.ReturnType, null);
					WriteSpace();
					OutputWrite(METHOD_OPEN_PAREN, DbgTextColor.Punctuation);
					var types = sig.GetParameterTypes();
					for (int i = 0; i < types.Count; i++) {
						if (i > 0)
							WriteCommaSpace();
						Format(types[i], null);
					}
					types = sig.GetVarArgsParameterTypes();
					if (types.Count > 0) {
						if (sig.GetParameterTypes().Count > 0)
							WriteCommaSpace();
						OutputWrite("...", DbgTextColor.Punctuation);
						for (int i = 0; i < types.Count; i++) {
							WriteCommaSpace();
							Format(types[i], null);
						}
					}
					OutputWrite(METHOD_CLOSE_PAREN, DbgTextColor.Punctuation);
					break;

				default:
					throw new InvalidOperationException();
				}
			}
			finally {
				recursionCounter--;
				if (arrayTypesList is not null) {
					foreach (var info in arrayTypesList) {
						if (info.value != value)
							info.value?.Dispose();
					}
				}
				disposeThisValue?.Dispose();
			}
		}

		void WriteGenericArguments(DmdType type, IList<DmdType> genericArgs, ref int genericArgsIndex) {
			var gas = type.GetGenericArguments();
			if (genericArgsIndex < genericArgs.Count && genericArgsIndex < gas.Count) {
				OutputWrite(GENERICS_OPEN_PAREN, DbgTextColor.Punctuation);
				OutputWrite(GENERICS_OF_KEYWORD, DbgTextColor.Keyword);
				WriteSpace();
				int startIndex = genericArgsIndex;
				for (int j = startIndex; j < genericArgs.Count && j < gas.Count; j++, genericArgsIndex++) {
					if (j > startIndex)
						WriteCommaSpace();
					Format(genericArgs[j], null);
				}
				OutputWrite(GENERICS_CLOSE_PAREN, DbgTextColor.Punctuation);
			}
		}

		DmdType? WriteTupleFields(DmdType type, ref int index) {
			var args = type.GetGenericArguments();
			Debug.Assert(0 < args.Count && args.Count <= TypeFormatterUtils.MAX_TUPLE_ARITY);
			if (args.Count > TypeFormatterUtils.MAX_TUPLE_ARITY) {
				OutputWrite("???", DbgTextColor.Error);
				return null;
			}
			for (int i = 0; i < args.Count && i < TypeFormatterUtils.MAX_TUPLE_ARITY - 1; i++) {
				if (i > 0)
					WriteCommaSpace();
				//TODO: Write tuple name used in source
				string? fieldName = null;
				if (fieldName is not null) {
					OutputWrite(fieldName, DbgTextColor.InstanceField);
					WriteSpace();
					OutputWrite("As", DbgTextColor.Keyword);
					WriteSpace();
				}
				Format(args[i], null);
				index++;
			}
			if (args.Count == TypeFormatterUtils.MAX_TUPLE_ARITY)
				return args[TypeFormatterUtils.MAX_TUPLE_ARITY - 1];
			return null;
		}

		void WriteNamespace(DmdType type) {
			if (!ShowNamespaces)
				return;
			var ns = type.MetadataNamespace;
			if (string2.IsNullOrEmpty(ns))
				return;
			foreach (var nsPart in ns.Split(namespaceSeparators)) {
				WriteIdentifier(nsPart, DbgTextColor.Namespace);
				OutputWrite(".", DbgTextColor.Operator);
			}
		}
		static readonly char[] namespaceSeparators = new[] { '.' };

		void WriteTypeName(DmdType type, KeywordType keywordType) {
			switch (keywordType) {
			case KeywordType.Boolean:	OutputWrite("Boolean", DbgTextColor.Keyword); return;
			case KeywordType.Byte:		OutputWrite("Byte", DbgTextColor.Keyword); return;
			case KeywordType.Char:		OutputWrite("Char", DbgTextColor.Keyword); return;
			case KeywordType.Date:		OutputWrite("Date", DbgTextColor.Keyword); return;
			case KeywordType.Decimal:	OutputWrite("Decimal", DbgTextColor.Keyword); return;
			case KeywordType.Double:	OutputWrite("Double", DbgTextColor.Keyword); return;
			case KeywordType.Integer:	OutputWrite("Integer", DbgTextColor.Keyword); return;
			case KeywordType.Long:		OutputWrite("Long", DbgTextColor.Keyword); return;
			case KeywordType.Object:	OutputWrite("Object", DbgTextColor.Keyword); return;
			case KeywordType.SByte:		OutputWrite("SByte", DbgTextColor.Keyword); return;
			case KeywordType.Short:		OutputWrite("Short", DbgTextColor.Keyword); return;
			case KeywordType.Single:	OutputWrite("Single", DbgTextColor.Keyword); return;
			case KeywordType.String:	OutputWrite("String", DbgTextColor.Keyword); return;
			case KeywordType.UInteger:	OutputWrite("UInteger", DbgTextColor.Keyword); return;
			case KeywordType.ULong:		OutputWrite("ULong", DbgTextColor.Keyword); return;
			case KeywordType.UShort:	OutputWrite("UShort", DbgTextColor.Keyword); return;

			case KeywordType.NoKeyword:
				break;

			default:
				throw new InvalidOperationException();
			}

			WriteIdentifier(TypeFormatterUtils.RemoveGenericTick(type.MetadataName ?? string.Empty), TypeFormatterUtils.GetColor(type, canBeModule: true));
			new VisualBasicPrimitiveValueFormatter(output, options.ToValueFormatterOptions(), cultureInfo).WriteTokenComment(type.MetadataToken);
		}

		enum KeywordType {
			NoKeyword,
			Boolean,
			Byte,
			Char,
			Date,
			Decimal,
			Double,
			Integer,
			Long,
			Object,
			SByte,
			Short,
			Single,
			String,
			UInteger,
			ULong,
			UShort,
		}

		KeywordType GetKeywordType(DmdType type) {
			const KeywordType defaultValue = KeywordType.NoKeyword;
			if (!ShowIntrinsicTypeKeywords)
				return defaultValue;
			if (type.MetadataNamespace == "System" && !type.IsNested) {
				switch (type.MetadataName) {
				case "Boolean":	return type == type.AppDomain.System_Boolean	? KeywordType.Boolean	: defaultValue;
				case "Byte":	return type == type.AppDomain.System_Byte		? KeywordType.Byte		: defaultValue;
				case "Char":	return type == type.AppDomain.System_Char		? KeywordType.Char		: defaultValue;
				case "DateTime":return type == type.AppDomain.System_DateTime	? KeywordType.Date		: defaultValue;
				case "Decimal":	return type == type.AppDomain.System_Decimal	? KeywordType.Decimal	: defaultValue;
				case "Double":	return type == type.AppDomain.System_Double		? KeywordType.Double	: defaultValue;
				case "Int32":	return type == type.AppDomain.System_Int32		? KeywordType.Integer	: defaultValue;
				case "Int64":	return type == type.AppDomain.System_Int64		? KeywordType.Long		: defaultValue;
				case "Object":	return type == type.AppDomain.System_Object		? KeywordType.Object	: defaultValue;
				case "SByte":	return type == type.AppDomain.System_SByte		? KeywordType.SByte		: defaultValue;
				case "Int16":	return type == type.AppDomain.System_Int16		? KeywordType.Short		: defaultValue;
				case "Single":	return type == type.AppDomain.System_Single		? KeywordType.Single	: defaultValue;
				case "String":	return type == type.AppDomain.System_String		? KeywordType.String	: defaultValue;
				case "UInt32":	return type == type.AppDomain.System_UInt32		? KeywordType.UInteger	: defaultValue;
				case "UInt64":	return type == type.AppDomain.System_UInt64		? KeywordType.ULong		: defaultValue;
				case "UInt16":	return type == type.AppDomain.System_UInt16		? KeywordType.UShort	: defaultValue;
				}
			}
			return defaultValue;
		}
	}
}

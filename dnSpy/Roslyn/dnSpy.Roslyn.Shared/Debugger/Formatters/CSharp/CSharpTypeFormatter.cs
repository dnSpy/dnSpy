/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.Formatters.CSharp {
	struct CSharpTypeFormatter {
		readonly ITextColorWriter output;
		readonly TypeFormatterOptions options;
		readonly CultureInfo cultureInfo;
		const int MAX_RECURSION = 200;
		int recursionCounter;

		const string ARRAY_OPEN_PAREN = "[";
		const string ARRAY_CLOSE_PAREN = "]";
		const string GENERICS_OPEN_PAREN = "<";
		const string GENERICS_CLOSE_PAREN = ">";
		const string TUPLE_OPEN_PAREN = "(";
		const string TUPLE_CLOSE_PAREN = ")";
		const string HEX_PREFIX = "0x";
		const string COMMENT_BEGIN = "/*";
		const string COMMENT_END = "*/";
		const string IDENTIFIER_ESCAPE = "@";
		const string BYREF_KEYWORD = "ref";
		const int MAX_ARRAY_RANK = 100;

		bool ShowArrayValueSizes => (options & TypeFormatterOptions.ShowArrayValueSizes) != 0;
		bool UseDecimal => (options & TypeFormatterOptions.UseDecimal) != 0;
		bool DigitSeparators => (options & TypeFormatterOptions.DigitSeparators) != 0;
		bool ShowIntrinsicTypeKeywords => (options & TypeFormatterOptions.IntrinsicTypeKeywords) != 0;
		bool ShowTokens => (options & TypeFormatterOptions.Tokens) != 0;
		bool ShowNamespaces => (options & TypeFormatterOptions.Namespaces) != 0;

		public CSharpTypeFormatter(ITextColorWriter output, TypeFormatterOptions options, CultureInfo cultureInfo) {
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.options = options;
			this.cultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
			recursionCounter = 0;
		}

		void OutputWrite(string s, object color) => output.Write(color, s);

		void WriteSpace() => OutputWrite(" ", BoxedTextColor.Text);

		void WriteCommaSpace() {
			OutputWrite(",", BoxedTextColor.Punctuation);
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

		string FormatHexInt32(int value) => ToFormattedHexNumber(value.ToString("X8"));

		void WriteUInt32(uint value) => OutputWrite(FormatUInt32(value), BoxedTextColor.Number);
		void WriteInt32(int value) => OutputWrite(FormatInt32(value), BoxedTextColor.Number);

		void WriteTokenComment(int metadataToken) {
			if (!ShowTokens)
				return;
			OutputWrite(COMMENT_BEGIN + FormatHexInt32(metadataToken) + COMMENT_END, BoxedTextColor.Comment);
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

		internal static string GetFormattedIdentifier(string id) {
			if (isKeyword.Contains(id))
				return IDENTIFIER_ESCAPE + IdentifierEscaper.Escape(id);
			return IdentifierEscaper.Escape(id);
		}

		void WriteIdentifier(string id, object color) => OutputWrite(GetFormattedIdentifier(id), color);

		public void Format(DmdType type, DbgDotNetValue value) {
			if ((object)type == null)
				throw new ArgumentNullException(nameof(type));

			DbgDotNetValue valueToDispose = null;
			List<(DmdType type, DbgDotNetValue value)> arrayTypesList = null;
			try {
				if (recursionCounter++ >= MAX_RECURSION)
					return;

				DbgDotNetValue elementValue;
				switch (type.TypeSignatureKind) {
				case DmdTypeSignatureKind.SZArray:
				case DmdTypeSignatureKind.MDArray:
					if (value != null && value.IsReference) {
						valueToDispose = value.Dereference();
						elementValue = valueToDispose ?? value;
					}
					else
						elementValue = value;

					// Array types are shown in reverse order
					arrayTypesList = new List<(DmdType type, DbgDotNetValue value)>();
					do {
						arrayTypesList.Add((type, elementValue));
						elementValue = elementValue?.Dereference();
						type = type.GetElementType();
					} while (type.IsArray);
					var t = arrayTypesList[arrayTypesList.Count - 1];
					Format(t.type.GetElementType(), elementValue = t.value?.Dereference());
					elementValue?.Dispose();
					foreach (var tuple in arrayTypesList) {
						var aryType = tuple.type;
						var aryValue = tuple.value;
						uint elementCount;
						if (aryType.IsVariableBoundArray) {
							OutputWrite(ARRAY_OPEN_PAREN, BoxedTextColor.Punctuation);
							int rank = Math.Min(aryType.GetArrayRank(), MAX_ARRAY_RANK);
							if (rank <= 0)
								OutputWrite("???", BoxedTextColor.Error);
							else {
								if (rank == 1)
									OutputWrite("*", BoxedTextColor.Operator);
								if (aryValue == null || !aryValue.GetArrayInfo(out elementCount, out var dimensionInfos))
									dimensionInfos = null;
								if (ShowArrayValueSizes && dimensionInfos != null && dimensionInfos.Length == rank) {
									for (int i = 0; i < rank; i++) {
										if (i > 0) {
											OutputWrite(",", BoxedTextColor.Punctuation);
											WriteSpace();
										}
										if (dimensionInfos[i].BaseIndex == 0)
											WriteUInt32(dimensionInfos[i].Length);
										else {
											WriteInt32(dimensionInfos[i].BaseIndex);
											OutputWrite("..", BoxedTextColor.Operator);
											WriteInt32(dimensionInfos[i].BaseIndex + (int)dimensionInfos[i].Length - 1);
										}
									}
								}
								else
									OutputWrite(TypeFormatterUtils.GetArrayCommas(rank), BoxedTextColor.Punctuation);
							}
							OutputWrite(ARRAY_CLOSE_PAREN, BoxedTextColor.Punctuation);
						}
						else {
							Debug.Assert(aryType.IsSZArray);
							OutputWrite(ARRAY_OPEN_PAREN, BoxedTextColor.Punctuation);
							if (ShowArrayValueSizes && aryValue != null) {
								if (aryValue.GetArrayCount(out elementCount))
									WriteUInt32(elementCount);
							}
							OutputWrite(ARRAY_CLOSE_PAREN, BoxedTextColor.Punctuation);
						}
					}
					break;

				case DmdTypeSignatureKind.Pointer:
					Format(type.GetElementType(), null);
					OutputWrite("*", BoxedTextColor.Operator);
					break;

				case DmdTypeSignatureKind.ByRef:
					OutputWrite(BYREF_KEYWORD, BoxedTextColor.Keyword);
					WriteSpace();
					Format(type.GetElementType(), valueToDispose = value.Dereference());
					break;

				case DmdTypeSignatureKind.TypeGenericParameter:
					WriteIdentifier(type.MetadataName, BoxedTextColor.TypeGenericParameter);
					break;

				case DmdTypeSignatureKind.MethodGenericParameter:
					WriteIdentifier(type.MetadataName, BoxedTextColor.MethodGenericParameter);
					break;

				case DmdTypeSignatureKind.Type:
				case DmdTypeSignatureKind.GenericInstance:
					if (type.IsNullable) {
						Format(type.GetNullableElementType(), null);
						OutputWrite("?", BoxedTextColor.Operator);
					}
					else if (TypeFormatterUtils.IsTupleType(type)) {
						OutputWrite(TUPLE_OPEN_PAREN, BoxedTextColor.Punctuation);
						var tupleType = type;
						int tupleIndex = 0;
						for (;;) {
							tupleType = WriteTupleFields(tupleType, ref tupleIndex);
							if ((object)tupleType != null)
								WriteCommaSpace();
							else
								break;
						}
						OutputWrite(TUPLE_CLOSE_PAREN, BoxedTextColor.Punctuation);
					}
					else {
						var genericArgs = type.GetGenericArguments();
						int genericArgsIndex = 0;
						KeywordType keywordType;
						if ((object)type.DeclaringType == null) {
							keywordType = GetKeywordType(type);
							if (keywordType == KeywordType.NoKeyword)
								WriteNamespace(type);
							WriteTypeName(type, keywordType);
							WriteGenericArguments(type, genericArgs, ref genericArgsIndex);
						}
						else {
							var typesList = new List<DmdType>();
							typesList.Add(type);
							while (type.DeclaringType != null) {
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
									OutputWrite(".", BoxedTextColor.Operator);
							}
						}
					}
					break;

				case DmdTypeSignatureKind.FunctionPointer:
					//TODO:
					OutputWrite("fnptr", BoxedTextColor.Keyword);
					break;

				default:
					throw new InvalidOperationException();
				}
			}
			finally {
				recursionCounter--;
				valueToDispose?.Dispose();
				if (arrayTypesList != null) {
					foreach (var info in arrayTypesList) {
						if (info.value != value)
							info.value?.Dispose();
					}
				}
			}
		}

		void WriteGenericArguments(DmdType type, IList<DmdType> genericArgs, ref int genericArgsIndex) {
			var gas = type.GetGenericArguments();
			if (genericArgsIndex < genericArgs.Count && genericArgsIndex < gas.Count) {
				OutputWrite(GENERICS_OPEN_PAREN, BoxedTextColor.Punctuation);
				int startIndex = genericArgsIndex;
				for (int j = startIndex; j < genericArgs.Count && j < gas.Count; j++, genericArgsIndex++) {
					if (j > startIndex)
						WriteCommaSpace();
					Format(genericArgs[j], null);
				}
				OutputWrite(GENERICS_CLOSE_PAREN, BoxedTextColor.Punctuation);
			}
		}

		DmdType WriteTupleFields(DmdType type, ref int index) {
			var args = type.GetGenericArguments();
			Debug.Assert(0 < args.Count && args.Count <= TypeFormatterUtils.MAX_TUPLE_ARITY);
			if (args.Count > TypeFormatterUtils.MAX_TUPLE_ARITY) {
				OutputWrite("???", BoxedTextColor.Error);
				return null;
			}
			for (int i = 0; i < args.Count && i < TypeFormatterUtils.MAX_TUPLE_ARITY - 1; i++) {
				if (i > 0)
					WriteCommaSpace();
				Format(args[i], null);
				//TODO: Write tuple name used in source
				string fieldName = null;
				if (fieldName != null) {
					WriteSpace();
					OutputWrite(fieldName, BoxedTextColor.InstanceField);
				}
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
			if (string.IsNullOrEmpty(ns))
				return;
			foreach (var nsPart in ns.Split(namespaceSeparators)) {
				WriteIdentifier(nsPart, BoxedTextColor.Namespace);
				OutputWrite(".", BoxedTextColor.Operator);
			}
		}
		static readonly char[] namespaceSeparators = new[] { '.' };

		void WriteTypeName(DmdType type, KeywordType keywordType) {
			switch (keywordType) {
			case KeywordType.Void:		OutputWrite("void", BoxedTextColor.Keyword); return;
			case KeywordType.Boolean:	OutputWrite("bool", BoxedTextColor.Keyword); return;
			case KeywordType.Char:		OutputWrite("char", BoxedTextColor.Keyword); return;
			case KeywordType.SByte:		OutputWrite("sbyte", BoxedTextColor.Keyword); return;
			case KeywordType.Byte:		OutputWrite("byte", BoxedTextColor.Keyword); return;
			case KeywordType.Int16:		OutputWrite("short", BoxedTextColor.Keyword); return;
			case KeywordType.UInt16:	OutputWrite("ushort", BoxedTextColor.Keyword); return;
			case KeywordType.Int32:		OutputWrite("int", BoxedTextColor.Keyword); return;
			case KeywordType.UInt32:	OutputWrite("uint", BoxedTextColor.Keyword); return;
			case KeywordType.Int64:		OutputWrite("long", BoxedTextColor.Keyword); return;
			case KeywordType.UInt64:	OutputWrite("ulong", BoxedTextColor.Keyword); return;
			case KeywordType.Single:	OutputWrite("float", BoxedTextColor.Keyword); return;
			case KeywordType.Double:	OutputWrite("double", BoxedTextColor.Keyword); return;
			case KeywordType.Object:	OutputWrite("object", BoxedTextColor.Keyword); return;
			case KeywordType.Decimal:	OutputWrite("decimal", BoxedTextColor.Keyword); return;
			case KeywordType.String:	OutputWrite("string", BoxedTextColor.Keyword); return;

			case KeywordType.NoKeyword:
				break;

			default:
				throw new InvalidOperationException();
			}

			WriteIdentifier(TypeFormatterUtils.RemoveGenericTick(type.MetadataName), TypeFormatterUtils.GetTypeColor(type, canBeModule: false));
			WriteTokenComment(type.MetadataToken);
		}

		enum KeywordType {
			NoKeyword,
			Void,
			Boolean,
			Char,
			SByte,
			Byte,
			Int16,
			UInt16,
			Int32,
			UInt32,
			Int64,
			UInt64,
			Single,
			Double,
			Object,
			Decimal,
			String,
		}

		KeywordType GetKeywordType(DmdType type) {
			const KeywordType defaultValue = KeywordType.NoKeyword;
			if (!ShowIntrinsicTypeKeywords)
				return defaultValue;
			if (type.MetadataNamespace == "System" && !type.IsNested) {
				switch (type.MetadataName) {
				case "Void":	return type == type.AppDomain.System_Void		? KeywordType.Void		: defaultValue;
				case "Boolean":	return type == type.AppDomain.System_Boolean	? KeywordType.Boolean	: defaultValue;
				case "Char":	return type == type.AppDomain.System_Char		? KeywordType.Char		: defaultValue;
				case "SByte":	return type == type.AppDomain.System_SByte		? KeywordType.SByte		: defaultValue;
				case "Byte":	return type == type.AppDomain.System_Byte		? KeywordType.Byte		: defaultValue;
				case "Int16":	return type == type.AppDomain.System_Int16		? KeywordType.Int16		: defaultValue;
				case "UInt16":	return type == type.AppDomain.System_UInt16		? KeywordType.UInt16	: defaultValue;
				case "Int32":	return type == type.AppDomain.System_Int32		? KeywordType.Int32		: defaultValue;
				case "UInt32":	return type == type.AppDomain.System_UInt32		? KeywordType.UInt32	: defaultValue;
				case "Int64":	return type == type.AppDomain.System_Int64		? KeywordType.Int64		: defaultValue;
				case "UInt64":	return type == type.AppDomain.System_UInt64		? KeywordType.UInt64	: defaultValue;
				case "Single":	return type == type.AppDomain.System_Single		? KeywordType.Single	: defaultValue;
				case "Double":	return type == type.AppDomain.System_Double		? KeywordType.Double	: defaultValue;
				case "Object":	return type == type.AppDomain.System_Object		? KeywordType.Object	: defaultValue;
				case "Decimal":	return type == type.AppDomain.System_Decimal	? KeywordType.Decimal	: defaultValue;
				case "String":	return type == type.AppDomain.System_String		? KeywordType.String	: defaultValue;
				}
			}
			return defaultValue;
		}
	}
}

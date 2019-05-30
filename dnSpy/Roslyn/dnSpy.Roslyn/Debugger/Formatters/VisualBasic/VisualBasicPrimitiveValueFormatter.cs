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
using System.Diagnostics;
using System.Globalization;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.Formatters.VisualBasic {
	readonly struct VisualBasicPrimitiveValueFormatter {
		readonly IDbgTextWriter output;
		readonly ValueFormatterOptions options;
		readonly CultureInfo? cultureInfo;

		const string Keyword_true = "True";
		const string Keyword_false = "False";
		const string Keyword_null = "Nothing";
		const string TypeNameOpenParen = "{";
		const string TypeNameCloseParen = "}";
		const string HexPrefix = "&H";
		const string DecimalSuffix = "D";
		const string EnumFlagsOrSeparatorKeyword = "Or";
		const string CommentBegin = "/*";
		const string CommentEnd = "*/";

		bool Edit => (options & ValueFormatterOptions.Edit) != 0;
		bool Decimal => (options & ValueFormatterOptions.Decimal) != 0;
		bool DigitSeparators => (options & ValueFormatterOptions.DigitSeparators) != 0;
		bool NoStringQuotes => (options & ValueFormatterOptions.NoStringQuotes) != 0;
		bool ShowTokens => (options & ValueFormatterOptions.Tokens) != 0;
		bool FullString => (options & ValueFormatterOptions.FullString) != 0;

		public VisualBasicPrimitiveValueFormatter(IDbgTextWriter output, ValueFormatterOptions options, CultureInfo? cultureInfo) {
			this.output = output;
			this.options = options;
			this.cultureInfo = cultureInfo;
		}

		void OutputWrite(string s, DbgTextColor color) => output.Write(color, s);

		void WriteSpace() => OutputWrite(" ", DbgTextColor.Text);

		void FormatType(DmdType type) {
			var typeOptions = options.ToTypeFormatterOptions(showArrayValueSizes: false);
			if (Edit) {
				typeOptions |= TypeFormatterOptions.Namespaces;
				typeOptions &= ~TypeFormatterOptions.Tokens;
			}
			new VisualBasicTypeFormatter(output, typeOptions, cultureInfo).Format(type, null);
		}

		string FormatHexInt32(int value) => ToFormattedHexNumber(value.ToString("X8"));
		public void WriteTokenComment(int metadataToken) {
			if (!ShowTokens)
				return;
			OutputWrite(CommentBegin + FormatHexInt32(metadataToken) + CommentEnd, DbgTextColor.Comment);
		}

		public bool TryFormat(DmdType type, DbgDotNetRawValue rawValue) {
			if (!rawValue.HasRawValue)
				return false;

			if (rawValue.RawValue is null) {
				OutputWrite(Keyword_null, DbgTextColor.Keyword);
				return true;
			}

			if (type.IsEnum && NumberUtils.TryConvertIntegerToUInt64ZeroExtend(rawValue.RawValue, out var enumValue)) {
				FormatEnum(enumValue, type);
				return true;
			}

			switch (rawValue.ValueType) {
			case DbgSimpleValueType.Other:
				if (rawValue.RawValue is DmdType) {
					// It's a type variable
					FormatType((DmdType)rawValue.RawValue);
					return true;
				}
				return false;

			case DbgSimpleValueType.Boolean:
				FormatBoolean((bool)rawValue.RawValue);
				return true;

			case DbgSimpleValueType.Char1:
				FormatChar((char)(byte)rawValue.RawValue);
				return true;

			case DbgSimpleValueType.CharUtf16:
				FormatChar((char)rawValue.RawValue);
				return true;

			case DbgSimpleValueType.Int8:
				FormatSByte((sbyte)rawValue.RawValue);
				return true;

			case DbgSimpleValueType.Int16:
				FormatInt16((short)rawValue.RawValue);
				return true;

			case DbgSimpleValueType.Int32:
				FormatInt32((int)rawValue.RawValue);
				return true;

			case DbgSimpleValueType.Int64:
				FormatInt64((long)rawValue.RawValue);
				return true;

			case DbgSimpleValueType.UInt8:
				FormatByte((byte)rawValue.RawValue);
				return true;

			case DbgSimpleValueType.UInt16:
				FormatUInt16((ushort)rawValue.RawValue);
				return true;

			case DbgSimpleValueType.UInt32:
				FormatUInt32((uint)rawValue.RawValue);
				return true;

			case DbgSimpleValueType.UInt64:
				FormatUInt64((ulong)rawValue.RawValue);
				return true;

			case DbgSimpleValueType.Float32:
				FormatSingle((float)rawValue.RawValue, type.AppDomain);
				return true;

			case DbgSimpleValueType.Float64:
				FormatDouble((double)rawValue.RawValue, type.AppDomain);
				return true;

			case DbgSimpleValueType.Decimal:
				FormatDecimal((decimal)rawValue.RawValue);
				return true;

			case DbgSimpleValueType.Ptr32:
				FormatPointer32((uint)rawValue.RawValue);
				return true;

			case DbgSimpleValueType.Ptr64:
				FormatPointer64((ulong)rawValue.RawValue);
				return true;

			case DbgSimpleValueType.StringUtf16:
				FormatString((string)rawValue.RawValue);
				return true;

			case DbgSimpleValueType.DateTime:
				FormatDateTime((DateTime)rawValue.RawValue);
				return true;

			default:
				throw new InvalidOperationException();
			}
		}

		void FormatEnum(ulong enumValue, DmdType type) {
			Debug.Assert(type.IsEnum);
			var enumInfo = EnumInfo.GetEnumInfo(type);
			if (enumInfo.HasFlagsAttribute && enumValue != 0) {
				var f = enumValue;
				bool needSep = false;
				foreach (var info in enumInfo.FieldInfos) {
					var flag = info.Value;
					if ((f & flag) == 0)
						continue;
					if (needSep)
						WriteEnumSeperator();
					needSep = true;
					WriteEnumField(info.Field);
					f &= ~flag;
					if (f == 0)
						break;
				}
				if (f != 0) {
					if (needSep)
						WriteEnumSeperator();
					WriteEnumInteger(type, f);
				}
				if (needSep)
					WriteRawValue(enumValue, type);
			}
			else {
				bool printed = false;
				foreach (var info in enumInfo.FieldInfos) {
					if (info.Value == enumValue) {
						WriteEnumField(info.Field);
						WriteRawValue(enumValue, type);
						printed = true;
						break;
					}
				}
				if (!printed)
					WriteEnumInteger(type, enumValue);
			}
		}

		void WriteRawValue(ulong enumValue, DmdType type) {
			Debug.Assert(type.IsEnum);
			if (Edit)
				return;
			WriteSpace();
			OutputWrite(TypeNameOpenParen, DbgTextColor.Error);
			WriteNumber(ToFormattedInteger(type.GetEnumUnderlyingType(), enumValue));
			OutputWrite(TypeNameCloseParen, DbgTextColor.Error);
		}

		void WriteEnumField(DmdFieldInfo field) {
			if (Edit) {
				FormatType(field.ReflectedType!);
				OutputWrite(".", DbgTextColor.Operator);
			}
			OutputWrite(VisualBasicTypeFormatter.GetFormattedIdentifier(field.Name), DbgTextColor.EnumField);
		}

		void WriteEnumInteger(DmdType type, ulong value) {
			if (Edit) {
				OutputWrite("CType", DbgTextColor.Keyword);
				OutputWrite("(", DbgTextColor.Punctuation);
				WriteNumber(ToFormattedInteger(type, value));
				OutputWrite(",", DbgTextColor.Punctuation);
				WriteSpace();
				FormatType(type);
				OutputWrite(")", DbgTextColor.Punctuation);
			}
			else
				WriteNumber(ToFormattedInteger(type, value));
		}

		void WriteEnumSeperator() {
			WriteSpace();
			OutputWrite(EnumFlagsOrSeparatorKeyword, DbgTextColor.Keyword);
			WriteSpace();
		}

		string ToFormattedInteger(DmdType type, ulong value) {
			switch (DmdType.GetTypeCode(type)) {
			case TypeCode.Boolean:	return ToFormattedByte((byte)value);
			case TypeCode.Char:		return ToFormattedUInt16((char)value);
			case TypeCode.SByte:	return ToFormattedSByte((sbyte)value);
			case TypeCode.Byte:		return ToFormattedByte((byte)value);
			case TypeCode.Int16:	return ToFormattedInt16((short)value);
			case TypeCode.UInt16:	return ToFormattedUInt16((ushort)value);
			case TypeCode.Int32:	return ToFormattedInt32((int)value);
			case TypeCode.UInt32:	return ToFormattedUInt32((uint)value);
			case TypeCode.Int64:	return ToFormattedInt64((long)value);
			case TypeCode.UInt64:	return ToFormattedUInt64(value);
			default:
				break;
			}
			if (type == type.AppDomain.System_IntPtr || type == type.AppDomain.System_UIntPtr) {
				if (type.AppDomain.Runtime.PointerSize == 4)
					return ToFormattedPointer32((uint)value);
				return ToFormattedPointer64(value);
			}
			Debug.Fail("Unknown type");
			return ToFormattedUInt64(value);
		}

		public void FormatBoolean(bool value) {
			if (value)
				OutputWrite(Keyword_true, DbgTextColor.Keyword);
			else
				OutputWrite(Keyword_false, DbgTextColor.Keyword);
		}

		public void FormatChar(char value) {
			if (!Edit) {
				FormatUInt16(value);
				WriteSpace();
			}

			switch (value) {
			case '\r':	OutputWrite("vbCr", DbgTextColor.LiteralField); break;
			case '\n':	OutputWrite("vbLf", DbgTextColor.LiteralField); break;
			case '\b':	OutputWrite("vbBack", DbgTextColor.LiteralField); break;
			case '\f':	OutputWrite("vbFormFeed", DbgTextColor.LiteralField); break;
			case '\t':	OutputWrite("vbTab", DbgTextColor.LiteralField); break;
			case '\v':	OutputWrite("vbVerticalTab", DbgTextColor.LiteralField); break;
			case '\0':	OutputWrite("vbNullChar", DbgTextColor.LiteralField); break;
			case '"':	OutputWrite("\"\"\"\"c", DbgTextColor.Char); break;
			default:
				if (char.IsControl(value))
					WriteCharW(value);
				else
					OutputWrite("\"" + value.ToString(cultureInfo) + "\"c", DbgTextColor.Char);
				break;
			}
		}

		void WriteCharW(char value) {
			OutputWrite("ChrW", DbgTextColor.StaticMethod);
			OutputWrite("(", DbgTextColor.Punctuation);
			FormatUInt16(value);
			OutputWrite(")", DbgTextColor.Punctuation);
		}

		public void FormatString(string value) {
			bool stringTooLong = !FullString && value.Length > ValueFormatterUtils.MaxStringLength;
			if (stringTooLong)
				value = value.Substring(0, ValueFormatterUtils.MaxStringLength);

			if (NoStringQuotes) {
				OutputWrite(value, DbgTextColor.DebuggerNoStringQuotesEval);
				if (stringTooLong)
					OutputWrite("[...]", DbgTextColor.DebuggerNoStringQuotesEval);
				return;
			}

			if (value == string.Empty) {
				OutputWrite("\"\"", DbgTextColor.String);
				return;
			}

			int index = 0;
			bool needSep = false;
			while (index < value.Length) {
				var s = GetSubString(value, ref index);
				if (s.Length != 0) {
					if (needSep)
						WriteStringConcatOperator();
					OutputWrite("\"" + s + "\"", DbgTextColor.String);
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
			if (stringTooLong) {
				if (needSep)
					WriteStringConcatOperator();
				OutputWrite("\"" + "[...]" + "\"", DbgTextColor.String);
			}
		}

		void WriteStringConcatOperator() {
			WriteSpace();
			OutputWrite("&", DbgTextColor.Operator);
			WriteSpace();
		}

		void WriteSpecialConstantString(string s, ref bool needSep) {
			if (needSep)
				WriteStringConcatOperator();
			OutputWrite(s, DbgTextColor.LiteralField);
			needSep = true;
		}

		string GetSubString(string value, ref int index) {
			var sb = ObjectCache.AllocStringBuilder();

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

			return ObjectCache.FreeAndToString(ref sb);
		}

		string ToFormattedDecimalNumber(string number) => ToFormattedNumber(string.Empty, number, ValueFormatterUtils.DigitGroupSizeDecimal);
		string ToFormattedHexNumber(string number) => ToFormattedNumber(HexPrefix, number, ValueFormatterUtils.DigitGroupSizeHex);
		string ToFormattedNumber(string prefix, string number, int digitGroupSize) => ValueFormatterUtils.ToFormattedNumber(DigitSeparators, prefix, number, digitGroupSize);
		void WriteNumber(string number) => OutputWrite(number, DbgTextColor.Number);

		public string ToFormattedSByte(sbyte value) {
			if (Decimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X2"));
		}

		public string ToFormattedByte(byte value) {
			if (Decimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X2"));
		}

		public string ToFormattedInt16(short value) {
			if (Decimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X4"));
		}

		public string ToFormattedUInt16(ushort value) {
			if (Decimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X4"));
		}

		public string ToFormattedInt32(int value) {
			if (Decimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X8"));
		}

		public string ToFormattedUInt32(uint value) {
			if (Decimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X8"));
		}

		public string ToFormattedInt64(long value) {
			if (Decimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X16"));
		}

		public string ToFormattedUInt64(ulong value) {
			if (Decimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X16"));
		}

		public string ToFormattedNumberFewDigits(ulong value) {
			if (Decimal)
				return ToFormattedDecimalNumber(value.ToString(cultureInfo));
			else
				return ToFormattedHexNumber(value.ToString("X"));
		}

		public void FormatSingle(float value, DmdAppDomain appDomain) {
			if (float.IsNaN(value)) {
				if (!Edit)
					OutputWrite(ValueFormatterUtils.NaN, DbgTextColor.Number);
				else {
					FormatType(appDomain.System_Single);
					OutputWrite(".", DbgTextColor.Operator);
					OutputWrite("NaN", DbgTextColor.LiteralField);
				}
			}
			else if (float.IsNegativeInfinity(value)) {
				if (!Edit)
					OutputWrite(ValueFormatterUtils.NegativeInfinity, DbgTextColor.Number);
				else {
					FormatType(appDomain.System_Single);
					OutputWrite(".", DbgTextColor.Operator);
					OutputWrite("NegativeInfinity", DbgTextColor.LiteralField);
				}
			}
			else if (float.IsPositiveInfinity(value)) {
				if (!Edit)
					OutputWrite(ValueFormatterUtils.PositiveInfinity, DbgTextColor.Number);
				else {
					FormatType(appDomain.System_Single);
					OutputWrite(".", DbgTextColor.Operator);
					OutputWrite("PositiveInfinity", DbgTextColor.LiteralField);
				}
			}
			else
				OutputWrite(value.ToString(cultureInfo), DbgTextColor.Number);
		}

		public void FormatDouble(double value, DmdAppDomain appDomain) {
			if (double.IsNaN(value)) {
				if (!Edit)
					OutputWrite(ValueFormatterUtils.NaN, DbgTextColor.Number);
				else {
					FormatType(appDomain.System_Double);
					OutputWrite(".", DbgTextColor.Operator);
					OutputWrite("NaN", DbgTextColor.LiteralField);
				}
			}
			else if (double.IsNegativeInfinity(value)) {
				if (!Edit)
					OutputWrite(ValueFormatterUtils.NegativeInfinity, DbgTextColor.Number);
				else {
					FormatType(appDomain.System_Double);
					OutputWrite(".", DbgTextColor.Operator);
					OutputWrite("NegativeInfinity", DbgTextColor.LiteralField);
				}
			}
			else if (double.IsPositiveInfinity(value)) {
				if (!Edit)
					OutputWrite(ValueFormatterUtils.PositiveInfinity, DbgTextColor.Number);
				else {
					FormatType(appDomain.System_Double);
					OutputWrite(".", DbgTextColor.Operator);
					OutputWrite("PositiveInfinity", DbgTextColor.LiteralField);
				}
			}
			else
				OutputWrite(value.ToString(cultureInfo), DbgTextColor.Number);
		}

		string ToFormattedDecimal(decimal value) {
			var s = value.ToString(cultureInfo);
			if (Edit)
				s += DecimalSuffix;
			return s;
		}

		public string ToFormattedPointer32(uint value) {
			// It's always hex
			return ToFormattedHexNumber(value.ToString("X8"));
		}

		public string ToFormattedPointer64(ulong value) {
			// It's always hex
			return ToFormattedHexNumber(value.ToString("X16"));
		}

		public void FormatSByte(sbyte value) => WriteNumber(ToFormattedSByte(value));
		public void FormatByte(byte value) => WriteNumber(ToFormattedByte(value));
		public void FormatInt16(short value) => WriteNumber(ToFormattedInt16(value));
		public void FormatUInt16(ushort value) => WriteNumber(ToFormattedUInt16(value));
		public void FormatInt32(int value) => WriteNumber(ToFormattedInt32(value));
		public void FormatUInt32(uint value) => WriteNumber(ToFormattedUInt32(value));
		public void FormatInt64(long value) => WriteNumber(ToFormattedInt64(value));
		public void FormatUInt64(ulong value) => WriteNumber(ToFormattedUInt64(value));
		public void FormatDecimal(decimal value) => WriteNumber(ToFormattedDecimal(value));
		public void FormatPointer32(uint value) => WriteNumber(ToFormattedPointer32(value));
		public void FormatPointer64(ulong value) => WriteNumber(ToFormattedPointer64(value));
		public void FormatFewDigits(ulong value) => WriteNumber(ToFormattedNumberFewDigits(value));

		public void FormatDateTime(DateTime value) {
			// Roslyn EE always uses invariant culture
			var s = value.ToString("#M/d/yyyy hh:mm:ss tt#", CultureInfo.InvariantCulture);
			OutputWrite(s, DbgTextColor.Number);
		}
	}
}

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
using System.Diagnostics;
using System.Threading;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Roslyn.Shared.Properties;

namespace dnSpy.Roslyn.Shared.Debugger.Formatters {
	struct VisualBasicValueFormatter {
		readonly ITextColorWriter output;
		readonly ValueFormatterOptions options;
		/*readonly*/ CancellationToken cancellationToken;
		const int MAX_RECURSION = 200;
		int recursionCounter;

		const string Keyword_true = "True";
		const string Keyword_false = "False";
		const string Keyword_null = "Nothing";
		const string TypeNameOpenParen = "{";
		const string TypeNameCloseParen = "}";
		const string HexPrefix = "&H";
		const string DecimalSuffix = "D";
		const string EnumFlagsOrSeparatorKeyword = "Or";

		bool Display => (options & ValueFormatterOptions.Display) != 0;
		bool Decimal => (options & ValueFormatterOptions.Decimal) != 0;
		bool FuncEval => (options & ValueFormatterOptions.FuncEval) != 0;
		bool UseToString => (options & ValueFormatterOptions.ToString) != 0;
		bool DigitSeparators => (options & ValueFormatterOptions.DigitSeparators) != 0;

		public VisualBasicValueFormatter(ITextColorWriter output, ValueFormatterOptions options, CancellationToken cancellationToken) {
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.options = options;
			this.cancellationToken = cancellationToken;
			recursionCounter = 0;
		}

		void OutputWrite(string s, object color) => output.Write(color, s);

		void WriteSpace() => OutputWrite(" ", BoxedTextColor.Text);

		public void Format(DbgDotNetValue value) {
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			cancellationToken.ThrowIfCancellationRequested();
			try {
				if (recursionCounter++ >= MAX_RECURSION) {
					OutputWrite("???", BoxedTextColor.Error);
					return;
				}

				if (TrySimpleFormat(value))
					return;
				var type = value.Type;
				if (type.IsNullable && TryFormatNullable(value))
					return;
				if (TypeFormatterUtils.IsTupleType(type) && TryFormatTuple(value))
					return;
				if (TryFormatWithDebuggerAttributes())
					return;
				if (TryFormatToString())
					return;
				FormatTypeName(value);
			}
			finally {
				recursionCounter--;
			}
		}

		void FormatTypeName(DbgDotNetValue value) {
			OutputWrite(TypeNameOpenParen, BoxedTextColor.Error);
			new VisualBasicTypeFormatter(output, options.ToTypeFormatterOptions(showArrayValueSizes: true)).Format(value.Type, value);
			OutputWrite(TypeNameCloseParen, BoxedTextColor.Error);
		}

		void FormatType(DmdType type) {
			var typeOptions = options.ToTypeFormatterOptions(showArrayValueSizes: false);
			if (!Display) {
				typeOptions |= TypeFormatterOptions.Namespaces;
				typeOptions &= ~TypeFormatterOptions.Tokens;
			}
			new VisualBasicTypeFormatter(output, typeOptions).Format(type, null);
		}

		bool TryFormatNullable(DbgDotNetValue value) {
			Debug.Assert(value.Type.IsNullable);
			//TODO:
			return false;
		}

		bool TryFormatTuple(DbgDotNetValue value) {
			Debug.Assert(TypeFormatterUtils.IsTupleType(value.Type));
			//TODO:
			return false;
		}

		bool TryFormatWithDebuggerAttributes() {
			if (!FuncEval)
				return false;
			//TODO: If it's derived from System.Type with no debugger attrs, format it as if it had
			//		[DebuggerDisplay(@"\{Name = {Name} FullName = {FullName}\}")]
			//TODO: Use debugger attributes if available
			//		System.Diagnostics.DebuggerBrowsableAttribute
			//		System.Diagnostics.DebuggerDisplayAttribute
			//		System.Diagnostics.DebuggerTypeProxyAttribute
			//		System.Diagnostics.DebuggerVisualizerAttribute
			return false;
		}

		bool TryFormatToString() {
			if (!FuncEval || !UseToString)
				return false;
			return false;//TODO: Call ToString()
		}

		bool TrySimpleFormat(DbgDotNetValue value) {
			var rawValue = value.GetRawValue();
			if (!rawValue.HasRawValue)
				return false;

			if (rawValue.RawValue == null) {
				OutputWrite(Keyword_null, BoxedTextColor.Keyword);
				return true;
			}

			var type = value.Type;
			if (type.IsEnum && NumberUtils.TryConvertIntegerToUInt64ZeroExtend(rawValue.RawValue, out var enumValue)) {
				FormatEnum(enumValue, type);
				return true;
			}

			switch (rawValue.ValueType) {
			case DbgSimpleValueType.OtherReferenceType:
			case DbgSimpleValueType.OtherValueType:
				return false;

			case DbgSimpleValueType.Void:
				Debug.Assert(type == type.AppDomain.System_Void);
				OutputWrite(dnSpy_Roslyn_Shared_Resources.DebuggerExpressionHasNoValue, BoxedTextColor.Text);
				return true;

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
				FormatSingle((float)rawValue.RawValue, value.Type.AppDomain);
				return true;

			case DbgSimpleValueType.Float64:
				FormatDouble((double)rawValue.RawValue, value.Type.AppDomain);
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
				FormatDateTime((ulong)rawValue.RawValue);
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
			}
			else {
				bool printed = false;
				foreach (var info in enumInfo.FieldInfos) {
					if (info.Value == enumValue) {
						WriteEnumField(info.Field);
						printed = true;
						break;
					}
				}
				if (!printed)
					WriteEnumInteger(type, enumValue);
			}
		}

		void WriteEnumField(DmdFieldInfo field) {
			if (!Display) {
				FormatType(field.ReflectedType);
				OutputWrite(".", BoxedTextColor.Operator);
			}
			OutputWrite(VisualBasicTypeFormatter.GetFormattedIdentifier(field.Name), BoxedTextColor.EnumField);
		}

		void WriteEnumInteger(DmdType type, ulong value) {
			if (!Display) {
				OutputWrite("CType", BoxedTextColor.Keyword);
				OutputWrite("(", BoxedTextColor.Punctuation);
				WriteNumber(ToFormattedInteger(type, value));
				OutputWrite(",", BoxedTextColor.Punctuation);
				WriteSpace();
				FormatType(type);
				OutputWrite(")", BoxedTextColor.Punctuation);
			}
			else
				WriteNumber(ToFormattedInteger(type, value));
		}

		void WriteEnumSeperator() {
			WriteSpace();
			OutputWrite(EnumFlagsOrSeparatorKeyword, BoxedTextColor.Keyword);
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

		void FormatBoolean(bool value) {
			if (value)
				OutputWrite(Keyword_true, BoxedTextColor.Keyword);
			else
				OutputWrite(Keyword_false, BoxedTextColor.Keyword);
		}

		void FormatChar(char value) {
			if (Display) {
				FormatUInt16(value);
				WriteSpace();
			}

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
			var sb = ValueFormatterObjectCache.AllocStringBuilder();

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

			return ValueFormatterObjectCache.FreeAndToString(ref sb);
		}

		string ToFormattedDecimalNumber(string number) => ToFormattedNumber(string.Empty, number, ValueFormatterUtils.DigitGroupSizeDecimal);
		string ToFormattedHexNumber(string number) => ToFormattedNumber(HexPrefix, number, ValueFormatterUtils.DigitGroupSizeHex);

		string ToFormattedNumber(string prefix, string number, int digitGroupSize) {
			if (DigitSeparators)
				number = AddDigitSeparators(number, digitGroupSize, ValueFormatterUtils.DigitSeparator);

			string res = number;
			if (prefix.Length != 0)
				res = prefix + res;
			return res;
		}

		static string AddDigitSeparators(string number, int digitGroupSize, string digitSeparator) {
			if (number.Length <= digitGroupSize)
				return number;

			var sb = ValueFormatterObjectCache.AllocStringBuilder();

			for (int i = 0; i < number.Length; i++) {
				int d = number.Length - i;
				if (i != 0 && (d % digitGroupSize) == 0)
					sb.Append(ValueFormatterUtils.DigitSeparator);
				sb.Append(number[i]);
			}

			return ValueFormatterObjectCache.FreeAndToString(ref sb);
		}

		void WriteNumber(string number) => OutputWrite(number, BoxedTextColor.Number);

		string ToFormattedSByte(sbyte value) {
			if (Decimal)
				return ToFormattedDecimalNumber(value.ToString());
			else
				return ToFormattedHexNumber(value.ToString("X2"));
		}

		string ToFormattedByte(byte value) {
			if (Decimal)
				return ToFormattedDecimalNumber(value.ToString());
			else
				return ToFormattedHexNumber(value.ToString("X2"));
		}

		string ToFormattedInt16(short value) {
			if (Decimal)
				return ToFormattedDecimalNumber(value.ToString());
			else
				return ToFormattedHexNumber(value.ToString("X4"));
		}

		string ToFormattedUInt16(ushort value) {
			if (Decimal)
				return ToFormattedDecimalNumber(value.ToString());
			else
				return ToFormattedHexNumber(value.ToString("X4"));
		}

		string ToFormattedInt32(int value) {
			if (Decimal)
				return ToFormattedDecimalNumber(value.ToString());
			else
				return ToFormattedHexNumber(value.ToString("X8"));
		}

		string ToFormattedUInt32(uint value) {
			if (Decimal)
				return ToFormattedDecimalNumber(value.ToString());
			else
				return ToFormattedHexNumber(value.ToString("X8"));
		}

		string ToFormattedInt64(long value) {
			if (Decimal)
				return ToFormattedDecimalNumber(value.ToString());
			else
				return ToFormattedHexNumber(value.ToString("X16"));
		}

		string ToFormattedUInt64(ulong value) {
			if (Decimal)
				return ToFormattedDecimalNumber(value.ToString());
			else
				return ToFormattedHexNumber(value.ToString("X16"));
		}

		void FormatSingle(float value, DmdAppDomain appDomain) {
			if (float.IsNaN(value)) {
				if (Display)
					OutputWrite(ValueFormatterUtils.NaN, BoxedTextColor.Number);
				else {
					FormatType(appDomain.System_Single);
					OutputWrite(".", BoxedTextColor.Operator);
					OutputWrite("NaN", BoxedTextColor.LiteralField);
				}
			}
			else if (float.IsNegativeInfinity(value)) {
				if (Display)
					OutputWrite(ValueFormatterUtils.NegativeInfinity, BoxedTextColor.Number);
				else {
					FormatType(appDomain.System_Single);
					OutputWrite(".", BoxedTextColor.Operator);
					OutputWrite("NegativeInfinity", BoxedTextColor.LiteralField);
				}
			}
			else if (float.IsPositiveInfinity(value)) {
				if (Display)
					OutputWrite(ValueFormatterUtils.PositiveInfinity, BoxedTextColor.Number);
				else {
					FormatType(appDomain.System_Single);
					OutputWrite(".", BoxedTextColor.Operator);
					OutputWrite("PositiveInfinity", BoxedTextColor.LiteralField);
				}
			}
			else
				OutputWrite(value.ToString(), BoxedTextColor.Number);
		}

		void FormatDouble(double value, DmdAppDomain appDomain) {
			if (double.IsNaN(value)) {
				if (Display)
					OutputWrite(ValueFormatterUtils.NaN, BoxedTextColor.Number);
				else {
					FormatType(appDomain.System_Double);
					OutputWrite(".", BoxedTextColor.Operator);
					OutputWrite("NaN", BoxedTextColor.LiteralField);
				}
			}
			else if (double.IsNegativeInfinity(value)) {
				if (Display)
					OutputWrite(ValueFormatterUtils.NegativeInfinity, BoxedTextColor.Number);
				else {
					FormatType(appDomain.System_Double);
					OutputWrite(".", BoxedTextColor.Operator);
					OutputWrite("NegativeInfinity", BoxedTextColor.LiteralField);
				}
			}
			else if (double.IsPositiveInfinity(value)) {
				if (Display)
					OutputWrite(ValueFormatterUtils.PositiveInfinity, BoxedTextColor.Number);
				else {
					FormatType(appDomain.System_Double);
					OutputWrite(".", BoxedTextColor.Operator);
					OutputWrite("PositiveInfinity", BoxedTextColor.LiteralField);
				}
			}
			else
				OutputWrite(value.ToString(), BoxedTextColor.Number);
		}

		string ToFormattedDecimal(decimal value) {
			var s = value.ToString();
			if (!Display)
				s += DecimalSuffix;
			return s;
		}

		string ToFormattedPointer32(uint value) {
			// It's always hex
			return ToFormattedHexNumber(value.ToString("X8"));
		}

		string ToFormattedPointer64(ulong value) {
			// It's always hex
			return ToFormattedHexNumber(value.ToString("X16"));
		}

		void FormatSByte(sbyte value) => WriteNumber(ToFormattedSByte(value));
		void FormatByte(byte value) => WriteNumber(ToFormattedByte(value));
		void FormatInt16(short value) => WriteNumber(ToFormattedInt16(value));
		void FormatUInt16(ushort value) => WriteNumber(ToFormattedUInt16(value));
		void FormatInt32(int value) => WriteNumber(ToFormattedInt32(value));
		void FormatUInt32(uint value) => WriteNumber(ToFormattedUInt32(value));
		void FormatInt64(long value) => WriteNumber(ToFormattedInt64(value));
		void FormatUInt64(ulong value) => WriteNumber(ToFormattedUInt64(value));
		void FormatDecimal(decimal value) => WriteNumber(ToFormattedDecimal(value));
		void FormatPointer32(uint value) => WriteNumber(ToFormattedPointer32(value));
		void FormatPointer64(ulong value) => WriteNumber(ToFormattedPointer64(value));

		void FormatDateTime(ulong value) {
			Debug.Assert(DateTime_ctor_UInt64 != null);
			if (DateTime_ctor_UInt64 != null) {
				var dateTime = (DateTime)DateTime_ctor_UInt64.Invoke(new object[] { value });
				var s = "#" + dateTime.ToString("M/d/yyyy hh:mm:ss tt") + "#";
				OutputWrite(s, BoxedTextColor.Number);
			}
			else
				FormatUInt64(value);
		}
		static readonly System.Reflection.ConstructorInfo DateTime_ctor_UInt64 = typeof(DateTime).GetConstructor(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new[] { typeof(ulong) }, null);
	}
}

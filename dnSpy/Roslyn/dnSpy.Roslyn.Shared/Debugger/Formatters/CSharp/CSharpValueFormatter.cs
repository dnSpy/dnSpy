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
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Roslyn.Shared.Properties;

namespace dnSpy.Roslyn.Shared.Debugger.Formatters.CSharp {
	struct CSharpValueFormatter {
		readonly ITextColorWriter output;
		readonly DbgEvaluationContext context;
		readonly DbgStackFrame frame;
		readonly ValueFormatterOptions options;
		/*readonly*/ CancellationToken cancellationToken;
		const int MAX_RECURSION = 200;
		int recursionCounter;

		const string Keyword_true = "true";
		const string Keyword_false = "false";
		const string Keyword_null = "null";
		const string TypeNameOpenParen = "{";
		const string TypeNameCloseParen = "}";
		const string HexPrefix = "0x";
		const string DecimalSuffix = "M";
		const string VerbatimStringPrefix = "@";
		const string EnumFlagsOrSeparator = "|";

		bool Display => (options & ValueFormatterOptions.Display) != 0;
		bool Decimal => (options & ValueFormatterOptions.Decimal) != 0;
		bool FuncEval => (options & ValueFormatterOptions.FuncEval) != 0;
		bool UseToString => (options & ValueFormatterOptions.ToString) != 0;
		bool DigitSeparators => (options & ValueFormatterOptions.DigitSeparators) != 0;

		public CSharpValueFormatter(ITextColorWriter output, DbgEvaluationContext context, DbgStackFrame frame, ValueFormatterOptions options, CancellationToken cancellationToken) {
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.context = context ?? throw new ArgumentNullException(nameof(context));
			this.frame = frame ?? throw new ArgumentNullException(nameof(frame));
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
				if (TryFormatToString(value))
					return;
				FormatTypeName(value);
			}
			finally {
				recursionCounter--;
			}
		}

		void FormatTypeName(DbgDotNetValue value) {
			OutputWrite(TypeNameOpenParen, BoxedTextColor.Error);
			new CSharpTypeFormatter(output, options.ToTypeFormatterOptions(showArrayValueSizes: true)).Format(value.Type, value);
			OutputWrite(TypeNameCloseParen, BoxedTextColor.Error);
		}

		void FormatType(DmdType type) {
			var typeOptions = options.ToTypeFormatterOptions(showArrayValueSizes: false);
			if (!Display) {
				typeOptions |= TypeFormatterOptions.Namespaces;
				typeOptions &= ~TypeFormatterOptions.Tokens;
			}
			new CSharpTypeFormatter(output, typeOptions).Format(type, null);
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

		bool TryFormatToString(DbgDotNetValue value) {
			if (!FuncEval || !UseToString)
				return false;
			var s = new ToStringFormatter(context, frame, cancellationToken).GetToStringValue(value);
			if (s == null)
				return false;
			OutputWrite(TypeNameOpenParen + s + TypeNameCloseParen, BoxedTextColor.ToStringEval);
			return true;
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
			case DbgSimpleValueType.Other:
			case DbgSimpleValueType.DateTime:
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
			OutputWrite(CSharpTypeFormatter.GetFormattedIdentifier(field.Name), BoxedTextColor.EnumField);
		}

		void WriteEnumInteger(DmdType type, ulong value) {
			if (!Display) {
				OutputWrite("(", BoxedTextColor.Punctuation);
				FormatType(type);
				OutputWrite(")", BoxedTextColor.Punctuation);
				var s = ToFormattedInteger(type, value);
				bool addParens = s.Length > 0 && s[0] == '-';
				if (addParens)
					OutputWrite("(", BoxedTextColor.Punctuation);
				WriteNumber(s);
				if (addParens)
					OutputWrite(")", BoxedTextColor.Punctuation);
			}
			else
				WriteNumber(ToFormattedInteger(type, value));
		}

		void WriteEnumSeperator() {
			WriteSpace();
			OutputWrite(EnumFlagsOrSeparator, BoxedTextColor.Operator);
			WriteSpace();
		}

		string ToFormattedInteger(DmdType type, ulong value) {
			switch (DmdType.GetTypeCode(type)) {
			case TypeCode.Boolean:	return ToFormattedByte((byte)value);
			case TypeCode.Char:		return ToFormattedChar((char)value);
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

			OutputWrite(ToFormattedChar(value), BoxedTextColor.Char);
		}

		string ToFormattedChar(char value) {
			var sb = ValueFormatterObjectCache.AllocStringBuilder();

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

			return ValueFormatterObjectCache.FreeAndToString(ref sb);
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
			var sb = ValueFormatterObjectCache.AllocStringBuilder();

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

			return ValueFormatterObjectCache.FreeAndToString(ref sb);
		}

		string GetFormattedVerbatimString(string value) {
			var sb = ValueFormatterObjectCache.AllocStringBuilder();

			sb.Append(VerbatimStringPrefix + "\"");
			foreach (var c in value) {
				if (c == '"')
					sb.Append("\"\"");
				else
					sb.Append(c);
			}
			sb.Append('"');

			return ValueFormatterObjectCache.FreeAndToString(ref sb);
		}

		string ToFormattedDecimalNumber(string number) => ToFormattedNumber(string.Empty, number, ValueFormatterUtils.DigitGroupSizeDecimal);
		string ToFormattedHexNumber(string number) => ToFormattedNumber(HexPrefix, number, ValueFormatterUtils.DigitGroupSizeHex);
		string ToFormattedNumber(string prefix, string number, int digitGroupSize) => ValueFormatterUtils.ToFormattedNumber(DigitSeparators, prefix, number, digitGroupSize);
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
	}
}

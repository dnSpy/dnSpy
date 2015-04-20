/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Globalization;
using System.Text;

namespace ICSharpCode.ILSpy.AsmEditor
{
	static class NumberVMUtils
	{
		public static byte[] ParseByteArray(string s)
		{
			s = s.Replace(" ", string.Empty);
			s = s.Replace("\t", string.Empty);
			s = s.Replace("\r", string.Empty);
			s = s.Replace("\n", string.Empty);
			if (s.Length % 2 != 0)
				throw new FormatException("A hex string must contain an even number of hex digits.");
			var bytes = new byte[s.Length / 2];
			for (int i = 0; i < s.Length; i += 2) {
				int upper = TryParseHexChar(s[i]);
				int lower = TryParseHexChar(s[i + 1]);
				if (upper < 0 || lower < 0)
					throw new FormatException("A hex string must contain only hex digits: 0-9 and A-F.");
				bytes[i / 2] = (byte)((upper << 4) | lower);
			}
			return bytes;
		}

		static int TryParseHexChar(char c)
		{
			if ('0' <= c && c <= '9')
				return (ushort)c - (ushort)'0';
			if ('a' <= c && c <= 'f')
				return 10 + (ushort)c - (ushort)'a';
			if ('A' <= c && c <= 'F')
				return 10 + (ushort)c - (ushort)'A';
			return -1;
		}

		public static string ByteArrayToString(byte[] value, bool upper = true)
		{
			if (value == null)
				return string.Empty;
			var chars = new char[value.Length * 2];
			for (int i = 0, j = 0; i < value.Length; i++) {
				byte b = value[i];
				chars[j++] = ToHexChar(b >> 4, upper);
				chars[j++] = ToHexChar(b & 0x0F, upper);
			}
			return new string(chars);
		}

		static char ToHexChar(int val, bool upper)
		{
			if (0 <= val && val <= 9)
				return (char)(val + (int)'0');
			return (char)(val - 10 + (upper ? (int)'A' : (int)'a'));
		}

		public static string ToString(ulong value, bool useDecimal)
		{
			if (value <= 9 || useDecimal)
				return value.ToString();
			return string.Format("0x{0:X}", value);
		}

		public static string ToString(long value, bool useDecimal)
		{
			if (-9 <= value && value <= 9 || useDecimal)
				return value.ToString();
			if (value < 0)
				return string.Format("-0x{0:X}", -value);
			return string.Format("0x{0:X}", value);
		}

		static string TryParseUnsigned(string s, ulong max, out ulong value)
		{
			const ulong min = 0;
			value = 0;
			bool isValid;
			s = s.Trim();
			if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
				var s2 = s.Substring(2);
				isValid = s2.Trim() == s2 && ulong.TryParse(s2, NumberStyles.HexNumber, null, out value);
			}
			else
				isValid = ulong.TryParse(s, NumberStyles.Integer, null, out value);
			if (!isValid) {
				if (s.StartsWith("-"))
					return "Only non-negative integers are allowed.";
				return "The value is not an unsigned hexadecimal or decimal integer.";
			}
			if (value < min || value > max)
				return string.Format("Value must be between {0} and {1} (0x{1:X}) inclusive.", min, max);

			return null;
		}

		static ulong ParseUnsigned(string s, ulong max)
		{
			ulong value;
			var err = TryParseUnsigned(s, max, out value);
			if (err != null)
				throw new FormatException(err);
			return value;
		}

		public static byte ParseByte(string s)
		{
			return (byte)ParseUnsigned(s, byte.MaxValue);
		}

		public static ushort ParseUInt16(string s)
		{
			return (ushort)ParseUnsigned(s, ushort.MaxValue);
		}

		public static uint ParseUInt32(string s)
		{
			return (uint)ParseUnsigned(s, uint.MaxValue);
		}

		public static ulong ParseUInt64(string s)
		{
			return ParseUnsigned(s, ulong.MaxValue);
		}

		static string TryParseSigned(string s, long min, long max, out long value)
		{
			value = 0;
			bool isValid;
			s = s.Trim();
			bool isSigned = s.StartsWith("-", StringComparison.OrdinalIgnoreCase);
			if (isSigned)
				s = s.Substring(1);
			ulong value2 = 0;
			if (s.Trim() != s)
				isValid = false;
			else if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
				var s2 = s.Substring(2);
				isValid = s2.Trim() == s2 && ulong.TryParse(s2, NumberStyles.HexNumber, null, out value2);
			}
			else
				isValid = ulong.TryParse(s, NumberStyles.Integer, null, out value2);
			if (!isValid)
				return "The value is not a hexadecimal or decimal integer.";
			if (isSigned) {
				if (value2 > (ulong)long.MaxValue + 1)
					return "The value is too small.";
				value = unchecked(-(long)value2);
			}
			else {
				if (value2 > (ulong)long.MaxValue)
					return "The value is too big.";
				value = (long)value2;
			}
			if (value < min || value > max)
				return string.Format("Value must be between {0} (0x{0:X}) and {1} (0x{1:X}) inclusive.", min, max);

			return null;
		}

		static long ParseSigned(string s, long min, long max)
		{
			long value;
			var err = TryParseSigned(s, min, max, out value);
			if (err != null)
				throw new FormatException(err);
			return value;
		}

		public static sbyte ParseSByte(string s)
		{
			return (sbyte)ParseSigned(s, sbyte.MinValue, sbyte.MaxValue);
		}

		public static short ParseInt16(string s)
		{
			return (short)ParseSigned(s, short.MinValue, short.MaxValue);
		}

		public static int ParseInt32(string s)
		{
			return (int)ParseSigned(s, int.MinValue, int.MaxValue);
		}

		public static long ParseInt64(string s)
		{
			return (long)ParseSigned(s, long.MinValue, long.MaxValue);
		}

		static string ToString<T>(T[] list, Func<T,string> toString)
		{
			var sb = new StringBuilder();
			for (int i = 0; i < list.Length; i++) {
				if (i != 0)
					sb.Append(", ");
				sb.Append(toString(list[i]));
			}
			return sb.ToString();
		}

		public static string ToString(byte[] values, bool useDecimal)
		{
			return ToString(values, v => ToString(v, useDecimal));
		}

		public static string ToString(ushort[] values, bool useDecimal)
		{
			return ToString(values, v => ToString(v, useDecimal));
		}

		public static string ToString(uint[] values, bool useDecimal)
		{
			return ToString(values, v => ToString(v, useDecimal));
		}

		public static string ToString(ulong[] values, bool useDecimal)
		{
			return ToString(values, v => ToString(v, useDecimal));
		}

		public static string ToString(sbyte[] values, bool useDecimal)
		{
			return ToString(values, v => ToString(v, useDecimal));
		}

		public static string ToString(short[] values, bool useDecimal)
		{
			return ToString(values, v => ToString(v, useDecimal));
		}

		public static string ToString(int[] values, bool useDecimal)
		{
			return ToString(values, v => ToString(v, useDecimal));
		}

		public static string ToString(long[] values, bool useDecimal)
		{
			return ToString(values, v => ToString(v, useDecimal));
		}

		static T[] ParseList<T>(string s, Func<string, T> parseValue)
		{
			var list = new List<T>();

			s = s.Trim();
			if (s == string.Empty)
				return list.ToArray();

			foreach (var elem in s.Split(',')) {
				var value = elem.Trim();
				if (value == string.Empty)
					throw new FormatException("Value in list can't be empty.");
				list.Add(parseValue(value));
			}

			return list.ToArray();
		}

		public static byte[] ParseByteList(string s)
		{
			return ParseList<byte>(s, v => ParseByte(v));
		}

		public static ushort[] ParseUInt16List(string s)
		{
			return ParseList<ushort>(s, v => ParseUInt16(v));
		}

		public static uint[] ParseUInt32List(string s)
		{
			return ParseList<uint>(s, v => ParseUInt32(v));
		}

		public static ulong[] ParseUInt64List(string s)
		{
			return ParseList<ulong>(s, v => ParseUInt64(v));
		}

		public static sbyte[] ParseSByteList(string s)
		{
			return ParseList<sbyte>(s, v => ParseSByte(v));
		}

		public static short[] ParseInt16List(string s)
		{
			return ParseList<short>(s, v => ParseInt16(v));
		}

		public static int[] ParseInt32List(string s)
		{
			return ParseList<int>(s, v => ParseInt32(v));
		}

		public static long[] ParseInt64List(string s)
		{
			return ParseList<long>(s, v => ParseInt64(v));
		}
	}

	struct CachedValidationError
	{
		readonly Func<string> checkError;
		bool errorMsgValid;
		string errorMsg;

		public bool HasError {
			get {
				CheckError();
				return !string.IsNullOrEmpty(errorMsg);
			}
		}

		public string ErrorMessage {
			get {
				CheckError();
				return errorMsg;
			}
		}

		public CachedValidationError(Func<string> checkError)
		{
			if (checkError == null)
				throw new ArgumentNullException();
			this.checkError = checkError;
			this.errorMsgValid = false;
			this.errorMsg = null;
		}

		public void Invalidate()
		{
			errorMsgValid = false;
		}

		void CheckError()
		{
			if (errorMsgValid)
				return;
			errorMsg = checkError();
			errorMsgValid = true;
		}
	}

	abstract class DataFieldVM<T> : ViewModelBase
	{
		readonly Action<DataFieldVM<T>> onUpdated;
		CachedValidationError cachedError;

		public T Value {
			get {
				T value;
				var s = ConvertToValue(out value);
				if (string.IsNullOrEmpty(s))
					return value;
				throw new FormatException(s);
			}
			set { SetValue(value); }
		}

		/// <summary>
		/// Gets the string representation of the value. This could be an invalid string. Use
		/// <see cref="Validate()"/> to check whether it's valid.
		/// </summary>
		public string StringValue {
			get { return stringValue; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				if (stringValue != value) {
					stringValue = value;
					cachedError.Invalidate();
					OnPropertyChanged("StringValue");
					onUpdated(this);
				}
			}
		}
		string stringValue = string.Empty;

		/// <summary>
		/// true if the value is null (<see cref="StringValue"/> is empty)
		/// </summary>
		public bool IsNull {
			get { return string.IsNullOrWhiteSpace(StringValue); }
		}

		protected DataFieldVM(Action<DataFieldVM<T>> onUpdated)
		{
			if (onUpdated == null)
				throw new ArgumentNullException();
			this.onUpdated = onUpdated;
			this.cachedError = new CachedValidationError(() => Validate());
		}

		protected abstract void SetValue(T value);
		protected abstract string ConvertToValue(out T value);

		string Validate()
		{
			T value;
			try {
				return ConvertToValue(out value);
			}
			catch (Exception ex) {
				if (!string.IsNullOrEmpty(ex.Message))
					return ex.Message;
				return string.Format("Could not convert '{0}'.", StringValue);
			}
		}

		protected override string Verify(string columnName)
		{
			if (columnName == "StringValue")
				return cachedError.ErrorMessage;

			return string.Empty;
		}

		public override bool HasError {
			get { return cachedError.HasError; }
		}
	}

	abstract class NumberDataFieldVM<T> : DataFieldVM<T>
	{
		/// <summary>
		/// Use decimal by default if it's a number
		/// </summary>
		public bool UseDecimal { get; set; }

		protected NumberDataFieldVM(Action<DataFieldVM<T>> onUpdated)
			: base(onUpdated)
		{
		}
	}

	sealed class NullableGuidVM : DataFieldVM<Guid?>
	{
		public NullableGuidVM(Action<DataFieldVM<Guid?>> onUpdated)
			: this(null, onUpdated)
		{
		}

		public NullableGuidVM(Guid? value, Action<DataFieldVM<Guid?>> onUpdated)
			: base(onUpdated)
		{
			SetValue(value);
		}

		protected override void SetValue(Guid? value)
		{
			this.StringValue = value == null ? string.Empty : value.Value.ToString();
		}

		protected override string ConvertToValue(out Guid? value)
		{
			if (IsNull)
				value = null;
			else
				value = Guid.Parse(StringValue);
			return null;
		}
	}

	sealed class HexStringVM : DataFieldVM<byte[]>
	{
		public bool UpperCaseHex { get; set; }

		public HexStringVM(Action<DataFieldVM<byte[]>> onUpdated)
			: this(null, onUpdated)
		{
		}

		public HexStringVM(byte[] value, Action<DataFieldVM<byte[]>> onUpdated)
			: base(onUpdated)
		{
			SetValue(value);
		}

		protected override void SetValue(byte[] value)
		{
			this.StringValue = NumberVMUtils.ByteArrayToString(value, UpperCaseHex);
		}

		protected override string ConvertToValue(out byte[] value)
		{
			value = NumberVMUtils.ParseByteArray(StringValue);
			return null;
		}
	}

	sealed class NullableByteVM : NumberDataFieldVM<byte?>
	{
		public NullableByteVM(Action<DataFieldVM<byte?>> onUpdated)
			: this(null, onUpdated)
		{
		}

		public NullableByteVM(byte? value, Action<DataFieldVM<byte?>> onUpdated)
			: base(onUpdated)
		{
			SetValue(value);
		}

		protected override void SetValue(byte? value)
		{
			this.StringValue = value == null ? string.Empty : NumberVMUtils.ToString(value.Value, UseDecimal);
		}

		protected override string ConvertToValue(out byte? value)
		{
			if (IsNull)
				value = null;
			else
				value = NumberVMUtils.ParseByte(StringValue);
			return null;
		}
	}

	sealed class NullableUInt16VM : NumberDataFieldVM<ushort?>
	{
		public NullableUInt16VM(Action<DataFieldVM<ushort?>> onUpdated)
			: this(null, onUpdated)
		{
		}

		public NullableUInt16VM(ushort? value, Action<DataFieldVM<ushort?>> onUpdated)
			: base(onUpdated)
		{
			SetValue(value);
		}

		protected override void SetValue(ushort? value)
		{
			this.StringValue = value == null ? string.Empty : NumberVMUtils.ToString(value.Value, UseDecimal);
		}

		protected override string ConvertToValue(out ushort? value)
		{
			if (IsNull)
				value = null;
			else
				value = NumberVMUtils.ParseUInt16(StringValue);
			return null;
		}
	}

	sealed class NullableUInt32VM : NumberDataFieldVM<uint?>
	{
		public NullableUInt32VM(Action<DataFieldVM<uint?>> onUpdated)
			: this(null, onUpdated)
		{
		}

		public NullableUInt32VM(uint? value, Action<DataFieldVM<uint?>> onUpdated)
			: base(onUpdated)
		{
			SetValue(value);
		}

		protected override void SetValue(uint? value)
		{
			this.StringValue = value == null ? string.Empty : NumberVMUtils.ToString(value.Value, UseDecimal);
		}

		protected override string ConvertToValue(out uint? value)
		{
			if (IsNull)
				value = null;
			else
				value = NumberVMUtils.ParseUInt32(StringValue);
			return null;
		}
	}

	sealed class NullableUInt64VM : NumberDataFieldVM<ulong?>
	{
		public NullableUInt64VM(Action<DataFieldVM<ulong?>> onUpdated)
			: this(null, onUpdated)
		{
		}

		public NullableUInt64VM(ulong? value, Action<DataFieldVM<ulong?>> onUpdated)
			: base(onUpdated)
		{
			SetValue(value);
		}

		protected override void SetValue(ulong? value)
		{
			this.StringValue = value == null ? string.Empty : NumberVMUtils.ToString(value.Value, UseDecimal);
		}

		protected override string ConvertToValue(out ulong? value)
		{
			if (IsNull)
				value = null;
			else
				value = NumberVMUtils.ParseUInt64(StringValue);
			return null;
		}
	}

	sealed class UInt16VM : NumberDataFieldVM<ushort>
	{
		public UInt16VM(Action<DataFieldVM<ushort>> onUpdated)
			: this(0, onUpdated)
		{
		}

		public UInt16VM(ushort value, Action<DataFieldVM<ushort>> onUpdated)
			: base(onUpdated)
		{
			SetValue(value);
		}

		protected override void SetValue(ushort value)
		{
			this.StringValue = NumberVMUtils.ToString(value, UseDecimal);
		}

		protected override string ConvertToValue(out ushort value)
		{
			value = NumberVMUtils.ParseUInt16(StringValue);
			return null;
		}
	}

	sealed class UInt32VM : NumberDataFieldVM<uint>
	{
		public UInt32VM(Action<DataFieldVM<uint>> onUpdated)
			: this(0, onUpdated)
		{
		}

		public UInt32VM(uint value, Action<DataFieldVM<uint>> onUpdated)
			: base(onUpdated)
		{
			SetValue(value);
		}

		protected override void SetValue(uint value)
		{
			this.StringValue = NumberVMUtils.ToString(value, UseDecimal);
		}

		protected override string ConvertToValue(out uint value)
		{
			value = NumberVMUtils.ParseUInt32(StringValue);
			return null;
		}
	}

	sealed class GuidVM : DataFieldVM<Guid>
	{
		public GuidVM(Action<DataFieldVM<Guid>> onUpdated)
			: this(new Guid(), onUpdated)
		{
		}

		public GuidVM(Guid value, Action<DataFieldVM<Guid>> onUpdated)
			: base(onUpdated)
		{
			SetValue(value);
		}

		protected override void SetValue(Guid value)
		{
			this.StringValue = value.ToString();
		}

		protected override string ConvertToValue(out Guid value)
		{
			value = Guid.Parse(StringValue);
			return null;
		}
	}

	sealed class UInt32ListDataFieldVM : NumberDataFieldVM<uint[]>
	{
		public UInt32ListDataFieldVM(Action<DataFieldVM<uint[]>> onUpdated)
			: this(new uint[0], onUpdated)
		{
		}

		public UInt32ListDataFieldVM(uint[] value, Action<DataFieldVM<uint[]>> onUpdated)
			: base(onUpdated)
		{
			SetValue(value);
		}

		protected override void SetValue(uint[] value)
		{
			this.StringValue = NumberVMUtils.ToString(value, UseDecimal);
		}

		protected override string ConvertToValue(out uint[] value)
		{
			value = NumberVMUtils.ParseUInt32List(StringValue);
			return null;
		}
	}

	sealed class Int32ListDataFieldVM : NumberDataFieldVM<int[]>
	{
		public Int32ListDataFieldVM(Action<DataFieldVM<int[]>> onUpdated)
			: this(new int[0], onUpdated)
		{
		}

		public Int32ListDataFieldVM(int[] value, Action<DataFieldVM<int[]>> onUpdated)
			: base(onUpdated)
		{
			SetValue(value);
		}

		protected override void SetValue(int[] value)
		{
			this.StringValue = NumberVMUtils.ToString(value, UseDecimal);
		}

		protected override string ConvertToValue(out int[] value)
		{
			value = NumberVMUtils.ParseInt32List(StringValue);
			return null;
		}
	}
}

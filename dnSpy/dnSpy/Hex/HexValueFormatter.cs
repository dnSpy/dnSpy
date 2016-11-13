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

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using dnSpy.Contracts.Hex;

namespace dnSpy.Hex {
	enum HexValueFormatterFlags {
		None					= 0,
		LowerCaseHex			= 0x00000001,
	}

	abstract class HexValueFormatter {
		protected static CultureInfo culture = CultureInfo.InvariantCulture;

		// These strings must not be longer than 7 chars (max length of floats)
		const string stringNaN = "NaN";
		const string stringPositiveInfinity = "Inf";
		const string stringNegativeInfinity = "-Inf";

		public HexBytesDisplayFormat Format { get; }
		public int ByteCount { get; }
		public int MaxFormattedLength { get; }

		protected HexValueFormatter(int byteCount, int maxFormattedLength, HexBytesDisplayFormat format) {
			Format = format;
			ByteCount = byteCount;
			MaxFormattedLength = maxFormattedLength;
		}

		/// <summary>
		/// Formats the value and returns the number of spaces that were inserted before the number
		/// so exactly <see cref="MaxFormattedLength"/> characters were written to <paramref name="dest"/>
		/// </summary>
		/// <param name="dest">Destination string builder</param>
		/// <param name="hexBytes">Bytes</param>
		/// <param name="valueIndex">Index of value in <paramref name="hexBytes"/></param>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public abstract int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags);

		protected int WriteInvalid(StringBuilder dest) {
			dest.Append('?', MaxFormattedLength);
			return 0;
		}

		protected void WriteHexByte(StringBuilder dest, HexValueFormatterFlags flags, byte b) {
			bool lower = (flags & HexValueFormatterFlags.LowerCaseHex) != 0;
			WriteHexNibble(dest, (b >> 4) & 0x0F, lower);
			WriteHexNibble(dest, b & 0x0F, lower);
		}

		protected void WriteHexUInt16(StringBuilder dest, HexValueFormatterFlags flags, ushort v) {
			bool lower = (flags & HexValueFormatterFlags.LowerCaseHex) != 0;
			WriteHexNibble(dest, (v >> 12) & 0x0F, lower);
			WriteHexNibble(dest, (v >> 8) & 0x0F, lower);
			WriteHexNibble(dest, (v >> 4) & 0x0F, lower);
			WriteHexNibble(dest, v & 0x0F, lower);
		}

		protected void WriteHexUInt32(StringBuilder dest, HexValueFormatterFlags flags, uint v) {
			bool lower = (flags & HexValueFormatterFlags.LowerCaseHex) != 0;
			WriteHexNibble(dest, (v >> 28) & 0x0F, lower);
			WriteHexNibble(dest, (v >> 24) & 0x0F, lower);
			WriteHexNibble(dest, (v >> 20) & 0x0F, lower);
			WriteHexNibble(dest, (v >> 16) & 0x0F, lower);
			WriteHexNibble(dest, (v >> 12) & 0x0F, lower);
			WriteHexNibble(dest, (v >> 8) & 0x0F, lower);
			WriteHexNibble(dest, (v >> 4) & 0x0F, lower);
			WriteHexNibble(dest, v & 0x0F, lower);
		}

		protected void WriteHexUInt64(StringBuilder dest, HexValueFormatterFlags flags, ulong v) {
			WriteHexUInt32(dest, flags, (uint)(v >> 32));
			WriteHexUInt32(dest, flags, (uint)v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void WriteHexNibble(StringBuilder dest, int nibble, bool lower) {
			Debug.Assert(0 <= nibble && nibble <= 0x0F);
			if (nibble < 10)
				dest.Append((char)('0' + nibble));
			else
				dest.Append((char)((lower ? 'a' : 'A') + nibble - 10));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void WriteHexNibble(StringBuilder dest, uint nibble, bool lower) {
			Debug.Assert(0 <= nibble && nibble <= 0x0F);
			if (nibble < 10)
				dest.Append((char)('0' + nibble));
			else
				dest.Append((char)((lower ? 'a' : 'A') + nibble - 10));
		}

		protected int WriteFormattedValue(StringBuilder dest, string formattedValue) {
			if (formattedValue.Length > MaxFormattedLength) {
				dest.Append(formattedValue, 0, MaxFormattedLength - 1);
				dest.Append('?');
				return 0;
			}
			int spaces = MaxFormattedLength - formattedValue.Length;
			if (spaces > 0)
				dest.Append(' ', spaces);
			dest.Append(formattedValue);
			return spaces;
		}

		protected int FormatHexUInt16(StringBuilder dest, HexValueFormatterFlags flags, ushort? v) {
			Debug.Assert(MaxFormattedLength == 2);
			if (v == null)
				return WriteInvalid(dest);
			WriteHexUInt16(dest, flags, v.Value);
			return 0;
		}

		protected int FormatHexUInt32(StringBuilder dest, HexValueFormatterFlags flags, uint? v) {
			Debug.Assert(MaxFormattedLength == 4);
			if (v == null)
				return WriteInvalid(dest);
			WriteHexUInt32(dest, flags, v.Value);
			return 0;
		}

		protected int FormatHexUInt64(StringBuilder dest, HexValueFormatterFlags flags, ulong? v) {
			Debug.Assert(MaxFormattedLength == 8);
			if (v == null)
				return WriteInvalid(dest);
			WriteHexUInt64(dest, flags, v.Value);
			return 0;
		}

		protected int FormatHexSByte(StringBuilder dest, HexValueFormatterFlags flags, sbyte? v) {
			Debug.Assert(MaxFormattedLength == 2);
			if (v == null)
				return WriteInvalid(dest);
			var value = v.Value;
			int spaces;
			if (value < 0) {
				dest.Append('-');
				value = (sbyte)-value;
				spaces = 0;
			}
			else {
				dest.Append(' ');
				spaces = 1;
			}
			WriteHexByte(dest, flags, (byte)value);
			return spaces;
		}

		protected int FormatHexInt16(StringBuilder dest, HexValueFormatterFlags flags, short? v) {
			Debug.Assert(MaxFormattedLength == 5);
			if (v == null)
				return WriteInvalid(dest);
			var value = v.Value;
			int spaces;
			if (value < 0) {
				dest.Append('-');
				value = (short)-value;
				spaces = 0;
			}
			else {
				dest.Append(' ');
				spaces = 1;
			}
			WriteHexUInt16(dest, flags, (ushort)value);
			return spaces;
		}

		protected int FormatHexInt32(StringBuilder dest, HexValueFormatterFlags flags, int? v) {
			Debug.Assert(MaxFormattedLength == 9);
			if (v == null)
				return WriteInvalid(dest);
			var value = v.Value;
			int spaces;
			if (value < 0) {
				dest.Append('-');
				value = -value;
				spaces = 0;
			}
			else {
				dest.Append(' ');
				spaces = 1;
			}
			WriteHexUInt32(dest, flags, (uint)value);
			return spaces;
		}

		protected int FormatHexInt64(StringBuilder dest, HexValueFormatterFlags flags, long? v) {
			Debug.Assert(MaxFormattedLength == 17);
			if (v == null)
				return WriteInvalid(dest);
			var value = v.Value;
			int spaces;
			if (value < 0) {
				dest.Append('-');
				value = -value;
				spaces = 0;
			}
			else {
				dest.Append(' ');
				spaces = 1;
			}
			WriteHexUInt64(dest, flags, (ulong)value);
			return spaces;
		}

		protected int FormatDecimalUInt16(StringBuilder dest, HexValueFormatterFlags flags, ushort? v) {
			if (v == null)
				return WriteInvalid(dest);
			return WriteFormattedValue(dest, v.Value.ToString(culture));
		}

		protected int FormatDecimalUInt32(StringBuilder dest, HexValueFormatterFlags flags, uint? v) {
			if (v == null)
				return WriteInvalid(dest);
			return WriteFormattedValue(dest, v.Value.ToString(culture));
		}

		protected int FormatDecimalUInt64(StringBuilder dest, HexValueFormatterFlags flags, ulong? v) {
			if (v == null)
				return WriteInvalid(dest);
			return WriteFormattedValue(dest, v.Value.ToString(culture));
		}

		protected int FormatDecimalSByte(StringBuilder dest, HexValueFormatterFlags flags, sbyte? v) {
			if (v == null)
				return WriteInvalid(dest);
			return WriteFormattedValue(dest, v.Value.ToString(culture));
		}

		protected int FormatDecimalInt16(StringBuilder dest, HexValueFormatterFlags flags, short? v) {
			if (v == null)
				return WriteInvalid(dest);
			return WriteFormattedValue(dest, v.Value.ToString(culture));
		}

		protected int FormatDecimalInt32(StringBuilder dest, HexValueFormatterFlags flags, int? v) {
			if (v == null)
				return WriteInvalid(dest);
			return WriteFormattedValue(dest, v.Value.ToString(culture));
		}

		protected int FormatDecimalInt64(StringBuilder dest, HexValueFormatterFlags flags, long? v) {
			if (v == null)
				return WriteInvalid(dest);
			return WriteFormattedValue(dest, v.Value.ToString(culture));
		}

		protected int FormatSingle(StringBuilder dest, HexValueFormatterFlags flags, float? v) {
			if (v == null)
				return WriteInvalid(dest);
			var value = v.Value;
			if (float.IsNaN(value))
				return WriteFormattedValue(dest, stringNaN);
			if (float.IsPositiveInfinity(value))
				return WriteFormattedValue(dest, stringPositiveInfinity);
			if (float.IsNegativeInfinity(value))
				return WriteFormattedValue(dest, stringNegativeInfinity);
			return WriteFormattedValue(dest, value.ToString("G7", culture));
		}

		protected int FormatDouble(StringBuilder dest, HexValueFormatterFlags flags, double? v) {
			if (v == null)
				return WriteInvalid(dest);
			var value = v.Value;
			if (double.IsNaN(value))
				return WriteFormattedValue(dest, stringNaN);
			if (double.IsPositiveInfinity(value))
				return WriteFormattedValue(dest, stringPositiveInfinity);
			if (double.IsNegativeInfinity(value))
				return WriteFormattedValue(dest, stringNegativeInfinity);
			return WriteFormattedValue(dest, value.ToString("G15", culture));
		}
	}

	sealed class HexByteValueFormatter : HexValueFormatter {
		public HexByteValueFormatter()
			: base(1, "FF".Length, HexBytesDisplayFormat.HexByte) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) {
			int b = hexBytes.TryReadByte(valueIndex);
			if (b < 0)
				return WriteInvalid(dest);
			WriteHexByte(dest, flags, (byte)b);
			return 0;
		}
	}

	sealed class HexUInt16ValueFormatter : HexValueFormatter {
		public HexUInt16ValueFormatter()
			: base(2, "FFFF".Length, HexBytesDisplayFormat.HexUInt16) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexUInt16(dest, flags, hexBytes.TryReadUInt16(valueIndex));
	}

	sealed class HexUInt32ValueFormatter : HexValueFormatter {
		public HexUInt32ValueFormatter()
			: base(4, "FFFFFFFF".Length, HexBytesDisplayFormat.HexUInt32) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexUInt32(dest, flags, hexBytes.TryReadUInt32(valueIndex));
	}

	sealed class HexUInt64ValueFormatter : HexValueFormatter {
		public HexUInt64ValueFormatter()
			: base(8, "FFFFFFFFFFFFFFFF".Length, HexBytesDisplayFormat.HexUInt64) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexUInt64(dest, flags, hexBytes.TryReadUInt64(valueIndex));
	}

	sealed class HexSByteValueFormatter : HexValueFormatter {
		public HexSByteValueFormatter()
			: base(1, "-80".Length, HexBytesDisplayFormat.HexSByte) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexSByte(dest, flags, hexBytes.TryReadSByte(valueIndex));
	}

	sealed class HexInt16ValueFormatter : HexValueFormatter {
		public HexInt16ValueFormatter()
			: base(2, "-8000".Length, HexBytesDisplayFormat.HexInt16) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexInt16(dest, flags, hexBytes.TryReadInt16(valueIndex));
	}

	sealed class HexInt32ValueFormatter : HexValueFormatter {
		public HexInt32ValueFormatter()
			: base(4, "-80000000".Length, HexBytesDisplayFormat.HexInt32) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexInt32(dest, flags, hexBytes.TryReadInt32(valueIndex));
	}

	sealed class HexInt64ValueFormatter : HexValueFormatter {
		public HexInt64ValueFormatter()
			: base(8, "-8000000000000000".Length, HexBytesDisplayFormat.HexInt64) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexInt64(dest, flags, hexBytes.TryReadInt64(valueIndex));
	}

	sealed class DecimalByteValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = byte.MaxValue.ToString(culture).Length;
		public DecimalByteValueFormatter()
			: base(1, MAX_LENGTH, HexBytesDisplayFormat.DecimalByte) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) {
			int b = hexBytes.TryReadByte(valueIndex);
			if (b < 0)
				return WriteInvalid(dest);
			return WriteFormattedValue(dest, b.ToString(culture));
		}
	}

	sealed class DecimalUInt16ValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = ushort.MaxValue.ToString(culture).Length;
		public DecimalUInt16ValueFormatter()
			: base(2, MAX_LENGTH, HexBytesDisplayFormat.DecimalUInt16) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalUInt16(dest, flags, hexBytes.TryReadUInt16(valueIndex));
	}

	sealed class DecimalUInt32ValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = uint.MaxValue.ToString(culture).Length;
		public DecimalUInt32ValueFormatter()
			: base(4, MAX_LENGTH, HexBytesDisplayFormat.DecimalUInt32) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalUInt32(dest, flags, hexBytes.TryReadUInt32(valueIndex));
	}

	sealed class DecimalUInt64ValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = ulong.MaxValue.ToString(culture).Length;
		public DecimalUInt64ValueFormatter()
			: base(8, MAX_LENGTH, HexBytesDisplayFormat.DecimalUInt64) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalUInt64(dest, flags, hexBytes.TryReadUInt64(valueIndex));
	}

	sealed class DecimalSByteValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = sbyte.MinValue.ToString(culture).Length;
		public DecimalSByteValueFormatter()
			: base(1, MAX_LENGTH, HexBytesDisplayFormat.DecimalSByte) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalSByte(dest, flags, hexBytes.TryReadSByte(valueIndex));
	}

	sealed class DecimalInt16ValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = short.MinValue.ToString(culture).Length;
		public DecimalInt16ValueFormatter()
			: base(2, MAX_LENGTH, HexBytesDisplayFormat.DecimalInt16) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalInt16(dest, flags, hexBytes.TryReadInt16(valueIndex));
	}

	sealed class DecimalInt32ValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = int.MinValue.ToString(culture).Length;
		public DecimalInt32ValueFormatter()
			: base(4, MAX_LENGTH, HexBytesDisplayFormat.DecimalInt32) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalInt32(dest, flags, hexBytes.TryReadInt32(valueIndex));
	}

	sealed class DecimalInt64ValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = long.MinValue.ToString(culture).Length;
		public DecimalInt64ValueFormatter()
			: base(8, MAX_LENGTH, HexBytesDisplayFormat.DecimalInt64) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalInt64(dest, flags, hexBytes.TryReadInt64(valueIndex));
	}

	sealed class SingleValueFormatter : HexValueFormatter {
		public SingleValueFormatter()
			: base(4, 7, HexBytesDisplayFormat.Single) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatSingle(dest, flags, hexBytes.TryReadSingle(valueIndex));
	}

	sealed class DoubleValueFormatter : HexValueFormatter {
		public DoubleValueFormatter()
			: base(8, 15, HexBytesDisplayFormat.Double) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDouble(dest, flags, hexBytes.TryReadDouble(valueIndex));
	}

	sealed class Bit8ValueFormatter : HexValueFormatter {
		public Bit8ValueFormatter()
			: base(1, "11111111".Length, HexBytesDisplayFormat.Bit8) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) {
			int b = hexBytes.TryReadByte(valueIndex);
			if (b < 0)
				return WriteInvalid(dest);
			for (int i = 0; i < 8; i++, b <<= 1)
				dest.Append((b & 0x80) != 0 ? '1' : '0');
			return 0;
		}
	}

	sealed class HexUInt16BigEndianValueFormatter : HexValueFormatter {
		public HexUInt16BigEndianValueFormatter()
			: base(2, "FFFF".Length, HexBytesDisplayFormat.HexUInt16BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexUInt16(dest, flags, hexBytes.TryReadUInt16BigEndian(valueIndex));
	}

	sealed class HexUInt32BigEndianValueFormatter : HexValueFormatter {
		public HexUInt32BigEndianValueFormatter()
			: base(4, "FFFFFFFF".Length, HexBytesDisplayFormat.HexUInt32BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexUInt32(dest, flags, hexBytes.TryReadUInt32BigEndian(valueIndex));
	}

	sealed class HexUInt64BigEndianValueFormatter : HexValueFormatter {
		public HexUInt64BigEndianValueFormatter()
			: base(8, "FFFFFFFFFFFFFFFF".Length, HexBytesDisplayFormat.HexUInt64BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexUInt64(dest, flags, hexBytes.TryReadUInt64BigEndian(valueIndex));
	}

	sealed class HexInt16BigEndianValueFormatter : HexValueFormatter {
		public HexInt16BigEndianValueFormatter()
			: base(2, "-8000".Length, HexBytesDisplayFormat.HexInt16BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexInt16(dest, flags, hexBytes.TryReadInt16BigEndian(valueIndex));
	}

	sealed class HexInt32BigEndianValueFormatter : HexValueFormatter {
		public HexInt32BigEndianValueFormatter()
			: base(4, "-80000000".Length, HexBytesDisplayFormat.HexInt32BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexInt32(dest, flags, hexBytes.TryReadInt32BigEndian(valueIndex));
	}

	sealed class HexInt64BigEndianValueFormatter : HexValueFormatter {
		public HexInt64BigEndianValueFormatter()
			: base(8, "-8000000000000000".Length, HexBytesDisplayFormat.HexInt64BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexInt64(dest, flags, hexBytes.TryReadInt64BigEndian(valueIndex));
	}

	sealed class DecimalUInt16BigEndianValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = ushort.MaxValue.ToString(culture).Length;
		public DecimalUInt16BigEndianValueFormatter()
			: base(2, MAX_LENGTH, HexBytesDisplayFormat.DecimalUInt16BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalUInt16(dest, flags, hexBytes.TryReadUInt16BigEndian(valueIndex));
	}

	sealed class DecimalUInt32BigEndianValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = uint.MaxValue.ToString(culture).Length;
		public DecimalUInt32BigEndianValueFormatter()
			: base(4, MAX_LENGTH, HexBytesDisplayFormat.DecimalUInt32BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalUInt32(dest, flags, hexBytes.TryReadUInt32BigEndian(valueIndex));
	}

	sealed class DecimalUInt64BigEndianValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = ulong.MaxValue.ToString(culture).Length;
		public DecimalUInt64BigEndianValueFormatter()
			: base(8, MAX_LENGTH, HexBytesDisplayFormat.DecimalUInt64BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalUInt64(dest, flags, hexBytes.TryReadUInt64BigEndian(valueIndex));
	}

	sealed class DecimalInt16BigEndianValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = short.MinValue.ToString(culture).Length;
		public DecimalInt16BigEndianValueFormatter()
			: base(2, MAX_LENGTH, HexBytesDisplayFormat.DecimalInt16BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalInt16(dest, flags, hexBytes.TryReadInt16BigEndian(valueIndex));
	}

	sealed class DecimalInt32BigEndianValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = int.MinValue.ToString(culture).Length;
		public DecimalInt32BigEndianValueFormatter()
			: base(4, MAX_LENGTH, HexBytesDisplayFormat.DecimalInt32BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalInt32(dest, flags, hexBytes.TryReadInt32BigEndian(valueIndex));
	}

	sealed class DecimalInt64BigEndianValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = long.MinValue.ToString(culture).Length;
		public DecimalInt64BigEndianValueFormatter()
			: base(8, MAX_LENGTH, HexBytesDisplayFormat.DecimalInt64BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalInt64(dest, flags, hexBytes.TryReadInt64BigEndian(valueIndex));
	}

	sealed class SingleBigEndianValueFormatter : HexValueFormatter {
		public SingleBigEndianValueFormatter()
			: base(4, 7, HexBytesDisplayFormat.SingleBigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatSingle(dest, flags, hexBytes.TryReadSingleBigEndian(valueIndex));
	}

	sealed class DoubleBigEndianValueFormatter : HexValueFormatter {
		public DoubleBigEndianValueFormatter()
			: base(8, 15, HexBytesDisplayFormat.DoubleBigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDouble(dest, flags, hexBytes.TryReadDoubleBigEndian(valueIndex));
	}
}

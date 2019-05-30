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

		// These strings must not be longer than MaxSingleFormattedLength chars
		const string stringNaN = "NaN";
		const string stringPositiveInfinity = "Inf";
		const string stringNegativeInfinity = "-Inf";

		// sign, precision, decimal separator, E+NN
		protected const int MaxSingleFormattedLength = 1 + 7 + 1 + 4;
		protected const int MaxDoubleFormattedLength = 1 + 15 + 1 + 5;
		const string SingleFormatString = "G7";
		const string DoubleFormatString = "G15";

		public HexValuesDisplayFormat Format { get; }
		public int ByteCount { get; }
		public int FormattedLength { get; }

		readonly StringBuilder stringBuilder;

		protected HexValueFormatter(int byteCount, int formattedLength, HexValuesDisplayFormat format) {
			Format = format;
			ByteCount = byteCount;
			FormattedLength = formattedLength;
			stringBuilder = new StringBuilder(FormattedLength);
		}

		/// <summary>
		/// Formats the value and returns the number of spaces that were inserted before the number
		/// so exactly <see cref="FormattedLength"/> characters were written to <paramref name="dest"/>
		/// </summary>
		/// <param name="dest">Destination string builder</param>
		/// <param name="hexBytes">Bytes</param>
		/// <param name="valueIndex">Index of value in <paramref name="hexBytes"/></param>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public abstract int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags);

		protected int WriteInvalid(StringBuilder dest) {
			dest.Append('?', FormattedLength);
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
			if (formattedValue.Length > FormattedLength) {
				Debug.Fail($"Formatted value is too long, truncating it: {formattedValue}");
				dest.Append(formattedValue, 0, FormattedLength - 1);
				dest.Append('?');
				return 0;
			}
			int spaces = FormattedLength - formattedValue.Length;
			if (spaces > 0)
				dest.Append(' ', spaces);
			dest.Append(formattedValue);
			return spaces;
		}

		protected int FormatHexUInt16(StringBuilder dest, HexValueFormatterFlags flags, ushort? v) {
			Debug.Assert(FormattedLength == 4);
			if (v is null)
				return WriteInvalid(dest);
			WriteHexUInt16(dest, flags, v.Value);
			return 0;
		}

		protected int FormatHexUInt32(StringBuilder dest, HexValueFormatterFlags flags, uint? v) {
			Debug.Assert(FormattedLength == 8);
			if (v is null)
				return WriteInvalid(dest);
			WriteHexUInt32(dest, flags, v.Value);
			return 0;
		}

		protected int FormatHexUInt64(StringBuilder dest, HexValueFormatterFlags flags, ulong? v) {
			Debug.Assert(FormattedLength == 16);
			if (v is null)
				return WriteInvalid(dest);
			WriteHexUInt64(dest, flags, v.Value);
			return 0;
		}

		protected int FormatHexSByte(StringBuilder dest, HexValueFormatterFlags flags, sbyte? v) {
			Debug.Assert(FormattedLength == 3);
			if (v is null)
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
			Debug.Assert(FormattedLength == 5);
			if (v is null)
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
			Debug.Assert(FormattedLength == 9);
			if (v is null)
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
			Debug.Assert(FormattedLength == 17);
			if (v is null)
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
			if (v is null)
				return WriteInvalid(dest);
			return WriteFormattedValue(dest, v.Value.ToString(culture));
		}

		protected int FormatDecimalUInt32(StringBuilder dest, HexValueFormatterFlags flags, uint? v) {
			if (v is null)
				return WriteInvalid(dest);
			return WriteFormattedValue(dest, v.Value.ToString(culture));
		}

		protected int FormatDecimalUInt64(StringBuilder dest, HexValueFormatterFlags flags, ulong? v) {
			if (v is null)
				return WriteInvalid(dest);
			return WriteFormattedValue(dest, v.Value.ToString(culture));
		}

		protected int FormatDecimalSByte(StringBuilder dest, HexValueFormatterFlags flags, sbyte? v) {
			if (v is null)
				return WriteInvalid(dest);
			return WriteFormattedValue(dest, v.Value.ToString(culture));
		}

		protected int FormatDecimalInt16(StringBuilder dest, HexValueFormatterFlags flags, short? v) {
			if (v is null)
				return WriteInvalid(dest);
			return WriteFormattedValue(dest, v.Value.ToString(culture));
		}

		protected int FormatDecimalInt32(StringBuilder dest, HexValueFormatterFlags flags, int? v) {
			if (v is null)
				return WriteInvalid(dest);
			return WriteFormattedValue(dest, v.Value.ToString(culture));
		}

		protected int FormatDecimalInt64(StringBuilder dest, HexValueFormatterFlags flags, long? v) {
			if (v is null)
				return WriteInvalid(dest);
			return WriteFormattedValue(dest, v.Value.ToString(culture));
		}

		protected int FormatSingle(StringBuilder dest, HexValueFormatterFlags flags, float? v) {
			if (v is null)
				return WriteInvalid(dest);
			var value = v.Value;
			if (float.IsNaN(value))
				return WriteFormattedValue(dest, stringNaN);
			if (float.IsPositiveInfinity(value))
				return WriteFormattedValue(dest, stringPositiveInfinity);
			if (float.IsNegativeInfinity(value))
				return WriteFormattedValue(dest, stringNegativeInfinity);
			return WriteFormattedValue(dest, value.ToString(SingleFormatString, culture));
		}

		protected int FormatDouble(StringBuilder dest, HexValueFormatterFlags flags, double? v) {
			if (v is null)
				return WriteInvalid(dest);
			var value = v.Value;
			if (double.IsNaN(value))
				return WriteFormattedValue(dest, stringNaN);
			if (double.IsPositiveInfinity(value))
				return WriteFormattedValue(dest, stringPositiveInfinity);
			if (double.IsNegativeInfinity(value))
				return WriteFormattedValue(dest, stringNegativeInfinity);
			return WriteFormattedValue(dest, value.ToString(DoubleFormatString, culture));
		}

		public virtual HexBufferSpan GetBufferSpan(HexBufferSpan bufferSpan, int cellPosition) => bufferSpan;

		public PositionAndData? Edit(HexBufferPoint position, int cellPosition, char c) {
			if (position.IsDefault)
				return null;
			if ((uint)cellPosition >= (uint)FormattedLength)
				return null;
			return EditCore(position, cellPosition, c);
		}

		public virtual bool CanEdit => false;
		protected virtual PositionAndData? EditCore(HexBufferPoint position, int cellPosition, char c) => null;

		protected PositionAndData? EditUnsignedHexLittleEndian(HexBufferPoint position, int cellPosition, char c) {
			int v = ConvertFromHexCharacter(c);
			if (v < 0)
				return null;

			int bytePos = (ByteCount - cellPosition / 2 - 1) % ByteCount;
			var dataPos = position.Position + bytePos;
			if (dataPos >= HexPosition.MaxEndPosition)
				return null;
			var newData = new byte[1];
			newData[0] = position.Buffer.ReadByte(dataPos);
			if ((cellPosition & 1) == 0)
				newData[0] = (byte)((newData[0] & 0x0F) | (v << 4));
			else
				newData[0] = (byte)((newData[0] & 0xF0) | v);

			return new PositionAndData(new HexBufferPoint(position.Buffer, dataPos), newData);
		}

		protected PositionAndData? EditUnsignedHexBigEndian(HexBufferPoint position, int cellPosition, char c) {
			int v = ConvertFromHexCharacter(c);
			if (v < 0)
				return null;

			var dataPos = position.Position + cellPosition / 2;
			if (dataPos >= HexPosition.MaxEndPosition)
				return null;
			var newData = new byte[1];
			newData[0] = position.Buffer.ReadByte(dataPos);
			if ((cellPosition & 1) == 0)
				newData[0] = (byte)((newData[0] & 0x0F) | (v << 4));
			else
				newData[0] = (byte)((newData[0] & 0xF0) | v);

			return new PositionAndData(new HexBufferPoint(position.Buffer, dataPos), newData);
		}

		// Assumes input is valid. First char is <space> <+> or <->
		static long ToSignedHex(StringBuilder sb, long minValue, long maxValue) {
			Debug.Assert((sb.Length & 1) == 1 && sb.Length <= 16 + 1);
			int i = 0;
			Debug.Assert(sb[i] == ' ' || sb[i] == '+' || sb[i] == '-');
			bool isNegative = sb[i] == '-';
			i++;
			ulong v = 0;
			while (i < sb.Length) {
				var hi = ConvertFromHexCharacter(sb[i++]);
				var lo = ConvertFromHexCharacter(sb[i++]);
				Debug.Assert(hi >= 0 && lo >= 0);
				v = (v << 8) | ((ulong)(uint)hi << 4) | (uint)lo;
			}
			if (isNegative) {
				if (v > (ulong)maxValue + 1)
					return minValue;
				return -(long)v;
			}
			if (v > (ulong)maxValue)
				return maxValue;
			return (long)v;
		}

		protected PositionAndData? EditSignedHexLittleEndian(HexBufferPoint position, int cellPosition, char c) =>
			EditSignedHex(position, cellPosition, c, isBigEndian: false);

		protected PositionAndData? EditSignedHexBigEndian(HexBufferPoint position, int cellPosition, char c) =>
			EditSignedHex(position, cellPosition, c, isBigEndian: true);

		protected PositionAndData? EditSignedHex(HexBufferPoint position, int cellPosition, char c, bool isBigEndian) {
			var hexBytes = position.Buffer.ReadHexBytes(position, ByteCount);
			stringBuilder.Clear();
			FormatValue(stringBuilder, hexBytes, 0, HexValueFormatterFlags.None);
			Debug.Assert(stringBuilder.Length == FormattedLength);
			Debug.Assert(cellPosition < stringBuilder.Length);
			if (cellPosition >= stringBuilder.Length)
				return null;

			if (cellPosition == 0) {
				if (ConvertToSign(c) == 0)
					return null;
			}
			else {
				if (ConvertFromHexCharacter(c) < 0)
					return null;
			}
			stringBuilder[cellPosition] = c;

			long minValue, maxValue;
			switch (ByteCount) {
			case 1:
				minValue = sbyte.MinValue;
				maxValue = sbyte.MaxValue;
				break;
			case 2:
				minValue = short.MinValue;
				maxValue = short.MaxValue;
				break;
			case 4:
				minValue = int.MinValue;
				maxValue = int.MaxValue;
				break;
			case 8:
				minValue = long.MinValue;
				maxValue = long.MaxValue;
				break;
			default:
				throw new InvalidOperationException();
			}

			var v = ToSignedHex(stringBuilder, minValue, maxValue);

			var bytes = new byte[ByteCount];
			switch (ByteCount) {
			case 1:
				bytes[0] = (byte)v;
				break;

			case 2:
				if (isBigEndian) {
					bytes[0] = (byte)(v >> 8);
					bytes[1] = (byte)v;
				}
				else {
					bytes[1] = (byte)(v >> 8);
					bytes[0] = (byte)v;
				}
				break;

			case 4:
				if (isBigEndian) {
					bytes[0] = (byte)(v >> 24);
					bytes[1] = (byte)(v >> 16);
					bytes[2] = (byte)(v >> 8);
					bytes[3] = (byte)v;
				}
				else {
					bytes[3] = (byte)(v >> 24);
					bytes[2] = (byte)(v >> 16);
					bytes[1] = (byte)(v >> 8);
					bytes[0] = (byte)v;
				}
				break;

			case 8:
				if (isBigEndian) {
					bytes[0] = (byte)(v >> 56);
					bytes[1] = (byte)(v >> 48);
					bytes[2] = (byte)(v >> 40);
					bytes[3] = (byte)(v >> 32);
					bytes[4] = (byte)(v >> 24);
					bytes[5] = (byte)(v >> 16);
					bytes[6] = (byte)(v >> 8);
					bytes[7] = (byte)v;
				}
				else {
					bytes[7] = (byte)(v >> 56);
					bytes[6] = (byte)(v >> 48);
					bytes[5] = (byte)(v >> 40);
					bytes[4] = (byte)(v >> 32);
					bytes[3] = (byte)(v >> 24);
					bytes[2] = (byte)(v >> 16);
					bytes[1] = (byte)(v >> 8);
					bytes[0] = (byte)v;
				}
				break;

			default:
				throw new InvalidOperationException();
			}

			return new PositionAndData(position, bytes);
		}

		protected PositionAndData? EditBit(HexBufferPoint position, int cellPosition, char c) {
			int newBit = ConvertFromBitCharacter(c);
			if (newBit < 0)
				return null;

			var dataPos = position.Position + cellPosition / 8;
			if (dataPos >= HexPosition.MaxEndPosition)
				return null;
			int bitNo = (8 - (cellPosition & 7) - 1) & 7;
			var newData = new byte[1];
			newData[0] = position.Buffer.ReadByte(dataPos);
			newData[0] = (byte)((newData[0] & ~(1 << bitNo)) | (newBit << bitNo));

			return new PositionAndData(new HexBufferPoint(position.Buffer, dataPos), newData);
		}

		static int ConvertFromHexCharacter(char c) {
			if ('0' <= c && c <= '9')
				return c - '0';
			if ('A' <= c && c <= 'F')
				return c - 'A' + 10;
			if ('a' <= c && c <= 'f')
				return c - 'a' + 10;
			return -1;
		}

		static int ConvertFromBitCharacter(char c) {
			if ('0' <= c && c <= '1')
				return c - '0';
			return -1;
		}

		static int ConvertToSign(char c) {
			if (c == '+' || c == ' ')
				return 1;
			if (c == '-')
				return -1;
			return 0;
		}
	}

	sealed class HexByteValueFormatter : HexValueFormatter {
		public HexByteValueFormatter()
			: base(1, "FF".Length, HexValuesDisplayFormat.HexByte) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) {
			int b = hexBytes.TryReadByte(valueIndex);
			if (b < 0)
				return WriteInvalid(dest);
			WriteHexByte(dest, flags, (byte)b);
			return 0;
		}

		public override bool CanEdit => true;
		protected override PositionAndData? EditCore(HexBufferPoint position, int cellPosition, char c) => EditUnsignedHexLittleEndian(position, cellPosition, c);
	}

	sealed class HexUInt16ValueFormatter : HexValueFormatter {
		public HexUInt16ValueFormatter()
			: base(2, "FFFF".Length, HexValuesDisplayFormat.HexUInt16) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexUInt16(dest, flags, hexBytes.TryReadUInt16(valueIndex));

		public override HexBufferSpan GetBufferSpan(HexBufferSpan bufferSpan, int cellPosition) {
			if ((uint)cellPosition >= 4)
				return base.GetBufferSpan(bufferSpan, cellPosition);
			var newPos = bufferSpan.Start.Position + (ulong)((2 - cellPosition / 2 - 1) & 1);
			if (newPos >= HexPosition.MaxEndPosition)
				return base.GetBufferSpan(bufferSpan, cellPosition);
			return new HexBufferSpan(new HexBufferPoint(bufferSpan.Buffer, newPos), 1);
		}

		public override bool CanEdit => true;
		protected override PositionAndData? EditCore(HexBufferPoint position, int cellPosition, char c) => EditUnsignedHexLittleEndian(position, cellPosition, c);
	}

	sealed class HexUInt32ValueFormatter : HexValueFormatter {
		public HexUInt32ValueFormatter()
			: base(4, "FFFFFFFF".Length, HexValuesDisplayFormat.HexUInt32) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexUInt32(dest, flags, hexBytes.TryReadUInt32(valueIndex));

		public override HexBufferSpan GetBufferSpan(HexBufferSpan bufferSpan, int cellPosition) {
			if ((uint)cellPosition >= 8)
				return base.GetBufferSpan(bufferSpan, cellPosition);
			var newPos = bufferSpan.Start.Position + (ulong)((4 - cellPosition / 2 - 1) & 3);
			if (newPos >= HexPosition.MaxEndPosition)
				return base.GetBufferSpan(bufferSpan, cellPosition);
			return new HexBufferSpan(new HexBufferPoint(bufferSpan.Buffer, newPos), 1);
		}

		public override bool CanEdit => true;
		protected override PositionAndData? EditCore(HexBufferPoint position, int cellPosition, char c) => EditUnsignedHexLittleEndian(position, cellPosition, c);
	}

	sealed class HexUInt64ValueFormatter : HexValueFormatter {
		public HexUInt64ValueFormatter()
			: base(8, "FFFFFFFFFFFFFFFF".Length, HexValuesDisplayFormat.HexUInt64) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexUInt64(dest, flags, hexBytes.TryReadUInt64(valueIndex));

		public override HexBufferSpan GetBufferSpan(HexBufferSpan bufferSpan, int cellPosition) {
			if ((uint)cellPosition >= 16)
				return base.GetBufferSpan(bufferSpan, cellPosition);
			var newPos = bufferSpan.Start.Position + (ulong)((8 - cellPosition / 2 - 1) & 7);
			if (newPos >= HexPosition.MaxEndPosition)
				return base.GetBufferSpan(bufferSpan, cellPosition);
			return new HexBufferSpan(new HexBufferPoint(bufferSpan.Buffer, newPos), 1);
		}

		public override bool CanEdit => true;
		protected override PositionAndData? EditCore(HexBufferPoint position, int cellPosition, char c) => EditUnsignedHexLittleEndian(position, cellPosition, c);
	}

	sealed class HexSByteValueFormatter : HexValueFormatter {
		public HexSByteValueFormatter()
			: base(1, "-80".Length, HexValuesDisplayFormat.HexSByte) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexSByte(dest, flags, hexBytes.TryReadSByte(valueIndex));

		public override bool CanEdit => true;
		protected override PositionAndData? EditCore(HexBufferPoint position, int cellPosition, char c) => EditSignedHexLittleEndian(position, cellPosition, c);
	}

	sealed class HexInt16ValueFormatter : HexValueFormatter {
		public HexInt16ValueFormatter()
			: base(2, "-8000".Length, HexValuesDisplayFormat.HexInt16) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexInt16(dest, flags, hexBytes.TryReadInt16(valueIndex));

		public override bool CanEdit => true;
		protected override PositionAndData? EditCore(HexBufferPoint position, int cellPosition, char c) => EditSignedHexLittleEndian(position, cellPosition, c);
	}

	sealed class HexInt32ValueFormatter : HexValueFormatter {
		public HexInt32ValueFormatter()
			: base(4, "-80000000".Length, HexValuesDisplayFormat.HexInt32) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexInt32(dest, flags, hexBytes.TryReadInt32(valueIndex));

		public override bool CanEdit => true;
		protected override PositionAndData? EditCore(HexBufferPoint position, int cellPosition, char c) => EditSignedHexLittleEndian(position, cellPosition, c);
	}

	sealed class HexInt64ValueFormatter : HexValueFormatter {
		public HexInt64ValueFormatter()
			: base(8, "-8000000000000000".Length, HexValuesDisplayFormat.HexInt64) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexInt64(dest, flags, hexBytes.TryReadInt64(valueIndex));

		public override bool CanEdit => true;
		protected override PositionAndData? EditCore(HexBufferPoint position, int cellPosition, char c) => EditSignedHexLittleEndian(position, cellPosition, c);
	}

	sealed class DecimalByteValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = byte.MaxValue.ToString(culture).Length;
		public DecimalByteValueFormatter()
			: base(1, MAX_LENGTH, HexValuesDisplayFormat.DecimalByte) {
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
			: base(2, MAX_LENGTH, HexValuesDisplayFormat.DecimalUInt16) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalUInt16(dest, flags, hexBytes.TryReadUInt16(valueIndex));
	}

	sealed class DecimalUInt32ValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = uint.MaxValue.ToString(culture).Length;
		public DecimalUInt32ValueFormatter()
			: base(4, MAX_LENGTH, HexValuesDisplayFormat.DecimalUInt32) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalUInt32(dest, flags, hexBytes.TryReadUInt32(valueIndex));
	}

	sealed class DecimalUInt64ValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = ulong.MaxValue.ToString(culture).Length;
		public DecimalUInt64ValueFormatter()
			: base(8, MAX_LENGTH, HexValuesDisplayFormat.DecimalUInt64) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalUInt64(dest, flags, hexBytes.TryReadUInt64(valueIndex));
	}

	sealed class DecimalSByteValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = sbyte.MinValue.ToString(culture).Length;
		public DecimalSByteValueFormatter()
			: base(1, MAX_LENGTH, HexValuesDisplayFormat.DecimalSByte) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalSByte(dest, flags, hexBytes.TryReadSByte(valueIndex));
	}

	sealed class DecimalInt16ValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = short.MinValue.ToString(culture).Length;
		public DecimalInt16ValueFormatter()
			: base(2, MAX_LENGTH, HexValuesDisplayFormat.DecimalInt16) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalInt16(dest, flags, hexBytes.TryReadInt16(valueIndex));
	}

	sealed class DecimalInt32ValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = int.MinValue.ToString(culture).Length;
		public DecimalInt32ValueFormatter()
			: base(4, MAX_LENGTH, HexValuesDisplayFormat.DecimalInt32) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalInt32(dest, flags, hexBytes.TryReadInt32(valueIndex));
	}

	sealed class DecimalInt64ValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = long.MinValue.ToString(culture).Length;
		public DecimalInt64ValueFormatter()
			: base(8, MAX_LENGTH, HexValuesDisplayFormat.DecimalInt64) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalInt64(dest, flags, hexBytes.TryReadInt64(valueIndex));
	}

	sealed class SingleValueFormatter : HexValueFormatter {
		public SingleValueFormatter()
			: base(4, MaxSingleFormattedLength, HexValuesDisplayFormat.Single) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatSingle(dest, flags, hexBytes.TryReadSingle(valueIndex));
	}

	sealed class DoubleValueFormatter : HexValueFormatter {
		public DoubleValueFormatter()
			: base(8, MaxDoubleFormattedLength, HexValuesDisplayFormat.Double) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDouble(dest, flags, hexBytes.TryReadDouble(valueIndex));
	}

	sealed class Bit8ValueFormatter : HexValueFormatter {
		public Bit8ValueFormatter()
			: base(1, "11111111".Length, HexValuesDisplayFormat.Bit8) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) {
			int b = hexBytes.TryReadByte(valueIndex);
			if (b < 0)
				return WriteInvalid(dest);
			for (int i = 0; i < 8; i++, b <<= 1)
				dest.Append((b & 0x80) != 0 ? '1' : '0');
			return 0;
		}

		public override bool CanEdit => true;
		protected override PositionAndData? EditCore(HexBufferPoint position, int cellPosition, char c) => EditBit(position, cellPosition, c);
	}

	sealed class HexUInt16BigEndianValueFormatter : HexValueFormatter {
		public HexUInt16BigEndianValueFormatter()
			: base(2, "FFFF".Length, HexValuesDisplayFormat.HexUInt16BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexUInt16(dest, flags, hexBytes.TryReadUInt16BigEndian(valueIndex));

		public override HexBufferSpan GetBufferSpan(HexBufferSpan bufferSpan, int cellPosition) {
			if ((uint)cellPosition >= 4)
				return base.GetBufferSpan(bufferSpan, cellPosition);
			var newPos = bufferSpan.Start.Position + (ulong)(cellPosition / 2);
			if (newPos >= HexPosition.MaxEndPosition)
				return base.GetBufferSpan(bufferSpan, cellPosition);
			return new HexBufferSpan(new HexBufferPoint(bufferSpan.Buffer, newPos), 1);
		}

		public override bool CanEdit => true;
		protected override PositionAndData? EditCore(HexBufferPoint position, int cellPosition, char c) => EditUnsignedHexBigEndian(position, cellPosition, c);
	}

	sealed class HexUInt32BigEndianValueFormatter : HexValueFormatter {
		public HexUInt32BigEndianValueFormatter()
			: base(4, "FFFFFFFF".Length, HexValuesDisplayFormat.HexUInt32BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexUInt32(dest, flags, hexBytes.TryReadUInt32BigEndian(valueIndex));

		public override HexBufferSpan GetBufferSpan(HexBufferSpan bufferSpan, int cellPosition) {
			if ((uint)cellPosition >= 8)
				return base.GetBufferSpan(bufferSpan, cellPosition);
			var newPos = bufferSpan.Start.Position + (ulong)(cellPosition / 2);
			if (newPos >= HexPosition.MaxEndPosition)
				return base.GetBufferSpan(bufferSpan, cellPosition);
			return new HexBufferSpan(new HexBufferPoint(bufferSpan.Buffer, newPos), 1);
		}

		public override bool CanEdit => true;
		protected override PositionAndData? EditCore(HexBufferPoint position, int cellPosition, char c) => EditUnsignedHexBigEndian(position, cellPosition, c);
	}

	sealed class HexUInt64BigEndianValueFormatter : HexValueFormatter {
		public HexUInt64BigEndianValueFormatter()
			: base(8, "FFFFFFFFFFFFFFFF".Length, HexValuesDisplayFormat.HexUInt64BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexUInt64(dest, flags, hexBytes.TryReadUInt64BigEndian(valueIndex));

		public override HexBufferSpan GetBufferSpan(HexBufferSpan bufferSpan, int cellPosition) {
			if ((uint)cellPosition >= 16)
				return base.GetBufferSpan(bufferSpan, cellPosition);
			var newPos = bufferSpan.Start.Position + (ulong)(cellPosition / 2);
			if (newPos >= HexPosition.MaxEndPosition)
				return base.GetBufferSpan(bufferSpan, cellPosition);
			return new HexBufferSpan(new HexBufferPoint(bufferSpan.Buffer, newPos), 1);
		}

		public override bool CanEdit => true;
		protected override PositionAndData? EditCore(HexBufferPoint position, int cellPosition, char c) => EditUnsignedHexBigEndian(position, cellPosition, c);
	}

	sealed class HexInt16BigEndianValueFormatter : HexValueFormatter {
		public HexInt16BigEndianValueFormatter()
			: base(2, "-8000".Length, HexValuesDisplayFormat.HexInt16BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexInt16(dest, flags, hexBytes.TryReadInt16BigEndian(valueIndex));

		public override bool CanEdit => true;
		protected override PositionAndData? EditCore(HexBufferPoint position, int cellPosition, char c) => EditSignedHexBigEndian(position, cellPosition, c);
	}

	sealed class HexInt32BigEndianValueFormatter : HexValueFormatter {
		public HexInt32BigEndianValueFormatter()
			: base(4, "-80000000".Length, HexValuesDisplayFormat.HexInt32BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexInt32(dest, flags, hexBytes.TryReadInt32BigEndian(valueIndex));

		public override bool CanEdit => true;
		protected override PositionAndData? EditCore(HexBufferPoint position, int cellPosition, char c) => EditSignedHexBigEndian(position, cellPosition, c);
	}

	sealed class HexInt64BigEndianValueFormatter : HexValueFormatter {
		public HexInt64BigEndianValueFormatter()
			: base(8, "-8000000000000000".Length, HexValuesDisplayFormat.HexInt64BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatHexInt64(dest, flags, hexBytes.TryReadInt64BigEndian(valueIndex));

		public override bool CanEdit => true;
		protected override PositionAndData? EditCore(HexBufferPoint position, int cellPosition, char c) => EditSignedHexBigEndian(position, cellPosition, c);
	}

	sealed class DecimalUInt16BigEndianValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = ushort.MaxValue.ToString(culture).Length;
		public DecimalUInt16BigEndianValueFormatter()
			: base(2, MAX_LENGTH, HexValuesDisplayFormat.DecimalUInt16BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalUInt16(dest, flags, hexBytes.TryReadUInt16BigEndian(valueIndex));
	}

	sealed class DecimalUInt32BigEndianValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = uint.MaxValue.ToString(culture).Length;
		public DecimalUInt32BigEndianValueFormatter()
			: base(4, MAX_LENGTH, HexValuesDisplayFormat.DecimalUInt32BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalUInt32(dest, flags, hexBytes.TryReadUInt32BigEndian(valueIndex));
	}

	sealed class DecimalUInt64BigEndianValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = ulong.MaxValue.ToString(culture).Length;
		public DecimalUInt64BigEndianValueFormatter()
			: base(8, MAX_LENGTH, HexValuesDisplayFormat.DecimalUInt64BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalUInt64(dest, flags, hexBytes.TryReadUInt64BigEndian(valueIndex));
	}

	sealed class DecimalInt16BigEndianValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = short.MinValue.ToString(culture).Length;
		public DecimalInt16BigEndianValueFormatter()
			: base(2, MAX_LENGTH, HexValuesDisplayFormat.DecimalInt16BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalInt16(dest, flags, hexBytes.TryReadInt16BigEndian(valueIndex));
	}

	sealed class DecimalInt32BigEndianValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = int.MinValue.ToString(culture).Length;
		public DecimalInt32BigEndianValueFormatter()
			: base(4, MAX_LENGTH, HexValuesDisplayFormat.DecimalInt32BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalInt32(dest, flags, hexBytes.TryReadInt32BigEndian(valueIndex));
	}

	sealed class DecimalInt64BigEndianValueFormatter : HexValueFormatter {
		static readonly int MAX_LENGTH = long.MinValue.ToString(culture).Length;
		public DecimalInt64BigEndianValueFormatter()
			: base(8, MAX_LENGTH, HexValuesDisplayFormat.DecimalInt64BigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDecimalInt64(dest, flags, hexBytes.TryReadInt64BigEndian(valueIndex));
	}

	sealed class SingleBigEndianValueFormatter : HexValueFormatter {
		public SingleBigEndianValueFormatter()
			: base(4, MaxSingleFormattedLength, HexValuesDisplayFormat.SingleBigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatSingle(dest, flags, hexBytes.TryReadSingleBigEndian(valueIndex));
	}

	sealed class DoubleBigEndianValueFormatter : HexValueFormatter {
		public DoubleBigEndianValueFormatter()
			: base(8, MaxDoubleFormattedLength, HexValuesDisplayFormat.DoubleBigEndian) {
		}

		public override int FormatValue(StringBuilder dest, HexBytes hexBytes, long valueIndex, HexValueFormatterFlags flags) =>
			FormatDouble(dest, flags, hexBytes.TryReadDoubleBigEndian(valueIndex));
	}
}

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

using dnSpy.Contracts.Hex;

namespace dnSpy.Hex.Commands {
	static class SettingsConstants {
		public static readonly (HexValuesDisplayFormat displayFormat, string text)[] ValueFormatList = new (HexValuesDisplayFormat, string)[] {
			(HexValuesDisplayFormat.HexByte, "Byte (hex)"),
			(HexValuesDisplayFormat.DecimalByte, "Byte (decimal)"),
			(HexValuesDisplayFormat.HexUInt16, "UInt16 (hex)"),
			(HexValuesDisplayFormat.DecimalUInt16, "UInt16 (decimal)"),
			(HexValuesDisplayFormat.HexUInt16BigEndian, "UInt16 (hex, big endian)"),
			(HexValuesDisplayFormat.DecimalUInt16BigEndian, "UInt16 (decimal, big endian)"),
			(HexValuesDisplayFormat.HexUInt32, "UInt32 (hex)"),
			(HexValuesDisplayFormat.DecimalUInt32, "UInt32 (decimal)"),
			(HexValuesDisplayFormat.HexUInt32BigEndian, "UInt32 (hex, big endian)"),
			(HexValuesDisplayFormat.DecimalUInt32BigEndian, "UInt32 (decimal, big endian)"),
			(HexValuesDisplayFormat.HexUInt64, "UInt64 (hex)"),
			(HexValuesDisplayFormat.DecimalUInt64, "UInt64 (decimal)"),
			(HexValuesDisplayFormat.HexUInt64BigEndian, "UInt64 (hex, big endian)"),
			(HexValuesDisplayFormat.DecimalUInt64BigEndian, "UInt64 (decimal, big endian)"),

			(HexValuesDisplayFormat.HexSByte, "SByte (hex)"),
			(HexValuesDisplayFormat.DecimalSByte, "SByte (decimal)"),
			(HexValuesDisplayFormat.HexInt16, "Int16 (hex)"),
			(HexValuesDisplayFormat.DecimalInt16, "Int16 (decimal)"),
			(HexValuesDisplayFormat.HexInt16BigEndian, "Int16 (hex, big endian)"),
			(HexValuesDisplayFormat.DecimalInt16BigEndian, "Int16 (decimal, big endian)"),
			(HexValuesDisplayFormat.HexInt32, "Int32 (hex)"),
			(HexValuesDisplayFormat.DecimalInt32, "Int32 (decimal)"),
			(HexValuesDisplayFormat.HexInt32BigEndian, "Int32 (hex, big endian)"),
			(HexValuesDisplayFormat.DecimalInt32BigEndian, "Int32 (decimal, big endian)"),
			(HexValuesDisplayFormat.HexInt64, "Int64 (hex)"),
			(HexValuesDisplayFormat.DecimalInt64, "Int64 (decimal)"),
			(HexValuesDisplayFormat.HexInt64BigEndian, "Int64 (hex, big endian)"),
			(HexValuesDisplayFormat.DecimalInt64BigEndian, "Int64 (decimal, big endian)"),

			(HexValuesDisplayFormat.Single, "Single"),
			(HexValuesDisplayFormat.SingleBigEndian, "Single (big endian)"),
			(HexValuesDisplayFormat.Double, "Double"),
			(HexValuesDisplayFormat.DoubleBigEndian, "Double (big endian)"),

			(HexValuesDisplayFormat.Bit8, "8 Bits"),
		};
	}
}

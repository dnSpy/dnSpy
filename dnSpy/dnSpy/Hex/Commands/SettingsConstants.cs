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

using System.Collections.Generic;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Hex.Commands {
	static class SettingsConstants {
		public static readonly KeyValuePair<HexValuesDisplayFormat, string>[] ValueFormatList = new KeyValuePair<HexValuesDisplayFormat, string>[] {
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.HexByte, "Byte (hex)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.DecimalByte, "Byte (decimal)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.HexUInt16, "UInt16 (hex)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.DecimalUInt16, "UInt16 (decimal)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.HexUInt16BigEndian, "UInt16 (hex, big endian)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.DecimalUInt16BigEndian, "UInt16 (decimal, big endian)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.HexUInt32, "UInt32 (hex)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.DecimalUInt32, "UInt32 (decimal)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.HexUInt32BigEndian, "UInt32 (hex, big endian)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.DecimalUInt32BigEndian, "UInt32 (decimal, big endian)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.HexUInt64, "UInt64 (hex)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.DecimalUInt64, "UInt64 (decimal)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.HexUInt64BigEndian, "UInt64 (hex, big endian)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.DecimalUInt64BigEndian, "UInt64 (decimal, big endian)"),

			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.HexSByte, "SByte (hex)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.DecimalSByte, "SByte (decimal)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.HexInt16, "Int16 (hex)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.DecimalInt16, "Int16 (decimal)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.HexInt16BigEndian, "Int16 (hex, big endian)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.DecimalInt16BigEndian, "Int16 (decimal, big endian)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.HexInt32, "Int32 (hex)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.DecimalInt32, "Int32 (decimal)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.HexInt32BigEndian, "Int32 (hex, big endian)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.DecimalInt32BigEndian, "Int32 (decimal, big endian)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.HexInt64, "Int64 (hex)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.DecimalInt64, "Int64 (decimal)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.HexInt64BigEndian, "Int64 (hex, big endian)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.DecimalInt64BigEndian, "Int64 (decimal, big endian)"),

			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.Single, "Single"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.SingleBigEndian, "Single (big endian)"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.Double, "Double"),
			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.DoubleBigEndian, "Double (big endian)"),

			new KeyValuePair<HexValuesDisplayFormat, string>(HexValuesDisplayFormat.Bit8, "8 Bits"),
		};
	}
}

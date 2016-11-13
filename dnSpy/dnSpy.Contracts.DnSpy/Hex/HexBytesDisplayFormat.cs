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

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Hex bytes display format
	/// </summary>
	public enum HexBytesDisplayFormat {
		/// <summary>
		/// Hex, <see cref="byte"/>
		/// </summary>
		HexByte,

		/// <summary>
		/// Hex, <see cref="ushort"/>
		/// </summary>
		HexUInt16,

		/// <summary>
		/// Hex, <see cref="uint"/>
		/// </summary>
		HexUInt32,

		/// <summary>
		/// Hex, <see cref="ulong"/>
		/// </summary>
		HexUInt64,

		/// <summary>
		/// Hex, <see cref="sbyte"/>
		/// </summary>
		HexSByte,

		/// <summary>
		/// Hex, <see cref="short"/>
		/// </summary>
		HexInt16,

		/// <summary>
		/// Hex, <see cref="int"/>
		/// </summary>
		HexInt32,

		/// <summary>
		/// Hex, <see cref="long"/>
		/// </summary>
		HexInt64,

		/// <summary>
		/// Decimal, <see cref="byte"/>
		/// </summary>
		DecimalByte,

		/// <summary>
		/// Decimal, <see cref="ushort"/>
		/// </summary>
		DecimalUInt16,

		/// <summary>
		/// Decimal, <see cref="uint"/>
		/// </summary>
		DecimalUInt32,

		/// <summary>
		/// Decimal, <see cref="ulong"/>
		/// </summary>
		DecimalUInt64,

		/// <summary>
		/// Decimal, <see cref="sbyte"/>
		/// </summary>
		DecimalSByte,

		/// <summary>
		/// Decimal, <see cref="short"/>
		/// </summary>
		DecimalInt16,

		/// <summary>
		/// Decimal, <see cref="int"/>
		/// </summary>
		DecimalInt32,

		/// <summary>
		/// Decimal, <see cref="long"/>
		/// </summary>
		DecimalInt64,

		/// <summary>
		/// <see cref="float"/>
		/// </summary>
		Single,

		/// <summary>
		/// <see cref="double"/>
		/// </summary>
		Double,

		/// <summary>
		/// 8 bits
		/// </summary>
		Bit8,

		/// <summary>
		/// Hex, <see cref="ushort"/>, Big Endian
		/// </summary>
		HexUInt16BigEndian,

		/// <summary>
		/// Hex, <see cref="uint"/>, Big Endian
		/// </summary>
		HexUInt32BigEndian,

		/// <summary>
		/// Hex, <see cref="ulong"/>, Big Endian
		/// </summary>
		HexUInt64BigEndian,

		/// <summary>
		/// Hex, <see cref="short"/>, Big Endian
		/// </summary>
		HexInt16BigEndian,

		/// <summary>
		/// Hex, <see cref="int"/>, Big Endian
		/// </summary>
		HexInt32BigEndian,

		/// <summary>
		/// Hex, <see cref="long"/>, Big Endian
		/// </summary>
		HexInt64BigEndian,

		/// <summary>
		/// Decimal, <see cref="ushort"/>, Big Endian
		/// </summary>
		DecimalUInt16BigEndian,

		/// <summary>
		/// Decimal, <see cref="uint"/>, Big Endian
		/// </summary>
		DecimalUInt32BigEndian,

		/// <summary>
		/// Decimal, <see cref="ulong"/>, Big Endian
		/// </summary>
		DecimalUInt64BigEndian,

		/// <summary>
		/// Decimal, <see cref="short"/>, Big Endian
		/// </summary>
		DecimalInt16BigEndian,

		/// <summary>
		/// Decimal, <see cref="int"/>, Big Endian
		/// </summary>
		DecimalInt32BigEndian,

		/// <summary>
		/// Decimal, <see cref="long"/>, Big Endian
		/// </summary>
		DecimalInt64BigEndian,

		/// <summary>
		/// <see cref="float"/>, Big Endian
		/// </summary>
		SingleBigEndian,

		/// <summary>
		/// <see cref="double"/>, Big Endian
		/// </summary>
		DoubleBigEndian,
	}
}

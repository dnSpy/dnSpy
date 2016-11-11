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

namespace dnSpy.Contracts.Hex.Editor {
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
	}
}

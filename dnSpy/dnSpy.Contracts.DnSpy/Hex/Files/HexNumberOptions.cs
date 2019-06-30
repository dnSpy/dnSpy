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

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// Formatted number options
	/// </summary>
	[Flags]
	public enum HexNumberOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None							= 0,

		/// <summary>
		/// C# hex numbers (0x56789ABC)
		/// </summary>
		HexCSharp						= 0x00000000,

		/// <summary>
		/// Visual Basic hex numbers (&amp;H56789ABC)
		/// </summary>
		HexVisualBasic					= 0x00000001,

		/// <summary>
		/// Assembly language hex numbers (56789ABCh)
		/// </summary>
		HexAssembly						= 0x00000002,

		/// <summary>
		/// Hex numbers (56789ABC)
		/// </summary>
		Hex								= 0x00000003,

		/// <summary>
		/// Decimal numbers
		/// </summary>
		Decimal							= 0x00000004,

		/// <summary>
		/// Number base mask
		/// </summary>
		NumberBaseMask					= 0x00000007,

		/// <summary>
		/// Lower case hex
		/// </summary>
		LowerCaseHex					= 0x40000000,

		/// <summary>
		/// Use as few digits as possible
		/// </summary>
		MinimumDigits					= int.MinValue,
	}
}

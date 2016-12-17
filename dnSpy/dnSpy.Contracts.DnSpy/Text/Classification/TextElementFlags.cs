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

using System;
using System.Windows;

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Text element flags
	/// </summary>
	[Flags]
	public enum TextElementFlags {
		/// <summary>
		/// No bit is set
		/// </summary>
		None							= 0,

		/// <summary>
		/// Filter out newlines from the string by replacing them with spaces
		/// </summary>
		FilterOutNewLines				= 0x00000001,

		/// <summary>
		/// Use the new text formatter, it's faster but doesn't support word wrap or all unicode characters
		/// </summary>
		NewFormatter					= 0x00000002,

		/// <summary>
		/// Mask to get word wrap enum
		/// </summary>
		WrapMask						= 0x0000000C,

		/// <summary>
		/// <see cref="TextWrapping.NoWrap"/>
		/// </summary>
		NoWrap							= 0x00000000,

		/// <summary>
		/// <see cref="TextWrapping.WrapWithOverflow"/>
		/// </summary>
		WrapWithOverflow				= 0x00000004,

		/// <summary>
		/// <see cref="TextWrapping.Wrap"/>
		/// </summary>
		Wrap							= 0x00000008,

		/// <summary>
		/// Mask to get text trimming enum
		/// </summary>
		TrimmingMask					= 0x00000030,

		/// <summary>
		/// <see cref="TextTrimming.None"/>
		/// </summary>
		NoTrimming						= 0x00000000,

		/// <summary>
		/// <see cref="TextTrimming.CharacterEllipsis"/>
		/// </summary>
		CharacterEllipsis				= 0x00000010,

		/// <summary>
		/// <see cref="TextTrimming.WordEllipsis"/>
		/// </summary>
		WordEllipsis					= 0x00000020,
	}
}

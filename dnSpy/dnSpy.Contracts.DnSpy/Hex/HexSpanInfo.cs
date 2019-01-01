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

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// <see cref="HexSpanInfo"/> flags
	/// </summary>
	[Flags]
	public enum HexSpanInfoFlags {
		/// <summary>
		/// No bit is set
		/// </summary>
		None							= 0,

		/// <summary>
		/// Set if the span contains data, clear if the span doesn't contain any data
		/// </summary>
		HasData							= 0x00000001,
	}

	/// <summary>
	/// Information about a span in a <see cref="HexBuffer"/>
	/// </summary>
	public readonly struct HexSpanInfo {
		/// <summary>
		/// Gets the span
		/// </summary>
		public HexSpan Span { get; }

		/// <summary>
		/// Gets the flags
		/// </summary>
		public HexSpanInfoFlags Flags { get; }

		/// <summary>
		/// true if <see cref="Span"/> contains data
		/// </summary>
		public bool HasData => (Flags & HexSpanInfoFlags.HasData) != 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="flags">Flags</param>
		public HexSpanInfo(HexSpan span, HexSpanInfoFlags flags) {
			Span = span;
			Flags = flags;
		}
	}
}

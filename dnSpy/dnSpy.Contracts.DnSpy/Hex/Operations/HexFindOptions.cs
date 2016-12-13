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

namespace dnSpy.Contracts.Hex.Operations {
	/// <summary>
	/// Find options used by <see cref="HexSearchService"/>
	/// </summary>
	[Flags]
	public enum HexFindOptions {
		/// <summary>
		/// No options have been set
		/// </summary>
		None				= 0,

		/// <summary>
		/// Search in reverse direction
		/// </summary>
		SearchReverse		= 0x00000001,

		/// <summary>
		/// Wrap around
		/// </summary>
		Wrap				= 0x00000002,

		/// <summary>
		/// Don't return spans that overlap a previous result. Useful by Replace All code.
		/// </summary>
		NoOverlaps			= 0x00000004,
	}
}

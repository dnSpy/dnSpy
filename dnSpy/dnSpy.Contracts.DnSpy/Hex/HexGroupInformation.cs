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
using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Group information
	/// </summary>
	public readonly struct HexGroupInformation {
		/// <summary>
		/// Group index
		/// </summary>
		public int GroupIndex { get; }

		/// <summary>
		/// Gets the full span including a possible separator at the end of the span
		/// </summary>
		public VST.Span FullSpan { get; }

		/// <summary>
		/// Gets the span without the separator at the end of the span
		/// </summary>
		public VST.Span Span { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="groupIndex">Group index</param>
		/// <param name="fullSpan">Full span including a possible separator at the end of the span</param>
		/// <param name="span">Span without the separator at the end of the span</param>
		public HexGroupInformation(int groupIndex, VST.Span fullSpan, VST.Span span) {
			if (groupIndex < 0 || groupIndex > 1)
				throw new ArgumentOutOfRangeException(nameof(groupIndex));
			if (!fullSpan.Contains(span))
				throw new ArgumentOutOfRangeException(nameof(span));
			GroupIndex = groupIndex;
			FullSpan = fullSpan;
			Span = span;
		}
	}
}

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

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using dnSpy.Contracts.Hex.Formatting;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Hex view line collection
	/// </summary>
	public abstract class HexViewLineCollection : IReadOnlyList<HexViewLine> {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexViewLineCollection() { }

		/// <summary>
		/// true if it's valid, false if it has been disposed
		/// </summary>
		public abstract bool IsValid { get; }

		/// <summary>
		/// Gets the number of lines in this collection
		/// </summary>
		public abstract int Count { get; }

		/// <summary>
		/// Gets a line
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public abstract HexViewLine this[int index] { get; }

		/// <summary>
		/// Gets the span of all lines in this collection
		/// </summary>
		public abstract HexBufferSpan FormattedSpan { get; }

		/// <summary>
		/// Gets the first visible line
		/// </summary>
		public abstract HexViewLine FirstVisibleLine { get; }

		/// <summary>
		/// Gets the last visible line
		/// </summary>
		public abstract HexViewLine LastVisibleLine { get; }

		/// <summary>
		/// Gets normalized text bounds
		/// </summary>
		/// <param name="bufferPosition">Position</param>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public abstract Collection<TextBounds> GetNormalizedTextBounds(HexBufferSpan bufferPosition, TextBoundsFlags flags);

		/// <summary>
		/// Returns true if this collection contains <paramref name="bufferPosition"/>
		/// </summary>
		/// <param name="bufferPosition">Position</param>
		/// <returns></returns>
		public abstract bool ContainsBufferPosition(HexBufferPoint bufferPosition);

		/// <summary>
		/// Returns true if this collection intersects with <paramref name="bufferSpan"/>
		/// </summary>
		/// <param name="bufferSpan">Span</param>
		/// <returns></returns>
		public abstract bool IntersectsBufferSpan(HexBufferSpan bufferSpan);

		/// <summary>
		/// Gets the line containing <paramref name="bufferPosition"/>
		/// </summary>
		/// <param name="bufferPosition">Position</param>
		/// <returns></returns>
		public abstract HexViewLine GetHexViewLineContainingBufferPosition(HexBufferPoint bufferPosition);

		/// <summary>
		/// Gets the line containing <paramref name="y"/>
		/// </summary>
		/// <param name="y">Y position</param>
		/// <returns></returns>
		public abstract HexViewLine GetHexViewLineContainingYCoordinate(double y);

		/// <summary>
		/// Gets all lines intersecting with <paramref name="bufferSpan"/>
		/// </summary>
		/// <param name="bufferSpan">Span</param>
		/// <returns></returns>
		public abstract Collection<HexViewLine> GetHexViewLinesIntersectingSpan(HexBufferSpan bufferSpan);

		/// <summary>
		/// Gets the enumerator
		/// </summary>
		/// <returns></returns>
		public abstract IEnumerator<HexViewLine> GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}

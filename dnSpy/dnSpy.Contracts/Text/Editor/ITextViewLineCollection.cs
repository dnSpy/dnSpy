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
using System.Collections.ObjectModel;
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// <see cref="ITextViewLine"/> collection
	/// </summary>
	public interface ITextViewLineCollection : IList<ITextViewLine> {
		/// <summary>
		/// Gets the first visible line
		/// </summary>
		ITextViewLine FirstVisibleLine { get; }

		/// <summary>
		/// Gets the last visible line
		/// </summary>
		ITextViewLine LastVisibleLine { get; }

		/// <summary>
		/// Gets the span of text contained in this collection
		/// </summary>
		SnapshotSpan FormattedSpan { get; }

		/// <summary>
		/// true if it's still valid
		/// </summary>
		bool IsValid { get; }

		/// <summary>
		/// Returns true if <paramref name="bufferPosition"/> is contained by any of the lines in this collection
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <returns></returns>
		bool ContainsBufferPosition(SnapshotPoint bufferPosition);

		/// <summary>
		/// Gets the text bounds of <paramref name="bufferPosition"/>
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <returns></returns>
		TextBounds GetCharacterBounds(SnapshotPoint bufferPosition);

		/// <summary>
		/// Returns the index of <paramref name="textLine"/> in <see cref="ITextView.TextViewLines"/>
		/// </summary>
		/// <param name="textLine">Text line</param>
		/// <returns></returns>
		int GetIndexOfTextLine(ITextViewLine textLine);

		/// <summary>
		/// Gets a collection of <see cref="TextBounds"/> structures for the text that corresponds to the given span
		/// </summary>
		/// <param name="bufferSpan">Buffer span</param>
		/// <returns></returns>
		Collection<TextBounds> GetNormalizedTextBounds(SnapshotSpan bufferSpan);

		/// <summary>
		/// Gets the span whose text element span contains the given buffer position
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <returns></returns>
		SnapshotSpan GetTextElementSpan(SnapshotPoint bufferPosition);

		/// <summary>
		/// Gets the <see cref="ITextViewLine"/> that contains the specified text buffer position
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <returns></returns>
		ITextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition);

		/// <summary>
		/// Gets the <see cref="ITextViewLine"/> that contains the specified y-coordinate
		/// </summary>
		/// <param name="y">Y coordinate</param>
		/// <returns></returns>
		ITextViewLine GetTextViewLineContainingYCoordinate(double y);

		/// <summary>
		/// Gets all of the <see cref="ITextViewLine"/> objects that intersect <paramref name="bufferSpan"/>
		/// </summary>
		/// <param name="bufferSpan"></param>
		/// <returns></returns>
		Collection<ITextViewLine> GetTextViewLinesIntersectingSpan(SnapshotSpan bufferSpan);

		/// <summary>
		/// Determines whether the specified buffer span intersects any of the <see cref="ITextViewLine"/> objects in the collection
		/// </summary>
		/// <param name="bufferSpan">Buffer span</param>
		/// <returns></returns>
		bool IntersectsBufferSpan(SnapshotSpan bufferSpan);
	}
}

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

namespace dnSpy.Contracts.Text.Editor.Operations {
	/// <summary>
	/// Text structure navigator
	/// </summary>
	public interface ITextStructureNavigator {
		/// <summary>
		/// Gets the content type that this navigator supports
		/// </summary>
		IContentType ContentType { get; }

		/// <summary>
		/// Gets the extent of the word at the given position.
		/// </summary>
		/// <param name="currentPosition">The text position</param>
		/// <returns></returns>
		TextExtent GetExtentOfWord(SnapshotPoint currentPosition);

		/// <summary>
		/// Gets the span of the enclosing syntactic element of the specified snapshot span.
		/// </summary>
		/// <param name="activeSpan">The <see cref="SnapshotSpan"/> from which to get the enclosing syntactic element</param>
		/// <returns></returns>
		SnapshotSpan GetSpanOfEnclosing(SnapshotSpan activeSpan);

		/// <summary>
		/// Gets the span of the first child syntactic element of the specified snapshot span
		/// </summary>
		/// <param name="activeSpan">The <see cref="SnapshotSpan"/> from which to get the span of the first child syntactic element</param>
		/// <returns></returns>
		SnapshotSpan GetSpanOfFirstChild(SnapshotSpan activeSpan);

		/// <summary>
		/// Gets the span of the next sibling syntactic element of the specified snapshot span
		/// </summary>
		/// <param name="activeSpan">The <see cref="SnapshotSpan"/> from which to get the span of the next sibling syntactic element</param>
		/// <returns></returns>
		SnapshotSpan GetSpanOfNextSibling(SnapshotSpan activeSpan);

		/// <summary>
		/// Gets the span of the previous sibling syntactic element of the specified snapshot span
		/// </summary>
		/// <param name="activeSpan">The <see cref="SnapshotSpan"/> from which to get the span of the previous sibling syntactic element</param>
		/// <returns></returns>
		SnapshotSpan GetSpanOfPreviousSibling(SnapshotSpan activeSpan);
	}
}

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
using System.IO;

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Text snapshot
	/// </summary>
	public interface ITextSnapshot {
		/// <summary>
		/// Gets a character at <paramref name="position"/>
		/// </summary>
		/// <param name="position">Character position</param>
		/// <returns></returns>
		char this[int position] { get; }

		/// <summary>
		/// Gets the content type
		/// </summary>
		IContentType ContentType { get; }

		/// <summary>
		/// Gets the length of the document
		/// </summary>
		int Length { get; }

		/// <summary>
		/// Number of lines
		/// </summary>
		int LineCount { get; }

		/// <summary>
		/// Gets all the lines
		/// </summary>
		IEnumerable<ITextSnapshotLine> Lines { get; }

		/// <summary>
		/// Gets the text buffer owner
		/// </summary>
		ITextBuffer TextBuffer { get; }

		/// <summary>
		/// Copies this to <paramref name="destination"/>
		/// </summary>
		/// <param name="sourceIndex">Source position</param>
		/// <param name="destination">Destination</param>
		/// <param name="destinationIndex">Destination index</param>
		/// <param name="count">Number of characters to copy</param>
		void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

		/// <summary>
		/// Gets a line
		/// </summary>
		/// <param name="lineNumber">Line number (0-based)</param>
		/// <returns></returns>
		ITextSnapshotLine GetLineFromLineNumber(int lineNumber);

		/// <summary>
		/// Gets a line
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		ITextSnapshotLine GetLineFromPosition(int position);

		/// <summary>
		/// Converts a position to a line number (0-based)
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		int GetLineNumberFromPosition(int position);

		/// <summary>
		/// Gets all the text
		/// </summary>
		/// <returns></returns>
		string GetText();

		/// <summary>
		/// Gets text
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		string GetText(Span span);

		/// <summary>
		/// Gets text
		/// </summary>
		/// <param name="startIndex">Start position</param>
		/// <param name="length">Length</param>
		/// <returns></returns>
		string GetText(int startIndex, int length);

		/// <summary>
		/// Copy part of this to a character array
		/// </summary>
		/// <param name="startIndex">Start position</param>
		/// <param name="length">Number of characters</param>
		/// <returns></returns>
		char[] ToCharArray(int startIndex, int length);

		/// <summary>
		/// Writes the snapshot to <paramref name="writer"/>
		/// </summary>
		/// <param name="writer">Destination</param>
		void Write(TextWriter writer);

		/// <summary>
		/// Writes a region to <paramref name="writer"/>
		/// </summary>
		/// <param name="writer">Destination</param>
		/// <param name="span">Span</param>
		void Write(TextWriter writer, Span span);
	}
}

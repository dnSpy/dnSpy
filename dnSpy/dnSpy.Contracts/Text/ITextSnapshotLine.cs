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

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// <see cref="ITextSnapshot"/> line
	/// </summary>
	public interface ITextSnapshotLine {
		/// <summary>
		/// Gets the line break character(s) as a string. Can be an empty string.
		/// </summary>
		/// <returns></returns>
		string GetLineBreakText();

		/// <summary>
		/// Gets the text excluding the line break character(s)
		/// </summary>
		/// <returns></returns>
		string GetText();

		/// <summary>
		/// Gets the text including the line break character(s)
		/// </summary>
		/// <returns></returns>
		string GetTextIncludingLineBreak();

		/// <summary>
		/// First character of the line
		/// </summary>
		SnapshotPoint Start { get; }

		/// <summary>
		/// First character after the line, but before any line break character(s)
		/// </summary>
		SnapshotPoint End { get; }

		/// <summary>
		/// First character after the line and after any line break character(s)
		/// </summary>
		SnapshotPoint EndIncludingLineBreak { get; }

		/// <summary>
		/// Extent of the line, excluding any line break character(s)
		/// </summary>
		SnapshotSpan Extent { get; }

		/// <summary>
		/// Extent of the line, including any line break character(s)
		/// </summary>
		SnapshotSpan ExtentIncludingLineBreak { get; }

		/// <summary>
		/// Length of the line, excluding any line break character(s)
		/// </summary>
		int Length { get; }

		/// <summary>
		/// Length of the line, including any line break character(s)
		/// </summary>
		int LengthIncludingLineBreak { get; }

		/// <summary>
		/// Length of the line break character(s)
		/// </summary>
		int LineBreakLength { get; }

		/// <summary>
		/// Line number, 0-based
		/// </summary>
		int LineNumber { get; }

		/// <summary>
		/// Gets the snapshot
		/// </summary>
		ITextSnapshot Snapshot { get; }
	}
}

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
using System.Linq;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Editor {
	static class TextViewExtensions {
		public static ITextViewLine GetFirstFullyVisibleLine(this ITextView textView) =>
			textView.TextViewLines.FirstOrDefault(a => a.VisibilityState == VisibilityState.FullyVisible) ?? textView.TextViewLines.FirstVisibleLine;

		public static ITextViewLine GetLastFullyVisibleLine(this ITextView textView) =>
			textView.TextViewLines.LastOrDefault(a => a.VisibilityState == VisibilityState.FullyVisible) ?? textView.TextViewLines.LastVisibleLine;

		/// <summary>
		/// Converts line and column to a position
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <param name="lineNumber">Line number, 0-based</param>
		/// <param name="columnNumber">Column number, 0-based</param>
		/// <returns></returns>
		public static int LineColumnToPosition(this ITextView textView, int lineNumber, int columnNumber) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			var snapshot = textView.TextSnapshot;
			if (lineNumber < 0)
				return 0;
			if (lineNumber >= snapshot.LineCount)
				return snapshot.Length;
			var line = snapshot.GetLineFromLineNumber(lineNumber);
			if (columnNumber >= line.LengthIncludingLineBreak)
				columnNumber = line.LengthIncludingLineBreak;
			return line.Start.Position + columnNumber;
		}
	}
}

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
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Text.Editor {
	static class TextViewExtensions {
		public static ITextViewLine GetVisibleTextViewLineContainingYCoordinate(this ITextView textView, double y) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			var line = textView.TextViewLines.GetTextViewLineContainingYCoordinate(y);
			if (line != null && line.IsVisible())
				return line;
			if (y < textView.TextViewLines.FirstVisibleLine.Bottom)
				return GetVisibleTextViewLineContainingYCoordinateBackwards(textView, textView.TextViewLines.FirstVisibleLine, y);
			return GetVisibleTextViewLineContainingYCoordinateForwards(textView, textView.TextViewLines.LastVisibleLine, y);
		}

		static ITextViewLine GetVisibleTextViewLineContainingYCoordinateBackwards(ITextView textView, ITextViewLine textLine, double y) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (textLine == null)
				throw new ArgumentNullException(nameof(textLine));
			var line = textLine;
			for (;;) {
				if (y >= line.Bottom)
					return null;
				if (line.Top <= y)
					return line.IsVisible() ? line : null;
				if (line.IsFirstDocumentLine())
					return null;
				line = textView.GetTextViewLineContainingBufferPosition(line.Start - 1);
			}
		}

		static ITextViewLine GetVisibleTextViewLineContainingYCoordinateForwards(ITextView textView, ITextViewLine textLine, double y) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (textLine == null)
				throw new ArgumentNullException(nameof(textLine));
			var line = textLine;
			for (;;) {
				if (y < line.Top)
					return null;
				if (y < line.Bottom)
					return line.IsVisible() ? line : null;
				if (line.IsLastDocumentLine())
					return line.IsVisible() && y >= textView.ViewportHeight ? line : null;
				line = textView.GetTextViewLineContainingBufferPosition(line.EndIncludingLineBreak);
			}
		}

		public static ITextViewLine GetFirstFullyVisibleLineOrGetNew(this ITextView textView) {
			var line = textView.TextViewLines.FirstOrDefault(a => a.VisibilityState == VisibilityState.FullyVisible) ?? textView.TextViewLines.First();
			for (;;) {
				if (line.Start.Position == 0)
					return line;
				var prev = textView.GetTextViewLineContainingBufferPosition(line.Start - 1);
				if (prev.VisibilityState != VisibilityState.FullyVisible)
					return line;
				line = prev;
			}
		}

		public static ITextViewLine GetLastFullyVisibleLineOrGetNew(this ITextView textView) {
			var line = textView.TextViewLines.LastOrDefault(a => a.VisibilityState == VisibilityState.FullyVisible) ?? textView.TextViewLines.Last();
			for (;;) {
				if (line.IsLastDocumentLine())
					return line;
				var next = textView.GetTextViewLineContainingBufferPosition(line.EndIncludingLineBreak);
				if (next.VisibilityState != VisibilityState.FullyVisible)
					return line;
				line = next;
			}
		}
	}
}

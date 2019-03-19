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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	static class SelectionUtilities {
		public static SnapshotSpan GetLineAnchorSpan(ITextSelection textSelection) {
			if (textSelection == null)
				throw new ArgumentNullException(nameof(textSelection));
			if (textSelection.IsEmpty)
				return textSelection.TextView.Caret.ContainingTextViewLine.ExtentIncludingLineBreak;
			var anchorExtent = textSelection.TextView.GetTextViewLineContainingBufferPosition(textSelection.AnchorPoint.Position).ExtentIncludingLineBreak;
			var activeExtent = textSelection.TextView.GetTextViewLineContainingBufferPosition(textSelection.ActivePoint.Position).ExtentIncludingLineBreak;
			if (textSelection.AnchorPoint >= textSelection.ActivePoint) {
				if (new VirtualSnapshotPoint(anchorExtent.Start) == textSelection.AnchorPoint && textSelection.AnchorPoint.Position.Position != 0)
					anchorExtent = textSelection.TextView.GetTextViewLineContainingBufferPosition(textSelection.AnchorPoint.Position - 1).ExtentIncludingLineBreak;
			}
			return anchorExtent;
		}
	}
}

/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Editor {
	struct BoxSelectionHelper {
		readonly ITextSelection textSelection;
		readonly ITextSnapshot textSnapshot;
		readonly double xLeft, xRight;

		public BoxSelectionHelper(ITextSelection textSelection) {
			Debug.Assert(textSelection.Mode == TextSelectionMode.Box);
			this.textSelection = textSelection;
			textSnapshot = textSelection.TextView.TextSnapshot;

			var anchorLine = textSelection.TextView.GetTextViewLineContainingBufferPosition(textSelection.AnchorPoint.Position);
			var activeLine = textSelection.TextView.GetTextViewLineContainingBufferPosition(textSelection.ActivePoint.Position);
			var anchorBounds = anchorLine.GetExtendedCharacterBounds(textSelection.AnchorPoint);
			var activeBounds = activeLine.GetExtendedCharacterBounds(textSelection.ActivePoint);
			if (anchorBounds.Left < activeBounds.Right) {
				xLeft = anchorBounds.Left;
				xRight = Math.Max(xLeft, activeBounds.Left);
			}
			else {
				xLeft = activeBounds.Left;
				xRight = Math.Max(xLeft, anchorBounds.Left);
			}
			Debug.Assert(xLeft <= xRight);
		}

		public VirtualSnapshotSpan GetSpan(ITextViewLine line) {
			if (textSelection.TextView.TextSnapshot != textSnapshot)
				throw new InvalidOperationException();
			var start = line.GetInsertionBufferPositionFromXCoordinate(xLeft);
			var end = line.GetInsertionBufferPositionFromXCoordinate(xRight);
			if (start <= end)
				return new VirtualSnapshotSpan(start, end);
			return new VirtualSnapshotSpan(end, start);
		}
	}
}

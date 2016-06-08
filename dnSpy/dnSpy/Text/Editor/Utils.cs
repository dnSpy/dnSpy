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

using dnSpy.Contracts.Text;
using ICSharpCode.AvalonEdit;

namespace dnSpy.Text.Editor {
	static class Utils {
		public static int GetVirtualSpaces(DnSpyTextEditor dnSpyTextEditor, int offset, int visualColumn) {
			if (visualColumn <= 0)
				return 0;
			var docLine = dnSpyTextEditor.TextArea.TextView.Document.GetLineByOffset(offset);
			var visualLine = dnSpyTextEditor.TextArea.TextView.GetOrConstructVisualLine(docLine);
			int vspaces = visualColumn - visualLine.VisualLengthWithEndOfLineMarker;
			if (vspaces > 0)
				return vspaces;
			return 0;
		}

		public static TextViewPosition ToTextViewPosition(DnSpyTextEditor dnSpyTextEditor, VirtualSnapshotPoint point, bool isAtEndOfLine) {
			var docLine = dnSpyTextEditor.TextArea.TextView.Document.GetLineByOffset(point.Position.Position);
			var visualLine = dnSpyTextEditor.TextArea.TextView.GetOrConstructVisualLine(docLine);
			int column = point.Position.Position - docLine.Offset;
			int visualColumn = column < docLine.Length ?
				visualLine.GetVisualColumn(column) :
				visualLine.VisualLength + point.VirtualSpaces;
			return new TextViewPosition(docLine.LineNumber, column + 1, visualColumn) {
				IsAtEndOfLine = isAtEndOfLine,
			};
		}
	}
}

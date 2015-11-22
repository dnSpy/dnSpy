/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using ICSharpCode.AvalonEdit;

namespace dnSpy.Files.Tabs.TextEditor {
	sealed class EditorPositionState {
		public readonly double VerticalOffset;
		public readonly double HorizontalOffset;
		public readonly TextViewPosition TextViewPosition;
		public readonly double DesiredXPos;

		public EditorPositionState(ICSharpCode.AvalonEdit.TextEditor textEditor)
			: this(textEditor.VerticalOffset, textEditor.HorizontalOffset, textEditor.TextArea.Caret.Position, textEditor.TextArea.Caret.DesiredXPos) {
		}

		public EditorPositionState(double verticalOffset, double horizontalOffset, TextViewPosition textViewPosition, double DesiredXPos) {
			this.VerticalOffset = verticalOffset;
			this.HorizontalOffset = horizontalOffset;
			this.TextViewPosition = textViewPosition;
			this.DesiredXPos = DesiredXPos;
		}
	}
}

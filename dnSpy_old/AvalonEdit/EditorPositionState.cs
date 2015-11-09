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

using System;
using dnSpy.Contracts.Settings;
using ICSharpCode.AvalonEdit;

namespace dnSpy.AvalonEdit {
	public struct EditorPositionState : IEquatable<EditorPositionState> {
		public double VerticalOffset, HorizontalOffset;
		public TextViewPosition TextViewPosition;
		public double DesiredXPos;

		public EditorPositionState(TextEditor textEditor) : this(textEditor.VerticalOffset, textEditor.HorizontalOffset, textEditor.TextArea.Caret.Position, textEditor.TextArea.Caret.DesiredXPos) {
		}

		public EditorPositionState(double verticalOffset, double horizontalOffset, TextViewPosition textViewPosition, double DesiredXPos) {
			this.VerticalOffset = verticalOffset;
			this.HorizontalOffset = horizontalOffset;
			this.TextViewPosition = textViewPosition;
			this.DesiredXPos = DesiredXPos;
		}

		public void Write(ISettingsSection section) {
			section.Attribute("VerticalOffset", VerticalOffset);
			section.Attribute("HorizontalOffset", HorizontalOffset);
			section.Attribute("Line", TextViewPosition.Line);
			section.Attribute("Column", TextViewPosition.Column);
			section.Attribute("VisualColumn", TextViewPosition.VisualColumn);
			section.Attribute("IsAtEndOfLine", TextViewPosition.IsAtEndOfLine);
			section.Attribute("DesiredXPos", DesiredXPos);
		}

		public static EditorPositionState Read(ISettingsSection section) {
			var state = new EditorPositionState();
			if (section != null) {
				state.VerticalOffset = section.Attribute<double?>("VerticalOffset") ?? 0.0;
				state.HorizontalOffset = section.Attribute<double?>("HorizontalOffset") ?? 0.0;
				state.TextViewPosition = new TextViewPosition {
					Line = section.Attribute<int?>("Line") ?? 0,
					Column = section.Attribute<int?>("Column") ?? 0,
					VisualColumn = section.Attribute<int?>("VisualColumn") ?? 0,
					IsAtEndOfLine = section.Attribute<bool?>("IsAtEndOfLine") ?? false,
				};
				state.DesiredXPos = section.Attribute<double?>("DesiredXPos") ?? 0.0;
			}
			return state;
		}

		public bool Equals(EditorPositionState other) {
			return VerticalOffset == other.VerticalOffset &&
				HorizontalOffset == other.HorizontalOffset &&
				TextViewPosition == other.TextViewPosition &&
				DesiredXPos == other.DesiredXPos;
		}
	}
}

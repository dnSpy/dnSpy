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
using System.Xml.Linq;
using ICSharpCode.AvalonEdit;

namespace ICSharpCode.ILSpy.AvalonEdit {
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

		public XElement ToXml(XElement xml) {
			xml.SetAttributeValue("VerticalOffset", SessionSettings.ToString(VerticalOffset));
			xml.SetAttributeValue("HorizontalOffset", SessionSettings.ToString(HorizontalOffset));
			xml.SetAttributeValue("Line", SessionSettings.ToString(TextViewPosition.Line));
			xml.SetAttributeValue("Column", SessionSettings.ToString(TextViewPosition.Column));
			xml.SetAttributeValue("VisualColumn", SessionSettings.ToString(TextViewPosition.VisualColumn));
			xml.SetAttributeValue("IsAtEndOfLine", SessionSettings.ToString(TextViewPosition.IsAtEndOfLine));
			xml.SetAttributeValue("DesiredXPos", SessionSettings.ToString(DesiredXPos));
			return xml;
		}

		public static EditorPositionState FromXml(XElement doc) {
			var state = new EditorPositionState();
			if (doc != null) {
				state.VerticalOffset = SessionSettings.FromString((string)doc.Attribute("VerticalOffset"), 0.0);
				state.HorizontalOffset = SessionSettings.FromString((string)doc.Attribute("HorizontalOffset"), 0.0);
				state.TextViewPosition = new TextViewPosition {
					Line = SessionSettings.FromString((string)doc.Attribute("Line"), 0),
					Column = SessionSettings.FromString((string)doc.Attribute("Column"), 0),
					VisualColumn = SessionSettings.FromString((string)doc.Attribute("VisualColumn"), 0),
					IsAtEndOfLine = SessionSettings.FromString((string)doc.Attribute("IsAtEndOfLine"), false),
				};
				state.DesiredXPos = SessionSettings.FromString((string)doc.Attribute("DesiredXPos"), 0.0);
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

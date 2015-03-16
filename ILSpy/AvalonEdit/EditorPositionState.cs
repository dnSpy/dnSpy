using System;
using System.Xml.Linq;
using ICSharpCode.AvalonEdit;

namespace ICSharpCode.ILSpy.AvalonEdit
{
	public struct EditorPositionState : IEquatable<EditorPositionState>
	{
		public double VerticalOffset, HorizontalOffset;
		public TextViewPosition TextViewPosition;
		public double DesiredXPos;

		public EditorPositionState(TextEditor textEditor) : this(textEditor.VerticalOffset, textEditor.HorizontalOffset, textEditor.TextArea.Caret.Position, textEditor.TextArea.Caret.DesiredXPos)
		{
		}

		public EditorPositionState(double verticalOffset, double horizontalOffset, TextViewPosition textViewPosition, double DesiredXPos)
		{
			this.VerticalOffset = verticalOffset;
			this.HorizontalOffset = horizontalOffset;
			this.TextViewPosition = textViewPosition;
			this.DesiredXPos = DesiredXPos;
		}

		public XElement ToXml(XElement xml)
		{
			xml.SetAttributeValue("VerticalOffset", SessionSettings.ToString(VerticalOffset));
			xml.SetAttributeValue("HorizontalOffset", SessionSettings.ToString(HorizontalOffset));
			xml.SetAttributeValue("Line", SessionSettings.ToString(TextViewPosition.Line));
			xml.SetAttributeValue("Column", SessionSettings.ToString(TextViewPosition.Column));
			xml.SetAttributeValue("VisualColumn", SessionSettings.ToString(TextViewPosition.VisualColumn));
			xml.SetAttributeValue("IsAtEndOfLine", SessionSettings.ToString(TextViewPosition.IsAtEndOfLine));
			xml.SetAttributeValue("DesiredXPos", SessionSettings.ToString(DesiredXPos));
			return xml;
		}

		public static EditorPositionState FromXml(XElement doc)
		{
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

		public bool Equals(EditorPositionState other)
		{
			return VerticalOffset == other.VerticalOffset &&
				HorizontalOffset == other.HorizontalOffset &&
				TextViewPosition == other.TextViewPosition &&
				DesiredXPos == other.DesiredXPos;
		}
	}
}

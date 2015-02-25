// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Editing
{
	enum CaretMovementType
	{
		None,
		CharLeft,
		CharRight,
		Backspace,
		WordLeft,
		WordRight,
		LineUp,
		LineDown,
		PageUp,
		PageDown,
		LineStart,
		LineEnd,
		DocumentStart,
		DocumentEnd
	}
	
	static class CaretNavigationCommandHandler
	{
		/// <summary>
		/// Creates a new <see cref="TextAreaInputHandler"/> for the text area.
		/// </summary>
		public static TextAreaInputHandler Create(TextArea textArea)
		{
			TextAreaInputHandler handler = new TextAreaInputHandler(textArea);
			handler.CommandBindings.AddRange(CommandBindings);
			handler.InputBindings.AddRange(InputBindings);
			return handler;
		}
		
		static readonly List<CommandBinding> CommandBindings = new List<CommandBinding>();
		static readonly List<InputBinding> InputBindings = new List<InputBinding>();
		
		static void AddBinding(ICommand command, ModifierKeys modifiers, Key key, ExecutedRoutedEventHandler handler)
		{
			CommandBindings.Add(new CommandBinding(command, handler));
			InputBindings.Add(TextAreaDefaultInputHandler.CreateFrozenKeyBinding(command, modifiers, key));
		}
		
		static CaretNavigationCommandHandler()
		{
			const ModifierKeys None = ModifierKeys.None;
			const ModifierKeys Ctrl = ModifierKeys.Control;
			const ModifierKeys Shift = ModifierKeys.Shift;
			const ModifierKeys Alt = ModifierKeys.Alt;
			
			AddBinding(EditingCommands.MoveLeftByCharacter, None, Key.Left, OnMoveCaret(CaretMovementType.CharLeft));
			AddBinding(EditingCommands.SelectLeftByCharacter, Shift, Key.Left, OnMoveCaretExtendSelection(CaretMovementType.CharLeft));
			AddBinding(RectangleSelection.BoxSelectLeftByCharacter, Alt | Shift, Key.Left, OnMoveCaretBoxSelection(CaretMovementType.CharLeft));
			AddBinding(EditingCommands.MoveRightByCharacter, None, Key.Right, OnMoveCaret(CaretMovementType.CharRight));
			AddBinding(EditingCommands.SelectRightByCharacter, Shift, Key.Right, OnMoveCaretExtendSelection(CaretMovementType.CharRight));
			AddBinding(RectangleSelection.BoxSelectRightByCharacter, Alt | Shift, Key.Right, OnMoveCaretBoxSelection(CaretMovementType.CharRight));
			
			AddBinding(EditingCommands.MoveLeftByWord, Ctrl, Key.Left, OnMoveCaret(CaretMovementType.WordLeft));
			AddBinding(EditingCommands.SelectLeftByWord, Ctrl | Shift, Key.Left, OnMoveCaretExtendSelection(CaretMovementType.WordLeft));
			AddBinding(RectangleSelection.BoxSelectLeftByWord, Ctrl | Alt | Shift, Key.Left, OnMoveCaretBoxSelection(CaretMovementType.WordLeft));
			AddBinding(EditingCommands.MoveRightByWord, Ctrl, Key.Right, OnMoveCaret(CaretMovementType.WordRight));
			AddBinding(EditingCommands.SelectRightByWord, Ctrl | Shift, Key.Right, OnMoveCaretExtendSelection(CaretMovementType.WordRight));
			AddBinding(RectangleSelection.BoxSelectRightByWord, Ctrl | Alt | Shift, Key.Right, OnMoveCaretBoxSelection(CaretMovementType.WordRight));
			
			AddBinding(EditingCommands.MoveUpByLine, None, Key.Up, OnMoveCaret(CaretMovementType.LineUp));
			AddBinding(EditingCommands.SelectUpByLine, Shift, Key.Up, OnMoveCaretExtendSelection(CaretMovementType.LineUp));
			AddBinding(RectangleSelection.BoxSelectUpByLine, Alt | Shift, Key.Up, OnMoveCaretBoxSelection(CaretMovementType.LineUp));
			AddBinding(EditingCommands.MoveDownByLine, None, Key.Down, OnMoveCaret(CaretMovementType.LineDown));
			AddBinding(EditingCommands.SelectDownByLine, Shift, Key.Down, OnMoveCaretExtendSelection(CaretMovementType.LineDown));
			AddBinding(RectangleSelection.BoxSelectDownByLine, Alt | Shift, Key.Down, OnMoveCaretBoxSelection(CaretMovementType.LineDown));
			
			AddBinding(EditingCommands.MoveDownByPage, None, Key.PageDown, OnMoveCaret(CaretMovementType.PageDown));
			AddBinding(EditingCommands.SelectDownByPage, Shift, Key.PageDown, OnMoveCaretExtendSelection(CaretMovementType.PageDown));
			AddBinding(EditingCommands.MoveUpByPage, None, Key.PageUp, OnMoveCaret(CaretMovementType.PageUp));
			AddBinding(EditingCommands.SelectUpByPage, Shift, Key.PageUp, OnMoveCaretExtendSelection(CaretMovementType.PageUp));
			
			AddBinding(EditingCommands.MoveToLineStart, None, Key.Home, OnMoveCaret(CaretMovementType.LineStart));
			AddBinding(EditingCommands.SelectToLineStart, Shift, Key.Home, OnMoveCaretExtendSelection(CaretMovementType.LineStart));
			AddBinding(RectangleSelection.BoxSelectToLineStart, Alt | Shift, Key.Home, OnMoveCaretBoxSelection(CaretMovementType.LineStart));
			AddBinding(EditingCommands.MoveToLineEnd, None, Key.End, OnMoveCaret(CaretMovementType.LineEnd));
			AddBinding(EditingCommands.SelectToLineEnd, Shift, Key.End, OnMoveCaretExtendSelection(CaretMovementType.LineEnd));
			AddBinding(RectangleSelection.BoxSelectToLineEnd, Alt | Shift, Key.End, OnMoveCaretBoxSelection(CaretMovementType.LineEnd));
			
			AddBinding(EditingCommands.MoveToDocumentStart, Ctrl, Key.Home, OnMoveCaret(CaretMovementType.DocumentStart));
			AddBinding(EditingCommands.SelectToDocumentStart, Ctrl | Shift, Key.Home, OnMoveCaretExtendSelection(CaretMovementType.DocumentStart));
			AddBinding(EditingCommands.MoveToDocumentEnd, Ctrl, Key.End, OnMoveCaret(CaretMovementType.DocumentEnd));
			AddBinding(EditingCommands.SelectToDocumentEnd, Ctrl | Shift, Key.End, OnMoveCaretExtendSelection(CaretMovementType.DocumentEnd));
			
			CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, OnSelectAll));
			
			TextAreaDefaultInputHandler.WorkaroundWPFMemoryLeak(InputBindings);
		}
		
		static void OnSelectAll(object target, ExecutedRoutedEventArgs args)
		{
			TextArea textArea = GetTextArea(target);
			if (textArea != null && textArea.Document != null) {
				args.Handled = true;
				textArea.Caret.Offset = textArea.Document.TextLength;
				textArea.Selection = SimpleSelection.Create(textArea, 0, textArea.Document.TextLength);
			}
		}
		
		static TextArea GetTextArea(object target)
		{
			return target as TextArea;
		}
		
		static ExecutedRoutedEventHandler OnMoveCaret(CaretMovementType direction)
		{
			return (target, args) => {
				TextArea textArea = GetTextArea(target);
				if (textArea != null && textArea.Document != null) {
					args.Handled = true;
					textArea.ClearSelection();
					MoveCaret(textArea, direction);
					textArea.Caret.BringCaretToView();
				}
			};
		}
		
		static ExecutedRoutedEventHandler OnMoveCaretExtendSelection(CaretMovementType direction)
		{
			return (target, args) => {
				TextArea textArea = GetTextArea(target);
				if (textArea != null && textArea.Document != null) {
					args.Handled = true;
					TextViewPosition oldPosition = textArea.Caret.Position;
					MoveCaret(textArea, direction);
					textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldPosition, textArea.Caret.Position);
					textArea.Caret.BringCaretToView();
				}
			};
		}
		
		static ExecutedRoutedEventHandler OnMoveCaretBoxSelection(CaretMovementType direction)
		{
			return (target, args) => {
				TextArea textArea = GetTextArea(target);
				if (textArea != null && textArea.Document != null) {
					args.Handled = true;
					// First, convert the selection into a rectangle selection
					// (this is required so that virtual space gets enabled for the caret movement)
					if (textArea.Options.EnableRectangularSelection && !(textArea.Selection is RectangleSelection)) {
						if (textArea.Selection.IsEmpty) {
							textArea.Selection = new RectangleSelection(textArea, textArea.Caret.Position, textArea.Caret.Position);
						} else {
							// Convert normal selection to rectangle selection
							textArea.Selection = new RectangleSelection(textArea, textArea.Selection.StartPosition, textArea.Caret.Position);
						}
					}
					// Now move the caret and extend the selection
					TextViewPosition oldPosition = textArea.Caret.Position;
					MoveCaret(textArea, direction);
					textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldPosition, textArea.Caret.Position);
					textArea.Caret.BringCaretToView();
				}
			};
		}
		
		#region Caret movement
		internal static void MoveCaret(TextArea textArea, CaretMovementType direction)
		{
			double desiredXPos = textArea.Caret.DesiredXPos;
			textArea.Caret.Position = GetNewCaretPosition(textArea.TextView, textArea.Caret.Position, direction, textArea.Selection.EnableVirtualSpace, ref desiredXPos);
			textArea.Caret.DesiredXPos = desiredXPos;
		}
		
		internal static TextViewPosition GetNewCaretPosition(TextView textView, TextViewPosition caretPosition, CaretMovementType direction, bool enableVirtualSpace, ref double desiredXPos)
		{
			switch (direction) {
				case CaretMovementType.None:
					return caretPosition;
				case CaretMovementType.DocumentStart:
					desiredXPos = double.NaN;
					return new TextViewPosition(0, 0);
				case CaretMovementType.DocumentEnd:
					desiredXPos = double.NaN;
					return new TextViewPosition(textView.Document.GetLocation(textView.Document.TextLength));
			}
			DocumentLine caretLine = textView.Document.GetLineByNumber(caretPosition.Line);
			VisualLine visualLine = textView.GetOrConstructVisualLine(caretLine);
			TextLine textLine = visualLine.GetTextLine(caretPosition.VisualColumn, caretPosition.IsAtEndOfLine);
			switch (direction) {
				case CaretMovementType.CharLeft:
					desiredXPos = double.NaN;
					return GetPrevCaretPosition(textView, caretPosition, visualLine, CaretPositioningMode.Normal, enableVirtualSpace);
				case CaretMovementType.Backspace:
					desiredXPos = double.NaN;
					return GetPrevCaretPosition(textView, caretPosition, visualLine, CaretPositioningMode.EveryCodepoint, enableVirtualSpace);
				case CaretMovementType.CharRight:
					desiredXPos = double.NaN;
					return GetNextCaretPosition(textView, caretPosition, visualLine, CaretPositioningMode.Normal, enableVirtualSpace);
				case CaretMovementType.WordLeft:
					desiredXPos = double.NaN;
					return GetPrevCaretPosition(textView, caretPosition, visualLine, CaretPositioningMode.WordStart, enableVirtualSpace);
				case CaretMovementType.WordRight:
					desiredXPos = double.NaN;
					return GetNextCaretPosition(textView, caretPosition, visualLine, CaretPositioningMode.WordStart, enableVirtualSpace);
				case CaretMovementType.LineUp:
				case CaretMovementType.LineDown:
				case CaretMovementType.PageUp:
				case CaretMovementType.PageDown:
					return GetUpDownCaretPosition(textView, caretPosition, direction, visualLine, textLine, enableVirtualSpace, ref desiredXPos);
				case CaretMovementType.LineStart:
					desiredXPos = double.NaN;
					return GetStartOfLineCaretPosition(caretPosition.VisualColumn, visualLine, textLine, enableVirtualSpace);
				case CaretMovementType.LineEnd:
					desiredXPos = double.NaN;
					return GetEndOfLineCaretPosition(visualLine, textLine);
				default:
					throw new NotSupportedException(direction.ToString());
			}
		}
		#endregion
		
		#region Home/End
		static TextViewPosition GetStartOfLineCaretPosition(int oldVC, VisualLine visualLine, TextLine textLine, bool enableVirtualSpace)
		{
			int newVC = visualLine.GetTextLineVisualStartColumn(textLine);
			if (newVC == 0)
				newVC = visualLine.GetNextCaretPosition(newVC - 1, LogicalDirection.Forward, CaretPositioningMode.WordStart, enableVirtualSpace);
			if (newVC < 0)
				throw ThrowUtil.NoValidCaretPosition();
			// when the caret is already at the start of the text, jump to start before whitespace
			if (newVC == oldVC)
				newVC = 0;
			return visualLine.GetTextViewPosition(newVC);
		}
		
		static TextViewPosition GetEndOfLineCaretPosition(VisualLine visualLine, TextLine textLine)
		{
			int newVC = visualLine.GetTextLineVisualStartColumn(textLine) + textLine.Length - textLine.TrailingWhitespaceLength;
			TextViewPosition pos = visualLine.GetTextViewPosition(newVC);
			pos.IsAtEndOfLine = true;
			return pos;
		}
		#endregion
		
		#region By-character / By-word movement
		static TextViewPosition GetNextCaretPosition(TextView textView, TextViewPosition caretPosition, VisualLine visualLine, CaretPositioningMode mode, bool enableVirtualSpace)
		{
			int pos = visualLine.GetNextCaretPosition(caretPosition.VisualColumn, LogicalDirection.Forward, mode, enableVirtualSpace);
			if (pos >= 0) {
				return visualLine.GetTextViewPosition(pos);
			} else {
				// move to start of next line
				DocumentLine nextDocumentLine = visualLine.LastDocumentLine.NextLine;
				if (nextDocumentLine != null) {
					VisualLine nextLine = textView.GetOrConstructVisualLine(nextDocumentLine);
					pos = nextLine.GetNextCaretPosition(-1, LogicalDirection.Forward, mode, enableVirtualSpace);
					if (pos < 0)
						throw ThrowUtil.NoValidCaretPosition();
					return nextLine.GetTextViewPosition(pos);
				} else {
					// at end of document
					Debug.Assert(visualLine.LastDocumentLine.Offset + visualLine.LastDocumentLine.TotalLength == textView.Document.TextLength);
					return new TextViewPosition(textView.Document.GetLocation(textView.Document.TextLength));
				}
			}
		}
		
		static TextViewPosition GetPrevCaretPosition(TextView textView, TextViewPosition caretPosition, VisualLine visualLine, CaretPositioningMode mode, bool enableVirtualSpace)
		{
			int pos = visualLine.GetNextCaretPosition(caretPosition.VisualColumn, LogicalDirection.Backward, mode, enableVirtualSpace);
			if (pos >= 0) {
				return visualLine.GetTextViewPosition(pos);
			} else {
				// move to end of previous line
				DocumentLine previousDocumentLine = visualLine.FirstDocumentLine.PreviousLine;
				if (previousDocumentLine != null) {
					VisualLine previousLine = textView.GetOrConstructVisualLine(previousDocumentLine);
					pos = previousLine.GetNextCaretPosition(previousLine.VisualLength + 1, LogicalDirection.Backward, mode, enableVirtualSpace);
					if (pos < 0)
						throw ThrowUtil.NoValidCaretPosition();
					return previousLine.GetTextViewPosition(pos);
				} else {
					// at start of document
					Debug.Assert(visualLine.FirstDocumentLine.Offset == 0);
					return new TextViewPosition(0, 0);
				}
			}
		}
		#endregion

		#region Line+Page up/down
		static TextViewPosition GetUpDownCaretPosition(TextView textView, TextViewPosition caretPosition, CaretMovementType direction, VisualLine visualLine, TextLine textLine, bool enableVirtualSpace, ref double xPos)
		{
			// moving up/down happens using the desired visual X position
			if (double.IsNaN(xPos))
				xPos = visualLine.GetTextLineVisualXPosition(textLine, caretPosition.VisualColumn);
			// now find the TextLine+VisualLine where the caret will end up in
			VisualLine targetVisualLine = visualLine;
			TextLine targetLine;
			int textLineIndex = visualLine.TextLines.IndexOf(textLine);
			switch (direction) {
				case CaretMovementType.LineUp:
					{
						// Move up: move to the previous TextLine in the same visual line
						// or move to the last TextLine of the previous visual line
						int prevLineNumber = visualLine.FirstDocumentLine.LineNumber - 1;
						if (textLineIndex > 0) {
							targetLine = visualLine.TextLines[textLineIndex - 1];
						} else if (prevLineNumber >= 1) {
							DocumentLine prevLine = textView.Document.GetLineByNumber(prevLineNumber);
							targetVisualLine = textView.GetOrConstructVisualLine(prevLine);
							targetLine = targetVisualLine.TextLines[targetVisualLine.TextLines.Count - 1];
						} else {
							targetLine = null;
						}
						break;
					}
				case CaretMovementType.LineDown:
					{
						// Move down: move to the next TextLine in the same visual line
						// or move to the first TextLine of the next visual line
						int nextLineNumber = visualLine.LastDocumentLine.LineNumber + 1;
						if (textLineIndex < visualLine.TextLines.Count - 1) {
							targetLine = visualLine.TextLines[textLineIndex + 1];
						} else if (nextLineNumber <= textView.Document.LineCount) {
							DocumentLine nextLine = textView.Document.GetLineByNumber(nextLineNumber);
							targetVisualLine = textView.GetOrConstructVisualLine(nextLine);
							targetLine = targetVisualLine.TextLines[0];
						} else {
							targetLine = null;
						}
						break;
					}
				case CaretMovementType.PageUp:
				case CaretMovementType.PageDown:
					{
						// Page up/down: find the target line using its visual position
						double yPos = visualLine.GetTextLineVisualYPosition(textLine, VisualYPosition.LineMiddle);
						if (direction == CaretMovementType.PageUp)
							yPos -= textView.RenderSize.Height;
						else
							yPos += textView.RenderSize.Height;
						DocumentLine newLine = textView.GetDocumentLineByVisualTop(yPos);
						targetVisualLine = textView.GetOrConstructVisualLine(newLine);
						targetLine = targetVisualLine.GetTextLineByVisualYPosition(yPos);
						break;
					}
				default:
					throw new NotSupportedException(direction.ToString());
			}
			if (targetLine != null) {
				double yPos = targetVisualLine.GetTextLineVisualYPosition(targetLine, VisualYPosition.LineMiddle);
				int newVisualColumn = targetVisualLine.GetVisualColumn(new Point(xPos, yPos), enableVirtualSpace);
				
				// prevent wrapping to the next line; TODO: could 'IsAtEnd' help here?
				int targetLineStartCol = targetVisualLine.GetTextLineVisualStartColumn(targetLine);
				if (newVisualColumn >= targetLineStartCol + targetLine.Length) {
					if (newVisualColumn <= targetVisualLine.VisualLength)
						newVisualColumn = targetLineStartCol + targetLine.Length - 1;
				}
				return targetVisualLine.GetTextViewPosition(newVisualColumn);
			} else {
				return caretPosition;
			}
		}
		#endregion
	}
}

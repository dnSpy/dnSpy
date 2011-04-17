// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Handles selection of text using the mouse.
	/// </summary>
	sealed class SelectionMouseHandler : ITextAreaInputHandler
	{
		#region enum SelectionMode
		enum SelectionMode
		{
			/// <summary>
			/// no selection (no mouse button down)
			/// </summary>
			None,
			/// <summary>
			/// left mouse button down on selection, might be normal click
			/// or might be drag'n'drop
			/// </summary>
			PossibleDragStart,
			/// <summary>
			/// dragging text
			/// </summary>
			Drag,
			/// <summary>
			/// normal selection (click+drag)
			/// </summary>
			Normal,
			/// <summary>
			/// whole-word selection (double click+drag or ctrl+click+drag)
			/// </summary>
			WholeWord,
			/// <summary>
			/// whole-line selection (triple click+drag)
			/// </summary>
			WholeLine,
			/// <summary>
			/// rectangular selection (alt+click+drag)
			/// </summary>
			Rectangular
		}
		#endregion
		
		readonly TextArea textArea;
		
		SelectionMode mode;
		AnchorSegment startWord;
		Point possibleDragStartMousePos;
		
		#region Constructor + Attach + Detach
		public SelectionMouseHandler(TextArea textArea)
		{
			if (textArea == null)
				throw new ArgumentNullException("textArea");
			this.textArea = textArea;
		}
		
		public TextArea TextArea {
			get { return textArea; }
		}
		
		public void Attach()
		{
			textArea.MouseLeftButtonDown += textArea_MouseLeftButtonDown;
			textArea.MouseMove += textArea_MouseMove;
			textArea.MouseLeftButtonUp += textArea_MouseLeftButtonUp;
			textArea.QueryCursor += textArea_QueryCursor;
			textArea.OptionChanged += textArea_OptionChanged;
			
			enableTextDragDrop = textArea.Options.EnableTextDragDrop;
			if (enableTextDragDrop) {
				AttachDragDrop();
			}
		}
		
		public void Detach()
		{
			mode = SelectionMode.None;
			textArea.MouseLeftButtonDown -= textArea_MouseLeftButtonDown;
			textArea.MouseMove -= textArea_MouseMove;
			textArea.MouseLeftButtonUp -= textArea_MouseLeftButtonUp;
			textArea.QueryCursor -= textArea_QueryCursor;
			textArea.OptionChanged -= textArea_OptionChanged;
			if (enableTextDragDrop) {
				DetachDragDrop();
			}
		}
		
		void AttachDragDrop()
		{
			textArea.AllowDrop = true;
			textArea.GiveFeedback += textArea_GiveFeedback;
			textArea.QueryContinueDrag += textArea_QueryContinueDrag;
			textArea.DragEnter += textArea_DragEnter;
			textArea.DragOver += textArea_DragOver;
			textArea.DragLeave += textArea_DragLeave;
			textArea.Drop += textArea_Drop;
		}
		
		void DetachDragDrop()
		{
			textArea.AllowDrop = false;
			textArea.GiveFeedback -= textArea_GiveFeedback;
			textArea.QueryContinueDrag -= textArea_QueryContinueDrag;
			textArea.DragEnter -= textArea_DragEnter;
			textArea.DragOver -= textArea_DragOver;
			textArea.DragLeave -= textArea_DragLeave;
			textArea.Drop -= textArea_Drop;
		}
		
		bool enableTextDragDrop;
		
		void textArea_OptionChanged(object sender, PropertyChangedEventArgs e)
		{
			bool newEnableTextDragDrop = textArea.Options.EnableTextDragDrop;
			if (newEnableTextDragDrop != enableTextDragDrop) {
				enableTextDragDrop = newEnableTextDragDrop;
				if (newEnableTextDragDrop)
					AttachDragDrop();
				else
					DetachDragDrop();
			}
		}
		#endregion
		
		#region Dropping text
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		void textArea_DragEnter(object sender, DragEventArgs e)
		{
			try {
				e.Effects = GetEffect(e);
				textArea.Caret.Show();
			} catch (Exception ex) {
				OnDragException(ex);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		void textArea_DragOver(object sender, DragEventArgs e)
		{
			try {
				e.Effects = GetEffect(e);
			} catch (Exception ex) {
				OnDragException(ex);
			}
		}
		
		DragDropEffects GetEffect(DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.UnicodeText, true)) {
				e.Handled = true;
				int visualColumn;
				int offset = GetOffsetFromMousePosition(e.GetPosition(textArea.TextView), out visualColumn);
				if (offset >= 0) {
					textArea.Caret.Position = new TextViewPosition(textArea.Document.GetLocation(offset), visualColumn);
					textArea.Caret.DesiredXPos = double.NaN;
					if (textArea.ReadOnlySectionProvider.CanInsert(offset)) {
						if ((e.AllowedEffects & DragDropEffects.Move) == DragDropEffects.Move
						    && (e.KeyStates & DragDropKeyStates.ControlKey) != DragDropKeyStates.ControlKey)
						{
							return DragDropEffects.Move;
						} else {
							return e.AllowedEffects & DragDropEffects.Copy;
						}
					}
				}
			}
			return DragDropEffects.None;
		}
		
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		void textArea_DragLeave(object sender, DragEventArgs e)
		{
			try {
				e.Handled = true;
				if (!textArea.IsKeyboardFocusWithin)
					textArea.Caret.Hide();
			} catch (Exception ex) {
				OnDragException(ex);
			}
		}
		
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		void textArea_Drop(object sender, DragEventArgs e)
		{
			try {
				DragDropEffects effect = GetEffect(e);
				e.Effects = effect;
				if (effect != DragDropEffects.None) {
					string text = e.Data.GetData(DataFormats.UnicodeText, true) as string;
					if (text != null) {
						int start = textArea.Caret.Offset;
						if (mode == SelectionMode.Drag && textArea.Selection.Contains(start)) {
							Debug.WriteLine("Drop: did not drop: drop target is inside selection");
							e.Effects = DragDropEffects.None;
						} else {
							Debug.WriteLine("Drop: insert at " + start);
							
							bool rectangular = e.Data.GetDataPresent(RectangleSelection.RectangularSelectionDataType);
							
							string newLine = TextUtilities.GetNewLineFromDocument(textArea.Document, textArea.Caret.Line);
							text = TextUtilities.NormalizeNewLines(text, newLine);
							
							// Mark the undo group with the currentDragDescriptor, if the drag
							// is originating from the same control. This allows combining
							// the undo groups when text is moved.
							textArea.Document.UndoStack.StartUndoGroup(this.currentDragDescriptor);
							try {
								if (rectangular && RectangleSelection.PerformRectangularPaste(textArea, start, text, true)) {
									
								} else {
									textArea.Document.Insert(start, text);
									textArea.Selection = new SimpleSelection(start, start + text.Length);
								}
							} finally {
								textArea.Document.UndoStack.EndUndoGroup();
							}
						}
						e.Handled = true;
					}
				}
			} catch (Exception ex) {
				OnDragException(ex);
			}
		}
		
		void OnDragException(Exception ex)
		{
			// WPF swallows exceptions during drag'n'drop or reports them incorrectly, so
			// we re-throw them later to allow the application's unhandled exception handler
			// to catch them
			textArea.Dispatcher.BeginInvoke(
				DispatcherPriority.Normal,
				new Action(delegate {
				           	throw new DragDropException("Exception during drag'n'drop", ex);
				           }));
		}
		
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		void textArea_GiveFeedback(object sender, GiveFeedbackEventArgs e)
		{
			try {
				e.UseDefaultCursors = true;
				e.Handled = true;
			} catch (Exception ex) {
				OnDragException(ex);
			}
		}
		
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		void textArea_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
		{
			try {
				if (e.EscapePressed) {
					e.Action = DragAction.Cancel;
				} else if ((e.KeyStates & DragDropKeyStates.LeftMouseButton) != DragDropKeyStates.LeftMouseButton) {
					e.Action = DragAction.Drop;
				} else {
					e.Action = DragAction.Continue;
				}
				e.Handled = true;
			} catch (Exception ex) {
				OnDragException(ex);
			}
		}
		#endregion
		
		#region Start Drag
		object currentDragDescriptor;
		
		void StartDrag()
		{
			// prevent nested StartDrag calls
			mode = SelectionMode.Drag;
			
			// mouse capture and Drag'n'Drop doesn't mix
			textArea.ReleaseMouseCapture();
			
			DataObject dataObject = textArea.Selection.CreateDataObject(textArea);
			
			DragDropEffects allowedEffects = DragDropEffects.All;
			var deleteOnMove = textArea.Selection.Segments.Select(s => new AnchorSegment(textArea.Document, s)).ToList();
			foreach (ISegment s in deleteOnMove) {
				ISegment[] result = textArea.GetDeletableSegments(s);
				if (result.Length != 1 || result[0].Offset != s.Offset || result[0].EndOffset != s.EndOffset) {
					allowedEffects &= ~DragDropEffects.Move;
				}
			}
			
			object dragDescriptor = new object();
			this.currentDragDescriptor = dragDescriptor;
			
			DragDropEffects resultEffect;
			using (textArea.AllowCaretOutsideSelection()) {
				var oldCaretPosition = textArea.Caret.Position;
				try {
					Debug.WriteLine("DoDragDrop with allowedEffects=" + allowedEffects);
					resultEffect = DragDrop.DoDragDrop(textArea, dataObject, allowedEffects);
					Debug.WriteLine("DoDragDrop done, resultEffect=" + resultEffect);
				} catch (COMException ex) {
					// ignore COM errors - don't crash on badly implemented drop targets
					Debug.WriteLine("DoDragDrop failed: " + ex.ToString());
					return;
				}
				if (resultEffect == DragDropEffects.None) {
					// reset caret if drag was aborted
					textArea.Caret.Position = oldCaretPosition;
				}
			}
			
			this.currentDragDescriptor = null;
			
			if (deleteOnMove != null && resultEffect == DragDropEffects.Move && (allowedEffects & DragDropEffects.Move) == DragDropEffects.Move) {
				bool draggedInsideSingleDocument = (dragDescriptor == textArea.Document.UndoStack.LastGroupDescriptor);
				if (draggedInsideSingleDocument)
					textArea.Document.UndoStack.StartContinuedUndoGroup(null);
				textArea.Document.BeginUpdate();
				try {
					foreach (ISegment s in deleteOnMove) {
						textArea.Document.Remove(s.Offset, s.Length);
					}
				} finally {
					textArea.Document.EndUpdate();
					if (draggedInsideSingleDocument)
						textArea.Document.UndoStack.EndUndoGroup();
				}
			}
		}
		#endregion
		
		#region QueryCursor
		// provide the IBeam Cursor for the text area
		void textArea_QueryCursor(object sender, QueryCursorEventArgs e)
		{
			if (!e.Handled) {
				if (mode != SelectionMode.None || !enableTextDragDrop) {
					e.Cursor = Cursors.IBeam;
					e.Handled = true;
				} else if (textArea.TextView.VisualLinesValid) {
					// Only query the cursor if the visual lines are valid.
					// If they are invalid, the cursor will get re-queried when the visual lines
					// get refreshed.
					Point p = e.GetPosition(textArea.TextView);
					if (p.X >= 0 && p.Y >= 0 && p.X <= textArea.TextView.ActualWidth && p.Y <= textArea.TextView.ActualHeight) {
						int visualColumn;
						int offset = GetOffsetFromMousePosition(e, out visualColumn);
						if (textArea.Selection.Contains(offset))
							e.Cursor = Cursors.Arrow;
						else
							e.Cursor = Cursors.IBeam;
						e.Handled = true;
					}
				}
			}
		}
		#endregion
		
		#region LeftButtonDown
		void textArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			mode = SelectionMode.None;
			if (!e.Handled && e.ChangedButton == MouseButton.Left) {
				ModifierKeys modifiers = Keyboard.Modifiers;
				bool shift = (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
				if (enableTextDragDrop && e.ClickCount == 1 && !shift) {
					int visualColumn;
					int offset = GetOffsetFromMousePosition(e, out visualColumn);
					if (textArea.Selection.Contains(offset)) {
						if (textArea.CaptureMouse()) {
							mode = SelectionMode.PossibleDragStart;
							possibleDragStartMousePos = e.GetPosition(textArea);
						}
						e.Handled = true;
						return;
					}
				}
				
				int oldOffset = textArea.Caret.Offset;
				SetCaretOffsetToMousePosition(e);
				
				
				if (!shift) {
					textArea.Selection = Selection.Empty;
				}
				if (textArea.CaptureMouse()) {
					if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && textArea.Options.EnableRectangularSelection) {
						mode = SelectionMode.Rectangular;
						if (shift && textArea.Selection is RectangleSelection) {
							textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldOffset, textArea.Caret.Offset);
						}
					} else if (e.ClickCount == 1 && ((modifiers & ModifierKeys.Control) == 0)) {
						mode = SelectionMode.Normal;
						if (shift && !(textArea.Selection is RectangleSelection)) {
							textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldOffset, textArea.Caret.Offset);
						}
					} else {
						SimpleSegment startWord;
						if (e.ClickCount == 3) {
							mode = SelectionMode.WholeLine;
							startWord = GetLineAtMousePosition(e);
						} else {
							mode = SelectionMode.WholeWord;
							startWord = GetWordAtMousePosition(e);
						}
						if (startWord == SimpleSegment.Invalid) {
							mode = SelectionMode.None;
							textArea.ReleaseMouseCapture();
							return;
						}
						if (shift && !textArea.Selection.IsEmpty) {
							if (startWord.Offset < textArea.Selection.SurroundingSegment.Offset) {
								textArea.Selection = textArea.Selection.SetEndpoint(startWord.Offset);
							} else if (startWord.EndOffset > textArea.Selection.SurroundingSegment.EndOffset) {
								textArea.Selection = textArea.Selection.SetEndpoint(startWord.EndOffset);
							}
							this.startWord = new AnchorSegment(textArea.Document, textArea.Selection.SurroundingSegment);
						} else {
							textArea.Selection = new SimpleSelection(startWord.Offset, startWord.EndOffset);
							this.startWord = new AnchorSegment(textArea.Document, startWord.Offset, startWord.Length);
						}
					}
				}
			}
			e.Handled = true;
		}
		#endregion
		
		#region Mouse Position <-> Text coordinates
		SimpleSegment GetWordAtMousePosition(MouseEventArgs e)
		{
			TextView textView = textArea.TextView;
			if (textView == null) return SimpleSegment.Invalid;
			Point pos = e.GetPosition(textView);
			if (pos.Y < 0)
				pos.Y = 0;
			if (pos.Y > textView.ActualHeight)
				pos.Y = textView.ActualHeight;
			pos += textView.ScrollOffset;
			VisualLine line = textView.GetVisualLineFromVisualTop(pos.Y);
			if (line != null) {
				int visualColumn = line.GetVisualColumn(pos);
				int wordStartVC = line.GetNextCaretPosition(visualColumn + 1, LogicalDirection.Backward, CaretPositioningMode.WordStartOrSymbol);
				if (wordStartVC == -1)
					wordStartVC = 0;
				int wordEndVC = line.GetNextCaretPosition(wordStartVC, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol);
				if (wordEndVC == -1)
					wordEndVC = line.VisualLength;
				int relOffset = line.FirstDocumentLine.Offset;
				int wordStartOffset = line.GetRelativeOffset(wordStartVC) + relOffset;
				int wordEndOffset = line.GetRelativeOffset(wordEndVC) + relOffset;
				return new SimpleSegment(wordStartOffset, wordEndOffset - wordStartOffset);
			} else {
				return SimpleSegment.Invalid;
			}
		}
		
		SimpleSegment GetLineAtMousePosition(MouseEventArgs e)
		{
			TextView textView = textArea.TextView;
			if (textView == null) return SimpleSegment.Invalid;
			Point pos = e.GetPosition(textView);
			if (pos.Y < 0)
				pos.Y = 0;
			if (pos.Y > textView.ActualHeight)
				pos.Y = textView.ActualHeight;
			pos += textView.ScrollOffset;
			VisualLine line = textView.GetVisualLineFromVisualTop(pos.Y);
			if (line != null) {
				return new SimpleSegment(line.StartOffset, line.LastDocumentLine.EndOffset - line.StartOffset);
			} else {
				return SimpleSegment.Invalid;
			}
		}
		
		int GetOffsetFromMousePosition(MouseEventArgs e, out int visualColumn)
		{
			return GetOffsetFromMousePosition(e.GetPosition(textArea.TextView), out visualColumn);
		}
		
		int GetOffsetFromMousePosition(Point positionRelativeToTextView, out int visualColumn)
		{
			visualColumn = 0;
			TextView textView = textArea.TextView;
			Point pos = positionRelativeToTextView;
			if (pos.Y < 0)
				pos.Y = 0;
			if (pos.Y > textView.ActualHeight)
				pos.Y = textView.ActualHeight;
			pos += textView.ScrollOffset;
			if (pos.Y > textView.DocumentHeight)
				pos.Y = textView.DocumentHeight - ExtensionMethods.Epsilon;
			VisualLine line = textView.GetVisualLineFromVisualTop(pos.Y);
			if (line != null) {
				visualColumn = line.GetVisualColumn(pos);
				return line.GetRelativeOffset(visualColumn) + line.FirstDocumentLine.Offset;
			}
			return -1;
		}
		#endregion
		
		#region MouseMove
		void textArea_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Handled)
				return;
			if (mode == SelectionMode.Normal || mode == SelectionMode.WholeWord || mode == SelectionMode.WholeLine || mode == SelectionMode.Rectangular) {
				e.Handled = true;
				if (textArea.TextView.VisualLinesValid) {
					// If the visual lines are not valid, don't extend the selection.
					// Extending the selection forces a VisualLine refresh, and it is sufficient
					// to do that on MouseUp, we don't have to do it every MouseMove.
					ExtendSelectionToMouse(e);
				}
			} else if (mode == SelectionMode.PossibleDragStart) {
				e.Handled = true;
				Vector mouseMovement = e.GetPosition(textArea) - possibleDragStartMousePos;
				if (Math.Abs(mouseMovement.X) > SystemParameters.MinimumHorizontalDragDistance
				    || Math.Abs(mouseMovement.Y) > SystemParameters.MinimumVerticalDragDistance)
				{
					StartDrag();
				}
			}
		}
		#endregion
		
		#region ExtendSelection
		void SetCaretOffsetToMousePosition(MouseEventArgs e)
		{
			SetCaretOffsetToMousePosition(e, null);
		}
		
		void SetCaretOffsetToMousePosition(MouseEventArgs e, ISegment allowedSegment)
		{
			int visualColumn;
			int offset = GetOffsetFromMousePosition(e, out visualColumn);
			if (allowedSegment != null) {
				offset = offset.CoerceValue(allowedSegment.Offset, allowedSegment.EndOffset);
			}
			if (offset >= 0) {
				textArea.Caret.Position = new TextViewPosition(textArea.Document.GetLocation(offset), visualColumn);
				textArea.Caret.DesiredXPos = double.NaN;
			}
		}
		
		void ExtendSelectionToMouse(MouseEventArgs e)
		{
			int oldOffset = textArea.Caret.Offset;
			if (mode == SelectionMode.Normal || mode == SelectionMode.Rectangular) {
				SetCaretOffsetToMousePosition(e);
				if (mode == SelectionMode.Normal && textArea.Selection is RectangleSelection)
					textArea.Selection = new SimpleSelection(oldOffset, textArea.Caret.Offset);
				else if (mode == SelectionMode.Rectangular && !(textArea.Selection is RectangleSelection))
					textArea.Selection = new RectangleSelection(textArea.Document, oldOffset, textArea.Caret.Offset);
				else
					textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldOffset, textArea.Caret.Offset);
			} else if (mode == SelectionMode.WholeWord || mode == SelectionMode.WholeLine) {
				var newWord = (mode == SelectionMode.WholeLine) ? GetLineAtMousePosition(e) : GetWordAtMousePosition(e);
				if (newWord != SimpleSegment.Invalid) {
					textArea.Selection = new SimpleSelection(Math.Min(newWord.Offset, startWord.Offset),
					                                         Math.Max(newWord.EndOffset, startWord.EndOffset));
					// Set caret offset, but limit the caret to stay inside the selection.
					// in whole-word selection, it's otherwise possible that we get the caret outside the
					// selection - but the TextArea doesn't like that and will reset the selection, causing
					// flickering.
					SetCaretOffsetToMousePosition(e, textArea.Selection.SurroundingSegment);
				}
			}
			textArea.Caret.BringCaretToView(5.0);
		}
		#endregion
		
		#region MouseLeftButtonUp
		void textArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (mode == SelectionMode.None || e.Handled)
				return;
			e.Handled = true;
			if (mode == SelectionMode.PossibleDragStart) {
				// -> this was not a drag start (mouse didn't move after mousedown)
				SetCaretOffsetToMousePosition(e);
				textArea.Selection = Selection.Empty;
			} else if (mode == SelectionMode.Normal || mode == SelectionMode.WholeWord || mode == SelectionMode.WholeLine || mode == SelectionMode.Rectangular) {
				ExtendSelectionToMouse(e);
			}
			mode = SelectionMode.None;
			textArea.ReleaseMouseCapture();
		}
		#endregion
	}
}

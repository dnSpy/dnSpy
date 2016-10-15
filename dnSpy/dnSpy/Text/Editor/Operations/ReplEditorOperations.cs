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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Operations;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Operations;

namespace dnSpy.Text.Editor.Operations {
	sealed class ReplEditorOperations : IReplEditorOperations {
		public IReplEditor ReplEditor => replEditor;
		readonly IWpfTextView wpfTextView;
		readonly IReplEditor2 replEditor;

		IEditorOperations EditorOperations { get; }

		public bool CanCut {
			get {
				if (!replEditor.IsAtEditingPosition)
					return false;
				return !wpfTextView.Selection.IsEmpty;
			}
		}

		public bool CanPaste {
			get {
				if (!replEditor.IsAtEditingPosition)
					return false;
				try {
					return Clipboard.ContainsText();
				}
				catch (ExternalException) { return false; }
			}
		}

		public ReplEditorOperations(IReplEditor2 replEditor, IWpfTextView wpfTextView, IEditorOperationsFactoryService editorOperationsFactoryService) {
			this.replEditor = replEditor;
			this.wpfTextView = wpfTextView;
			EditorOperations = editorOperationsFactoryService.GetEditorOperations(wpfTextView);
		}

		int CaretOffset => wpfTextView.Caret.Position.BufferPosition.Position;

		/// <summary>
		/// Returns false if the caret isn't within the editing area. If the caret is within the
		/// prompt or continue text (eg. first two chars of the line), then the caret is moved to
		/// the first character after that text.
		/// </summary>
		/// <returns></returns>
		bool UpdateCaretForEdit() {
			if (!replEditor.IsAtEditingPosition)
				return false;
			wpfTextView.Caret.MoveTo(new SnapshotPoint(wpfTextView.TextSnapshot, replEditor.FilterOffset(CaretOffset)));

			if (!wpfTextView.Selection.IsEmpty) {
				int start = replEditor.FilterOffset(wpfTextView.Selection.AnchorPoint.Position);
				int end = replEditor.FilterOffset(wpfTextView.Selection.ActivePoint.Position);
				Select(start, end);
			}

			return true;
		}

		void Select(int start, int end) {
			bool isReversed = start > end;
			var pos = !isReversed ?
				new SnapshotSpan(new SnapshotPoint(wpfTextView.TextSnapshot, start), new SnapshotPoint(wpfTextView.TextSnapshot, end)) :
				new SnapshotSpan(new SnapshotPoint(wpfTextView.TextSnapshot, end), new SnapshotPoint(wpfTextView.TextSnapshot, start));
			wpfTextView.Selection.Select(pos, isReversed);
		}

		// Always returns at least one span, even if it's empty
		IEnumerable<SnapshotSpan> GetNormalizedSpansToReplaceWithText() {
			if (wpfTextView.Selection.IsEmpty)
				yield return new SnapshotSpan(wpfTextView.Caret.Position.BufferPosition, 0);
			else {
				var selectedSpans = wpfTextView.Selection.SelectedSpans;
				Debug.Assert(selectedSpans.Count != 0);
				foreach (var s in selectedSpans)
					yield return s;
			}
		}

		string GetNewString(string s) {
			var lineBreak = GetLineBreak();
			var sb = new StringBuilder(s.Length);
			int so = 0;
			while (so < s.Length) {
				int nlOffs = s.IndexOfAny(LineConstants.newLineChars, so);
				if (nlOffs >= 0) {
					sb.Append(s, so, nlOffs - so);
					sb.Append(lineBreak);
					sb.Append(replEditor.SecondaryPrompt);
					so = nlOffs;
					int nlLen = s[so] == '\r' && so + 1 < s.Length && s[so + 1] == '\n' ? 2 : 1;
					so += nlLen;
				}
				else {
					sb.Append(s, so, s.Length - so);
					break;
				}
			}
			return sb.ToString();
		}

		public void AddUserInput(string text, bool clearSearchText = true) {
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (!UpdateCaretForEdit())
				return;
			var s = GetNewString(text);
			var firstSpan = default(SnapshotSpan);
			using (var ed = wpfTextView.TextBuffer.CreateEdit()) {
				foreach (var span in GetNormalizedSpansToReplaceWithText()) {
					Debug.Assert(span.Snapshot != null);
					if (firstSpan.Snapshot == null)
						firstSpan = span;
					ed.Replace(span, s);
				}
				ed.Apply();
			}
			Debug.Assert(firstSpan.Snapshot != null);
			wpfTextView.Selection.Clear();
			wpfTextView.Caret.MoveTo(new SnapshotPoint(wpfTextView.TextSnapshot, firstSpan.Start.Position + s.Length));
			wpfTextView.Caret.EnsureVisible();
			if (clearSearchText)
				replEditor.SearchText = null;
		}

		public void AddUserInput(Span span, string text, bool clearSearchText = true) {
			if (replEditor.FilterOffset(span.Start) != span.Start)
				throw new ArgumentOutOfRangeException(nameof(span));
			if (span.End > wpfTextView.TextSnapshot.Length)
				throw new ArgumentOutOfRangeException(nameof(span));
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (!UpdateCaretForEdit())
				return;

			Debug.Assert(wpfTextView.Selection.IsEmpty);
			wpfTextView.Selection.Clear();

			var s = GetNewString(text);
			wpfTextView.TextBuffer.Replace(span, s);
			wpfTextView.Caret.MoveTo(new SnapshotPoint(wpfTextView.TextSnapshot, span.Start + s.Length));
			wpfTextView.Caret.EnsureVisible();
			if (clearSearchText)
				replEditor.SearchText = null;
		}

		public bool Backspace() {
			if (!UpdateCaretForEdit())
				return false;
			if (!wpfTextView.Selection.IsEmpty)
				AddUserInput(string.Empty);
			else {
				int start = replEditor.FilterOffset(replEditor.OffsetOfPrompt.Value);
				int offs = CaretOffset;
				if (offs <= start)
					return false;
				int end = offs;
				start = end - 1;
				var line = wpfTextView.TextSnapshot.GetLineFromPosition(end);
				if (line.Start.Position == end || replEditor.FilterOffset(start) != start) {
					var prevLine = wpfTextView.TextSnapshot.GetLineFromLineNumber(line.LineNumber - 1);
					start = prevLine.End.Position;
				}
				AddUserInput(Span.FromBounds(start, end), string.Empty);
			}
			return true;
		}

		public bool Capitalize() {
			return false;//TODO:
		}

		public bool ConvertSpacesToTabs() {
			return false;//TODO:
		}

		public bool ConvertTabsToSpaces() {
			return false;//TODO:
		}

		public bool CutFullLine() {
			return false;//TODO:
		}

		public bool CutSelection() {
			if (!UpdateCaretForEdit())
				return false;
			var text = wpfTextView.Selection.GetText();
			try {
				Clipboard.SetText(text);
			}
			catch (ExternalException) { return false; }
			AddUserInput(string.Empty);
			return true;
		}

		public bool DecreaseLineIndent() {
			return false;//TODO:
		}

		public bool Delete() {
			if (!UpdateCaretForEdit())
				return false;
			if (!wpfTextView.Selection.IsEmpty)
				AddUserInput(string.Empty);
			else {
				if (CaretOffset >= wpfTextView.TextSnapshot.Length)
					return false;
				int start = replEditor.FilterOffset(CaretOffset);
				int end = start + 1;
				var startLine = wpfTextView.TextSnapshot.GetLineFromPosition(start);
				var endLine = wpfTextView.TextSnapshot.GetLineFromPosition(end);
				if (startLine.Start != endLine.Start || end > endLine.End.Position) {
					endLine = wpfTextView.TextSnapshot.GetLineFromLineNumber(startLine.LineNumber + 1);
					end = replEditor.FilterOffset(endLine.Start.Position);
				}
				AddUserInput(Span.FromBounds(start, end), string.Empty);
			}
			return true;
		}

		public bool DeleteBlankLines() {
			return false;//TODO:
		}

		public bool DeleteFullLine() {
			return false;//TODO:
		}

		public bool DeleteHorizontalWhiteSpace() {
			return false;//TODO:
		}

		public bool DeleteToBeginningOfLine() {
			return false;//TODO:
		}

		public bool DeleteToEndOfLine() {
			return false;//TODO:
		}

		public bool DeleteWordToLeft() {
			return false;//TODO:
		}

		public bool DeleteWordToRight() {
			return false;//TODO:
		}

		public bool IncreaseLineIndent() {
			return false;//TODO:
		}

		public bool Indent() {
			if (!UpdateCaretForEdit())
				return false;

			//TODO: Add spaces if we should convert tabs to spaces
			AddUserInput("\t");

			return true;
		}

		public bool InsertNewLine() => HandleEnter(false);

		public bool InsertText(string text) {
			if (!UpdateCaretForEdit())
				return false;

			if (wpfTextView.Caret.OverwriteMode && wpfTextView.Selection.IsEmpty) {
				var line = wpfTextView.Caret.Position.BufferPosition.GetContainingLine();
				if (wpfTextView.Caret.Position.BufferPosition < line.End) {
					wpfTextView.Selection.Mode = TextSelectionMode.Stream;
					MoveToNextCharacter(true);
				}
			}
			AddUserInput(text);

			return true;
		}

		public bool MakeLowercase() {
			return false;//TODO:
		}

		public bool MakeUppercase() {
			return false;//TODO:
		}

		public bool MoveSelectedLinesDown() {
			return false;//TODO:
		}

		public bool MoveSelectedLinesUp() {
			return false;//TODO:
		}

		public bool OpenLineAbove() {
			return false;//TODO:
		}

		public bool OpenLineBelow() {
			return false;//TODO:
		}

		public bool Paste() {
			if (!UpdateCaretForEdit())
				return false;
			string text;
			try {
				if (!Clipboard.ContainsText())
					return false;
				text = Clipboard.GetText();
			}
			catch (ExternalException) { return false; }
			if (string.IsNullOrEmpty(text))
				return false;
			AddUserInput(text);
			return true;
		}

		public bool Tabify() {
			return false;//TODO:
		}

		public bool ToggleCase() {
			return false;//TODO:
		}

		public bool TransposeCharacter() {
			return false;//TODO:
		}

		public bool TransposeLine() {
			return false;//TODO:
		}

		public bool TransposeWord() {
			return false;//TODO:
		}

		public bool Unindent() {
			return false;//TODO:
		}

		public bool Untabify() {
			return false;//TODO:
		}

		public bool Submit() {
			MoveToEndOfDocument(false);
			wpfTextView.Selection.Clear();
			return HandleEnter(true);
		}

		public bool InsertNewLineDontSubmit() {
			if (!UpdateCaretForEdit())
				return false;
			AddUserInput(GetLineBreak());
			return true;
		}

		string GetLineBreak() => GetLineBreak(wpfTextView.Caret.Position.BufferPosition);
		string GetLineBreak(SnapshotPoint pos) => Options.GetLineBreak(pos);

		bool HandleEnter(bool force) {
			if (!UpdateCaretForEdit())
				return false;

			if (CaretOffset == wpfTextView.TextSnapshot.Length && wpfTextView.Selection.IsEmpty) {
				if (replEditor.TrySubmit(force))
					return true;
			}
			return InsertNewLineDontSubmit();
		}

		public bool CanCopyCode => replEditor.CanCopyCode;
		public void CopyCode() => replEditor.CopyCode();
		public void ClearInput() => replEditor.ClearInput();
		public void ClearScreen() => replEditor.ClearScreen();
		public void Reset() => replEditor.Reset();
		public void SelectPreviousCommand() => replEditor.SelectPreviousCommand();
		public void SelectNextCommand() => replEditor.SelectNextCommand();
		public bool CanSelectPreviousCommand => replEditor.CanSelectPreviousCommand;
		public bool CanSelectNextCommand => replEditor.CanSelectNextCommand;
		public bool CanDelete => replEditor.IsAtEditingPosition && EditorOperations.CanDelete;
		public IEditorOptions Options => EditorOperations.Options;
		public ITrackingSpan ProvisionalCompositionSpan => EditorOperations.ProvisionalCompositionSpan;
		public string SelectedText => EditorOperations.SelectedText;
		public ITextView TextView => EditorOperations.TextView;
		public void SelectSameTextPreviousCommand() => replEditor.SelectSameTextPreviousCommand();
		public void SelectSameTextNextCommand() => replEditor.SelectSameTextNextCommand();

		public void SelectAll() {
			var buf = replEditor.FindBuffer(CaretOffset).Buffer;
			var newSel = new SnapshotSpan(new SnapshotPoint(wpfTextView.TextSnapshot, buf.Kind == ReplBufferKind.Code ? buf.Span.Start + replEditor.PrimaryPrompt.Length : buf.Span.Start), new SnapshotPoint(wpfTextView.TextSnapshot, buf.Span.End));
			if (newSel.IsEmpty || (wpfTextView.Selection.Mode == TextSelectionMode.Stream && wpfTextView.Selection.StreamSelectionSpan == new VirtualSnapshotSpan(newSel)))
				newSel = new SnapshotSpan(wpfTextView.TextSnapshot, 0, wpfTextView.TextSnapshot.Length);
			wpfTextView.Selection.Select(newSel, false);
			// We don't move the caret to the end of the selection because then we can't press
			// Ctrl+A again to toggle between selecting all or just the current buffer.
			wpfTextView.Caret.EnsureVisible();
		}

		public bool InsertFile(string filePath) {
			if (filePath == null)
				throw new ArgumentNullException(nameof(filePath));
			AddUserInput(File.ReadAllText(filePath));
			return true;
		}

		public bool InsertProvisionalText(string text) {
			return false;//TODO:
		}

		public bool InsertTextAsBox(string text, out VirtualSnapshotPoint boxStart, out VirtualSnapshotPoint boxEnd) {
			boxStart = new VirtualSnapshotPoint(wpfTextView.TextSnapshot, 0);
			boxEnd = new VirtualSnapshotPoint(wpfTextView.TextSnapshot, 0);
			return false;//TODO:
		}

		public bool NormalizeLineEndings(string replacement) {
			return false;//TODO:
		}

		public int ReplaceAllMatches(string searchText, string replaceText, bool matchCase, bool matchWholeWord, bool useRegularExpressions) {
			return 0;//TODO:
		}

		public bool ReplaceSelection(string text) {
			return false;//TODO:
		}

		public bool ReplaceText(Span replaceSpan, string text) {
			return false;//TODO:
		}

		public void MoveToHome(bool extendSelection) {
			//TODO: Ignore the prompt
			EditorOperations.MoveToHome(extendSelection);
		}

		public void MoveToLastNonWhiteSpaceCharacter(bool extendSelection) {
			//TODO: Ignore the prompt
			EditorOperations.MoveToLastNonWhiteSpaceCharacter(extendSelection);
		}

		public void MoveToNextCharacter(bool extendSelection) {
			//TODO: Ignore the prompt
			EditorOperations.MoveToNextCharacter(extendSelection);
		}

		public void MoveToNextWord(bool extendSelection) {
			//TODO: Ignore the prompt
			EditorOperations.MoveToNextWord(extendSelection);
		}

		public void MoveToPreviousCharacter(bool extendSelection) {
			//TODO: Ignore the prompt
			EditorOperations.MoveToPreviousCharacter(extendSelection);
		}

		public void MoveToPreviousWord(bool extendSelection) {
			//TODO: Ignore the prompt
			EditorOperations.MoveToPreviousWord(extendSelection);
		}

		public void MoveToStartOfDocument(bool extendSelection) {
			//TODO: Ignore the prompt
			EditorOperations.MoveToStartOfDocument(extendSelection);
		}

		public void MoveToStartOfLine(bool extendSelection) {
			//TODO: Ignore the prompt
			EditorOperations.MoveToStartOfLine(extendSelection);
		}

		public void MoveToStartOfLineAfterWhiteSpace(bool extendSelection) {
			//TODO: Ignore the prompt
			EditorOperations.MoveToStartOfLineAfterWhiteSpace(extendSelection);
		}

		public void MoveToStartOfNextLineAfterWhiteSpace(bool extendSelection) {
			//TODO: Ignore the prompt
			EditorOperations.MoveToStartOfNextLineAfterWhiteSpace(extendSelection);
		}

		public void MoveToStartOfPreviousLineAfterWhiteSpace(bool extendSelection) {
			//TODO: Ignore the prompt
			EditorOperations.MoveToStartOfPreviousLineAfterWhiteSpace(extendSelection);
		}

		public void SelectCurrentWord() {
			//TODO: Ignore the prompt
			EditorOperations.SelectCurrentWord();
		}

		public void SelectLine(ITextViewLine viewLine, bool extendSelection) {
			//TODO: If in a code buffer, don't select the prompt
			EditorOperations.SelectLine(viewLine, extendSelection);
		}

		public void AddAfterTextBufferChangePrimitive() => EditorOperations.AddAfterTextBufferChangePrimitive();
		public void AddBeforeTextBufferChangePrimitive() => EditorOperations.AddBeforeTextBufferChangePrimitive();
		public bool CopySelection() => EditorOperations.CopySelection();
		public void ExtendSelection(int newEnd) => EditorOperations.ExtendSelection(newEnd);
		public string GetWhitespaceForVirtualSpace(VirtualSnapshotPoint point) => EditorOperations.GetWhitespaceForVirtualSpace(point);
		public void GotoLine(int lineNumber) => EditorOperations.GotoLine(lineNumber);
		public void MoveCaret(ITextViewLine textLine, double horizontalOffset, bool extendSelection) => EditorOperations.MoveCaret(textLine, horizontalOffset, extendSelection);
		public void MoveCurrentLineToBottom() => EditorOperations.MoveCurrentLineToBottom();
		public void MoveCurrentLineToTop() => EditorOperations.MoveCurrentLineToTop();
		public void MoveLineDown(bool extendSelection) => EditorOperations.MoveLineDown(extendSelection);
		public void MoveLineUp(bool extendSelection) => EditorOperations.MoveLineUp(extendSelection);
		public void MoveToBottomOfView(bool extendSelection) => EditorOperations.MoveToBottomOfView(extendSelection);
		public void MoveToEndOfDocument(bool extendSelection) => EditorOperations.MoveToEndOfDocument(extendSelection);
		public void MoveToEndOfLine(bool extendSelection) => EditorOperations.MoveToEndOfLine(extendSelection);
		public void MoveToTopOfView(bool extendSelection) => EditorOperations.MoveToTopOfView(extendSelection);
		public void PageDown(bool extendSelection) => EditorOperations.PageDown(extendSelection);
		public void PageUp(bool extendSelection) => EditorOperations.PageUp(extendSelection);
		public void ResetSelection() => EditorOperations.ResetSelection();
		public void ScrollColumnLeft() => EditorOperations.ScrollColumnLeft();
		public void ScrollColumnRight() => EditorOperations.ScrollColumnRight();
		public void ScrollDownAndMoveCaretIfNecessary() => EditorOperations.ScrollDownAndMoveCaretIfNecessary();
		public void ScrollLineBottom() => EditorOperations.ScrollLineBottom();
		public void ScrollLineCenter() => EditorOperations.ScrollLineCenter();
		public void ScrollLineTop() => EditorOperations.ScrollLineTop();
		public void ScrollPageDown() => EditorOperations.ScrollPageDown();
		public void ScrollPageUp() => EditorOperations.ScrollPageUp();
		public void ScrollUpAndMoveCaretIfNecessary() => EditorOperations.ScrollUpAndMoveCaretIfNecessary();
		public void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint) => EditorOperations.SelectAndMoveCaret(anchorPoint, activePoint);
		public void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, TextSelectionMode selectionMode) => EditorOperations.SelectAndMoveCaret(anchorPoint, activePoint, selectionMode);
		public void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, TextSelectionMode selectionMode, EnsureSpanVisibleOptions? scrollOptions) => EditorOperations.SelectAndMoveCaret(anchorPoint, activePoint, selectionMode, scrollOptions);
		public void SelectEnclosing() => EditorOperations.SelectEnclosing();
		public void SelectFirstChild() => EditorOperations.SelectFirstChild();
		public void SelectNextSibling(bool extendSelection) => EditorOperations.SelectNextSibling(extendSelection);
		public void SelectPreviousSibling(bool extendSelection) => EditorOperations.SelectPreviousSibling(extendSelection);
		public void SwapCaretAndAnchor() => EditorOperations.SwapCaretAndAnchor();
		public void ZoomIn() => EditorOperations.ZoomIn();
		public void ZoomOut() => EditorOperations.ZoomOut();
		public void ZoomTo(double zoomLevel) => EditorOperations.ZoomTo(zoomLevel);
	}
}

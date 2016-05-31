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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Operations;
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Text.Editor.Operations {
	sealed class EditorOperations : IEditorOperations2 {
		public bool CanCut {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		public bool CanDelete {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		public bool CanPaste {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		public IEditorOptions Options => TextView.Options;

		public ITrackingSpan ProvisionalCompositionSpan {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		public string SelectedText {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		public ITextView TextView { get; }

		public EditorOperations(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			TextView = textView;
		}

		public void AddAfterTextBufferChangePrimitive() {
			throw new NotImplementedException();//TODO:
		}

		public void AddBeforeTextBufferChangePrimitive() {
			throw new NotImplementedException();//TODO:
		}

		public bool Backspace() {
			throw new NotImplementedException();//TODO:
		}

		public bool Capitalize() {
			throw new NotImplementedException();//TODO:
		}

		public bool ConvertSpacesToTabs() {
			throw new NotImplementedException();//TODO:
		}

		public bool ConvertTabsToSpaces() {
			throw new NotImplementedException();//TODO:
		}

		public bool CopySelection() {
			throw new NotImplementedException();//TODO:
		}

		public bool CutFullLine() {
			throw new NotImplementedException();//TODO:
		}

		public bool CutSelection() {
			throw new NotImplementedException();//TODO:
		}

		public bool DecreaseLineIndent() {
			throw new NotImplementedException();//TODO:
		}

		public bool Delete() {
			throw new NotImplementedException();//TODO:
		}

		public bool DeleteBlankLines() {
			throw new NotImplementedException();//TODO:
		}

		public bool DeleteFullLine() {
			throw new NotImplementedException();//TODO:
		}

		public bool DeleteHorizontalWhiteSpace() {
			throw new NotImplementedException();//TODO:
		}

		public bool DeleteToBeginningOfLine() {
			throw new NotImplementedException();//TODO:
		}

		public bool DeleteToEndOfLine() {
			throw new NotImplementedException();//TODO:
		}

		public bool DeleteWordToLeft() {
			throw new NotImplementedException();//TODO:
		}

		public bool DeleteWordToRight() {
			throw new NotImplementedException();//TODO:
		}

		public void ExtendSelection(int newEnd) {
			throw new NotImplementedException();//TODO:
		}

		public string GetWhitespaceForVirtualSpace(VirtualSnapshotPoint point) {
			throw new NotImplementedException();//TODO:
		}

		public void GotoLine(int lineNumber) {
			throw new NotImplementedException();//TODO:
		}

		public bool IncreaseLineIndent() {
			throw new NotImplementedException();//TODO:
		}

		public bool Indent() {
			throw new NotImplementedException();//TODO:
		}

		public bool InsertFile(string filePath) {
			throw new NotImplementedException();//TODO:
		}

		public bool InsertNewLine() {
			throw new NotImplementedException();//TODO:
		}

		public bool InsertProvisionalText(string text) {
			throw new NotImplementedException();//TODO:
		}

		public bool InsertText(string text) {
			throw new NotImplementedException();//TODO:
		}

		public bool InsertTextAsBox(string text, out VirtualSnapshotPoint boxStart, out VirtualSnapshotPoint boxEnd) {
			throw new NotImplementedException();//TODO:
		}

		public bool MakeLowercase() {
			throw new NotImplementedException();//TODO:
		}

		public bool MakeUppercase() {
			throw new NotImplementedException();//TODO:
		}

		public void MoveCaret(ITextViewLine textLine, double horizontalOffset, bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveCurrentLineToBottom() {
			throw new NotImplementedException();//TODO:
		}

		public void MoveCurrentLineToTop() {
			throw new NotImplementedException();//TODO:
		}

		public void MoveLineDown(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveLineUp(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public bool MoveSelectedLinesDown() {
			throw new NotImplementedException();//TODO:
		}

		public bool MoveSelectedLinesUp() {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToBottomOfView(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToEndOfDocument(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToEndOfLine(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToHome(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToLastNonWhiteSpaceCharacter(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToNextCharacter(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToNextWord(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToPreviousCharacter(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToPreviousWord(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToStartOfDocument(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToStartOfLine(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToStartOfLineAfterWhiteSpace(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToStartOfNextLineAfterWhiteSpace(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToStartOfPreviousLineAfterWhiteSpace(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void MoveToTopOfView(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public bool NormalizeLineEndings(string replacement) {
			throw new NotImplementedException();//TODO:
		}

		public bool OpenLineAbove() {
			throw new NotImplementedException();//TODO:
		}

		public bool OpenLineBelow() {
			throw new NotImplementedException();//TODO:
		}

		public void PageDown(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void PageUp(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public bool Paste() {
			throw new NotImplementedException();//TODO:
		}

		public int ReplaceAllMatches(string searchText, string replaceText, bool matchCase, bool matchWholeWord, bool useRegularExpressions) {
			throw new NotImplementedException();//TODO:
		}

		public bool ReplaceSelection(string text) {
			throw new NotImplementedException();//TODO:
		}

		public bool ReplaceText(Span replaceSpan, string text) {
			throw new NotImplementedException();//TODO:
		}

		public void ResetSelection() {
			throw new NotImplementedException();//TODO:
		}

		public void ScrollColumnLeft() {
			throw new NotImplementedException();//TODO:
		}

		public void ScrollColumnRight() {
			throw new NotImplementedException();//TODO:
		}

		public void ScrollDownAndMoveCaretIfNecessary() {
			throw new NotImplementedException();//TODO:
		}

		public void ScrollLineBottom() {
			throw new NotImplementedException();//TODO:
		}

		public void ScrollLineCenter() {
			throw new NotImplementedException();//TODO:
		}

		public void ScrollLineTop() {
			throw new NotImplementedException();//TODO:
		}

		public void ScrollPageDown() {
			throw new NotImplementedException();//TODO:
		}

		public void ScrollPageUp() {
			throw new NotImplementedException();//TODO:
		}

		public void ScrollUpAndMoveCaretIfNecessary() {
			throw new NotImplementedException();//TODO:
		}

		public void SelectAll() {
			throw new NotImplementedException();//TODO:
		}

		public void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint) {
			throw new NotImplementedException();//TODO:
		}

		public void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, TextSelectionMode selectionMode) {
			throw new NotImplementedException();//TODO:
		}

		public void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, TextSelectionMode selectionMode, EnsureSpanVisibleOptions? scrollOptions) {
			throw new NotImplementedException();//TODO:
		}

		public void SelectCurrentWord() {
			throw new NotImplementedException();//TODO:
		}

		public void SelectEnclosing() {
			throw new NotImplementedException();//TODO:
		}

		public void SelectFirstChild() {
			throw new NotImplementedException();//TODO:
		}

		public void SelectLine(ITextViewLine viewLine, bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void SelectNextSibling(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void SelectPreviousSibling(bool extendSelection) {
			throw new NotImplementedException();//TODO:
		}

		public void SwapCaretAndAnchor() {
			throw new NotImplementedException();//TODO:
		}

		public bool Tabify() {
			throw new NotImplementedException();//TODO:
		}

		public bool ToggleCase() {
			throw new NotImplementedException();//TODO:
		}

		public bool TransposeCharacter() {
			throw new NotImplementedException();//TODO:
		}

		public bool TransposeLine() {
			throw new NotImplementedException();//TODO:
		}

		public bool TransposeWord() {
			throw new NotImplementedException();//TODO:
		}

		public bool Unindent() {
			throw new NotImplementedException();//TODO:
		}

		public bool Untabify() {
			throw new NotImplementedException();//TODO:
		}

		public void ZoomIn() {
			throw new NotImplementedException();//TODO:
		}

		public void ZoomOut() {
			throw new NotImplementedException();//TODO:
		}

		public void ZoomTo(double zoomLevel) {
			throw new NotImplementedException();//TODO:
		}
	}
}

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

using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Contracts.Text.Editor.Operations {
	/// <summary>
	/// Editor operations
	/// </summary>
	public interface IEditorOperations {
		/// <summary>
		/// true if it's possible to cut
		/// </summary>
		bool CanCut { get; }

		/// <summary>
		/// true if it's possible to delete
		/// </summary>
		bool CanDelete { get; }

		/// <summary>
		/// true if it's possible to paste
		/// </summary>
		bool CanPaste { get; }

		/// <summary>
		/// Gets the <see cref="TextView"/>'s options
		/// </summary>
		IEditorOptions Options { get; }

		/// <summary>
		/// Gets the span of the current provisional composition, or null if there is no provisional composition).
		/// </summary>
		ITrackingSpan ProvisionalCompositionSpan { get; }

		/// <summary>
		/// Gets the selected text
		/// </summary>
		string SelectedText { get; }

		/// <summary>
		/// Gets the text view
		/// </summary>
		ITextView TextView { get; }

		/// <summary>
		/// Adds an ITextUndoPrimitive to the ITextUndoHistory for the buffer that will revert the selection to the current state when it is redone.
		/// </summary>
		void AddAfterTextBufferChangePrimitive();

		/// <summary>
		/// Adds an ITextUndoPrimitive to the ITextUndoHistory for the buffer that will revert the selection to the current state when it is undone.
		/// </summary>
		void AddBeforeTextBufferChangePrimitive();

		/// <summary>
		/// Deletes a character to the left of the current caret.
		/// </summary>
		/// <returns></returns>
		bool Backspace();

		/// <summary>
		/// Converts all the characters in the selection to lowercase, then converts the first character in each word in the selection to uppercase. If the selection is empty, then it makes the next character uppercase.
		/// </summary>
		/// <returns></returns>
		bool Capitalize();

		/// <summary>
		/// Converts spaces to tabs in the selection, or, if the selection is empty, on the line the caret is on.
		/// </summary>
		/// <returns></returns>
		bool ConvertSpacesToTabs();

		/// <summary>
		/// Converts tabs to spaces in the selection, or, if the selection is empty, on the line the caret is on.
		/// </summary>
		/// <returns></returns>
		bool ConvertTabsToSpaces();

		/// <summary>
		/// Copies the selected text to the clipboard.
		/// </summary>
		/// <returns></returns>
		bool CopySelection();

		/// <summary>
		/// If there is a selection, deletes all the lines touched by the selection, including line break characters, and copies the text to the clipboard. Otherwise, deletes the line the caret is on, including the line break characters, and copies the text to the clipboard.
		/// </summary>
		/// <returns></returns>
		bool CutFullLine();

		/// <summary>
		/// Cuts the selected text.
		/// </summary>
		/// <returns></returns>
		bool CutSelection();

		/// <summary>
		/// If there is a multi-line selection, removes indentation from every line in the selection, otherwise removes indentation from the line the caret is on.
		/// </summary>
		/// <returns></returns>
		bool DecreaseLineIndent();

		/// <summary>
		/// Deletes the selection if there is one. If there is no selection, deletes the next character in the buffer if one exists.
		/// </summary>
		/// <returns></returns>
		bool Delete();

		/// <summary>
		/// Deletes all empty lines or lines that contain only white space in the selection.
		/// </summary>
		/// <returns></returns>
		bool DeleteBlankLines();

		/// <summary>
		/// If there is a selection, deletes all the lines touched by the selection, including line break characters. Otherwise, deletes the line the caret is on, including the line break characters.
		/// </summary>
		/// <returns></returns>
		bool DeleteFullLine();

		/// <summary>
		/// Deletes all white space from the beginnings and ends of the selected lines, and trims internal white space.
		/// </summary>
		/// <returns></returns>
		bool DeleteHorizontalWhiteSpace();

		/// <summary>
		/// Deletes the line the caret is on, up to the previous line break character and the selection, if present.
		/// </summary>
		/// <returns></returns>
		bool DeleteToBeginningOfLine();

		/// <summary>
		/// Deletes the line the caret is on, up to the line break character and the selection, if present.
		/// </summary>
		/// <returns></returns>
		bool DeleteToEndOfLine();

		/// <summary>
		/// Deletes the word to the left of the current caret position.
		/// </summary>
		/// <returns></returns>
		bool DeleteWordToLeft();

		/// <summary>
		/// Deletes the word to the right of the current caret position.
		/// </summary>
		/// <returns></returns>
		bool DeleteWordToRight();

		/// <summary>
		/// Extends the current selection span to the specified position.
		/// </summary>
		/// <param name="newEnd">The new character position to which the selection is to be extended.</param>
		void ExtendSelection(int newEnd);

		/// <summary>
		/// Gets a string composed of whitespace characters that would be inserted to fill the gap between a given <see cref="VirtualSnapshotPoint"/> and the closest <see cref="SnapshotPoint"/> on the same line.
		/// </summary>
		/// <param name="point">The point in virtual space</param>
		/// <returns></returns>
		string GetWhitespaceForVirtualSpace(VirtualSnapshotPoint point);

		/// <summary>
		/// Moves the caret to the start of the specified line.
		/// </summary>
		/// <param name="lineNumber">The line number to which to move the caret.</param>
		void GotoLine(int lineNumber);

		/// <summary>
		/// If there is a multi-line selection, adds indentation to every line in the selection, otherwise adds indentation to the line the caret is on.
		/// </summary>
		/// <returns></returns>
		bool IncreaseLineIndent();

		/// <summary>
		/// If there is a multi-line selection indents the selection, otherwise inserts a tab at the caret location.
		/// </summary>
		/// <returns></returns>
		bool Indent();

		/// <summary>
		/// Inserts the contents of a file on disk into the text buffer.
		/// </summary>
		/// <param name="filePath">The path of the file on disk.</param>
		/// <returns></returns>
		bool InsertFile(string filePath);

		/// <summary>
		/// Inserts a new line at the current caret position.
		/// </summary>
		/// <returns></returns>
		bool InsertNewLine();

		/// <summary>
		/// Inserts the given text at the current caret position as provisional text.
		/// </summary>
		/// <param name="text">The text to be inserted in the buffer.</param>
		/// <returns></returns>
		bool InsertProvisionalText(string text);

		/// <summary>
		/// Inserts the given text at the current caret position.
		/// </summary>
		/// <param name="text">The text to be inserted in the buffer.</param>
		/// <returns></returns>
		bool InsertText(string text);

		/// <summary>
		/// Inserts the specified text at the current caret position as a box.
		/// </summary>
		/// <param name="text">The text to be inserted in the buffer. Each "line" from the text will be written out a line at a time.</param>
		/// <param name="boxStart">The start of the newly inserted box.</param>
		/// <param name="boxEnd">The end of the newly inserted box.</param>
		/// <returns></returns>
		bool InsertTextAsBox(string text, out VirtualSnapshotPoint boxStart, out VirtualSnapshotPoint boxEnd);

		/// <summary>
		/// Converts uppercase letters to lowercase in the selection. If the selection is empty, makes the next character lowercase.
		/// </summary>
		/// <returns></returns>
		bool MakeLowercase();

		/// <summary>
		/// Converts lowercase letters to uppercase in the selection. If the selection is empty, makes the next character uppercase.
		/// </summary>
		/// <returns></returns>
		bool MakeUppercase();

		/// <summary>
		/// Moves the caret to the given line at the given offset.
		/// </summary>
		/// <param name="textLine">The <see cref="ITextViewLine"/> on which to place the caret.</param>
		/// <param name="horizontalOffset">The horizontal location in the given textLine to which to move the caret.</param>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveCaret(ITextViewLine textLine, double horizontalOffset, bool extendSelection);

		/// <summary>
		/// Moves the current line to the bottom of the view.
		/// </summary>
		void MoveCurrentLineToBottom();

		/// <summary>
		/// Moves the current line to the top of the view.
		/// </summary>
		void MoveCurrentLineToTop();

		/// <summary>
		/// Moves the caret one line down.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveLineDown(bool extendSelection);

		/// <summary>
		/// Moves the caret one line up.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveLineUp(bool extendSelection);

		/// <summary>
		/// Moves the caret to the last fully-visible line of the view.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveToBottomOfView(bool extendSelection);

		/// <summary>
		/// Moves the caret at the end of the document.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveToEndOfDocument(bool extendSelection);

		/// <summary>
		/// Moves the caret to the end of the line.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveToEndOfLine(bool extendSelection);

		/// <summary>
		/// Moves the caret to the first text column on the line.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveToHome(bool extendSelection);

		/// <summary>
		/// Moves the caret to just before the last non-white space character in the line.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveToLastNonWhiteSpaceCharacter(bool extendSelection);

		/// <summary>
		/// Moves the caret to the next character.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveToNextCharacter(bool extendSelection);

		/// <summary>
		/// Moves the caret to the next word.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveToNextWord(bool extendSelection);

		/// <summary>
		/// Moves the caret to the previous character.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveToPreviousCharacter(bool extendSelection);

		/// <summary>
		/// Moves the caret to the previous word.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveToPreviousWord(bool extendSelection);

		/// <summary>
		/// Moves the caret to the start of the document.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveToStartOfDocument(bool extendSelection);

		/// <summary>
		/// Moves the caret to the start of the line.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveToStartOfLine(bool extendSelection);

		/// <summary>
		/// Moves the caret to the first non-whitespace character of the line.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveToStartOfLineAfterWhiteSpace(bool extendSelection);

		/// <summary>
		/// Moves the caret to the first non-whitespace character in the next line.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveToStartOfNextLineAfterWhiteSpace(bool extendSelection);

		/// <summary>
		/// Moves the caret to the first non-whitespace character on the previous line.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveToStartOfPreviousLineAfterWhiteSpace(bool extendSelection);

		/// <summary>
		/// Moves the caret to the first fully-visible line of the view.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void MoveToTopOfView(bool extendSelection);

		/// <summary>
		/// Replaces all the line endings that do not match the specified string.
		/// </summary>
		/// <param name="replacement">The character sequence with which to replace the line endings.</param>
		/// <returns></returns>
		bool NormalizeLineEndings(string replacement);

		/// <summary>
		/// Inserts a new line at the start of the line the caret is on.
		/// </summary>
		/// <returns></returns>
		bool OpenLineAbove();

		/// <summary>
		/// Inserts a new line at the end of the line the caret is on.
		/// </summary>
		/// <returns></returns>
		bool OpenLineBelow();

		/// <summary>
		/// Moves the caret one page down.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void PageDown(bool extendSelection);

		/// <summary>
		/// Moves the caret one page up.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void PageUp(bool extendSelection);

		/// <summary>
		/// Pastes text from the clipboard to the text buffer.
		/// </summary>
		/// <returns></returns>
		bool Paste();

		/// <summary>
		/// Replaces all matching occurrences of the given string.
		/// </summary>
		/// <param name="searchText">The text to match.</param>
		/// <param name="replaceText">The replacement text.</param>
		/// <param name="matchCase">true if the search should match case, otherwise false.</param>
		/// <param name="matchWholeWord">true if the search should match whole words, otherwise false.</param>
		/// <param name="useRegularExpressions">true if the search should use regular expressions, otherwise false.</param>
		/// <returns></returns>
		int ReplaceAllMatches(string searchText, string replaceText, bool matchCase, bool matchWholeWord, bool useRegularExpressions);

		/// <summary>
		/// Replaces the text selection with the specified text.
		/// </summary>
		/// <param name="text">The text to use as a replacement.</param>
		/// <returns></returns>
		bool ReplaceSelection(string text);

		/// <summary>
		/// Replaces text from the specified span with the specified text.
		/// </summary>
		/// <param name="replaceSpan">The span of text to be replaced.</param>
		/// <param name="text">The new text.</param>
		/// <returns></returns>
		bool ReplaceText(Span replaceSpan, string text);

		/// <summary>
		/// Resets any selection in the text.
		/// </summary>
		void ResetSelection();

		/// <summary>
		/// Scrolls the view one column to the left.
		/// </summary>
		void ScrollColumnLeft();

		/// <summary>
		/// Scrolls the view one column to the right.
		/// </summary>
		void ScrollColumnRight();

		/// <summary>
		/// Scrolls the view down by one line and repositions the caret to the first fully-visible line in the view, if it is scrolled off the page.
		/// </summary>
		void ScrollDownAndMoveCaretIfNecessary();

		/// <summary>
		/// Scrolls the line the caret is on, so that it is the last fully-visible line in the view.
		/// </summary>
		void ScrollLineBottom();

		/// <summary>
		/// Scrolls the line the caret is on, so that it is centered in the view.
		/// </summary>
		void ScrollLineCenter();

		/// <summary>
		/// Scrolls the line the caret is on, so that it is the first fully-visible line in the view.
		/// </summary>
		void ScrollLineTop();

		/// <summary>
		/// Scrolls the view down a page without moving the caret.
		/// </summary>
		void ScrollPageDown();

		/// <summary>
		/// Scrolls the view up a page without moving the caret.
		/// </summary>
		void ScrollPageUp();

		/// <summary>
		/// Scrolls the view up by one line and repositions the caret, if it is scrolled off the page, to the last fully-visible line in the view.
		/// </summary>
		void ScrollUpAndMoveCaretIfNecessary();

		/// <summary>
		/// Selects all text.
		/// </summary>
		void SelectAll();

		/// <summary>
		/// Selects from the given anchor point to the active point, moving the caret to the new active point of the selection. The selected span will be made visible.
		/// </summary>
		/// <param name="anchorPoint">The anchor point of the new selection.</param>
		/// <param name="activePoint">The active point of the new selection and position of the caret.</param>
		void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint);

		/// <summary>
		/// Selects from the specified anchor point to the active point, moving the caret to the new active point of the selection, and ensuring that the selection is in the specified selection mode, and making the selected span visible.
		/// </summary>
		/// <param name="anchorPoint">The anchor point of the new selection.</param>
		/// <param name="activePoint">The active point of the new selection and position of the caret.</param>
		/// <param name="selectionMode">The selection mode of the new selection.</param>
		void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, TextSelectionMode selectionMode);

		/// <summary>
		/// Selects from the given anchor point to active point, moving the caret to the new active point of the selection, ensuring that the selection is in the specified selection mode and making the selected span visible.
		/// </summary>
		/// <param name="anchorPoint">The anchor point of the new selection.</param>
		/// <param name="activePoint">The active point of the new selection and position of the caret.</param>
		/// <param name="selectionMode">The selection mode of the new selection.</param>
		/// <param name="scrollOptions">The scrolling to be done in the view after the selection is made. If null, no scrolling is done.</param>
		void SelectAndMoveCaret(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint, TextSelectionMode selectionMode, EnsureSpanVisibleOptions? scrollOptions);

		/// <summary>
		/// Selects the current word.
		/// </summary>
		void SelectCurrentWord();

		/// <summary>
		/// Selects the enclosing parent.
		/// </summary>
		void SelectEnclosing();

		/// <summary>
		/// Selects the first child.
		/// </summary>
		void SelectFirstChild();

		/// <summary>
		/// Selects the specified line.
		/// </summary>
		/// <param name="viewLine">The line to select.</param>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void SelectLine(ITextViewLine viewLine, bool extendSelection);

		/// <summary>
		/// Selects the next sibling.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void SelectNextSibling(bool extendSelection);

		/// <summary>
		/// Selects the previous sibling.
		/// </summary>
		/// <param name="extendSelection">If true, the selection is extended when the caret is moved; if false, the selection is not extended.</param>
		void SelectPreviousSibling(bool extendSelection);

		/// <summary>
		/// Swaps the caret from its current position to the other end of the selection.
		/// </summary>
		void SwapCaretAndAnchor();

		/// <summary>
		/// Converts the leading white space to tabs on all lines touched by the selection and caret.
		/// </summary>
		/// <returns></returns>
		bool Tabify();

		/// <summary>
		/// Switches the case of each character in the selection. If the selection is empty, changes the case of the next character.
		/// </summary>
		/// <returns></returns>
		bool ToggleCase();

		/// <summary>
		/// Transposes the character at the cursor with the next character.
		/// </summary>
		/// <returns></returns>
		bool TransposeCharacter();

		/// <summary>
		/// Transposes the line containing the cursor with the next line.
		/// </summary>
		/// <returns></returns>
		bool TransposeLine();

		/// <summary>
		/// Transposes the current word with the next one.
		/// </summary>
		/// <returns></returns>
		bool TransposeWord();

		/// <summary>
		/// Unindents the text.
		/// </summary>
		/// <returns></returns>
		bool Unindent();

		/// <summary>
		/// Converts the leading whitespace to spaces on all lines touched by the selection and the caret.
		/// </summary>
		/// <returns></returns>
		bool Untabify();

		/// <summary>
		/// Zooms in to the text view by a scaling factor of 10%.
		/// </summary>
		void ZoomIn();

		/// <summary>
		/// Zooms out of the text view by a scaling factor of 10%.
		/// </summary>
		void ZoomOut();

		/// <summary>
		/// Applies the specified zoom level to the text view.
		/// </summary>
		/// <param name="zoomLevel">The zoom level to apply, between 20% to 400%.</param>
		void ZoomTo(double zoomLevel);
	}
}

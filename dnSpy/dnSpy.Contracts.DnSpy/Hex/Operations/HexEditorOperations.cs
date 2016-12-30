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

using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Hex.Operations {
	/// <summary>
	/// Hex editor operations
	/// </summary>
	public abstract class HexEditorOperations {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexEditorOperations() { }

		/// <summary>
		/// Gets the hex view
		/// </summary>
		public abstract HexView HexView { get; }

		/// <summary>
		/// Gets the editor options
		/// </summary>
		public VSTE.IEditorOptions Options => HexView.Options;

		/// <summary>
		/// Gets/sets the provisional composition span
		/// </summary>
		public abstract HexBufferSpan? ProvisionalCompositionSpan { get; set; }

		/// <summary>
		/// true if it's possible to copy text to the clipboard
		/// </summary>
		public abstract bool CanCopy { get; }

		/// <summary>
		/// true if it's possible to paste data from the clipboard
		/// </summary>
		public abstract bool CanPaste { get; }

		/// <summary>
		/// Selects data and moves the caret
		/// </summary>
		/// <param name="column">Column</param>
		/// <param name="anchorPoint">Anchor position</param>
		/// <param name="activePoint">Active position</param>
		/// <param name="alignPoints">true to align the span to include all bytes of the cells</param>
		public void SelectAndMoveCaret(HexColumnType column, HexBufferPoint anchorPoint, HexBufferPoint activePoint, bool alignPoints) =>
			SelectAndMoveCaret(column, anchorPoint, activePoint, alignPoints, VSTE.EnsureSpanVisibleOptions.MinimumScroll);

		/// <summary>
		/// Selects data and moves the caret
		/// </summary>
		/// <param name="column">Column</param>
		/// <param name="anchorPoint">Anchor position</param>
		/// <param name="activePoint">Active position</param>
		/// <param name="alignPoints">true to align the span to include all bytes of the cells</param>
		/// <param name="scrollOptions">Scroll options</param>
		public abstract void SelectAndMoveCaret(HexColumnType column, HexBufferPoint anchorPoint, HexBufferPoint activePoint, bool alignPoints, VSTE.EnsureSpanVisibleOptions? scrollOptions);

		/// <summary>
		/// Moves the caret to the next character
		/// </summary>
		/// <param name="extendSelection">true to extend the selection</param>
		public abstract void MoveToNextCharacter(bool extendSelection);

		/// <summary>
		/// Moves the caret to the previous character
		/// </summary>
		/// <param name="extendSelection">true to extend the selection</param>
		public abstract void MoveToPreviousCharacter(bool extendSelection);

		/// <summary>
		/// Moves the caret to the next word (cell)
		/// </summary>
		/// <param name="extendSelection">true to extend the selection</param>
		public abstract void MoveToNextWord(bool extendSelection);

		/// <summary>
		/// Moves the caret to the previous word (cell)
		/// </summary>
		/// <param name="extendSelection">true to extend the selection</param>
		public abstract void MoveToPreviousWord(bool extendSelection);

		/// <summary>
		/// Move up a line
		/// </summary>
		/// <param name="extendSelection">true to extend the selection</param>
		public abstract void MoveLineUp(bool extendSelection);

		/// <summary>
		/// Move down a line
		/// </summary>
		/// <param name="extendSelection">true to extend the selection</param>
		public abstract void MoveLineDown(bool extendSelection);

		/// <summary>
		/// Page up
		/// </summary>
		/// <param name="extendSelection">true to extend the selection</param>
		public abstract void PageUp(bool extendSelection);

		/// <summary>
		/// Page down
		/// </summary>
		/// <param name="extendSelection">true to extend the selection</param>
		public abstract void PageDown(bool extendSelection);

		/// <summary>
		/// Move to the end of the line
		/// </summary>
		/// <param name="extendSelection">true to extend the selection</param>
		public abstract void MoveToEndOfLine(bool extendSelection);

		/// <summary>
		/// Move to the start of the line
		/// </summary>
		/// <param name="extendSelection">true to extend the selection</param>
		public abstract void MoveToStartOfLine(bool extendSelection);

		/// <summary>
		/// Move to start of document
		/// </summary>
		/// <param name="extendSelection">true to extend the selection</param>
		public abstract void MoveToStartOfDocument(bool extendSelection);

		/// <summary>
		/// Move to end of document
		/// </summary>
		/// <param name="extendSelection">true to extend the selection</param>
		public abstract void MoveToEndOfDocument(bool extendSelection);

		/// <summary>
		/// Move current line to top of the view
		/// </summary>
		public abstract void MoveCurrentLineToTop();

		/// <summary>
		/// Move current line to bottom of the view
		/// </summary>
		public abstract void MoveCurrentLineToBottom();

		/// <summary>
		/// Move the caret to the top of the view
		/// </summary>
		/// <param name="extendSelection">true to extend the selection</param>
		public abstract void MoveToTopOfView(bool extendSelection);

		/// <summary>
		/// Move the caret to the bottom of the view
		/// </summary>
		/// <param name="extendSelection">true to extend the selection</param>
		public abstract void MoveToBottomOfView(bool extendSelection);

		/// <summary>
		/// Swap selection caret and anchor positions
		/// </summary>
		public abstract void SwapCaretAndAnchor();

		/// <summary>
		/// Inserts text
		/// </summary>
		/// <param name="text">Text</param>
		/// <returns></returns>
		public abstract bool InsertText(string text);

		/// <summary>
		/// Selects the line
		/// </summary>
		/// <param name="viewLine">Line</param>
		/// <param name="extendSelection">true to extend the selection</param>
		public abstract void SelectLine(HexViewLine viewLine, bool extendSelection);

		/// <summary>
		/// Selects the current word (cell)
		/// </summary>
		public abstract void SelectCurrentWord();

		/// <summary>
		/// Select all
		/// </summary>
		public abstract void SelectAll();

		/// <summary>
		/// Extend selection
		/// </summary>
		/// <param name="newEnd">New end position</param>
		public abstract void ExtendSelection(HexBufferPoint newEnd);

		/// <summary>
		/// Move caret to a line
		/// </summary>
		/// <param name="hexLine">Line</param>
		/// <param name="horizontalOffset">Horizontal offset</param>
		/// <param name="extendSelection">true to extend the selection</param>
		public void MoveCaret(HexViewLine hexLine, double horizontalOffset, bool extendSelection) =>
			MoveCaret(hexLine, horizontalOffset, extendSelection, HexMoveToFlags.CaptureHorizontalPosition);

		/// <summary>
		/// Move caret to a line
		/// </summary>
		/// <param name="hexLine">Line</param>
		/// <param name="horizontalOffset">Horizontal offset</param>
		/// <param name="extendSelection">true to extend the selection</param>
		/// <param name="flags">Flags</param>
		public abstract void MoveCaret(HexViewLine hexLine, double horizontalOffset, bool extendSelection, HexMoveToFlags flags);

		/// <summary>
		/// Reset selection
		/// </summary>
		public abstract void ResetSelection();

		/// <summary>
		/// Copy selection, bytes (as text)
		/// </summary>
		/// <returns></returns>
		public abstract bool CopySelectionBytes();

		/// <summary>
		/// Copy selection, UI text
		/// </summary>
		/// <returns></returns>
		public abstract bool CopySelectionText();

		/// <summary>
		/// Copies text to the clipboard
		/// </summary>
		/// <param name="copyKind">What kind of data to copy</param>
		/// <returns></returns>
		public abstract bool CopySpecial(HexCopySpecialKind copyKind);

		/// <summary>
		/// Paste
		/// </summary>
		/// <returns></returns>
		public abstract bool Paste();

		/// <summary>
		/// Pastes data from the clipboard
		/// </summary>
		/// <param name="pasteKind">What kind of data to paste</param>
		/// <returns></returns>
		public abstract bool PasteSpecial(HexPasteSpecialKind pasteKind);

		/// <summary>
		/// Scroll up and move caret so it's within the viewport
		/// </summary>
		public abstract void ScrollUpAndMoveCaretIfNecessary();

		/// <summary>
		/// Scroll down and move caret so it's within the viewport
		/// </summary>
		public abstract void ScrollDownAndMoveCaretIfNecessary();

		/// <summary>
		/// Page up, but don't move caret
		/// </summary>
		public abstract void ScrollPageUp();

		/// <summary>
		/// Page down, but don't move caret
		/// </summary>
		public abstract void ScrollPageDown();

		/// <summary>
		/// Scoll one column left
		/// </summary>
		public abstract void ScrollColumnLeft();

		/// <summary>
		/// Scoll one column right
		/// </summary>
		public abstract void ScrollColumnRight();

		/// <summary>
		/// Move current line to the bottom of the view, don't move the caret
		/// </summary>
		public abstract void ScrollLineBottom();

		/// <summary>
		/// Move current line to the top of the view, don't move the caret
		/// </summary>
		public abstract void ScrollLineTop();

		/// <summary>
		/// Move current line to the center of the view, don't move the caret
		/// </summary>
		public abstract void ScrollLineCenter();

		/// <summary>
		/// Zoom in
		/// </summary>
		public abstract void ZoomIn();

		/// <summary>
		/// Zoom out
		/// </summary>
		public abstract void ZoomOut();

		/// <summary>
		/// Zoom to <paramref name="zoomLevel"/>
		/// </summary>
		/// <param name="zoomLevel">Zoom level, between 20% and 400% (20.0 and 400.0)</param>
		public abstract void ZoomTo(double zoomLevel);

		/// <summary>
		/// Toggles active column
		/// </summary>
		public abstract void ToggleColumn();

		/// <summary>
		/// Clears data
		/// </summary>
		public abstract bool ClearData();

		/// <summary>
		/// Shows all bytes (<see cref="HexBuffer.Span"/>)
		/// </summary>
		public abstract void ShowAllBytes();

		/// <summary>
		/// Shows only the selected bytes
		/// </summary>
		public abstract void ShowOnlySelectedBytes();

		/// <summary>
		/// Refreshes the screen and clears any read caches
		/// </summary>
		public abstract void Refresh();

		/// <summary>
		/// Selects all bytes in the current block, unless the caret is in a memory hole
		/// </summary>
		public abstract void SelectAllBytesBlock();

		/// <summary>
		/// Move to the next closest start/end position of a block of memory
		/// </summary>
		/// <param name="extendSelection">true to extend the selection</param>
		public abstract void MoveToNextValidStartEnd(bool extendSelection);

		/// <summary>
		/// Move to the previous closest start/end position of a block of memory
		/// </summary>
		/// <param name="extendSelection">true to extend the selection</param>
		public abstract void MoveToPreviousValidStartEnd(bool extendSelection);
	}

	/// <summary>
	/// Passed to <see cref="HexEditorOperations.CopySpecial(HexCopySpecialKind)"/>
	/// </summary>
	public enum HexCopySpecialKind {
		/// <summary>
		/// UTF-8 string
		/// </summary>
		Utf8String,

		/// <summary>
		/// Unicode string
		/// </summary>
		UnicodeString,

		/// <summary>
		/// C# array
		/// </summary>
		CSharpArray,

		/// <summary>
		/// Visual Basic array
		/// </summary>
		VisualBasicArray,

		/// <summary>
		/// Offset
		/// </summary>
		Offset,

		/// <summary>
		/// Value at caret
		/// </summary>
		Value,

		/// <summary>
		/// <see cref="ushort"/> (little endian) at caret
		/// </summary>
		UInt16,

		/// <summary>
		/// <see cref="ushort"/> (big endian) at caret
		/// </summary>
		UInt16BigEndian,

		/// <summary>
		/// <see cref="uint"/> (little endian) at caret
		/// </summary>
		UInt32,

		/// <summary>
		/// <see cref="uint"/> (big endian) at caret
		/// </summary>
		UInt32BigEndian,

		/// <summary>
		/// <see cref="ulong"/> (little endian) at caret
		/// </summary>
		UInt64,

		/// <summary>
		/// <see cref="ulong"/> (big endian) at caret
		/// </summary>
		UInt64BigEndian,

		/// <summary>
		/// File offset. If it's a PE file, the position is converted to a position
		/// within the PE file on disk. If it's not a PE file, it's the offset relative
		/// to the start of the file.
		/// </summary>
		FileOffset,

		/// <summary>
		/// Current position
		/// </summary>
		AbsoluteFileOffset,

		/// <summary>
		/// RVA
		/// </summary>
		RVA,
	}

	/// <summary>
	/// Passed to <see cref="HexEditorOperations.PasteSpecial(HexPasteSpecialKind)"/>
	/// </summary>
	public enum HexPasteSpecialKind {
		/// <summary>
		/// UTF-8 string
		/// </summary>
		Utf8String,

		/// <summary>
		/// 7-bit encoded length followed by UTF-8 bytes
		/// </summary>
		Utf8String7BitEncodedLengthPrefix,

		/// <summary>
		/// Unicode (UTF-16) string
		/// </summary>
		UnicodeString,

		/// <summary>
		/// 7-bit encoded length followed by Unicode (UTF-16) bytes
		/// </summary>
		UnicodeString7BitEncodedLengthPrefix,

		/// <summary>
		/// Metadata blob
		/// </summary>
		Blob,
	}
}

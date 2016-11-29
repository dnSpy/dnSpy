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

namespace dnSpy.Contracts.Command {
	/// <summary>
	/// Text editor command IDs (group = <see cref="CommandConstants.HexEditorGroup"/>)
	/// </summary>
	public enum HexEditorIds {
		/// <summary>
		/// Type character. The argument is the string to add.
		/// </summary>
		TYPECHAR,
		/// <summary>
		/// Backspace
		/// </summary>
		BACKSPACE,
		/// <summary>
		/// ENTER
		/// </summary>
		RETURN,
		/// <summary>
		/// Tab
		/// </summary>
		TAB,
		/// <summary>
		/// Tab Left
		/// </summary>
		BACKTAB,
		/// <summary>
		/// Delete
		/// </summary>
		DELETE,
		/// <summary>
		/// Char Left; Move the caret left one character.
		/// </summary>
		LEFT,
		/// <summary>
		/// Char Left Extend; Move the caret left one character, extending the selection.
		/// </summary>
		LEFT_EXT,
		/// <summary>
		/// Char Right; Move the caret right one character.
		/// </summary>
		RIGHT,
		/// <summary>
		/// Char Right Extend; Move the caret right one character, extending the selection.
		/// </summary>
		RIGHT_EXT,
		/// <summary>
		/// Line Up.
		/// </summary>
		UP,
		/// <summary>
		/// Line Up Extend; Move the caret up one line, extending the selection.
		/// </summary>
		UP_EXT,
		/// <summary>
		/// Line Down; Move the caret down one line.
		/// </summary>
		DOWN,
		/// <summary>
		/// Line Down Extend; Move the caret down one line, extending the selection.
		/// </summary>
		DOWN_EXT,
		/// <summary>
		/// Document Start; Move the caret to the start of the document.
		/// </summary>
		HOME,
		/// <summary>
		/// Document Start Extend; Move the caret to the start of the document, extending the selection.
		/// </summary>
		HOME_EXT,
		/// <summary>
		/// Document End; Move the caret to the end of the document.
		/// </summary>
		END,
		/// <summary>
		/// Document End Extend; Move the caret to the end of the document, extending the selection.
		/// </summary>
		END_EXT,
		/// <summary>
		/// Line Start; Move the caret to the start of the line.
		/// </summary>
		BOL,
		/// <summary>
		/// Line Start Extend; Move the caret to the start of the line, extending the selection.
		/// </summary>
		BOL_EXT,
		/// <summary>
		/// Line End; Move the caret to the end of the line..
		/// </summary>
		EOL,
		/// <summary>
		/// Line End Extend; Move the caret to the end of the line, extending the selection.
		/// </summary>
		EOL_EXT,
		/// <summary>
		/// Page Up; Move the caret up one page.
		/// </summary>
		PAGEUP,
		/// <summary>
		/// Page Up Extend; Move the caret up one page, extending the selection.
		/// </summary>
		PAGEUP_EXT,
		/// <summary>
		/// Page Down; Move the caret down one page.
		/// </summary>
		PAGEDN,
		/// <summary>
		/// Page Down Extend; Move the caret down one page, extending the selection.
		/// </summary>
		PAGEDN_EXT,
		/// <summary>
		/// View Top; Move the caret to the top line in view.
		/// </summary>
		TOPLINE,
		/// <summary>
		/// View Top Extend; Move the caret to the top line in view, extending the selection.
		/// </summary>
		TOPLINE_EXT,
		/// <summary>
		/// View Bottom; Move the caret to the last line in view.
		/// </summary>
		BOTTOMLINE,
		/// <summary>
		/// View Bottom Extend; Move the caret to the last line in view, extending the selection.
		/// </summary>
		BOTTOMLINE_EXT,
		/// <summary>
		/// Scroll Line Up: Scroll the document up one line.
		/// </summary>
		SCROLLUP,
		/// <summary>
		/// Scroll Line Down; Scroll the document down one line.
		/// </summary>
		SCROLLDN,
		/// <summary>
		/// Scroll Page Up: Scroll the document up one page..
		/// </summary>
		SCROLLPAGEUP,
		/// <summary>
		/// Scroll Page Down: Scroll the document down one page.
		/// </summary>
		SCROLLPAGEDN,
		/// <summary>
		/// Scroll Column Left; Scroll the document left one column.
		/// </summary>
		SCROLLLEFT,
		/// <summary>
		/// Scroll Column Right; Scroll the document right one column.
		/// </summary>
		SCROLLRIGHT,
		/// <summary>
		/// Scroll Line Bottom; Scroll the current line to the bottom of the view.
		/// </summary>
		SCROLLBOTTOM,
		/// <summary>
		/// Scroll Line Center; Scroll the current line to the center of the view.
		/// </summary>
		SCROLLCENTER,
		/// <summary>
		/// Scroll Line Top: Scroll the current line to the top of the view.
		/// </summary>
		SCROLLTOP,
		/// <summary>
		/// Select All; Select all of the document.
		/// </summary>
		SELECTALL,
		/// <summary>
		/// Swap Anchor; Swap the anchor and end points of the current selection.
		/// </summary>
		SELSWAPANCHOR,
		/// <summary>
		/// Overtype Mode; Toggle between insert and overtype insertion modes.
		/// </summary>
		TOGGLE_OVERTYPE_MODE,
		/// <summary>
		/// Delete Line; Delete all selected lines, or the current line if no selection.
		/// </summary>
		DELETELINE,
		/// <summary>
		/// Delete To EOL; Delete from the caret position to the end of the line.
		/// </summary>
		DELETETOEOL,
		/// <summary>
		/// Delete To BOL; Delete from the caret position to the beginning of the line.
		/// </summary>
		DELETETOBOL,
		/// <summary>
		/// Select Current Word; Select the word under the caret.
		/// </summary>
		SELECTCURRENTWORD,
		/// <summary>
		/// Word Previous; Move the caret left one word.
		/// </summary>
		WORDPREV,
		/// <summary>
		/// Word Previous Extend; Move the caret left one word, extending the selection.
		/// </summary>
		WORDPREV_EXT,
		/// <summary>
		/// Word Next; Move the caret right one word.
		/// </summary>
		WORDNEXT,
		/// <summary>
		/// Word Next Extend; Move the caret right one word, extending the selection.
		/// </summary>
		WORDNEXT_EXT,
		/// <summary>
		/// Selection Cancel; Cancel the current selection moving the caret to the anchor point.
		/// </summary>
		CANCEL,
		/// <summary>
		/// Zoom in
		/// </summary>
		ZoomIn,
		/// <summary>
		/// Zoom out
		/// </summary>
		ZoomOut,
		/// <summary>
		/// Resets the zoom level to the default zoom level
		/// </summary>
		ZoomReset,
		/// <summary>
		/// Quick Info; Display Quick Info based on the current language.
		/// </summary>
		QUICKINFO,
		/// <summary>
		/// Decrease filter
		/// </summary>
		DECREASEFILTER,
		/// <summary>
		/// Increase filter
		/// </summary>
		INCREASEFILTER,
		/// <summary>
		/// Copies the text shown in the UI
		/// </summary>
		CopyText,
		/// <summary>
		/// Copies data (UTF-8)
		/// </summary>
		CopyUtf8String,
		/// <summary>
		/// Copies data (Unicode)
		/// </summary>
		CopyUnicodeString,
		/// <summary>
		/// Copies data (C# array)
		/// </summary>
		CopyCSharpArray,
		/// <summary>
		/// Copies data (Visual Basic array)
		/// </summary>
		CopyVisualBasicArray,
		/// <summary>
		/// Copies the offset
		/// </summary>
		CopyOffset,
		/// <summary>
		/// Pastes UTF-8 data
		/// </summary>
		PasteUtf8String,
		/// <summary>
		/// Pastes unicode data
		/// </summary>
		PasteUnicodeString,
		/// <summary>
		/// Pastes blob data
		/// </summary>
		PasteBlob,
	}
}

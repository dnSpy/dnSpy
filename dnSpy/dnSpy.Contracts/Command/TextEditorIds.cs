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
	/// Text editor command IDs (group = <see cref="CommandConstants.TextEditorGroup"/>)
	/// </summary>
	public enum TextEditorIds {
		/// <summary>
		/// Type character. The argument is the string to add.
		/// </summary>
		TYPECHAR,
		/// <summary>
		/// Delete Backwards; Delete the current selection, or if no selection, the previous character.
		/// </summary>
		BACKSPACE,
		/// <summary>
		/// Break Line; Insert a line break at the current caret position.
		/// </summary>
		RETURN,
		/// <summary>
		/// Insert Tab; Insert a tab character at the current caret position.
		/// </summary>
		TAB,
		/// <summary>
		/// Tab Left; Move the caret back one tab stop.
		/// </summary>
		BACKTAB,
		/// <summary>
		/// Delete; Delete the current selection.
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
		/// Line Start After Indentation; Move the caret to first non-white space character on the line.
		/// </summary>
		FIRSTCHAR,
		/// <summary>
		/// Line Start After Indentation Extend; Move the caret to first non-white space character on the line, extending the selection.
		/// </summary>
		FIRSTCHAR_EXT,
		/// <summary>
		/// Line End; Move the caret to the end of the line..
		/// </summary>
		EOL,
		/// <summary>
		/// Line End Extend; Move the caret to the end of the line, extending the selection.
		/// </summary>
		EOL_EXT,
		/// <summary>
		/// Line Last Char; Move the caret after the last non-white space character on the line.
		/// </summary>
		LASTCHAR,
		/// <summary>
		/// Line Last Char Extend; Move the caret after the last non-white space character on the line, extending the selection..
		/// </summary>
		LASTCHAR_EXT,
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
		/// Tabify Selection: Replace spaces in the current selection with tabs.
		/// </summary>
		SELTABIFY,
		/// <summary>
		/// Untabify Selection; Replace tabs in the current selection with spaces.
		/// </summary>
		SELUNTABIFY,
		/// <summary>
		/// Make Lowercase; Change the text in the current selection to all lower case.
		/// </summary>
		SELLOWCASE,
		/// <summary>
		/// Make Uppercase; Change the text in the current selection to all upper case.
		/// </summary>
		SELUPCASE,
		/// <summary>
		/// Toggle Case: Toggle the case of the text in the current selection.
		/// </summary>
		SELTOGGLECASE,
		/// <summary>
		/// Capitalize; Capitalize the first letter of words in the selection.
		/// </summary>
		SELTITLECASE,
		/// <summary>
		/// Swap Anchor; Swap the anchor and end points of the current selection.
		/// </summary>
		SELSWAPANCHOR,
		/// <summary>
		/// Go To Line; Go to the indicated line.
		/// </summary>
		GOTOLINE,
		/// <summary>
		/// Goto Brace; Move the caret forward to the matching brace.
		/// </summary>
		GOTOBRACE,
		/// <summary>
		/// Goto Brace Extend; Move the caret forward to the matching brace, extending the selection.
		/// </summary>
		GOTOBRACE_EXT,
		/// <summary>
		/// Overtype Mode; Toggle between insert and overtype insertion modes.
		/// </summary>
		TOGGLE_OVERTYPE_MODE,
		/// <summary>
		/// Line Cut; Cut all selected lines, or the current line if no selection, to the clipboard.
		/// </summary>
		CUTLINE,
		/// <summary>
		/// Delete Line; Delete all selected lines, or the current line if no selection.
		/// </summary>
		DELETELINE,
		/// <summary>
		/// Delete Blank Lines; Delete all blank lines in the selection, or the current blank line if no selection.
		/// </summary>
		DELETEBLANKLINES,
		/// <summary>
		/// Delete Horizontal White Space; Collapse white space in the selection, or delete white space adjacent to the caret if no selection.
		/// </summary>
		DELETEWHITESPACE,
		/// <summary>
		/// Delete To EOL; Delete from the caret position to the end of the line.
		/// </summary>
		DELETETOEOL,
		/// <summary>
		/// Delete To BOL; Delete from the caret position to the beginning of the line.
		/// </summary>
		DELETETOBOL,
		/// <summary>
		/// Line Open Above; Open a new line above the current line.
		/// </summary>
		OPENLINEABOVE,
		/// <summary>
		/// Line Open Below: Open a new line below the current line.
		/// </summary>
		OPENLINEBELOW,
		/// <summary>
		/// Increase Line Indent; Increase Indent.
		/// </summary>
		INDENT,
		/// <summary>
		/// Decrease Line Indent; Line Unindent.
		/// </summary>
		UNINDENT,
		/// <summary>
		/// Char Transpose: Transpose the characters on either side of the caret.
		/// </summary>
		TRANSPOSECHAR,
		/// <summary>
		/// Word Transpose; Transpose the words on either side of the caret.
		/// </summary>
		TRANSPOSEWORD,
		/// <summary>
		/// Line Transpose; Transpose the current line and the line below.
		/// </summary>
		TRANSPOSELINE,
		/// <summary>
		/// Select Current Word; Select the word under the caret.
		/// </summary>
		SELECTCURRENTWORD,
		/// <summary>
		/// Word Delete To End; Delete the word to the right of the caret.
		/// </summary>
		DELETEWORDRIGHT,
		/// <summary>
		/// Word Delete To Start; Delete the word to the left of the caret.
		/// </summary>
		DELETEWORDLEFT,
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
		/// View White Space; Toggle the visibility of white space characters.
		/// </summary>
		TOGGLEVISSPACE,
		/// <summary>
		/// Line Start After Indentation Next; Move the caret to the first non-white-space character on the previous line.
		/// </summary>
		FIRSTNONWHITEPREV,
		/// <summary>
		/// Line Start After Indentation Next; Move the caret to the first non-white-space character on the next line.
		/// </summary>
		FIRSTNONWHITENEXT,
		/// <summary>
		/// Char Left Extend Column; Move the caret left one character, extending the column selection.
		/// </summary>
		LEFT_EXT_COL,
		/// <summary>
		/// Char Right Extend Column; Move the caret right one character, extending the column selection.
		/// </summary>
		RIGHT_EXT_COL,
		/// <summary>
		/// Line Up Extend Column; Move the caret up one line, extending the column selection.
		/// </summary>
		UP_EXT_COL,
		/// <summary>
		/// Line Down Extend Column; Move the caret down one line, extending the column selection.
		/// </summary>
		DOWN_EXT_COL,
		/// <summary>
		/// Toggle Word Wrap; Toggle Word Wrap mode.
		/// </summary>
		TOGGLEWORDWRAP,
		/// <summary>
		/// Line Start Extend Column; Move the caret to the start of the line, extending the column selection.
		/// </summary>
		BOL_EXT_COL,
		/// <summary>
		/// Line End Extend Column; Move the caret to the end of the line, extending the column selection.
		/// </summary>
		EOL_EXT_COL,
		/// <summary>
		/// Word Previous Extend Column; Move the caret left one word, extending the column selection.
		/// </summary>
		WORDPREV_EXT_COL,
		/// <summary>
		/// Word Next Extend Column; Move the caret right one word, extending the column selection.
		/// </summary>
		WORDNEXT_EXT_COL,
		/// <summary>
		/// Convert tabs to spaces
		/// </summary>
		ECMD_CONVERTTABSTOSPACES,
		/// <summary>
		/// Convert spaces to tabs
		/// </summary>
		ECMD_CONVERTSPACESTOTABS,
		/// <summary>
		/// Editor line first column
		/// </summary>
		EditorLineFirstColumn,
		/// <summary>
		/// Editor line first column extended
		/// </summary>
		EditorLineFirstColumnExtend,
		/// <summary>
		/// Zoom in
		/// </summary>
		ZoomIn,
		/// <summary>
		/// Zoom out
		/// </summary>
		ZoomOut,
		/// <summary>
		/// Move selected lines up
		/// </summary>
		MoveSelLinesUp,
		/// <summary>
		/// Move seleted lines down
		/// </summary>
		MoveSelLinesDown,
		/// <summary>
		/// Smart Break Line
		/// </summary>
		SmartBreakLine,
	}
}

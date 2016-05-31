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
using System.Diagnostics;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Text.Editor {
	//TODO: [ExportCommandTargetFilterCreator(CommandConstants.CMDFILTERCREATOR_ORDER_TEXT_EDITOR)]
	sealed class DefaultTextViewCommandTargetFilterCreator : ICommandTargetFilterCreator {
		public ICommandTargetFilter Create(object target) {
			var textView = target as ITextView;
			if (textView != null)
				return new DefaultTextViewCommandTarget(textView);
			return null;
		}
	}

	sealed class DefaultTextViewCommandTarget : ICommandTargetFilter {
		readonly ITextView textView;

		public DefaultTextViewCommandTarget(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			this.textView = textView;
		}

		public CommandTargetStatus CanExecute(object target, Guid group, int cmdId) {
			Debug.Assert(target == textView);
			if (group == CommandConstants.DefaultGroup) {
				switch ((DefaultIds)cmdId) {
				case DefaultIds.Copy:
				case DefaultIds.Cut:
				case DefaultIds.Paste:
				case DefaultIds.Redo:
				case DefaultIds.Undo:
					return CommandTargetStatus.Handled;
				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			else if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.BACKSPACE:
				case TextEditorIds.BACKTAB:
				case TextEditorIds.BOL:
				case TextEditorIds.BOL_EXT:
				case TextEditorIds.BOL_EXT_COL:
				case TextEditorIds.BOTTOMLINE:
				case TextEditorIds.BOTTOMLINE_EXT:
				case TextEditorIds.CANCEL:
				case TextEditorIds.CUTLINE:
				case TextEditorIds.DELETE:
				case TextEditorIds.DELETEBLANKLINES:
				case TextEditorIds.DELETELINE:
				case TextEditorIds.DELETETOBOL:
				case TextEditorIds.DELETETOEOL:
				case TextEditorIds.DELETEWHITESPACE:
				case TextEditorIds.DELETEWORDLEFT:
				case TextEditorIds.DELETEWORDRIGHT:
				case TextEditorIds.DOWN:
				case TextEditorIds.DOWN_EXT:
				case TextEditorIds.DOWN_EXT_COL:
				case TextEditorIds.ECMD_CONVERTSPACESTOTABS:
				case TextEditorIds.ECMD_CONVERTTABSTOSPACES:
				case TextEditorIds.EditorLineFirstColumn:
				case TextEditorIds.EditorLineFirstColumnExtend:
				case TextEditorIds.END:
				case TextEditorIds.END_EXT:
				case TextEditorIds.EOL:
				case TextEditorIds.EOL_EXT:
				case TextEditorIds.EOL_EXT_COL:
				case TextEditorIds.FIRSTCHAR:
				case TextEditorIds.FIRSTCHAR_EXT:
				case TextEditorIds.FIRSTNONWHITENEXT:
				case TextEditorIds.FIRSTNONWHITEPREV:
				case TextEditorIds.GOTOBRACE:
				case TextEditorIds.GOTOBRACE_EXT:
				case TextEditorIds.GOTOLINE:
				case TextEditorIds.HOME:
				case TextEditorIds.HOME_EXT:
				case TextEditorIds.INDENT:
				case TextEditorIds.LASTCHAR:
				case TextEditorIds.LASTCHAR_EXT:
				case TextEditorIds.LEFT:
				case TextEditorIds.LEFT_EXT:
				case TextEditorIds.LEFT_EXT_COL:
				case TextEditorIds.MoveSelLinesDown:
				case TextEditorIds.MoveSelLinesUp:
				case TextEditorIds.OPENLINEABOVE:
				case TextEditorIds.OPENLINEBELOW:
				case TextEditorIds.PAGEDN:
				case TextEditorIds.PAGEDN_EXT:
				case TextEditorIds.PAGEUP:
				case TextEditorIds.PAGEUP_EXT:
				case TextEditorIds.RETURN:
				case TextEditorIds.RIGHT:
				case TextEditorIds.RIGHT_EXT:
				case TextEditorIds.RIGHT_EXT_COL:
				case TextEditorIds.SCROLLBOTTOM:
				case TextEditorIds.SCROLLCENTER:
				case TextEditorIds.SCROLLDN:
				case TextEditorIds.SCROLLLEFT:
				case TextEditorIds.SCROLLPAGEDN:
				case TextEditorIds.SCROLLPAGEUP:
				case TextEditorIds.SCROLLRIGHT:
				case TextEditorIds.SCROLLTOP:
				case TextEditorIds.SCROLLUP:
				case TextEditorIds.SELECTALL:
				case TextEditorIds.SELECTCURRENTWORD:
				case TextEditorIds.SELLOWCASE:
				case TextEditorIds.SELSWAPANCHOR:
				case TextEditorIds.SELTABIFY:
				case TextEditorIds.SELTITLECASE:
				case TextEditorIds.SELTOGGLECASE:
				case TextEditorIds.SELUNTABIFY:
				case TextEditorIds.SELUPCASE:
				case TextEditorIds.SmartBreakLine:
				case TextEditorIds.TAB:
				case TextEditorIds.TOGGLE_OVERTYPE_MODE:
				case TextEditorIds.TOGGLEVISSPACE:
				case TextEditorIds.TOGGLEWORDWRAP:
				case TextEditorIds.TOPLINE:
				case TextEditorIds.TOPLINE_EXT:
				case TextEditorIds.TRANSPOSECHAR:
				case TextEditorIds.TRANSPOSELINE:
				case TextEditorIds.TRANSPOSEWORD:
				case TextEditorIds.TYPECHAR:
				case TextEditorIds.UNINDENT:
				case TextEditorIds.UP:
				case TextEditorIds.UP_EXT:
				case TextEditorIds.UP_EXT_COL:
				case TextEditorIds.WORDNEXT:
				case TextEditorIds.WORDNEXT_EXT:
				case TextEditorIds.WORDNEXT_EXT_COL:
				case TextEditorIds.WORDPREV:
				case TextEditorIds.WORDPREV_EXT:
				case TextEditorIds.WORDPREV_EXT_COL:
				case TextEditorIds.ZoomIn:
				case TextEditorIds.ZoomOut:
					return CommandTargetStatus.Handled;
				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public CommandTargetStatus Execute(object target, Guid group, int cmdId, object args, ref object result) {
			Debug.Assert(target == textView);
			if (group == CommandConstants.DefaultGroup) {
				switch ((DefaultIds)cmdId) {
				case DefaultIds.Copy:
					return CommandTargetStatus.Handled;//TODO:

				case DefaultIds.Cut:
					return CommandTargetStatus.Handled;//TODO:

				case DefaultIds.Paste:
					return CommandTargetStatus.Handled;//TODO:

				case DefaultIds.Redo:
					return CommandTargetStatus.Handled;//TODO:

				case DefaultIds.Undo:
					return CommandTargetStatus.Handled;//TODO:

				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			else if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.BACKSPACE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.BACKTAB:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.BOL:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.BOL_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.BOL_EXT_COL:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.BOTTOMLINE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.BOTTOMLINE_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.CANCEL:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.CUTLINE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.DELETE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.DELETEBLANKLINES:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.DELETELINE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.DELETETOBOL:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.DELETETOEOL:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.DELETEWHITESPACE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.DELETEWORDLEFT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.DELETEWORDRIGHT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.DOWN:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.DOWN_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.DOWN_EXT_COL:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.ECMD_CONVERTSPACESTOTABS:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.ECMD_CONVERTTABSTOSPACES:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.EditorLineFirstColumn:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.EditorLineFirstColumnExtend:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.END:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.END_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.EOL:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.EOL_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.EOL_EXT_COL:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.FIRSTCHAR:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.FIRSTCHAR_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.FIRSTNONWHITENEXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.FIRSTNONWHITEPREV:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.GOTOBRACE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.GOTOBRACE_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.GOTOLINE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.HOME:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.HOME_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.INDENT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.LASTCHAR:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.LASTCHAR_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.LEFT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.LEFT_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.LEFT_EXT_COL:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.MoveSelLinesDown:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.MoveSelLinesUp:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.OPENLINEABOVE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.OPENLINEBELOW:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.PAGEDN:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.PAGEDN_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.PAGEUP:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.PAGEUP_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.RETURN:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.RIGHT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.RIGHT_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.RIGHT_EXT_COL:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SCROLLBOTTOM:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SCROLLCENTER:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SCROLLDN:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SCROLLLEFT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SCROLLPAGEDN:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SCROLLPAGEUP:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SCROLLRIGHT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SCROLLTOP:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SCROLLUP:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SELECTALL:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SELECTCURRENTWORD:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SELLOWCASE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SELSWAPANCHOR:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SELTABIFY:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SELTITLECASE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SELTOGGLECASE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SELUNTABIFY:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SELUPCASE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.SmartBreakLine:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.TAB:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.TOGGLE_OVERTYPE_MODE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.TOGGLEVISSPACE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.TOGGLEWORDWRAP:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.TOPLINE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.TOPLINE_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.TRANSPOSECHAR:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.TRANSPOSELINE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.TRANSPOSEWORD:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.TYPECHAR:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.UNINDENT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.UP:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.UP_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.UP_EXT_COL:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.WORDNEXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.WORDNEXT_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.WORDNEXT_EXT_COL:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.WORDPREV:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.WORDPREV_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.WORDPREV_EXT_COL:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.ZoomIn:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.ZoomOut:
					return CommandTargetStatus.Handled;//TODO:

				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public void Dispose() { }
	}
}

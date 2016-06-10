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
	[ExportCommandTargetFilterCreator(CommandConstants.CMDTARGETFILTER_ORDER_TEXT_EDITOR)]
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

		static bool IsEditCommand(Guid group, int cmdId) {
			if (group == CommandConstants.DefaultGroup) {
				switch ((DefaultIds)cmdId) {
				case DefaultIds.Cut:
				case DefaultIds.Paste:
				case DefaultIds.Redo:
				case DefaultIds.Undo:
					return true;

				case DefaultIds.Unknown:
				case DefaultIds.Copy:
					return false;

				default:
					Debug.Fail($"Unknown {nameof(DefaultIds)} value: {group} {(DefaultIds)cmdId}");
					return true;
				}
			}
			else if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.BACKSPACE:
				case TextEditorIds.BACKTAB:
				case TextEditorIds.CUTLINE:
				case TextEditorIds.DELETE:
				case TextEditorIds.DELETEBLANKLINES:
				case TextEditorIds.DELETELINE:
				case TextEditorIds.DELETETOBOL:
				case TextEditorIds.DELETETOEOL:
				case TextEditorIds.DELETEWHITESPACE:
				case TextEditorIds.DELETEWORDLEFT:
				case TextEditorIds.DELETEWORDRIGHT:
				case TextEditorIds.ECMD_CONVERTSPACESTOTABS:
				case TextEditorIds.ECMD_CONVERTTABSTOSPACES:
				case TextEditorIds.INDENT:
				case TextEditorIds.MoveSelLinesDown:
				case TextEditorIds.MoveSelLinesUp:
				case TextEditorIds.OPENLINEABOVE:
				case TextEditorIds.OPENLINEBELOW:
				case TextEditorIds.RETURN:
				case TextEditorIds.SELLOWCASE:
				case TextEditorIds.SELTABIFY:
				case TextEditorIds.SELTITLECASE:
				case TextEditorIds.SELTOGGLECASE:
				case TextEditorIds.SELUNTABIFY:
				case TextEditorIds.SELUPCASE:
				case TextEditorIds.SmartBreakLine:
				case TextEditorIds.TAB:
				case TextEditorIds.TRANSPOSECHAR:
				case TextEditorIds.TRANSPOSELINE:
				case TextEditorIds.TRANSPOSEWORD:
				case TextEditorIds.TYPECHAR:
				case TextEditorIds.UNINDENT:
					return true;

				case TextEditorIds.BOL:
				case TextEditorIds.BOL_EXT:
				case TextEditorIds.BOL_EXT_COL:
				case TextEditorIds.BOTTOMLINE:
				case TextEditorIds.BOTTOMLINE_EXT:
				case TextEditorIds.CANCEL:
				case TextEditorIds.DOWN:
				case TextEditorIds.DOWN_EXT:
				case TextEditorIds.DOWN_EXT_COL:
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
				case TextEditorIds.LASTCHAR:
				case TextEditorIds.LASTCHAR_EXT:
				case TextEditorIds.LEFT:
				case TextEditorIds.LEFT_EXT:
				case TextEditorIds.LEFT_EXT_COL:
				case TextEditorIds.PAGEDN:
				case TextEditorIds.PAGEDN_EXT:
				case TextEditorIds.PAGEUP:
				case TextEditorIds.PAGEUP_EXT:
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
				case TextEditorIds.SELSWAPANCHOR:
				case TextEditorIds.TOGGLE_OVERTYPE_MODE:
				case TextEditorIds.TOGGLEVISSPACE:
				case TextEditorIds.TOGGLEWORDWRAP:
				case TextEditorIds.TOPLINE:
				case TextEditorIds.TOPLINE_EXT:
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
					return false;

				default:
					Debug.Fail($"Unknown {nameof(TextEditorIds)} value: {group} {(TextEditorIds)cmdId}");
					return true;
				}
			}
			return false;
		}

		bool IsReadOnly => textView.Options.GetOptionValue(DefaultTextViewOptions.ViewProhibitUserInputId);

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (IsReadOnly && IsEditCommand(group, cmdId))
				return CommandTargetStatus.NotHandled;

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

		public CommandTargetStatus Execute(Guid group, int cmdId, object args = null) {
			object result = null;
			return Execute(group, cmdId, args, ref result);
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args, ref object result) {
			if (IsReadOnly && IsEditCommand(group, cmdId))
				return CommandTargetStatus.NotHandled;

			if (group == CommandConstants.DefaultGroup) {
				switch ((DefaultIds)cmdId) {
				case DefaultIds.Copy:
					textView.EditorOperations.CopySelection();
					return CommandTargetStatus.Handled;

				case DefaultIds.Cut:
					textView.EditorOperations.CutSelection();
					return CommandTargetStatus.Handled;

				case DefaultIds.Paste:
					textView.EditorOperations.Paste();
					return CommandTargetStatus.Handled;

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
					if (textView.EditorOperations.ProvisionalCompositionSpan != null)
						textView.EditorOperations.InsertText(string.Empty);
					else
						textView.EditorOperations.Backspace();
					return CommandTargetStatus.Handled;

				case TextEditorIds.BACKTAB:
					textView.EditorOperations.Unindent();
					return CommandTargetStatus.Handled;

				case TextEditorIds.BOL:
					textView.EditorOperations.MoveToHome(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.BOL_EXT:
					textView.EditorOperations.MoveToHome(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.BOL_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					textView.EditorOperations.MoveToHome(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.BOTTOMLINE:
					textView.EditorOperations.MoveToBottomOfView(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.BOTTOMLINE_EXT:
					textView.EditorOperations.MoveToBottomOfView(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.CANCEL:
					textView.EditorOperations.ResetSelection();
					return CommandTargetStatus.Handled;

				case TextEditorIds.CUTLINE:
					textView.EditorOperations.CutFullLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETE:
					textView.EditorOperations.Delete();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETEBLANKLINES:
					textView.EditorOperations.DeleteBlankLines();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETELINE:
					textView.EditorOperations.DeleteFullLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETETOBOL:
					textView.EditorOperations.DeleteToBeginningOfLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETETOEOL:
					textView.EditorOperations.DeleteToEndOfLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETEWHITESPACE:
					textView.EditorOperations.DeleteHorizontalWhiteSpace();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETEWORDLEFT:
					textView.EditorOperations.DeleteWordToLeft();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETEWORDRIGHT:
					textView.EditorOperations.DeleteWordToRight();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DOWN:
					textView.EditorOperations.MoveLineDown(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.DOWN_EXT:
					textView.EditorOperations.MoveLineDown(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.DOWN_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					textView.EditorOperations.MoveLineDown(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.ECMD_CONVERTSPACESTOTABS:
					textView.EditorOperations.ConvertSpacesToTabs();
					return CommandTargetStatus.Handled;

				case TextEditorIds.ECMD_CONVERTTABSTOSPACES:
					textView.EditorOperations.ConvertTabsToSpaces();
					return CommandTargetStatus.Handled;

				case TextEditorIds.EditorLineFirstColumn:
					textView.EditorOperations.MoveToStartOfLine(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.EditorLineFirstColumnExtend:
					textView.EditorOperations.MoveToStartOfLine(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.END:
					textView.EditorOperations.MoveToEndOfDocument(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.END_EXT:
					textView.EditorOperations.MoveToEndOfDocument(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.EOL:
					textView.EditorOperations.MoveToEndOfLine(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.EOL_EXT:
					textView.EditorOperations.MoveToEndOfLine(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.EOL_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					textView.EditorOperations.MoveToEndOfLine(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.FIRSTCHAR:
					textView.EditorOperations.MoveToStartOfLineAfterWhiteSpace(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.FIRSTCHAR_EXT:
					textView.EditorOperations.MoveToStartOfLineAfterWhiteSpace(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.FIRSTNONWHITENEXT:
					textView.EditorOperations.MoveToStartOfNextLineAfterWhiteSpace(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.FIRSTNONWHITEPREV:
					textView.EditorOperations.MoveToStartOfPreviousLineAfterWhiteSpace(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.GOTOBRACE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.GOTOBRACE_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.GOTOLINE:
					if (args is int)
						textView.EditorOperations.GotoLine((int)args);
					return CommandTargetStatus.Handled;

				case TextEditorIds.HOME:
					textView.EditorOperations.MoveToStartOfDocument(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.HOME_EXT:
					textView.EditorOperations.MoveToStartOfDocument(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.INDENT:
					textView.EditorOperations.IncreaseLineIndent();
					return CommandTargetStatus.Handled;

				case TextEditorIds.LASTCHAR:
					textView.EditorOperations.MoveToLastNonWhiteSpaceCharacter(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.LASTCHAR_EXT:
					textView.EditorOperations.MoveToLastNonWhiteSpaceCharacter(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.LEFT:
					textView.EditorOperations.MoveToPreviousCharacter(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.LEFT_EXT:
					textView.EditorOperations.MoveToPreviousCharacter(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.LEFT_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					textView.EditorOperations.MoveToPreviousCharacter(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.MoveSelLinesDown:
					textView.EditorOperations.MoveSelectedLinesDown();
					return CommandTargetStatus.Handled;

				case TextEditorIds.MoveSelLinesUp:
					textView.EditorOperations.MoveSelectedLinesUp();
					return CommandTargetStatus.Handled;

				case TextEditorIds.OPENLINEABOVE:
					textView.EditorOperations.OpenLineAbove();
					return CommandTargetStatus.Handled;

				case TextEditorIds.OPENLINEBELOW:
					textView.EditorOperations.OpenLineBelow();
					return CommandTargetStatus.Handled;

				case TextEditorIds.PAGEDN:
					textView.EditorOperations.PageDown(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.PAGEDN_EXT:
					textView.EditorOperations.PageDown(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.PAGEUP:
					textView.EditorOperations.PageUp(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.PAGEUP_EXT:
					textView.EditorOperations.PageUp(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.RETURN:
					textView.EditorOperations.InsertNewLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.RIGHT:
					textView.EditorOperations.MoveToNextCharacter(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.RIGHT_EXT:
					textView.EditorOperations.MoveToNextCharacter(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.RIGHT_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					textView.EditorOperations.MoveToNextCharacter(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLBOTTOM:
					textView.EditorOperations.ScrollLineBottom();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLCENTER:
					textView.EditorOperations.ScrollLineCenter();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLDN:
					textView.EditorOperations.ScrollDownAndMoveCaretIfNecessary();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLLEFT:
					textView.EditorOperations.ScrollColumnLeft();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLPAGEDN:
					textView.EditorOperations.ScrollPageDown();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLPAGEUP:
					textView.EditorOperations.ScrollPageUp();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLRIGHT:
					textView.EditorOperations.ScrollColumnRight();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLTOP:
					textView.EditorOperations.ScrollLineTop();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLUP:
					textView.EditorOperations.ScrollUpAndMoveCaretIfNecessary();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELECTALL:
					textView.EditorOperations.SelectAll();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELECTCURRENTWORD:
					textView.EditorOperations.SelectCurrentWord();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELLOWCASE:
					textView.EditorOperations.MakeLowercase();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELSWAPANCHOR:
					textView.EditorOperations.SwapCaretAndAnchor();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELTABIFY:
					textView.EditorOperations.Tabify();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELTITLECASE:
					textView.EditorOperations.Capitalize();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELTOGGLECASE:
					textView.EditorOperations.ToggleCase();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELUNTABIFY:
					textView.EditorOperations.Untabify();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELUPCASE:
					textView.EditorOperations.MakeUppercase();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SmartBreakLine:
					textView.EditorOperations.InsertNewLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.TAB:
					textView.EditorOperations.Indent();
					return CommandTargetStatus.Handled;

				case TextEditorIds.TOGGLE_OVERTYPE_MODE:
					if (textView.Options.GetOptionValue(DefaultTextViewOptions.CanChangeOverwriteModeId))
						textView.Options.SetOptionValue(DefaultTextViewOptions.OverwriteModeId, !textView.Options.GetOptionValue(DefaultTextViewOptions.OverwriteModeId));
					return CommandTargetStatus.Handled;

				case TextEditorIds.TOGGLEVISSPACE:
					if (textView.Options.GlobalOptions.GetOptionValue(DefaultTextViewOptions.CanChangeUseVisibleWhitespaceId))
						textView.Options.GlobalOptions.SetOptionValue(DefaultTextViewOptions.UseVisibleWhitespaceId, !textView.Options.GlobalOptions.GetOptionValue(DefaultTextViewOptions.UseVisibleWhitespaceId));
					return CommandTargetStatus.Handled;

				case TextEditorIds.TOGGLEWORDWRAP:
					if (textView.Options.GlobalOptions.GetOptionValue(DefaultTextViewOptions.CanChangeWordWrapStyleId)) {
						var newWordwrapStyle = textView.Options.GlobalOptions.GetOptionValue(DefaultTextViewOptions.WordWrapStyleId) ^ WordWrapStyles.WordWrap;
						textView.Options.GlobalOptions.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, newWordwrapStyle);
						if ((newWordwrapStyle & WordWrapStyles.WordWrap) != 0 && textView.Options.GetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId))
							textView.Options.SetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId, false);
					}
					return CommandTargetStatus.Handled;

				case TextEditorIds.TOPLINE:
					textView.EditorOperations.MoveToTopOfView(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.TOPLINE_EXT:
					textView.EditorOperations.MoveToTopOfView(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.TRANSPOSECHAR:
					textView.EditorOperations.TransposeCharacter();
					return CommandTargetStatus.Handled;

				case TextEditorIds.TRANSPOSELINE:
					textView.EditorOperations.TransposeLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.TRANSPOSEWORD:
					textView.EditorOperations.TransposeWord();
					return CommandTargetStatus.Handled;

				case TextEditorIds.TYPECHAR:
					if (args is string)
						textView.EditorOperations.InsertText((string)args);
					return CommandTargetStatus.Handled;

				case TextEditorIds.UNINDENT:
					textView.EditorOperations.DecreaseLineIndent();
					return CommandTargetStatus.Handled;

				case TextEditorIds.UP:
					textView.EditorOperations.MoveLineUp(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.UP_EXT:
					textView.EditorOperations.MoveLineUp(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.UP_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					textView.EditorOperations.MoveLineUp(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDNEXT:
					textView.EditorOperations.MoveToNextWord(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDNEXT_EXT:
					textView.EditorOperations.MoveToNextWord(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDNEXT_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					textView.EditorOperations.MoveToNextWord(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDPREV:
					textView.EditorOperations.MoveToPreviousWord(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDPREV_EXT:
					textView.EditorOperations.MoveToPreviousWord(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDPREV_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					textView.EditorOperations.MoveToPreviousWord(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.ZoomIn:
					textView.EditorOperations.ZoomIn();
					return CommandTargetStatus.Handled;

				case TextEditorIds.ZoomOut:
					textView.EditorOperations.ZoomOut();
					return CommandTargetStatus.Handled;

				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}
}

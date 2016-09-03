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
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace dnSpy.Text.Editor {
	sealed class ReplCommandTargetFilter : ICommandTargetFilter {
		readonly ITextView textView;

		public ReplCommandTargetFilter(ITextView textView) {
			this.textView = textView;
		}

		ReplEditor TryGetInstance() =>
			__replEditor ?? (__replEditor = ReplEditorUtils.TryGetInstance(textView) as ReplEditor);
		ReplEditor __replEditor;

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			var replEditor = TryGetInstance();
			if (replEditor == null)
				return CommandTargetStatus.NotHandled;

			if (group == CommandConstants.StandardGroup) {
				switch ((StandardIds)cmdId) {
				case StandardIds.Copy:
					return CommandTargetStatus.Handled;

				case StandardIds.Cut:
					if (replEditor.ReplEditorOperations.CanCut)
						return CommandTargetStatus.Handled;
					return CommandTargetStatus.LetWpfHandleCommand;

				case StandardIds.Paste:
					if (replEditor.ReplEditorOperations.CanPaste)
						return CommandTargetStatus.Handled;
					return CommandTargetStatus.LetWpfHandleCommand;

				case StandardIds.Redo:
				case StandardIds.Undo:
					Debug.Assert(nextCommandTarget != null);
					return nextCommandTarget?.CanExecute(group, cmdId) ?? CommandTargetStatus.LetWpfHandleCommand;

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
				case TextEditorIds.ZoomReset:
					return CommandTargetStatus.Handled;

				case TextEditorIds.COMPLETEWORD:
				case TextEditorIds.DECREASEFILTER:
				case TextEditorIds.GOTOLINE:
				case TextEditorIds.INCREASEFILTER:
				case TextEditorIds.SHOWMEMBERLIST:
				case TextEditorIds.TOGGLE_OVERTYPE_MODE:
				case TextEditorIds.ToggleConsumeFirstCompletionMode:
				case TextEditorIds.TOGGLEVISSPACE:
					return CommandTargetStatus.NotHandled;

				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			else if (group == CommandConstants.ReplGroup) {
				switch ((ReplIds)cmdId) {
				case ReplIds.CopyCode:
					return replEditor.ReplEditorOperations.CanCopyCode ? CommandTargetStatus.Handled : CommandTargetStatus.NotHandled;

				case ReplIds.Submit:
				case ReplIds.NewLineDontSubmit:
				case ReplIds.ClearInput:
				case ReplIds.ClearScreen:
				case ReplIds.Reset:
				case ReplIds.SelectPreviousCommand:
				case ReplIds.SelectNextCommand:
				case ReplIds.SelectSameTextPreviousCommand:
				case ReplIds.SelectSameTextNextCommand:
					return CommandTargetStatus.Handled;

				default:
					Debug.Fail($"Unknown {nameof(ReplIds)}: {(ReplIds)cmdId}");
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
			var replEditor = TryGetInstance();
			if (replEditor == null)
				return CommandTargetStatus.NotHandled;

			if (group == CommandConstants.StandardGroup) {
				switch ((StandardIds)cmdId) {
				case StandardIds.Copy:
					replEditor.ReplEditorOperations.CopySelection();
					return CommandTargetStatus.Handled;

				case StandardIds.Cut:
					replEditor.ReplEditorOperations.CutSelection();
					return CommandTargetStatus.Handled;

				case StandardIds.Paste:
					replEditor.ReplEditorOperations.Paste();
					return CommandTargetStatus.Handled;

				case StandardIds.Redo:
				case StandardIds.Undo:
					replEditor.SearchText = null;
					Debug.Assert(nextCommandTarget != null);
					return nextCommandTarget?.Execute(group, cmdId, args, ref result) ?? CommandTargetStatus.LetWpfHandleCommand;

				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			else if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.BACKSPACE:
					if (replEditor.ReplEditorOperations.ProvisionalCompositionSpan != null)
						replEditor.ReplEditorOperations.InsertText(string.Empty);
					else
						replEditor.ReplEditorOperations.Backspace();
					return CommandTargetStatus.Handled;

				case TextEditorIds.BACKTAB:
					replEditor.ReplEditorOperations.Unindent();
					return CommandTargetStatus.Handled;

				case TextEditorIds.BOL:
					replEditor.ReplEditorOperations.MoveToHome(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.BOL_EXT:
					replEditor.ReplEditorOperations.MoveToHome(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.BOL_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					replEditor.ReplEditorOperations.MoveToHome(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.BOTTOMLINE:
					replEditor.ReplEditorOperations.MoveToBottomOfView(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.BOTTOMLINE_EXT:
					replEditor.ReplEditorOperations.MoveToBottomOfView(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.CANCEL:
					replEditor.ReplEditorOperations.ClearInput();
					replEditor.ReplEditorOperations.ResetSelection();
					return CommandTargetStatus.Handled;

				case TextEditorIds.CUTLINE:
					replEditor.ReplEditorOperations.CutFullLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETE:
					replEditor.ReplEditorOperations.Delete();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETEBLANKLINES:
					replEditor.ReplEditorOperations.DeleteBlankLines();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETELINE:
					replEditor.ReplEditorOperations.DeleteFullLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETETOBOL:
					replEditor.ReplEditorOperations.DeleteToBeginningOfLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETETOEOL:
					replEditor.ReplEditorOperations.DeleteToEndOfLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETEWHITESPACE:
					replEditor.ReplEditorOperations.DeleteHorizontalWhiteSpace();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETEWORDLEFT:
					replEditor.ReplEditorOperations.DeleteWordToLeft();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETEWORDRIGHT:
					replEditor.ReplEditorOperations.DeleteWordToRight();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DOWN:
					replEditor.ReplEditorOperations.MoveLineDown(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.DOWN_EXT:
					replEditor.ReplEditorOperations.MoveLineDown(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.DOWN_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					replEditor.ReplEditorOperations.MoveLineDown(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.TOGGLEWORDWRAP:
					textView.Options.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, textView.Options.WordWrapStyle() ^ WordWrapStyles.WordWrap);
					return CommandTargetStatus.Handled;

				case TextEditorIds.ECMD_CONVERTSPACESTOTABS:
					replEditor.ReplEditorOperations.ConvertSpacesToTabs();
					return CommandTargetStatus.Handled;

				case TextEditorIds.ECMD_CONVERTTABSTOSPACES:
					replEditor.ReplEditorOperations.ConvertTabsToSpaces();
					return CommandTargetStatus.Handled;

				case TextEditorIds.EditorLineFirstColumn:
					replEditor.ReplEditorOperations.MoveToStartOfLine(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.EditorLineFirstColumnExtend:
					replEditor.ReplEditorOperations.MoveToStartOfLine(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.END:
					replEditor.ReplEditorOperations.MoveToEndOfDocument(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.END_EXT:
					replEditor.ReplEditorOperations.MoveToEndOfDocument(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.EOL:
					replEditor.ReplEditorOperations.MoveToEndOfLine(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.EOL_EXT:
					replEditor.ReplEditorOperations.MoveToEndOfLine(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.EOL_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					replEditor.ReplEditorOperations.MoveToEndOfLine(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.FIRSTCHAR:
					replEditor.ReplEditorOperations.MoveToStartOfLineAfterWhiteSpace(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.FIRSTCHAR_EXT:
					replEditor.ReplEditorOperations.MoveToStartOfLineAfterWhiteSpace(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.FIRSTNONWHITENEXT:
					replEditor.ReplEditorOperations.MoveToStartOfNextLineAfterWhiteSpace(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.FIRSTNONWHITEPREV:
					replEditor.ReplEditorOperations.MoveToStartOfPreviousLineAfterWhiteSpace(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.GOTOBRACE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.GOTOBRACE_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.HOME:
					replEditor.ReplEditorOperations.MoveToStartOfDocument(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.HOME_EXT:
					replEditor.ReplEditorOperations.MoveToStartOfDocument(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.INDENT:
					replEditor.ReplEditorOperations.IncreaseLineIndent();
					return CommandTargetStatus.Handled;

				case TextEditorIds.LASTCHAR:
					replEditor.ReplEditorOperations.MoveToLastNonWhiteSpaceCharacter(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.LASTCHAR_EXT:
					replEditor.ReplEditorOperations.MoveToLastNonWhiteSpaceCharacter(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.LEFT:
					replEditor.ReplEditorOperations.MoveToPreviousCharacter(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.LEFT_EXT:
					replEditor.ReplEditorOperations.MoveToPreviousCharacter(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.LEFT_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					replEditor.ReplEditorOperations.MoveToPreviousCharacter(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.MoveSelLinesDown:
					replEditor.ReplEditorOperations.MoveSelectedLinesDown();
					return CommandTargetStatus.Handled;

				case TextEditorIds.MoveSelLinesUp:
					replEditor.ReplEditorOperations.MoveSelectedLinesUp();
					return CommandTargetStatus.Handled;

				case TextEditorIds.OPENLINEABOVE:
					replEditor.ReplEditorOperations.OpenLineAbove();
					return CommandTargetStatus.Handled;

				case TextEditorIds.OPENLINEBELOW:
					replEditor.ReplEditorOperations.OpenLineBelow();
					return CommandTargetStatus.Handled;

				case TextEditorIds.PAGEDN:
					replEditor.ReplEditorOperations.PageDown(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.PAGEDN_EXT:
					replEditor.ReplEditorOperations.PageDown(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.PAGEUP:
					replEditor.ReplEditorOperations.PageUp(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.PAGEUP_EXT:
					replEditor.ReplEditorOperations.PageUp(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.RETURN:
					replEditor.ReplEditorOperations.InsertNewLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.RIGHT:
					replEditor.ReplEditorOperations.MoveToNextCharacter(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.RIGHT_EXT:
					replEditor.ReplEditorOperations.MoveToNextCharacter(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.RIGHT_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					replEditor.ReplEditorOperations.MoveToNextCharacter(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLBOTTOM:
					replEditor.ReplEditorOperations.ScrollLineBottom();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLCENTER:
					replEditor.ReplEditorOperations.ScrollLineCenter();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLDN:
					replEditor.ReplEditorOperations.ScrollDownAndMoveCaretIfNecessary();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLLEFT:
					replEditor.ReplEditorOperations.ScrollColumnLeft();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLPAGEDN:
					replEditor.ReplEditorOperations.ScrollPageDown();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLPAGEUP:
					replEditor.ReplEditorOperations.ScrollPageUp();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLRIGHT:
					replEditor.ReplEditorOperations.ScrollColumnRight();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLTOP:
					replEditor.ReplEditorOperations.ScrollLineTop();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLUP:
					replEditor.ReplEditorOperations.ScrollUpAndMoveCaretIfNecessary();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELECTALL:
					replEditor.ReplEditorOperations.SelectAll();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELECTCURRENTWORD:
					replEditor.ReplEditorOperations.SelectCurrentWord();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELLOWCASE:
					replEditor.ReplEditorOperations.MakeLowercase();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELSWAPANCHOR:
					replEditor.ReplEditorOperations.SwapCaretAndAnchor();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELTABIFY:
					replEditor.ReplEditorOperations.Tabify();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELTITLECASE:
					replEditor.ReplEditorOperations.Capitalize();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELTOGGLECASE:
					replEditor.ReplEditorOperations.ToggleCase();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELUNTABIFY:
					replEditor.ReplEditorOperations.Untabify();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELUPCASE:
					replEditor.ReplEditorOperations.MakeUppercase();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SmartBreakLine:
					replEditor.ReplEditorOperations.InsertNewLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.TAB:
					replEditor.ReplEditorOperations.Indent();
					return CommandTargetStatus.Handled;

				case TextEditorIds.COMPLETEWORD:
				case TextEditorIds.DECREASEFILTER:
				case TextEditorIds.GOTOLINE:
				case TextEditorIds.INCREASEFILTER:
				case TextEditorIds.SHOWMEMBERLIST:
				case TextEditorIds.TOGGLE_OVERTYPE_MODE:
				case TextEditorIds.ToggleConsumeFirstCompletionMode:
				case TextEditorIds.TOGGLEVISSPACE:
					return CommandTargetStatus.NotHandled;

				case TextEditorIds.TOPLINE:
					replEditor.ReplEditorOperations.MoveToTopOfView(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.TOPLINE_EXT:
					replEditor.ReplEditorOperations.MoveToTopOfView(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.TRANSPOSECHAR:
					replEditor.ReplEditorOperations.TransposeCharacter();
					return CommandTargetStatus.Handled;

				case TextEditorIds.TRANSPOSELINE:
					replEditor.ReplEditorOperations.TransposeLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.TRANSPOSEWORD:
					replEditor.ReplEditorOperations.TransposeWord();
					return CommandTargetStatus.Handled;

				case TextEditorIds.TYPECHAR:
					if (args is string)
						replEditor.ReplEditorOperations.InsertText((string)args);
					return CommandTargetStatus.Handled;

				case TextEditorIds.UNINDENT:
					replEditor.ReplEditorOperations.DecreaseLineIndent();
					return CommandTargetStatus.Handled;

				case TextEditorIds.UP:
					replEditor.ReplEditorOperations.MoveLineUp(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.UP_EXT:
					replEditor.ReplEditorOperations.MoveLineUp(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.UP_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					replEditor.ReplEditorOperations.MoveLineUp(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDNEXT:
					replEditor.ReplEditorOperations.MoveToNextWord(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDNEXT_EXT:
					replEditor.ReplEditorOperations.MoveToNextWord(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDNEXT_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					replEditor.ReplEditorOperations.MoveToNextWord(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDPREV:
					replEditor.ReplEditorOperations.MoveToPreviousWord(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDPREV_EXT:
					replEditor.ReplEditorOperations.MoveToPreviousWord(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDPREV_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					replEditor.ReplEditorOperations.MoveToPreviousWord(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.ZoomIn:
					replEditor.ReplEditorOperations.ZoomIn();
					return CommandTargetStatus.Handled;

				case TextEditorIds.ZoomOut:
					replEditor.ReplEditorOperations.ZoomOut();
					return CommandTargetStatus.Handled;

				case TextEditorIds.ZoomReset:
					replEditor.ReplEditorOperations.ZoomTo(ZoomConstants.DefaultZoom);
					return CommandTargetStatus.Handled;

				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			else if (group == CommandConstants.ReplGroup) {
				switch ((ReplIds)cmdId) {
				case ReplIds.CopyCode:
					replEditor.ReplEditorOperations.CopyCode();
					return CommandTargetStatus.Handled;

				case ReplIds.Submit:
					replEditor.ReplEditorOperations.Submit();
					return CommandTargetStatus.Handled;

				case ReplIds.NewLineDontSubmit:
					replEditor.ReplEditorOperations.InsertNewLineDontSubmit();
					return CommandTargetStatus.Handled;

				case ReplIds.ClearInput:
					replEditor.ReplEditorOperations.ClearInput();
					return CommandTargetStatus.Handled;

				case ReplIds.ClearScreen:
					replEditor.ReplEditorOperations.ClearScreen();
					return CommandTargetStatus.Handled;

				case ReplIds.Reset:
					replEditor.ReplEditorOperations.Reset();
					return CommandTargetStatus.Handled;

				case ReplIds.SelectPreviousCommand:
					replEditor.ReplEditorOperations.SelectPreviousCommand();
					return CommandTargetStatus.Handled;

				case ReplIds.SelectNextCommand:
					replEditor.ReplEditorOperations.SelectNextCommand();
					return CommandTargetStatus.Handled;

				case ReplIds.SelectSameTextPreviousCommand:
					replEditor.ReplEditorOperations.SelectSameTextPreviousCommand();
					return CommandTargetStatus.Handled;

				case ReplIds.SelectSameTextNextCommand:
					replEditor.ReplEditorOperations.SelectSameTextNextCommand();
					return CommandTargetStatus.Handled;

				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) => nextCommandTarget = commandTarget;
		ICommandTarget nextCommandTarget;

		public void Dispose() { }
	}
}

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
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;

namespace dnSpy.Text.Editor {
	[ExportCommandTargetFilterCreator(CommandConstants.CMDTARGETFILTER_ORDER_TEXT_EDITOR)]
	sealed class DefaultTextViewCommandTargetFilterCreator : ICommandTargetFilterCreator {
		readonly IEditorOperationsFactoryService editorOperationsFactoryService;

		[ImportingConstructor]
		DefaultTextViewCommandTargetFilterCreator(IEditorOperationsFactoryService editorOperationsFactoryService) {
			this.editorOperationsFactoryService = editorOperationsFactoryService;
		}

		public ICommandTargetFilter Create(object target) {
			var textView = target as ITextView;
			if (textView != null)
				return new DefaultTextViewCommandTarget(textView, editorOperationsFactoryService);
			return null;
		}
	}

	sealed class DefaultTextViewCommandTarget : ICommandTargetFilter {
		readonly ITextView textView;

		IEditorOperations EditorOperations { get; }
		IEditorOperations2 EditorOperations2 => EditorOperations as IEditorOperations2;

		public DefaultTextViewCommandTarget(ITextView textView, IEditorOperationsFactoryService editorOperationsFactoryService) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			this.textView = textView;
			EditorOperations = editorOperationsFactoryService.GetEditorOperations(textView);
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

		bool IsReadOnly => textView.Options.DoesViewProhibitUserInput();

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
				case TextEditorIds.CANCEL:
					// Seems to match VS behavior. If we handle ESC when there's no selection, we can't press
					// ESC in the log editor and move back to the document tab.
					return textView.Selection.IsEmpty ? CommandTargetStatus.NotHandled : CommandTargetStatus.Handled;
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
					EditorOperations.CopySelection();
					return CommandTargetStatus.Handled;

				case DefaultIds.Cut:
					EditorOperations.CutSelection();
					return CommandTargetStatus.Handled;

				case DefaultIds.Paste:
					EditorOperations.Paste();
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
					if (EditorOperations.ProvisionalCompositionSpan != null)
						EditorOperations.InsertText(string.Empty);
					else
						EditorOperations.Backspace();
					return CommandTargetStatus.Handled;

				case TextEditorIds.BACKTAB:
					EditorOperations.Unindent();
					return CommandTargetStatus.Handled;

				case TextEditorIds.BOL:
					EditorOperations.MoveToHome(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.BOL_EXT:
					EditorOperations.MoveToHome(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.BOL_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					EditorOperations.MoveToHome(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.BOTTOMLINE:
					EditorOperations.MoveToBottomOfView(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.BOTTOMLINE_EXT:
					EditorOperations.MoveToBottomOfView(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.CANCEL:
					EditorOperations.ResetSelection();
					return CommandTargetStatus.Handled;

				case TextEditorIds.CUTLINE:
					EditorOperations.CutFullLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETE:
					EditorOperations.Delete();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETEBLANKLINES:
					EditorOperations.DeleteBlankLines();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETELINE:
					EditorOperations.DeleteFullLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETETOBOL:
					EditorOperations.DeleteToBeginningOfLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETETOEOL:
					EditorOperations.DeleteToEndOfLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETEWHITESPACE:
					EditorOperations.DeleteHorizontalWhiteSpace();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETEWORDLEFT:
					EditorOperations.DeleteWordToLeft();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DELETEWORDRIGHT:
					EditorOperations.DeleteWordToRight();
					return CommandTargetStatus.Handled;

				case TextEditorIds.DOWN:
					EditorOperations.MoveLineDown(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.DOWN_EXT:
					EditorOperations.MoveLineDown(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.DOWN_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					EditorOperations.MoveLineDown(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.ECMD_CONVERTSPACESTOTABS:
					EditorOperations.ConvertSpacesToTabs();
					return CommandTargetStatus.Handled;

				case TextEditorIds.ECMD_CONVERTTABSTOSPACES:
					EditorOperations.ConvertTabsToSpaces();
					return CommandTargetStatus.Handled;

				case TextEditorIds.EditorLineFirstColumn:
					EditorOperations.MoveToStartOfLine(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.EditorLineFirstColumnExtend:
					EditorOperations.MoveToStartOfLine(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.END:
					EditorOperations.MoveToEndOfDocument(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.END_EXT:
					EditorOperations.MoveToEndOfDocument(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.EOL:
					EditorOperations.MoveToEndOfLine(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.EOL_EXT:
					EditorOperations.MoveToEndOfLine(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.EOL_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					EditorOperations.MoveToEndOfLine(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.FIRSTCHAR:
					EditorOperations.MoveToStartOfLineAfterWhiteSpace(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.FIRSTCHAR_EXT:
					EditorOperations.MoveToStartOfLineAfterWhiteSpace(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.FIRSTNONWHITENEXT:
					EditorOperations.MoveToStartOfNextLineAfterWhiteSpace(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.FIRSTNONWHITEPREV:
					EditorOperations.MoveToStartOfPreviousLineAfterWhiteSpace(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.GOTOBRACE:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.GOTOBRACE_EXT:
					return CommandTargetStatus.Handled;//TODO:

				case TextEditorIds.GOTOLINE:
					if (args is int)
						EditorOperations.GotoLine((int)args);
					return CommandTargetStatus.Handled;

				case TextEditorIds.HOME:
					EditorOperations.MoveToStartOfDocument(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.HOME_EXT:
					EditorOperations.MoveToStartOfDocument(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.INDENT:
					EditorOperations.IncreaseLineIndent();
					return CommandTargetStatus.Handled;

				case TextEditorIds.LASTCHAR:
					EditorOperations.MoveToLastNonWhiteSpaceCharacter(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.LASTCHAR_EXT:
					EditorOperations.MoveToLastNonWhiteSpaceCharacter(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.LEFT:
					EditorOperations.MoveToPreviousCharacter(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.LEFT_EXT:
					EditorOperations.MoveToPreviousCharacter(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.LEFT_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					EditorOperations.MoveToPreviousCharacter(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.MoveSelLinesDown:
					EditorOperations2?.MoveSelectedLinesDown();
					return CommandTargetStatus.Handled;

				case TextEditorIds.MoveSelLinesUp:
					EditorOperations2?.MoveSelectedLinesUp();
					return CommandTargetStatus.Handled;

				case TextEditorIds.OPENLINEABOVE:
					EditorOperations.OpenLineAbove();
					return CommandTargetStatus.Handled;

				case TextEditorIds.OPENLINEBELOW:
					EditorOperations.OpenLineBelow();
					return CommandTargetStatus.Handled;

				case TextEditorIds.PAGEDN:
					EditorOperations.PageDown(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.PAGEDN_EXT:
					EditorOperations.PageDown(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.PAGEUP:
					EditorOperations.PageUp(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.PAGEUP_EXT:
					EditorOperations.PageUp(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.RETURN:
					EditorOperations.InsertNewLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.RIGHT:
					EditorOperations.MoveToNextCharacter(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.RIGHT_EXT:
					EditorOperations.MoveToNextCharacter(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.RIGHT_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					EditorOperations.MoveToNextCharacter(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLBOTTOM:
					EditorOperations.ScrollLineBottom();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLCENTER:
					EditorOperations.ScrollLineCenter();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLDN:
					EditorOperations.ScrollDownAndMoveCaretIfNecessary();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLLEFT:
					EditorOperations.ScrollColumnLeft();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLPAGEDN:
					EditorOperations.ScrollPageDown();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLPAGEUP:
					EditorOperations.ScrollPageUp();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLRIGHT:
					EditorOperations.ScrollColumnRight();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLTOP:
					EditorOperations.ScrollLineTop();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SCROLLUP:
					EditorOperations.ScrollUpAndMoveCaretIfNecessary();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELECTALL:
					EditorOperations.SelectAll();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELECTCURRENTWORD:
					EditorOperations.SelectCurrentWord();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELLOWCASE:
					EditorOperations.MakeLowercase();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELSWAPANCHOR:
					EditorOperations.SwapCaretAndAnchor();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELTABIFY:
					EditorOperations.Tabify();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELTITLECASE:
					EditorOperations.Capitalize();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELTOGGLECASE:
					EditorOperations.ToggleCase();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELUNTABIFY:
					EditorOperations.Untabify();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SELUPCASE:
					EditorOperations.MakeUppercase();
					return CommandTargetStatus.Handled;

				case TextEditorIds.SmartBreakLine:
					EditorOperations.InsertNewLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.TAB:
					EditorOperations.Indent();
					return CommandTargetStatus.Handled;

				case TextEditorIds.TOGGLE_OVERTYPE_MODE:
					if (textView.Options.IsCanChangeOverwriteModeEnabled())
						textView.Options.SetOptionValue(DefaultTextViewOptions.OverwriteModeId, !textView.Options.IsOverwriteModeEnabled());
					return CommandTargetStatus.Handled;

				case TextEditorIds.TOGGLEVISSPACE:
					if (textView.Options.GlobalOptions.IsCanChangeUseVisibleWhitespaceEnabled())
						textView.Options.GlobalOptions.SetOptionValue(DefaultTextViewOptions.UseVisibleWhitespaceId, !textView.Options.GlobalOptions.IsVisibleWhitespaceEnabled());
					return CommandTargetStatus.Handled;

				case TextEditorIds.TOGGLEWORDWRAP:
					if (textView.Options.GlobalOptions.IsCanChangeWordWrapStyleEnabled()) {
						var newWordwrapStyle = textView.Options.GlobalOptions.WordWrapStyle() ^ WordWrapStyles.WordWrap;
						textView.Options.GlobalOptions.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, newWordwrapStyle);
						if ((newWordwrapStyle & WordWrapStyles.WordWrap) != 0 && textView.Options.IsVirtualSpaceEnabled())
							textView.Options.SetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId, false);
					}
					return CommandTargetStatus.Handled;

				case TextEditorIds.TOPLINE:
					EditorOperations.MoveToTopOfView(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.TOPLINE_EXT:
					EditorOperations.MoveToTopOfView(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.TRANSPOSECHAR:
					EditorOperations.TransposeCharacter();
					return CommandTargetStatus.Handled;

				case TextEditorIds.TRANSPOSELINE:
					EditorOperations.TransposeLine();
					return CommandTargetStatus.Handled;

				case TextEditorIds.TRANSPOSEWORD:
					EditorOperations.TransposeWord();
					return CommandTargetStatus.Handled;

				case TextEditorIds.TYPECHAR:
					if (args is string)
						EditorOperations.InsertText((string)args);
					return CommandTargetStatus.Handled;

				case TextEditorIds.UNINDENT:
					EditorOperations.DecreaseLineIndent();
					return CommandTargetStatus.Handled;

				case TextEditorIds.UP:
					EditorOperations.MoveLineUp(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.UP_EXT:
					EditorOperations.MoveLineUp(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.UP_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					EditorOperations.MoveLineUp(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDNEXT:
					EditorOperations.MoveToNextWord(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDNEXT_EXT:
					EditorOperations.MoveToNextWord(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDNEXT_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					EditorOperations.MoveToNextWord(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDPREV:
					EditorOperations.MoveToPreviousWord(false);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDPREV_EXT:
					EditorOperations.MoveToPreviousWord(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.WORDPREV_EXT_COL:
					textView.Selection.Mode = TextSelectionMode.Box;
					EditorOperations.MoveToPreviousWord(true);
					return CommandTargetStatus.Handled;

				case TextEditorIds.ZoomIn:
					EditorOperations.ZoomIn();
					return CommandTargetStatus.Handled;

				case TextEditorIds.ZoomOut:
					EditorOperations.ZoomOut();
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

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
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using dnSpy.Contracts.Hex.Operations;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Hex.Editor {
	[ExportCommandTargetFilterProvider(CommandTargetFilterOrder.HexEditor)]
	sealed class DefaultHexViewCommandTargetFilterProvider : ICommandTargetFilterProvider {
		readonly Lazy<HexEditorOperationsFactoryService> editorOperationsFactoryService;

		[ImportingConstructor]
		DefaultHexViewCommandTargetFilterProvider(Lazy<HexEditorOperationsFactoryService> editorOperationsFactoryService) {
			this.editorOperationsFactoryService = editorOperationsFactoryService;
		}

		public ICommandTargetFilter Create(object target) {
			var hexView = target as HexView;
			if (hexView != null)
				return new DefaultHexViewCommandTarget(hexView, editorOperationsFactoryService.Value);
			return null;
		}
	}

	sealed class DefaultHexViewCommandTarget : ICommandTargetFilter {
		readonly HexView hexView;

		HexEditorOperations EditorOperations { get; }

		public DefaultHexViewCommandTarget(HexView hexView, HexEditorOperationsFactoryService editorOperationsFactoryService) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			this.hexView = hexView;
			EditorOperations = editorOperationsFactoryService.GetEditorOperations(hexView);
		}

		static bool IsEditCommand(Guid group, int cmdId) {
			if (group == CommandConstants.StandardGroup) {
				switch ((StandardIds)cmdId) {
				case StandardIds.Cut:
				case StandardIds.Paste:
				case StandardIds.Redo:
				case StandardIds.Undo:
				case StandardIds.Replace:
					return true;

				case StandardIds.Unknown:
				case StandardIds.Copy:
				case StandardIds.Find:
				case StandardIds.IncrementalSearchForward:
				case StandardIds.IncrementalSearchBackward:
				case StandardIds.FindNext:
				case StandardIds.FindPrevious:
				case StandardIds.FindNextSelected:
				case StandardIds.FindPreviousSelected:
					return false;

				default:
					Debug.Fail($"Unknown {nameof(StandardIds)} value: {group} {(StandardIds)cmdId}");
					return true;
				}
			}
			else if (group == CommandConstants.HexEditorGroup) {
				switch ((HexEditorIds)cmdId) {
				case HexEditorIds.BACKSPACE:
				case HexEditorIds.BACKTAB:
				case HexEditorIds.DELETE:
				case HexEditorIds.DELETELINE:
				case HexEditorIds.DELETETOBOL:
				case HexEditorIds.DELETETOEOL:
				case HexEditorIds.PasteBlob:
				case HexEditorIds.PasteUnicodeString:
				case HexEditorIds.PasteUnicodeString7BitEncodedLengthPrefix:
				case HexEditorIds.PasteUtf8String:
				case HexEditorIds.PasteUtf8String7BitEncodedLengthPrefix:
				case HexEditorIds.TYPECHAR:
					return true;

				case HexEditorIds.BOL:
				case HexEditorIds.BOL_EXT:
				case HexEditorIds.BOTTOMLINE:
				case HexEditorIds.BOTTOMLINE_EXT:
				case HexEditorIds.CANCEL:
				case HexEditorIds.CopyAbsoluteFileOffset:
				case HexEditorIds.CopyCSharpArray:
				case HexEditorIds.CopyFileOffset:
				case HexEditorIds.CopyOffset:
				case HexEditorIds.CopyRVA:
				case HexEditorIds.CopyText:
				case HexEditorIds.CopyUInt16:
				case HexEditorIds.CopyUInt16BigEndian:
				case HexEditorIds.CopyUInt32:
				case HexEditorIds.CopyUInt32BigEndian:
				case HexEditorIds.CopyUInt64:
				case HexEditorIds.CopyUInt64BigEndian:
				case HexEditorIds.CopyUnicodeString:
				case HexEditorIds.CopyUtf8String:
				case HexEditorIds.CopyValue:
				case HexEditorIds.CopyVisualBasicArray:
				case HexEditorIds.DECREASEFILTER:
				case HexEditorIds.DOWN:
				case HexEditorIds.DOWN_EXT:
				case HexEditorIds.END:
				case HexEditorIds.END_EXT:
				case HexEditorIds.EOL:
				case HexEditorIds.EOL_EXT:
				case HexEditorIds.HOME:
				case HexEditorIds.HOME_EXT:
				case HexEditorIds.INCREASEFILTER:
				case HexEditorIds.LEFT:
				case HexEditorIds.LEFT_EXT:
				case HexEditorIds.MoveToNextValidStartEnd:
				case HexEditorIds.MoveToNextValidStartEndExt:
				case HexEditorIds.MoveToPreviousValidStartEnd:
				case HexEditorIds.MoveToPreviousValidStartEndExt:
				case HexEditorIds.PAGEDN:
				case HexEditorIds.PAGEDN_EXT:
				case HexEditorIds.PAGEUP:
				case HexEditorIds.PAGEUP_EXT:
				case HexEditorIds.QUICKINFO:
				case HexEditorIds.Refresh:
				case HexEditorIds.RETURN:
				case HexEditorIds.RIGHT:
				case HexEditorIds.RIGHT_EXT:
				case HexEditorIds.SCROLLBOTTOM:
				case HexEditorIds.SCROLLCENTER:
				case HexEditorIds.SCROLLDN:
				case HexEditorIds.SCROLLLEFT:
				case HexEditorIds.SCROLLPAGEDN:
				case HexEditorIds.SCROLLPAGEUP:
				case HexEditorIds.SCROLLRIGHT:
				case HexEditorIds.SCROLLTOP:
				case HexEditorIds.SCROLLUP:
				case HexEditorIds.SELECTALL:
				case HexEditorIds.SelectAllBytesBlock:
				case HexEditorIds.SELECTCURRENTWORD:
				case HexEditorIds.SELSWAPANCHOR:
				case HexEditorIds.ShowAllBytes:
				case HexEditorIds.ShowOnlySelectedBytes:
				case HexEditorIds.TAB:
				case HexEditorIds.TOGGLE_OVERTYPE_MODE:
				case HexEditorIds.TOPLINE:
				case HexEditorIds.TOPLINE_EXT:
				case HexEditorIds.UP:
				case HexEditorIds.UP_EXT:
				case HexEditorIds.WORDNEXT:
				case HexEditorIds.WORDNEXT_EXT:
				case HexEditorIds.WORDPREV:
				case HexEditorIds.WORDPREV_EXT:
				case HexEditorIds.ZoomIn:
				case HexEditorIds.ZoomOut:
				case HexEditorIds.ZoomReset:
					return false;

				default:
					Debug.Fail($"Unknown {nameof(HexEditorIds)} value: {group} {(HexEditorIds)cmdId}");
					return true;
				}
			}
			return false;
		}

		bool IsReadOnly => hexView.Options.DoesViewProhibitUserInput();

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (IsReadOnly && IsEditCommand(group, cmdId))
				return CommandTargetStatus.NotHandled;

			if (group == CommandConstants.StandardGroup) {
				switch ((StandardIds)cmdId) {
				case StandardIds.Copy:
				case StandardIds.Cut:
				case StandardIds.Paste:
				case StandardIds.Replace:
					return CommandTargetStatus.Handled;
				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			else if (group == CommandConstants.HexEditorGroup) {
				switch ((HexEditorIds)cmdId) {
				case HexEditorIds.BACKSPACE:
				case HexEditorIds.BACKTAB:
				case HexEditorIds.BOL:
				case HexEditorIds.BOL_EXT:
				case HexEditorIds.BOTTOMLINE:
				case HexEditorIds.BOTTOMLINE_EXT:
				case HexEditorIds.CopyAbsoluteFileOffset:
				case HexEditorIds.CopyCSharpArray:
				case HexEditorIds.CopyFileOffset:
				case HexEditorIds.CopyOffset:
				case HexEditorIds.CopyRVA:
				case HexEditorIds.CopyText:
				case HexEditorIds.CopyUInt16:
				case HexEditorIds.CopyUInt16BigEndian:
				case HexEditorIds.CopyUInt32:
				case HexEditorIds.CopyUInt32BigEndian:
				case HexEditorIds.CopyUInt64:
				case HexEditorIds.CopyUInt64BigEndian:
				case HexEditorIds.CopyUnicodeString:
				case HexEditorIds.CopyUtf8String:
				case HexEditorIds.CopyValue:
				case HexEditorIds.CopyVisualBasicArray:
				case HexEditorIds.DELETE:
				case HexEditorIds.DELETELINE:
				case HexEditorIds.DELETETOBOL:
				case HexEditorIds.DELETETOEOL:
				case HexEditorIds.DOWN:
				case HexEditorIds.DOWN_EXT:
				case HexEditorIds.END:
				case HexEditorIds.END_EXT:
				case HexEditorIds.EOL:
				case HexEditorIds.EOL_EXT:
				case HexEditorIds.HOME:
				case HexEditorIds.HOME_EXT:
				case HexEditorIds.LEFT:
				case HexEditorIds.LEFT_EXT:
				case HexEditorIds.MoveToNextValidStartEnd:
				case HexEditorIds.MoveToNextValidStartEndExt:
				case HexEditorIds.MoveToPreviousValidStartEnd:
				case HexEditorIds.MoveToPreviousValidStartEndExt:
				case HexEditorIds.PAGEDN:
				case HexEditorIds.PAGEDN_EXT:
				case HexEditorIds.PAGEUP:
				case HexEditorIds.PAGEUP_EXT:
				case HexEditorIds.PasteBlob:
				case HexEditorIds.PasteUnicodeString:
				case HexEditorIds.PasteUnicodeString7BitEncodedLengthPrefix:
				case HexEditorIds.PasteUtf8String:
				case HexEditorIds.PasteUtf8String7BitEncodedLengthPrefix:
				case HexEditorIds.Refresh:
				case HexEditorIds.RETURN:
				case HexEditorIds.RIGHT:
				case HexEditorIds.RIGHT_EXT:
				case HexEditorIds.SCROLLBOTTOM:
				case HexEditorIds.SCROLLCENTER:
				case HexEditorIds.SCROLLDN:
				case HexEditorIds.SCROLLLEFT:
				case HexEditorIds.SCROLLPAGEDN:
				case HexEditorIds.SCROLLPAGEUP:
				case HexEditorIds.SCROLLRIGHT:
				case HexEditorIds.SCROLLTOP:
				case HexEditorIds.SCROLLUP:
				case HexEditorIds.SELECTALL:
				case HexEditorIds.SelectAllBytesBlock:
				case HexEditorIds.SELECTCURRENTWORD:
				case HexEditorIds.SELSWAPANCHOR:
				case HexEditorIds.ShowAllBytes:
				case HexEditorIds.ShowOnlySelectedBytes:
				case HexEditorIds.TAB:
				case HexEditorIds.TOGGLE_OVERTYPE_MODE:
				case HexEditorIds.TOPLINE:
				case HexEditorIds.TOPLINE_EXT:
				case HexEditorIds.TYPECHAR:
				case HexEditorIds.UP:
				case HexEditorIds.UP_EXT:
				case HexEditorIds.WORDNEXT:
				case HexEditorIds.WORDNEXT_EXT:
				case HexEditorIds.WORDPREV:
				case HexEditorIds.WORDPREV_EXT:
				case HexEditorIds.ZoomIn:
				case HexEditorIds.ZoomOut:
				case HexEditorIds.ZoomReset:
					return CommandTargetStatus.Handled;
				case HexEditorIds.CANCEL:
					// Seems to match VS behavior. If we handle ESC when there's no selection, we can't press
					// ESC in the log editor and move back to the document tab.
					return hexView.Selection.IsEmpty ? CommandTargetStatus.NotHandled : CommandTargetStatus.Handled;
				case HexEditorIds.DECREASEFILTER:
				case HexEditorIds.INCREASEFILTER:
				case HexEditorIds.QUICKINFO:
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

			if (group == CommandConstants.StandardGroup) {
				switch ((StandardIds)cmdId) {
				case StandardIds.Copy:
					EditorOperations.CopySelectionBytes();
					return CommandTargetStatus.Handled;

				case StandardIds.Paste:
					EditorOperations.Paste();
					return CommandTargetStatus.Handled;

				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			else if (group == CommandConstants.HexEditorGroup) {
				switch ((HexEditorIds)cmdId) {
				case HexEditorIds.BOL:
					EditorOperations.MoveToStartOfLine(false);
					return CommandTargetStatus.Handled;

				case HexEditorIds.BOL_EXT:
					EditorOperations.MoveToStartOfLine(true);
					return CommandTargetStatus.Handled;

				case HexEditorIds.BOTTOMLINE:
					EditorOperations.MoveToBottomOfView(false);
					return CommandTargetStatus.Handled;

				case HexEditorIds.BOTTOMLINE_EXT:
					EditorOperations.MoveToBottomOfView(true);
					return CommandTargetStatus.Handled;

				case HexEditorIds.CANCEL:
					EditorOperations.ResetSelection();
					return CommandTargetStatus.Handled;

				case HexEditorIds.CopyText:
					EditorOperations.CopySelectionText();
					return CommandTargetStatus.Handled;

				case HexEditorIds.CopyUtf8String:
					EditorOperations.CopySpecial(HexCopySpecialKind.Utf8String);
					return CommandTargetStatus.Handled;

				case HexEditorIds.CopyUnicodeString:
					EditorOperations.CopySpecial(HexCopySpecialKind.UnicodeString);
					return CommandTargetStatus.Handled;

				case HexEditorIds.CopyCSharpArray:
					EditorOperations.CopySpecial(HexCopySpecialKind.CSharpArray);
					return CommandTargetStatus.Handled;

				case HexEditorIds.CopyVisualBasicArray:
					EditorOperations.CopySpecial(HexCopySpecialKind.VisualBasicArray);
					return CommandTargetStatus.Handled;

				case HexEditorIds.CopyOffset:
					EditorOperations.CopySpecial(HexCopySpecialKind.Offset);
					return CommandTargetStatus.Handled;

				case HexEditorIds.CopyValue:
					EditorOperations.CopySpecial(HexCopySpecialKind.Value);
					return CommandTargetStatus.Handled;

				case HexEditorIds.CopyUInt16:
					EditorOperations.CopySpecial(HexCopySpecialKind.UInt16);
					return CommandTargetStatus.Handled;

				case HexEditorIds.CopyUInt16BigEndian:
					EditorOperations.CopySpecial(HexCopySpecialKind.UInt16BigEndian);
					return CommandTargetStatus.Handled;

				case HexEditorIds.CopyUInt32:
					EditorOperations.CopySpecial(HexCopySpecialKind.UInt32);
					return CommandTargetStatus.Handled;

				case HexEditorIds.CopyUInt32BigEndian:
					EditorOperations.CopySpecial(HexCopySpecialKind.UInt32BigEndian);
					return CommandTargetStatus.Handled;

				case HexEditorIds.CopyUInt64:
					EditorOperations.CopySpecial(HexCopySpecialKind.UInt64);
					return CommandTargetStatus.Handled;

				case HexEditorIds.CopyUInt64BigEndian:
					EditorOperations.CopySpecial(HexCopySpecialKind.UInt64BigEndian);
					return CommandTargetStatus.Handled;

				case HexEditorIds.CopyFileOffset:
					EditorOperations.CopySpecial(HexCopySpecialKind.FileOffset);
					return CommandTargetStatus.Handled;

				case HexEditorIds.CopyAbsoluteFileOffset:
					EditorOperations.CopySpecial(HexCopySpecialKind.AbsoluteFileOffset);
					return CommandTargetStatus.Handled;

				case HexEditorIds.CopyRVA:
					EditorOperations.CopySpecial(HexCopySpecialKind.RVA);
					return CommandTargetStatus.Handled;

				case HexEditorIds.DELETE:
					EditorOperations.ClearData();
					return CommandTargetStatus.Handled;

				case HexEditorIds.DOWN:
					EditorOperations.MoveLineDown(false);
					return CommandTargetStatus.Handled;

				case HexEditorIds.DOWN_EXT:
					EditorOperations.MoveLineDown(true);
					return CommandTargetStatus.Handled;

				case HexEditorIds.END:
					EditorOperations.MoveToEndOfDocument(false);
					return CommandTargetStatus.Handled;

				case HexEditorIds.END_EXT:
					EditorOperations.MoveToEndOfDocument(true);
					return CommandTargetStatus.Handled;

				case HexEditorIds.EOL:
					EditorOperations.MoveToEndOfLine(false);
					return CommandTargetStatus.Handled;

				case HexEditorIds.EOL_EXT:
					EditorOperations.MoveToEndOfLine(true);
					return CommandTargetStatus.Handled;

				case HexEditorIds.HOME:
					EditorOperations.MoveToStartOfDocument(false);
					return CommandTargetStatus.Handled;

				case HexEditorIds.HOME_EXT:
					EditorOperations.MoveToStartOfDocument(true);
					return CommandTargetStatus.Handled;

				case HexEditorIds.LEFT:
					EditorOperations.MoveToPreviousCharacter(false);
					return CommandTargetStatus.Handled;

				case HexEditorIds.LEFT_EXT:
					EditorOperations.MoveToPreviousCharacter(true);
					return CommandTargetStatus.Handled;

				case HexEditorIds.MoveToNextValidStartEnd:
					EditorOperations.MoveToNextValidStartEnd(false);
					return CommandTargetStatus.Handled;

				case HexEditorIds.MoveToNextValidStartEndExt:
					EditorOperations.MoveToNextValidStartEnd(true);
					return CommandTargetStatus.Handled;

				case HexEditorIds.MoveToPreviousValidStartEnd:
					EditorOperations.MoveToPreviousValidStartEnd(false);
					return CommandTargetStatus.Handled;

				case HexEditorIds.MoveToPreviousValidStartEndExt:
					EditorOperations.MoveToPreviousValidStartEnd(true);
					return CommandTargetStatus.Handled;

				case HexEditorIds.PAGEDN:
					EditorOperations.PageDown(false);
					return CommandTargetStatus.Handled;

				case HexEditorIds.PAGEDN_EXT:
					EditorOperations.PageDown(true);
					return CommandTargetStatus.Handled;

				case HexEditorIds.PAGEUP:
					EditorOperations.PageUp(false);
					return CommandTargetStatus.Handled;

				case HexEditorIds.PAGEUP_EXT:
					EditorOperations.PageUp(true);
					return CommandTargetStatus.Handled;

				case HexEditorIds.PasteBlob:
					EditorOperations.PasteSpecial(HexPasteSpecialKind.Blob);
					return CommandTargetStatus.Handled;

				case HexEditorIds.PasteUnicodeString:
					EditorOperations.PasteSpecial(HexPasteSpecialKind.UnicodeString);
					return CommandTargetStatus.Handled;

				case HexEditorIds.PasteUnicodeString7BitEncodedLengthPrefix:
					EditorOperations.PasteSpecial(HexPasteSpecialKind.UnicodeString7BitEncodedLengthPrefix);
					return CommandTargetStatus.Handled;

				case HexEditorIds.PasteUtf8String:
					EditorOperations.PasteSpecial(HexPasteSpecialKind.Utf8String);
					return CommandTargetStatus.Handled;

				case HexEditorIds.PasteUtf8String7BitEncodedLengthPrefix:
					EditorOperations.PasteSpecial(HexPasteSpecialKind.Utf8String7BitEncodedLengthPrefix);
					return CommandTargetStatus.Handled;

				case HexEditorIds.Refresh:
					EditorOperations.Refresh();
					return CommandTargetStatus.Handled;

				case HexEditorIds.RIGHT:
					EditorOperations.MoveToNextCharacter(false);
					return CommandTargetStatus.Handled;

				case HexEditorIds.RIGHT_EXT:
					EditorOperations.MoveToNextCharacter(true);
					return CommandTargetStatus.Handled;

				case HexEditorIds.SCROLLBOTTOM:
					EditorOperations.ScrollLineBottom();
					return CommandTargetStatus.Handled;

				case HexEditorIds.SCROLLCENTER:
					EditorOperations.ScrollLineCenter();
					return CommandTargetStatus.Handled;

				case HexEditorIds.SCROLLDN:
					EditorOperations.ScrollDownAndMoveCaretIfNecessary();
					return CommandTargetStatus.Handled;

				case HexEditorIds.SCROLLLEFT:
					EditorOperations.ScrollColumnLeft();
					return CommandTargetStatus.Handled;

				case HexEditorIds.SCROLLPAGEDN:
					EditorOperations.ScrollPageDown();
					return CommandTargetStatus.Handled;

				case HexEditorIds.SCROLLPAGEUP:
					EditorOperations.ScrollPageUp();
					return CommandTargetStatus.Handled;

				case HexEditorIds.SCROLLRIGHT:
					EditorOperations.ScrollColumnRight();
					return CommandTargetStatus.Handled;

				case HexEditorIds.SCROLLTOP:
					EditorOperations.ScrollLineTop();
					return CommandTargetStatus.Handled;

				case HexEditorIds.SCROLLUP:
					EditorOperations.ScrollUpAndMoveCaretIfNecessary();
					return CommandTargetStatus.Handled;

				case HexEditorIds.SELECTALL:
					EditorOperations.SelectAll();
					return CommandTargetStatus.Handled;

				case HexEditorIds.SelectAllBytesBlock:
					EditorOperations.SelectAllBytesBlock();
					return CommandTargetStatus.Handled;

				case HexEditorIds.SELECTCURRENTWORD:
					EditorOperations.SelectCurrentWord();
					return CommandTargetStatus.Handled;

				case HexEditorIds.SELSWAPANCHOR:
					EditorOperations.SwapCaretAndAnchor();
					return CommandTargetStatus.Handled;

				case HexEditorIds.ShowAllBytes:
					EditorOperations.ShowAllBytes();
					return CommandTargetStatus.Handled;

				case HexEditorIds.ShowOnlySelectedBytes:
					EditorOperations.ShowOnlySelectedBytes();
					return CommandTargetStatus.Handled;

				case HexEditorIds.TAB:
					EditorOperations.ToggleColumn();
					return CommandTargetStatus.Handled;

				case HexEditorIds.TOPLINE:
					EditorOperations.MoveToTopOfView(false);
					return CommandTargetStatus.Handled;

				case HexEditorIds.TOPLINE_EXT:
					EditorOperations.MoveToTopOfView(true);
					return CommandTargetStatus.Handled;

				case HexEditorIds.TYPECHAR:
					if (args is string)
						EditorOperations.InsertText((string)args);
					return CommandTargetStatus.Handled;

				case HexEditorIds.UP:
					EditorOperations.MoveLineUp(false);
					return CommandTargetStatus.Handled;

				case HexEditorIds.UP_EXT:
					EditorOperations.MoveLineUp(true);
					return CommandTargetStatus.Handled;

				case HexEditorIds.WORDNEXT:
					EditorOperations.MoveToNextWord(false);
					return CommandTargetStatus.Handled;

				case HexEditorIds.WORDNEXT_EXT:
					EditorOperations.MoveToNextWord(true);
					return CommandTargetStatus.Handled;

				case HexEditorIds.WORDPREV:
					EditorOperations.MoveToPreviousWord(false);
					return CommandTargetStatus.Handled;

				case HexEditorIds.WORDPREV_EXT:
					EditorOperations.MoveToPreviousWord(true);
					return CommandTargetStatus.Handled;

				case HexEditorIds.ZoomIn:
					EditorOperations.ZoomIn();
					return CommandTargetStatus.Handled;

				case HexEditorIds.ZoomOut:
					EditorOperations.ZoomOut();
					return CommandTargetStatus.Handled;

				case HexEditorIds.ZoomReset:
					EditorOperations.ZoomTo(VSTE.ZoomConstants.DefaultZoom);
					return CommandTargetStatus.Handled;

				case HexEditorIds.BACKSPACE:
				case HexEditorIds.BACKTAB:
				case HexEditorIds.DELETELINE:
				case HexEditorIds.DELETETOBOL:
				case HexEditorIds.DELETETOEOL:
				case HexEditorIds.RETURN:
				case HexEditorIds.TOGGLE_OVERTYPE_MODE:
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

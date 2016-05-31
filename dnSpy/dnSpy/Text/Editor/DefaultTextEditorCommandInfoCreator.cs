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
using System.Collections.Generic;
using System.Windows.Input;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Text.Editor {
	[ExportCommandInfoCreator(CommandConstants.CMDINFO_ORDER_TEXT_EDITOR)]
	sealed class DefaultTextEditorCommandInfoCreator : ICommandInfoCreator {
		public IEnumerable<Tuple<KeyShortcut, CommandInfo>> GetKeyShortcuts(object target) {
			if (!(target is ITextView))
				yield break;

			yield return Tuple.Create(KeyShortcut.Create(Key.Back), TextEditorIds.BACKSPACE.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.BACKTAB.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.BOL.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.BOL_EXT.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.BOL_EXT_COL.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.PageDown), TextEditorIds.BOTTOMLINE.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.CtrlShift(Key.PageDown), TextEditorIds.BOTTOMLINE_EXT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Create(Key.Escape), TextEditorIds.CANCEL.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.CUTLINE.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Create(Key.Delete), TextEditorIds.DELETE.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.DELETEBLANKLINES.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.DELETELINE.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.DELETETOBOL.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.DELETETOEOL.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.DELETEWHITESPACE.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.DELETEWORDLEFT.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.DELETEWORDRIGHT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Create(Key.Down), TextEditorIds.DOWN.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Shift(Key.Down), TextEditorIds.DOWN_EXT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.ShiftAlt(Key.Down), TextEditorIds.DOWN_EXT_COL.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.ECMD_CONVERTSPACESTOTABS.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.ECMD_CONVERTTABSTOSPACES.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.EditorLineFirstColumn.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.EditorLineFirstColumnExtend.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.End), TextEditorIds.END.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.CtrlShift(Key.End), TextEditorIds.END_EXT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Create(Key.End), TextEditorIds.EOL.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Shift(Key.End), TextEditorIds.EOL_EXT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.ShiftAlt(Key.End), TextEditorIds.EOL_EXT_COL.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Create(Key.Home), TextEditorIds.FIRSTCHAR.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Shift(Key.Home), TextEditorIds.FIRSTCHAR_EXT.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.FIRSTNONWHITENEXT.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.FIRSTNONWHITEPREV.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.GOTOBRACE.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.GOTOBRACE_EXT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.G), TextEditorIds.GOTOLINE.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.Home), TextEditorIds.HOME.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.CtrlShift(Key.Home), TextEditorIds.HOME_EXT.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.INDENT.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.LASTCHAR.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.LASTCHAR_EXT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Create(Key.Left), TextEditorIds.LEFT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Shift(Key.Left), TextEditorIds.LEFT_EXT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.ShiftAlt(Key.Left), TextEditorIds.LEFT_EXT_COL.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Alt(Key.Down), TextEditorIds.MoveSelLinesDown.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Alt(Key.Up), TextEditorIds.MoveSelLinesUp.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.Enter), TextEditorIds.OPENLINEABOVE.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.CtrlShift(Key.Enter), TextEditorIds.OPENLINEBELOW.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Create(Key.PageDown), TextEditorIds.PAGEDN.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Shift(Key.PageDown), TextEditorIds.PAGEDN_EXT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Create(Key.PageUp), TextEditorIds.PAGEUP.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Shift(Key.PageUp), TextEditorIds.PAGEUP_EXT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Create(Key.Enter), TextEditorIds.RETURN.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Create(Key.Right), TextEditorIds.RIGHT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Shift(Key.Right), TextEditorIds.RIGHT_EXT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.ShiftAlt(Key.Right), TextEditorIds.RIGHT_EXT_COL.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.SCROLLBOTTOM.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.SCROLLCENTER.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.Down), TextEditorIds.SCROLLDN.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.SCROLLLEFT.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.SCROLLPAGEDN.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.SCROLLPAGEUP.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.SCROLLRIGHT.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.SCROLLTOP.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.Up), TextEditorIds.SCROLLUP.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.A), TextEditorIds.SELECTALL.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.W), TextEditorIds.SELECTCURRENTWORD.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.U), TextEditorIds.SELLOWCASE.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.SELSWAPANCHOR.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.SELTABIFY.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.SELTITLECASE.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.SELTOGGLECASE.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.SELTOGOBACK.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.SELUNTABIFY.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.CtrlShift(Key.U), TextEditorIds.SELUPCASE.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.SmartBreakLine.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Create(Key.Tab), TextEditorIds.TAB.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Create(Key.Insert), TextEditorIds.TOGGLE_OVERTYPE_MODE.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.CtrlShift(Key.D8), TextEditorIds.TOGGLEVISSPACE.ToCommandInfo());
			yield return Tuple.Create(new KeyShortcut(KeyInput.Control(Key.R), KeyInput.Control(Key.W)), TextEditorIds.TOGGLEVISSPACE.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.TOGGLEWORDWRAP.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.PageUp), TextEditorIds.TOPLINE.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.CtrlShift(Key.PageUp), TextEditorIds.TOPLINE_EXT.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.TRANSPOSECHAR.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.TRANSPOSELINE.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.TRANSPOSEWORD.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.TYPECHAR.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.UNINDENT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Create(Key.Up), TextEditorIds.UP.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Shift(Key.Up), TextEditorIds.UP_EXT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.ShiftAlt(Key.Up), TextEditorIds.UP_EXT_COL.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.Right), TextEditorIds.WORDNEXT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.CtrlShift(Key.Right), TextEditorIds.WORDNEXT_EXT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.CtrlShiftAlt(Key.Right), TextEditorIds.WORDNEXT_EXT_COL.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.Left), TextEditorIds.WORDPREV.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.CtrlShift(Key.Left), TextEditorIds.WORDPREV_EXT.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.CtrlShiftAlt(Key.Left), TextEditorIds.WORDPREV_EXT_COL.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.ZoomIn.ToCommandInfo());
			//TODO: yield return Tuple.Create(KeyShortcut.Control(Key.XXXXX), TextEditorIds.ZoomOut.ToCommandInfo());
		}

		public CommandInfo? CreateFromTextInput(object target, string text) => TextEditorIds.TYPECHAR.ToCommandInfo(text);
	}
}

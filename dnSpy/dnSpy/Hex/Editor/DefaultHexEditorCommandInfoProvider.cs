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

using System.Collections.Generic;
using System.Windows.Input;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Hex.Editor {
	[ExportCommandInfoProvider(CommandInfoProviderOrder.HexEditor)]
	sealed class DefaultHexEditorCommandInfoProvider : ICommandInfoProvider2 {
		public IEnumerable<CommandShortcut> GetCommandShortcuts(object target) {
			if (!(target is HexView))
				yield break;

			yield return CommandShortcut.Create(Key.Back, HexEditorIds.BACKSPACE.ToCommandInfo());
			yield return CommandShortcut.Shift(Key.Back, HexEditorIds.BACKSPACE.ToCommandInfo());
			yield return CommandShortcut.Shift(Key.Tab, HexEditorIds.BACKTAB.ToCommandInfo());
			//TODO: yield return CommandShortcut.Control(Key.XXXXX, HexEditorIds.BOL.ToCommandInfo());
			//TODO: yield return CommandShortcut.Control(Key.XXXXX, HexEditorIds.BOL_EXT.ToCommandInfo());
			yield return CommandShortcut.Control(Key.PageDown, HexEditorIds.BOTTOMLINE.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.PageDown, HexEditorIds.BOTTOMLINE_EXT.ToCommandInfo());
			yield return CommandShortcut.Create(Key.Escape, HexEditorIds.CANCEL.ToCommandInfo());
			yield return CommandShortcut.Create(Key.Delete, HexEditorIds.DELETE.ToCommandInfo());
			//TODO: yield return CommandShortcut.Control(Key.XXXXX, HexEditorIds.DELETEBLANKLINES.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.L, HexEditorIds.DELETELINE.ToCommandInfo());
			//TODO: yield return CommandShortcut.Control(Key.XXXXX, HexEditorIds.DELETETOBOL.ToCommandInfo());
			//TODO: yield return CommandShortcut.Control(Key.XXXXX, HexEditorIds.DELETETOEOL.ToCommandInfo());
			yield return CommandShortcut.Create(Key.Down, HexEditorIds.DOWN.ToCommandInfo());
			yield return CommandShortcut.Shift(Key.Down, HexEditorIds.DOWN_EXT.ToCommandInfo());
			yield return CommandShortcut.Control(Key.End, HexEditorIds.END.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.End, HexEditorIds.END_EXT.ToCommandInfo());
			yield return CommandShortcut.Create(Key.End, HexEditorIds.EOL.ToCommandInfo());
			yield return CommandShortcut.Shift(Key.End, HexEditorIds.EOL_EXT.ToCommandInfo());
			yield return CommandShortcut.Create(Key.Home, HexEditorIds.BOL.ToCommandInfo());
			yield return CommandShortcut.Shift(Key.Home, HexEditorIds.BOL_EXT.ToCommandInfo());
			yield return CommandShortcut.Control(Key.Home, HexEditorIds.HOME.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.Home, HexEditorIds.HOME_EXT.ToCommandInfo());
			yield return CommandShortcut.Create(Key.Left, HexEditorIds.LEFT.ToCommandInfo());
			yield return CommandShortcut.Shift(Key.Left, HexEditorIds.LEFT_EXT.ToCommandInfo());
			yield return CommandShortcut.Create(Key.PageDown, HexEditorIds.PAGEDN.ToCommandInfo());
			yield return CommandShortcut.Shift(Key.PageDown, HexEditorIds.PAGEDN_EXT.ToCommandInfo());
			yield return CommandShortcut.Create(Key.PageUp, HexEditorIds.PAGEUP.ToCommandInfo());
			yield return CommandShortcut.Shift(Key.PageUp, HexEditorIds.PAGEUP_EXT.ToCommandInfo());
			yield return CommandShortcut.Create(Key.Enter, HexEditorIds.RETURN.ToCommandInfo());
			yield return CommandShortcut.Create(Key.Right, HexEditorIds.RIGHT.ToCommandInfo());
			yield return CommandShortcut.Shift(Key.Right, HexEditorIds.RIGHT_EXT.ToCommandInfo());
			//TODO: yield return CommandShortcut.Control(Key.XXXXX, HexEditorIds.SCROLLBOTTOM.ToCommandInfo());
			//TODO: yield return CommandShortcut.Control(Key.XXXXX, HexEditorIds.SCROLLCENTER.ToCommandInfo());
			yield return CommandShortcut.Control(Key.Down, HexEditorIds.SCROLLDN.ToCommandInfo());
			//TODO: yield return CommandShortcut.Control(Key.XXXXX, HexEditorIds.SCROLLLEFT.ToCommandInfo());
			//TODO: yield return CommandShortcut.Control(Key.XXXXX, HexEditorIds.SCROLLPAGEDN.ToCommandInfo());
			//TODO: yield return CommandShortcut.Control(Key.XXXXX, HexEditorIds.SCROLLPAGEUP.ToCommandInfo());
			//TODO: yield return CommandShortcut.Control(Key.XXXXX, HexEditorIds.SCROLLRIGHT.ToCommandInfo());
			//TODO: yield return CommandShortcut.Control(Key.XXXXX, HexEditorIds.SCROLLTOP.ToCommandInfo());
			yield return CommandShortcut.Control(Key.Up, HexEditorIds.SCROLLUP.ToCommandInfo());
			yield return CommandShortcut.Control(Key.A, HexEditorIds.SELECTALL.ToCommandInfo());
			yield return CommandShortcut.Control(Key.W, HexEditorIds.SELECTCURRENTWORD.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.W, HexEditorIds.SELECTCURRENTWORD.ToCommandInfo());
			//yield return CommandShortcut.Create(KeyInput.Control(Key.K), KeyInput.Control(Key.A), HexEditorIds.SELSWAPANCHOR.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.E), KeyInput.Create(Key.A), HexEditorIds.SELSWAPANCHOR.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.E), KeyInput.Control(Key.A), HexEditorIds.SELSWAPANCHOR.ToCommandInfo());
			yield return CommandShortcut.Create(Key.Tab, HexEditorIds.TAB.ToCommandInfo());
			yield return CommandShortcut.Create(Key.Insert, HexEditorIds.TOGGLE_OVERTYPE_MODE.ToCommandInfo());
			yield return CommandShortcut.Control(Key.PageUp, HexEditorIds.TOPLINE.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.PageUp, HexEditorIds.TOPLINE_EXT.ToCommandInfo());
			yield return CommandShortcut.Create(Key.Up, HexEditorIds.UP.ToCommandInfo());
			yield return CommandShortcut.Shift(Key.Up, HexEditorIds.UP_EXT.ToCommandInfo());
			yield return CommandShortcut.Control(Key.Right, HexEditorIds.WORDNEXT.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.Right, HexEditorIds.WORDNEXT_EXT.ToCommandInfo());
			yield return CommandShortcut.Control(Key.Left, HexEditorIds.WORDPREV.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.Left, HexEditorIds.WORDPREV_EXT.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.OemPeriod, HexEditorIds.ZoomIn.ToCommandInfo());
			yield return CommandShortcut.Control(Key.OemPlus, HexEditorIds.ZoomIn.ToCommandInfo());
			yield return CommandShortcut.Control(Key.Add, HexEditorIds.ZoomIn.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.OemComma, HexEditorIds.ZoomOut.ToCommandInfo());
			yield return CommandShortcut.Control(Key.OemMinus, HexEditorIds.ZoomOut.ToCommandInfo());
			yield return CommandShortcut.Control(Key.Subtract, HexEditorIds.ZoomOut.ToCommandInfo());
			yield return CommandShortcut.Control(Key.D0, HexEditorIds.ZoomReset.ToCommandInfo());
			yield return CommandShortcut.Control(Key.NumPad0, HexEditorIds.ZoomReset.ToCommandInfo());
			yield return CommandShortcut.Alt(Key.OemComma, HexEditorIds.DECREASEFILTER.ToCommandInfo());
			yield return CommandShortcut.Alt(Key.OemPeriod, HexEditorIds.INCREASEFILTER.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.K), KeyInput.Create(Key.I), HexEditorIds.QUICKINFO.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.K), KeyInput.Control(Key.I), HexEditorIds.QUICKINFO.ToCommandInfo());
		}

		public CommandInfo? CreateFromTextInput(object target, string text) {
			if (text.Length == 0 || (text.Length == 1 && (text[0] == '\u001B' || text[0] == '\b')))
				return null;
			return HexEditorIds.TYPECHAR.ToCommandInfo(text);
		}
	}
}

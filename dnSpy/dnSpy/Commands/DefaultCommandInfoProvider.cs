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

namespace dnSpy.Commands {
	[ExportCommandInfoProvider(CommandInfoProviderOrder.Default)]
	sealed class DefaultCommandInfoProvider : ICommandInfoProvider {
		public IEnumerable<CommandShortcut> GetCommandShortcuts(object target) {
			yield return CommandShortcut.Control(Key.Z, StandardIds.Undo.ToCommandInfo());
			yield return CommandShortcut.Alt(Key.Back, StandardIds.Undo.ToCommandInfo());
			yield return CommandShortcut.Control(Key.Y, StandardIds.Redo.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.Z, StandardIds.Redo.ToCommandInfo());
			yield return CommandShortcut.ShiftAlt(Key.Back, StandardIds.Redo.ToCommandInfo());
			yield return CommandShortcut.Control(Key.X, StandardIds.Cut.ToCommandInfo());
			yield return CommandShortcut.Shift(Key.Delete, StandardIds.Cut.ToCommandInfo());
			yield return CommandShortcut.Control(Key.C, StandardIds.Copy.ToCommandInfo());
			yield return CommandShortcut.Control(Key.Insert, StandardIds.Copy.ToCommandInfo());
			yield return CommandShortcut.Control(Key.V, StandardIds.Paste.ToCommandInfo());
			yield return CommandShortcut.Shift(Key.Insert, StandardIds.Paste.ToCommandInfo());

			yield return CommandShortcut.Control(Key.F, StandardIds.Find.ToCommandInfo());
			yield return CommandShortcut.Control(Key.H, StandardIds.Replace.ToCommandInfo());
			yield return CommandShortcut.Control(Key.I, StandardIds.IncrementalSearchForward.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.I, StandardIds.IncrementalSearchBackward.ToCommandInfo());
			yield return CommandShortcut.Create(Key.F3, StandardIds.FindNext.ToCommandInfo());
			yield return CommandShortcut.Shift(Key.F3, StandardIds.FindPrevious.ToCommandInfo());
			yield return CommandShortcut.Control(Key.F3, StandardIds.FindNextSelected.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.F3, StandardIds.FindPreviousSelected.ToCommandInfo());
		}
	}
}

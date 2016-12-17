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

namespace dnSpy.Hex.Commands {
	[ExportCommandInfoProvider(CommandInfoProviderOrder.HexEditor - 1)]
	sealed class CommandInfoProvider : ICommandInfoProvider {
		public IEnumerable<CommandShortcut> GetCommandShortcuts(object target) {
			if (!(target is HexView))
				yield break;

			yield return CommandShortcut.Control(Key.G, HexCommandIds.GoToPosition.ToCommandInfo());
			yield return CommandShortcut.Control(Key.L, HexCommandIds.Select.ToCommandInfo());
			yield return CommandShortcut.CtrlAlt(Key.S, HexCommandIds.SaveSelection.ToCommandInfo());
			yield return CommandShortcut.Control(Key.R, HexCommandIds.ToggleUseRelativePositions.ToCommandInfo());
			yield return CommandShortcut.Create(Key.F12, HexCommandIds.GoToCodeOrStructure.ToCommandInfo());
		}
	}
}

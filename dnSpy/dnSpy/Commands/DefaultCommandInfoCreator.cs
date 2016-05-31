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

namespace dnSpy.Commands {
	[ExportCommandInfoCreator(CommandConstants.CMDINFO_ORDER_DEFAULT)]
	sealed class DefaultCommandInfoCreator : ICommandInfoCreator {
		public IEnumerable<Tuple<KeyShortcut, CommandInfo>> GetKeyShortcuts(object target) {
			yield return Tuple.Create(KeyShortcut.Control(Key.Z), DefaultIds.Undo.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Alt(Key.Back), DefaultIds.Undo.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.Y), DefaultIds.Redo.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.CtrlShift(Key.Z), DefaultIds.Redo.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.ShiftAlt(Key.Back), DefaultIds.Redo.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.X), DefaultIds.Cut.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Shift(Key.Delete), DefaultIds.Cut.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.C), DefaultIds.Copy.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.Insert), DefaultIds.Copy.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Control(Key.V), DefaultIds.Paste.ToCommandInfo());
			yield return Tuple.Create(KeyShortcut.Shift(Key.Insert), DefaultIds.Paste.ToCommandInfo());
		}

		public CommandInfo? CreateFromTextInput(object target, string text) => null;
	}
}

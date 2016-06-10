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
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Files.Tabs.TextEditor {
	[ExportCommandInfoCreator(FileTabCommandConstants.CMDTARGETFILTER_ORDER_FILETAB)]
	sealed class FileTabCommandInfoCreator : ICommandInfoCreator {
		public IEnumerable<CommandShortcut> GetCommandShortcuts(object target) {
			var textView = target as ITextView;
			if (textView?.Roles.Contains(FileTabTextViewRoles.FileTab) != true)
				yield break;

			yield return CommandShortcut.Create(Key.Tab, FileTabIds.MoveToNextReference.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.Down, FileTabIds.MoveToNextReference.ToCommandInfo());
			yield return CommandShortcut.Shift(Key.Tab, FileTabIds.MoveToPreviousReference.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.Up, FileTabIds.MoveToPreviousReference.ToCommandInfo());
			yield return CommandShortcut.Alt(Key.Down, FileTabIds.MoveToNextDefinition.ToCommandInfo());
			yield return CommandShortcut.Alt(Key.Up, FileTabIds.MoveToPreviousDefinition.ToCommandInfo());
			yield return CommandShortcut.Create(Key.F12, FileTabIds.FollowReference.ToCommandInfo());
			yield return CommandShortcut.Create(Key.Enter, FileTabIds.FollowReference.ToCommandInfo());
			yield return CommandShortcut.Control(Key.F12, FileTabIds.FollowReferenceNewTab.ToCommandInfo());
			yield return CommandShortcut.Control(Key.Enter, FileTabIds.FollowReferenceNewTab.ToCommandInfo());
			yield return CommandShortcut.Create(Key.Escape, FileTabIds.ClearMarkedReferencesAndToolTip.ToCommandInfo());
		}

		public CommandInfo? CreateFromTextInput(object target, string text) => null;
	}
}

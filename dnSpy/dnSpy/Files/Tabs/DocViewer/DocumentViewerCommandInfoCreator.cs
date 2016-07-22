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
using dnSpy.Contracts.Files.Tabs.DocViewer;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Files.Tabs.DocViewer {
	[ExportCommandInfoCreator(DocumentViewerCommandConstants.CMDINFO_ORDER_DOCUMENTVIEWER)]
	sealed class DocumentViewerCommandInfoCreator : ICommandInfoCreator {
		public IEnumerable<CommandShortcut> GetCommandShortcuts(object target) {
			var textView = target as ITextView;
			if (textView?.Roles.Contains(DocumentViewerConstants.TextViewRole) != true)
				yield break;

			yield return CommandShortcut.Create(Key.Tab, DocumentViewerIds.MoveToNextReference.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.Down, DocumentViewerIds.MoveToNextReference.ToCommandInfo());
			yield return CommandShortcut.Shift(Key.Tab, DocumentViewerIds.MoveToPreviousReference.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.Up, DocumentViewerIds.MoveToPreviousReference.ToCommandInfo());
			yield return CommandShortcut.Alt(Key.Down, DocumentViewerIds.MoveToNextDefinition.ToCommandInfo());
			yield return CommandShortcut.Alt(Key.Up, DocumentViewerIds.MoveToPreviousDefinition.ToCommandInfo());
			yield return CommandShortcut.Create(Key.F12, DocumentViewerIds.FollowReference.ToCommandInfo());
			yield return CommandShortcut.Create(Key.Enter, DocumentViewerIds.FollowReference.ToCommandInfo());
			yield return CommandShortcut.Control(Key.F12, DocumentViewerIds.FollowReferenceNewTab.ToCommandInfo());
			yield return CommandShortcut.Control(Key.Enter, DocumentViewerIds.FollowReferenceNewTab.ToCommandInfo());
			yield return CommandShortcut.Create(Key.Escape, DocumentViewerIds.ClearMarkedReferencesAndToolTip.ToCommandInfo());
		}
	}
}

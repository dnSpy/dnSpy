/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

using System.Windows.Documents;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents.Tabs;

namespace dnSpy.AsmEditor.Commands {
	static class CommandUtils {
		static readonly RoutedCommand SettingsRoutedCommand = new RoutedCommand("Settings", typeof(CommandUtils));
		static CommandUtils() {
			SettingsRoutedCommand.InputGestures.Add(new KeyGesture(Key.Enter, ModifierKeys.Alt));
		}

		public static void AddRemoveCommand(this IWpfCommandService wpfCommandService, EditMenuHandler settingsCmd) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DOCUMENT_TREEVIEW);
			cmds.Add(ApplicationCommands.Delete, new EditMenuHandlerCommandProxy(settingsCmd));
		}

		public static void AddRemoveCommand(this IWpfCommandService wpfCommandService, CodeContextMenuHandler settingsCmd, IDocumentTabService documentTabService) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DOCUMENTVIEWER_UICONTEXT);
			cmds.Add(EditingCommands.Delete, new CodeContextMenuHandlerCommandProxy(settingsCmd, documentTabService), ModifierKeys.None, Key.Delete);
		}

		public static void AddSettingsCommand(this IWpfCommandService wpfCommandService, IDocumentTabService documentTabService, EditMenuHandler treeViewCmd, CodeContextMenuHandler textEditorCmd) {
			if (treeViewCmd != null) {
				var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DOCUMENT_TREEVIEW);
				cmds.Add(SettingsRoutedCommand, new EditMenuHandlerCommandProxy(treeViewCmd));
			}
			if (textEditorCmd != null) {
				var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DOCUMENTVIEWER_UICONTEXT);
				cmds.Add(SettingsRoutedCommand, new CodeContextMenuHandlerCommandProxy(textEditorCmd, documentTabService), ModifierKeys.Alt, Key.Enter);
			}
		}
	}
}

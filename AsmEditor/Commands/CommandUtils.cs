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

using System.Windows.Documents;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;

namespace dnSpy.AsmEditor.Commands {
	static class CommandUtils {
		static readonly RoutedCommand SettingsRoutedCommand = new RoutedCommand("Settings", typeof(CommandUtils));
		static CommandUtils() {
			SettingsRoutedCommand.InputGestures.Add(new KeyGesture(Key.Enter, ModifierKeys.Alt));
		}

		public static void AddRemoveCommand(this IWpfCommandManager wpfCommandManager, EditMenuHandler settingsCmd) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_FILE_TREEVIEW);
			cmds.Add(ApplicationCommands.Delete, new EditMenuHandlerCommandProxy(settingsCmd));
		}

		public static void AddRemoveCommand(this IWpfCommandManager wpfCommandManager, CodeContextMenuHandler settingsCmd, IFileTabManager fileTabManager) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_TEXTEDITOR_UICONTEXT);
			cmds.Add(EditingCommands.Delete, new CodeContextMenuHandlerCommandProxy(settingsCmd, fileTabManager), ModifierKeys.None, Key.Delete);
		}

		public static void AddSettingsCommand(this IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager, EditMenuHandler treeViewCmd, CodeContextMenuHandler textEditorCmd) {
			if (treeViewCmd != null) {
				var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_FILE_TREEVIEW);
				cmds.Add(SettingsRoutedCommand, new EditMenuHandlerCommandProxy(treeViewCmd));
			}
			if (textEditorCmd != null) {
				var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_TEXTEDITOR_UICONTEXT);
				cmds.Add(SettingsRoutedCommand, new CodeContextMenuHandlerCommandProxy(textEditorCmd, fileTabManager), ModifierKeys.Alt, Key.Enter);
			}
		}
	}
}

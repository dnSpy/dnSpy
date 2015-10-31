/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using System.Linq;
using System.Windows.Input;
using dnSpy.Files;
using ICSharpCode.ILSpy;

namespace dnSpy.AsmEditor {
	static class Utils {
		public static void NotifyModifiedAssembly(DnSpyFile asm) {
			MainWindow.Instance.ModuleModified(asm);
		}

		public static void InstallSettingsCommand(IContextMenuEntry treeViewCmd, IContextMenuEntry textEditorCmd) {
			InstallTreeViewAndTextEditorCommand(SettingsRoutedCommand, treeViewCmd, textEditorCmd, ModifierKeys.Alt, Key.Enter);
		}
		static readonly RoutedCommand SettingsRoutedCommand = new RoutedCommand("Settings", typeof(Utils));

		public static void InstallTreeViewAndTextEditorCommand(RoutedCommand routedCmd, IContextMenuEntry treeViewCmd, IContextMenuEntry textEditorCmd, ModifierKeys modifiers, Key key) {
			if (treeViewCmd != null) {
				var elem = MainWindow.Instance.TreeView;
				elem.AddCommandBinding(routedCmd, new TreeViewCommandProxy(treeViewCmd));
				bool keyBindingExists = elem.InputBindings.OfType<KeyBinding>().Any(a => a.Key == key && a.Modifiers == modifiers);
				if (!keyBindingExists)
					elem.InputBindings.Add(new KeyBinding(routedCmd, key, modifiers));
			}
			if (textEditorCmd != null)
				MainWindow.Instance.CodeBindings.Add(routedCmd, new TextEditorCommandProxy(textEditorCmd), modifiers, key);
		}
	}
}

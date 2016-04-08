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

using System.ComponentModel.Composition;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.Memory {
	[ExportAutoLoaded]
	sealed class MemoryContentCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		MemoryContentCommandLoader(IWpfCommandManager wpfCommandManager, MemoryToolWindowContentCreator memoryToolWindowContentCreator, IMainToolWindowManager mainToolWindowManager) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);
			for (int i = 0; i < DebugRoutedCommands.ShowMemoryCommands.Length; i++) {
				var info = memoryToolWindowContentCreator.Contents[i];
				cmds.Add(DebugRoutedCommands.ShowMemoryCommands[i], new RelayCommand(a => mainToolWindowManager.Show(info.Guid)));
			}
			for (int i = 0; i < DebugRoutedCommands.ShowMemoryCommands.Length && i < 10; i++) {
				var cmd = DebugRoutedCommands.ShowMemoryCommands[i];
				if (i == 0)
					cmds.Add(cmd, ModifierKeys.Alt, Key.D6);
				cmds.Add(cmd, ModifierKeys.Control | ModifierKeys.Shift, Key.D0 + (i + 1) % 10);
			}
		}
	}
}

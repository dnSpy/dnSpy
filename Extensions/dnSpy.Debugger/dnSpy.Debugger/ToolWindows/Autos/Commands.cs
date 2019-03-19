/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.MVVM;
using dnSpy.Debugger.Evaluation.ViewModel;

namespace dnSpy.Debugger.ToolWindows.Autos {
	[ExportAutoLoaded]
	sealed class CommandsLoader : IAutoLoaded {
		[ImportingConstructor]
		CommandsLoader(IWpfCommandService wpfCommandService, Lazy<VariablesWindowOperations> operations, Lazy<AutosContent> content) {
			var cmds = wpfCommandService.GetCommands(AutosContent.VariablesWindowGuid);
			cmds.Add(new RelayCommand(a => operations.Value.Copy(content.Value.VM.VM), a => operations.Value.CanCopy(content.Value.VM.VM)), ModifierKeys.Control, Key.C);
			cmds.Add(new RelayCommand(a => operations.Value.Copy(content.Value.VM.VM), a => operations.Value.CanCopy(content.Value.VM.VM)), ModifierKeys.Control, Key.Insert);
			cmds.Add(new RelayCommand(a => operations.Value.Edit(content.Value.VM.VM), a => operations.Value.CanEdit(content.Value.VM.VM)), ModifierKeys.None, Key.F2);
			cmds.Add(new RelayCommand(a => operations.Value.CopyValue(content.Value.VM.VM), a => operations.Value.CanCopyValue(content.Value.VM.VM)), ModifierKeys.Control | ModifierKeys.Shift, Key.C);
			cmds.Add(new RelayCommand(a => operations.Value.ToggleExpanded(content.Value.VM.VM), a => operations.Value.CanToggleExpanded(content.Value.VM.VM)), ModifierKeys.None, Key.Enter);
			cmds.Add(new RelayCommand(a => operations.Value.ShowInMemoryWindow(content.Value.VM.VM), a => operations.Value.CanShowInMemoryWindow(content.Value.VM.VM)), ModifierKeys.Control, Key.X);
			for (int i = 0; i < 10 && i < Memory.MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS; i++) {
				int windowIndex = i;
				cmds.Add(new RelayCommand(a => operations.Value.ShowInMemoryWindow(content.Value.VM.VM, windowIndex), a => operations.Value.CanShowInMemoryWindow(content.Value.VM.VM, windowIndex)), ModifierKeys.Control, Key.D0 + (i + 1) % 10);
			}
		}
	}
}

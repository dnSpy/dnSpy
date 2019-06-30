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

namespace dnSpy.Debugger.ToolWindows.Watch {
	[ExportAutoLoaded]
	sealed class CommandsLoader : IAutoLoaded {
		[ImportingConstructor]
		CommandsLoader(IWpfCommandService wpfCommandService, Lazy<VariablesWindowOperations> operations, Lazy<WatchContentFactory> watchContentFactory) {
			for (int i = 0; i < WatchWindowsHelper.NUMBER_OF_WATCH_WINDOWS; i++) {
				var cmds = wpfCommandService.GetCommands(WatchContent.GetVariablesWindowGuid(i));
				int windowIndex = i;
				cmds.Add(new RelayCommand(a => operations.Value.Copy(watchContentFactory.Value.GetContent(windowIndex).VM.VM), a => operations.Value.CanCopy(watchContentFactory.Value.GetContent(windowIndex).VM.VM)), ModifierKeys.Control, Key.C);
				cmds.Add(new RelayCommand(a => operations.Value.Copy(watchContentFactory.Value.GetContent(windowIndex).VM.VM), a => operations.Value.CanCopy(watchContentFactory.Value.GetContent(windowIndex).VM.VM)), ModifierKeys.Control, Key.Insert);
				cmds.Add(new RelayCommand(a => operations.Value.Paste(watchContentFactory.Value.GetContent(windowIndex).VM.VM), a => operations.Value.CanPaste(watchContentFactory.Value.GetContent(windowIndex).VM.VM)), ModifierKeys.Control, Key.V);
				cmds.Add(new RelayCommand(a => operations.Value.Paste(watchContentFactory.Value.GetContent(windowIndex).VM.VM), a => operations.Value.CanPaste(watchContentFactory.Value.GetContent(windowIndex).VM.VM)), ModifierKeys.Shift, Key.Insert);
				cmds.Add(new RelayCommand(a => operations.Value.DeleteWatch(watchContentFactory.Value.GetContent(windowIndex).VM.VM), a => operations.Value.CanDeleteWatch(watchContentFactory.Value.GetContent(windowIndex).VM.VM)), ModifierKeys.None, Key.Delete);
				cmds.Add(new RelayCommand(a => operations.Value.Edit(watchContentFactory.Value.GetContent(windowIndex).VM.VM), a => operations.Value.CanEdit(watchContentFactory.Value.GetContent(windowIndex).VM.VM)), ModifierKeys.None, Key.F2);
				cmds.Add(new RelayCommand(a => operations.Value.CopyValue(watchContentFactory.Value.GetContent(windowIndex).VM.VM), a => operations.Value.CanCopyValue(watchContentFactory.Value.GetContent(windowIndex).VM.VM)), ModifierKeys.Control | ModifierKeys.Shift, Key.C);
				cmds.Add(new RelayCommand(a => operations.Value.ToggleExpanded(watchContentFactory.Value.GetContent(windowIndex).VM.VM), a => operations.Value.CanToggleExpanded(watchContentFactory.Value.GetContent(windowIndex).VM.VM)), ModifierKeys.None, Key.Enter);
				cmds.Add(new RelayCommand(a => operations.Value.ShowInMemoryWindow(watchContentFactory.Value.GetContent(windowIndex).VM.VM), a => operations.Value.CanShowInMemoryWindow(watchContentFactory.Value.GetContent(windowIndex).VM.VM)), ModifierKeys.Control, Key.X);
				for (int j = 0; j < 10 && j < Memory.MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS; j++) {
					int memoryWindowIndex = j;
					cmds.Add(new RelayCommand(a => operations.Value.ShowInMemoryWindow(watchContentFactory.Value.GetContent(memoryWindowIndex).VM.VM, memoryWindowIndex), a => operations.Value.CanShowInMemoryWindow(watchContentFactory.Value.GetContent(memoryWindowIndex).VM.VM, memoryWindowIndex)), ModifierKeys.Control, Key.D0 + (j + 1) % 10);
				}
			}
		}
	}
}

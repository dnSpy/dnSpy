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
using System.ComponentModel.Composition;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;

namespace dnSpy.Debugger {
	[ExportAutoLoaded]
	sealed class DebugManagerCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		DebugManagerCommandLoader(Lazy<DebugManager> debugManager, IWpfCommandManager wpfCommandManager) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);
			cmds.Add(DebugRoutedCommands.DebugCurrentAssembly, (s, e) => debugManager.Value.DebugCurrentAssembly(e.Parameter), (s, e) => e.CanExecute = debugManager.Value.CanDebugCurrentAssembly(e.Parameter));
			cmds.Add(DebugRoutedCommands.DebugAssembly, (s, e) => debugManager.Value.DebugAssembly(), (s, e) => e.CanExecute = debugManager.Value.CanDebugAssembly);
			cmds.Add(DebugRoutedCommands.DebugCoreCLRAssembly, (s, e) => debugManager.Value.DebugCoreCLRAssembly(), (s, e) => e.CanExecute = debugManager.Value.CanDebugCoreCLRAssembly);
			cmds.Add(DebugRoutedCommands.StartWithoutDebugging, (s, e) => debugManager.Value.StartWithoutDebugging(), (s, e) => e.CanExecute = debugManager.Value.CanStartWithoutDebugging, ModifierKeys.Control, Key.F5);
			cmds.Add(DebugRoutedCommands.Attach, (s, e) => debugManager.Value.Attach(), (s, e) => e.CanExecute = debugManager.Value.CanAttach, ModifierKeys.Control | ModifierKeys.Alt, Key.P);
			cmds.Add(DebugRoutedCommands.Break, (s, e) => debugManager.Value.Break(), (s, e) => e.CanExecute = debugManager.Value.CanBreak, ModifierKeys.Control, Key.Cancel);
			cmds.Add(DebugRoutedCommands.Restart, (s, e) => debugManager.Value.Restart(), (s, e) => e.CanExecute = debugManager.Value.CanRestart, ModifierKeys.Control | ModifierKeys.Shift, Key.F5);
			cmds.Add(DebugRoutedCommands.Stop, (s, e) => debugManager.Value.Stop(), (s, e) => e.CanExecute = debugManager.Value.CanStop, ModifierKeys.Shift, Key.F5);
			cmds.Add(DebugRoutedCommands.Detach, (s, e) => debugManager.Value.Detach(), (s, e) => e.CanExecute = debugManager.Value.CanDetach);
			cmds.Add(DebugRoutedCommands.Continue, (s, e) => debugManager.Value.Continue(), (s, e) => e.CanExecute = debugManager.Value.CanContinue, ModifierKeys.None, Key.F5);
			cmds.Add(DebugRoutedCommands.StepInto, (s, e) => debugManager.Value.StepInto(), (s, e) => e.CanExecute = debugManager.Value.CanStepInto(), ModifierKeys.None, Key.F11);
			cmds.Add(DebugRoutedCommands.StepOver, (s, e) => debugManager.Value.StepOver(), (s, e) => e.CanExecute = debugManager.Value.CanStepOver(), ModifierKeys.None, Key.F10);
			cmds.Add(DebugRoutedCommands.StepOut, (s, e) => debugManager.Value.StepOut(), (s, e) => e.CanExecute = debugManager.Value.CanStepOut(), ModifierKeys.Shift, Key.F11);
			cmds.Add(DebugRoutedCommands.ShowNextStatement, (s, e) => debugManager.Value.ShowNextStatement(), (s, e) => e.CanExecute = debugManager.Value.CanShowNextStatement, ModifierKeys.Alt, Key.Multiply);
			cmds.Add(DebugRoutedCommands.SetNextStatement, (s, e) => debugManager.Value.SetNextStatement(e.Parameter), (s, e) => e.CanExecute = debugManager.Value.CanSetNextStatement(e.Parameter), ModifierKeys.Control | ModifierKeys.Shift, Key.F10);
			cmds.Add(DebugRoutedCommands.Continue, (s, e) => debugManager.Value.DebugAssembly(), (s, e) => e.CanExecute = debugManager.Value.CanDebugAssembly, ModifierKeys.None, Key.F5);
			cmds.Add(DebugRoutedCommands.StepInto, (s, e) => debugManager.Value.DebugAssembly(), (s, e) => e.CanExecute = debugManager.Value.CanDebugAssembly, ModifierKeys.None, Key.F11);
			cmds.Add(DebugRoutedCommands.StepOver, (s, e) => debugManager.Value.DebugAssembly(), (s, e) => e.CanExecute = debugManager.Value.CanDebugAssembly, ModifierKeys.None, Key.F10);
		}
	}
}

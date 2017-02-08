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

using System;
using System.ComponentModel.Composition;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;

namespace dnSpy.Debugger {
	//[ExportAutoLoaded]
	sealed class DebugServiceCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		DebugServiceCommandLoader(Lazy<DebugService> debugService, IWpfCommandService wpfCommandService) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);
			cmds.Add(DebugRoutedCommands.DebugCurrentAssembly, (s, e) => debugService.Value.DebugCurrentAssembly(e.Parameter), (s, e) => e.CanExecute = debugService.Value.CanDebugCurrentAssembly(e.Parameter));
			cmds.Add(DebugRoutedCommands.DebugAssembly, (s, e) => debugService.Value.DebugAssembly(), (s, e) => e.CanExecute = debugService.Value.CanDebugAssembly);
			cmds.Add(DebugRoutedCommands.DebugCoreCLRAssembly, (s, e) => debugService.Value.DebugCoreCLRAssembly(), (s, e) => e.CanExecute = debugService.Value.CanDebugCoreCLRAssembly);
			cmds.Add(DebugRoutedCommands.StartWithoutDebugging, (s, e) => debugService.Value.StartWithoutDebugging(), (s, e) => e.CanExecute = debugService.Value.CanStartWithoutDebugging, ModifierKeys.Control, Key.F5);
			cmds.Add(DebugRoutedCommands.Attach, (s, e) => debugService.Value.Attach(), (s, e) => e.CanExecute = debugService.Value.CanAttach, ModifierKeys.Control | ModifierKeys.Alt, Key.P);
			cmds.Add(DebugRoutedCommands.Break, (s, e) => debugService.Value.Break(), (s, e) => e.CanExecute = debugService.Value.CanBreak, ModifierKeys.Control, Key.Cancel);
			cmds.Add(DebugRoutedCommands.Restart, (s, e) => debugService.Value.Restart(), (s, e) => e.CanExecute = debugService.Value.CanRestart, ModifierKeys.Control | ModifierKeys.Shift, Key.F5);
			cmds.Add(DebugRoutedCommands.Stop, (s, e) => debugService.Value.Stop(), (s, e) => e.CanExecute = debugService.Value.CanStop, ModifierKeys.Shift, Key.F5);
			cmds.Add(DebugRoutedCommands.Detach, (s, e) => debugService.Value.Detach(), (s, e) => e.CanExecute = debugService.Value.CanDetach);
			cmds.Add(DebugRoutedCommands.Continue, (s, e) => debugService.Value.Continue(), (s, e) => e.CanExecute = debugService.Value.CanContinue, ModifierKeys.None, Key.F5);
			cmds.Add(DebugRoutedCommands.StepInto, (s, e) => debugService.Value.StepInto(), (s, e) => e.CanExecute = debugService.Value.CanStepInto(), ModifierKeys.None, Key.F11);
			cmds.Add(DebugRoutedCommands.StepOver, (s, e) => debugService.Value.StepOver(), (s, e) => e.CanExecute = debugService.Value.CanStepOver(), ModifierKeys.None, Key.F10);
			cmds.Add(DebugRoutedCommands.StepOut, (s, e) => debugService.Value.StepOut(), (s, e) => e.CanExecute = debugService.Value.CanStepOut(), ModifierKeys.Shift, Key.F11);
			cmds.Add(DebugRoutedCommands.ShowNextStatement, (s, e) => debugService.Value.ShowNextStatement(), (s, e) => e.CanExecute = debugService.Value.CanShowNextStatement, ModifierKeys.Alt, Key.Multiply);
			cmds.Add(DebugRoutedCommands.SetNextStatement, (s, e) => debugService.Value.SetNextStatement(e.Parameter), (s, e) => e.CanExecute = debugService.Value.CanSetNextStatement(e.Parameter), ModifierKeys.Control | ModifierKeys.Shift, Key.F10);
			cmds.Add(DebugRoutedCommands.Continue, (s, e) => debugService.Value.DebugAssembly(), (s, e) => e.CanExecute = debugService.Value.CanDebugAssembly, ModifierKeys.None, Key.F5);
			cmds.Add(DebugRoutedCommands.StepInto, (s, e) => debugService.Value.DebugAssembly(), (s, e) => e.CanExecute = debugService.Value.CanDebugAssembly, ModifierKeys.None, Key.F11);
			cmds.Add(DebugRoutedCommands.StepOver, (s, e) => debugService.Value.DebugAssembly(), (s, e) => e.CanExecute = debugService.Value.CanDebugAssembly, ModifierKeys.None, Key.F10);
		}
	}
}

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

namespace dnSpy.Debugger.DbgUI {
	[ExportAutoLoaded]
	sealed class KeyboardShortcutCommands : IAutoLoaded {
		[ImportingConstructor]
		KeyboardShortcutCommands(IWpfCommandService wpfCommandService, Lazy<Debugger> debugger) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);
			cmds.Add(new RelayCommand(a => debugger.Value.DeleteAllBreakpointsAskUser(), a => debugger.Value.CanDeleteAllBreakpoints), ModifierKeys.Control | ModifierKeys.Shift, Key.F9);
			cmds.Add(new RelayCommand(a => debugger.Value.ToggleCreateBreakpoint(), a => debugger.Value.CanToggleCreateBreakpoint), ModifierKeys.None, Key.F9);
			cmds.Add(new RelayCommand(a => debugger.Value.ToggleEnableBreakpoint(), a => debugger.Value.CanToggleEnableBreakpoint), ModifierKeys.Control, Key.F9);

			cmds.Add(new RelayCommand(a => debugger.Value.StartWithoutDebugging(), a => debugger.Value.CanStartWithoutDebugging), ModifierKeys.Control, Key.F5);
			cmds.Add(new RelayCommand(a => debugger.Value.AttachProgram(), a => debugger.Value.CanAttachProgram), ModifierKeys.Control | ModifierKeys.Alt, Key.P);
			cmds.Add(new RelayCommand(a => debugger.Value.BreakAll(), a => debugger.Value.CanBreakAll), ModifierKeys.Control | ModifierKeys.Alt, Key.Cancel);
			cmds.Add(new RelayCommand(a => debugger.Value.Restart(), a => debugger.Value.CanRestart), ModifierKeys.Control | ModifierKeys.Shift, Key.F5);
			cmds.Add(new RelayCommand(a => debugger.Value.StopDebugging(), a => debugger.Value.CanStopDebugging), ModifierKeys.Shift, Key.F5);
			cmds.Add(new RelayCommand(a => debugger.Value.ContinueOrDebugProgram(), a => debugger.Value.CanContinueOrDebugProgram), ModifierKeys.None, Key.F5);
			cmds.Add(new RelayCommand(a => debugger.Value.StepIntoOrDebugProgram(), a => debugger.Value.CanStepIntoOrDebugProgram), ModifierKeys.None, Key.F11);
			cmds.Add(new RelayCommand(a => debugger.Value.StepOverOrDebugProgram(), a => debugger.Value.CanStepOverOrDebugProgram), ModifierKeys.None, Key.F10);
			cmds.Add(new RelayCommand(a => debugger.Value.StepOut(), a => debugger.Value.CanStepOut), ModifierKeys.Shift, Key.F11);
			cmds.Add(new RelayCommand(a => debugger.Value.ShowNextStatement(), a => debugger.Value.CanShowNextStatement), ModifierKeys.Alt, Key.Multiply);
			cmds.Add(new RelayCommand(a => debugger.Value.SetNextStatement(), a => debugger.Value.CanSetNextStatement), ModifierKeys.Control | ModifierKeys.Shift, Key.F10);
			cmds.Add(new RelayCommand(a => debugger.Value.StepIntoCurrentProcess(), a => debugger.Value.CanStepIntoCurrentProcess), ModifierKeys.Control | ModifierKeys.Alt, Key.F11);
			cmds.Add(new RelayCommand(a => debugger.Value.StepOverCurrentProcess(), a => debugger.Value.CanStepOverCurrentProcess), ModifierKeys.Control | ModifierKeys.Alt, Key.F10);
			cmds.Add(new RelayCommand(a => debugger.Value.StepOutCurrentProcess(), a => debugger.Value.CanStepOutCurrentProcess), ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, Key.F11);
		}
	}
}

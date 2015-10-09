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

using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger {
	[Export(typeof(IPlugin))]
	sealed class DebuggerPluginLoader : IPlugin {
		void IPlugin.OnLoaded() {
			MainWindow.Instance.SetMenuAlwaysRegenerate("_Debug");
			InstallRoutedCommands();
			InstallKeyboardShortcutCommands();
			DebugManager.Instance.OnLoaded();
			ToolbarDebugCommand.OnLoaded();
			Breakpoints.BreakpointManager.Instance.OnLoaded();
			CallStack.StackFrameManager.Instance.OnLoaded();
			Exceptions.ExceptionManager.Instance.OnLoaded();
		}

		void InstallRoutedCommands() {
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.DebugCurrentAssembly, DebugManager.Instance.DebugCurrentAssemblyCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.DebugAssembly, DebugManager.Instance.DebugAssemblyCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.DebugCoreCLRAssembly, DebugManager.Instance.DebugCoreCLRAssemblyCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.StartWithoutDebugging, DebugManager.Instance.StartWithoutDebuggingCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.Attach, DebugManager.Instance.AttachCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.Break, DebugManager.Instance.BreakCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.Restart, DebugManager.Instance.RestartCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.Stop, DebugManager.Instance.StopCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.Detach, DebugManager.Instance.DetachCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.Continue, DebugManager.Instance.ContinueCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.StepInto, DebugManager.Instance.StepIntoCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.StepOver, DebugManager.Instance.StepOverCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.StepOut, DebugManager.Instance.StepOutCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.DeleteAllBreakpoints, Breakpoints.BreakpointManager.Instance.ClearCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.ToggleBreakpoint, Breakpoints.BreakpointManager.Instance.ToggleBreakpointCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.DisableBreakpoint, Breakpoints.BreakpointManager.Instance.DisableBreakpointCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.DisableAllBreakpoints, Breakpoints.BreakpointManager.Instance.DisableAllBreakpointsCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.EnableAllBreakpoints, Breakpoints.BreakpointManager.Instance.EnableAllBreakpointsCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.ShowNextStatement, DebugManager.Instance.ShowNextStatementCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.SetNextStatement, DebugManager.Instance.SetNextStatementCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.ShowCallStack, CallStack.CallStackControlCreator.CallStackControlInstance.ShowCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.ShowBreakpoints, Breakpoints.BreakpointsControlCreator.BreakpointsControlInstance.ShowCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.ShowThreads, Threads.ThreadsControlCreator.ThreadsControlInstance.ShowCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.ShowModules, Modules.ModulesControlCreator.ModulesControlInstance.ShowCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.ShowLocals, Locals.LocalsControlCreator.LocalsControlInstance.ShowCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.ShowExceptions, Exceptions.ExceptionsControlCreator.ExceptionsControlInstance.ShowCommand);
			for (int i = 0; i < DebugRoutedCommands.ShowMemoryCommands.Length; i++)
				MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.ShowMemoryCommands[i], Memory.MemoryControlCreator.GetMemoryControlInstance(i).ShowCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.Continue, DebugManager.Instance.DebugAssemblyCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.StepInto, DebugManager.Instance.DebugAssemblyCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.StepOver, DebugManager.Instance.DebugAssemblyCommand);
		}

		void InstallKeyboardShortcutCommands() {
			AddCommand(MainWindow.Instance, DebugRoutedCommands.StartWithoutDebugging, ModifierKeys.Control, Key.F5);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.Attach, ModifierKeys.Control | ModifierKeys.Alt, Key.P);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.Break, ModifierKeys.Control, Key.Cancel);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.Restart, ModifierKeys.Control | ModifierKeys.Shift, Key.F5);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.Stop, ModifierKeys.Shift, Key.F5);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.Continue, ModifierKeys.None, Key.F5);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.StepInto, ModifierKeys.None, Key.F11);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.StepOver, ModifierKeys.None, Key.F10);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.StepOut, ModifierKeys.Shift, Key.F11);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.DeleteAllBreakpoints, ModifierKeys.Control | ModifierKeys.Shift, Key.F9);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.ToggleBreakpoint, ModifierKeys.None, Key.F9);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.DisableBreakpoint, ModifierKeys.Control, Key.F9);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.ShowNextStatement, ModifierKeys.Alt, Key.Multiply);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.SetNextStatement, ModifierKeys.Control | ModifierKeys.Shift, Key.F10);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.ShowCallStack, ModifierKeys.Control | ModifierKeys.Alt, Key.C);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.ShowCallStack, ModifierKeys.Alt, Key.D7);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.ShowCallStack, ModifierKeys.Alt, Key.NumPad7);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.ShowBreakpoints, ModifierKeys.Control | ModifierKeys.Alt, Key.B);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.ShowBreakpoints, ModifierKeys.Alt, Key.F9);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.ShowThreads, ModifierKeys.Control | ModifierKeys.Alt, Key.H);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.ShowModules, ModifierKeys.Control | ModifierKeys.Alt, Key.U);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.ShowLocals, ModifierKeys.Alt, Key.D4);
			AddCommand(MainWindow.Instance, DebugRoutedCommands.ShowExceptions, ModifierKeys.Control | ModifierKeys.Alt, Key.E);
			for (int i = 0; i < DebugRoutedCommands.ShowMemoryCommands.Length && i < 10; i++) {
				var cmd = DebugRoutedCommands.ShowMemoryCommands[i];
				if (i == 0)
					AddCommand(MainWindow.Instance, cmd, ModifierKeys.Alt, Key.D6);
				AddCommand(MainWindow.Instance, cmd, ModifierKeys.Control | ModifierKeys.Alt, Key.D0 + (i + 1) % 10);
			}
		}

		void AddCommand(UIElement elem, ICommand routedCommand, ModifierKeys modifiers, Key key) {
			elem.InputBindings.Add(new KeyBinding(routedCommand, key, modifiers));
		}
	}
}

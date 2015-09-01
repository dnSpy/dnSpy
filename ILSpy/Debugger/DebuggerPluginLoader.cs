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
			BreakpointManager.Instance.OnLoaded();
			StackFrameManager.Instance.OnLoaded();
		}

		void InstallRoutedCommands() {
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.DebugCurrentAssembly, DebugManager.Instance.DebugCurrentAssemblyCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.DebugAssembly, DebugManager.Instance.DebugAssemblyCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.Attach, DebugManager.Instance.AttachCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.Break, DebugManager.Instance.BreakCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.Restart, DebugManager.Instance.RestartCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.Stop, DebugManager.Instance.StopCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.Detach, DebugManager.Instance.DetachCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.Continue, DebugManager.Instance.ContinueCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.StepInto, DebugManager.Instance.StepIntoCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.StepOver, DebugManager.Instance.StepOverCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.StepOut, DebugManager.Instance.StepOutCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.DeleteAllBreakpoints, BreakpointManager.Instance.ClearCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.ToggleBreakpoint, BreakpointManager.Instance.ToggleBreakpointCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.DisableBreakpoint, BreakpointManager.Instance.DisableBreakpointCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.ShowNextStatement, DebugManager.Instance.ShowNextStatementCommand);
			MainWindow.Instance.AddCommandBinding(DebugRoutedCommands.SetNextStatement, DebugManager.Instance.SetNextStatementCommand);
		}

		void InstallKeyboardShortcutCommands() {
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
		}

		void AddCommand(UIElement elem, ICommand routedCommand, ModifierKeys modifiers, Key key) {
			elem.InputBindings.Add(new KeyBinding(routedCommand, key, modifiers));
		}
	}
}

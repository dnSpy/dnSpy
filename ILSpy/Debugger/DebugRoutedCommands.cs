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

using System.Windows.Input;

namespace dnSpy.Debugger {
	public static class DebugRoutedCommands {
		public static readonly RoutedCommand DebugCurrentAssembly = new RoutedCommand("DebugCurrentAssembly", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand DebugAssembly = new RoutedCommand("DebugAssembly", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand DebugCoreCLRAssembly = new RoutedCommand("DebugCoreCLRAssembly", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand Attach = new RoutedCommand("Attach", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand StartWithoutDebugging = new RoutedCommand("StartWithoutDebugging", typeof(DebugRoutedCommands));

		public static readonly RoutedCommand Break = new RoutedCommand("Break", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand Restart = new RoutedCommand("Restart", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand Stop = new RoutedCommand("Stop", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand Detach = new RoutedCommand("Detach", typeof(DebugRoutedCommands));

		public static readonly RoutedCommand Continue = new RoutedCommand("Continue", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand StepInto = new RoutedCommand("StepInto", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand StepOver = new RoutedCommand("StepOver", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand StepOut = new RoutedCommand("StepOut", typeof(DebugRoutedCommands));

		public static readonly RoutedCommand DeleteAllBreakpoints = new RoutedCommand("DeleteAllBreakpoints", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand ToggleBreakpoint = new RoutedCommand("ToggleBreakpoint", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand DisableBreakpoint = new RoutedCommand("DisableBreakpoint", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand DisableAllBreakpoints = new RoutedCommand("DisableAllBreakpoints", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand EnableAllBreakpoints = new RoutedCommand("EnableAllBreakpoints", typeof(DebugRoutedCommands));

		public static readonly RoutedCommand ShowNextStatement = new RoutedCommand("ShowNextStatement", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand SetNextStatement = new RoutedCommand("SetNextStatement", typeof(DebugRoutedCommands));

		public static readonly RoutedCommand ShowCallStack = new RoutedCommand("ShowCallStack", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand ShowBreakpoints = new RoutedCommand("ShowBreakpoints", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand ShowThreads = new RoutedCommand("ShowThreads", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand ShowModules = new RoutedCommand("ShowModules", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand ShowLocals = new RoutedCommand("ShowLocals", typeof(DebugRoutedCommands));
		public static readonly RoutedCommand ShowExceptions = new RoutedCommand("ShowExceptions", typeof(DebugRoutedCommands));
	}
}

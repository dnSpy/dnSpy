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

using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger {
	abstract class ToolbarDebugCommand : CommandWrapper, IToolbarCommand {
		protected ToolbarDebugCommand(ICommand command)
			: base(command) {
		}

		public virtual bool IsVisible {
			get { return DebugManager.Instance.IsDebugging; }
		}
	}

	[ExportToolbarCommand(ToolTip = "Debug an Executable", ToolbarIcon = "StartDebugging", ToolbarCategory = "Debug1", ToolbarOrder = 6000)]
	sealed class DebugAssemblyToolbarCommand : ToolbarDebugCommand {
		public DebugAssemblyToolbarCommand()
			: base(DebugRoutedCommands.DebugAssembly) {
		}

		public override bool IsVisible {
			get { return !DebugManager.Instance.IsDebugging; }
		}
	}

	[ExportToolbarCommand(ToolTip = "Continue (F5)", ToolbarIcon = "ContinueDebugging", ToolbarCategory = "Debug2", ToolbarOrder = 7000)]
	sealed class ContinueToolbarDebugCommand : ToolbarDebugCommand {
		public ContinueToolbarDebugCommand()
			: base(DebugRoutedCommands.Continue) {
		}
	}

	[ExportToolbarCommand(ToolTip = "Break (Ctrl+Break)", ToolbarIcon = "Break", ToolbarCategory = "Debug2", ToolbarOrder = 7100)]
	sealed class BreakToolbarDebugCommand : ToolbarDebugCommand {
		public BreakToolbarDebugCommand()
			: base(DebugRoutedCommands.Break) {
		}
	}

	[ExportToolbarCommand(ToolTip = "Stop Debugging (Shift+F5)", ToolbarIcon = "StopProcess", ToolbarCategory = "Debug2", ToolbarOrder = 7200)]
	sealed class StopToolbarDebugCommand : ToolbarDebugCommand {
		public StopToolbarDebugCommand()
			: base(DebugRoutedCommands.Stop) {
		}
	}

	[ExportToolbarCommand(ToolTip = "Restart (Ctrl+Shift+F5)", ToolbarIcon = "RestartProcess", ToolbarCategory = "Debug2", ToolbarOrder = 7300)]
	sealed class RestartToolbarDebugCommand : ToolbarDebugCommand {
		public RestartToolbarDebugCommand()
			: base(DebugRoutedCommands.Restart) {
		}
	}

	[ExportToolbarCommand(ToolTip = "Show Next Statement (Alt+Num *)", ToolbarIcon = "CurrentLineToolBar", ToolbarCategory = "Debug3", ToolbarOrder = 8000)]
	sealed class ShowNextStatementToolbarDebugCommand : ToolbarDebugCommand {
		public ShowNextStatementToolbarDebugCommand()
			: base(DebugRoutedCommands.ShowNextStatement) {
		}
	}

	[ExportToolbarCommand(ToolTip = "Step Into (F11)", ToolbarIcon = "StepInto", ToolbarCategory = "Debug3", ToolbarOrder = 8100)]
	sealed class StepIntoToolbarDebugCommand : ToolbarDebugCommand {
		public StepIntoToolbarDebugCommand()
			: base(DebugRoutedCommands.StepInto) {
		}
	}

	[ExportToolbarCommand(ToolTip = "Step Over (F10)", ToolbarIcon = "StepOver", ToolbarCategory = "Debug3", ToolbarOrder = 8200)]
	sealed class StepOverToolbarDebugCommand : ToolbarDebugCommand {
		public StepOverToolbarDebugCommand()
			: base(DebugRoutedCommands.StepOver) {
		}
	}

	[ExportToolbarCommand(ToolTip = "Step Out (Shift+F11)", ToolbarIcon = "StepOut", ToolbarCategory = "Debug3", ToolbarOrder = 8300)]
	sealed class StepOutToolbarDebugCommand : ToolbarDebugCommand {
		public StepOutToolbarDebugCommand()
			: base(DebugRoutedCommands.StepOut) {
		}
	}

	abstract class DebugCtxMenuCommand : IContextMenuEntry2 {
		readonly ICommand realCmd;

		protected DebugCtxMenuCommand(ICommand realCmd) {
			this.realCmd = realCmd;
		}

		public void Execute(ContextMenuEntryContext context) {
			realCmd.Execute(null);
		}

		public virtual void Initialize(ContextMenuEntryContext context, MenuItem menuItem) {
		}

		public bool IsEnabled(ContextMenuEntryContext context) {
			return true;
		}

		public bool IsVisible(ContextMenuEntryContext context) {
			return realCmd.CanExecute(null);
		}
	}

	[ExportContextMenuEntry(Header = "_Debug Assembly", Icon = "StartDebugging", Order = 200, Category = "Debug")]
	sealed class DebugAssemblyDebugCtxMenuCommand : DebugCtxMenuCommand {
		public DebugAssemblyDebugCtxMenuCommand()
			: base(DebugRoutedCommands.DebugAssembly) {
		}
	}

	[ExportContextMenuEntry(Icon = "BreakpointMenu", InputGestureText = "F9", Category = "Debug", Order = 210)]
	sealed class ToggleBreakpointDebugCtxMenuCommand : DebugCtxMenuCommand {
		public ToggleBreakpointDebugCtxMenuCommand()
			: base(DebugRoutedCommands.ToggleBreakpoint) {
		}

		public override void Initialize(ContextMenuEntryContext context, MenuItem menuItem) {
			//TODO:
		}
	}

	[ExportContextMenuEntry(InputGestureText = "Ctrl+F9", Category = "Debug", Order = 220)]
	sealed class EnableDisableBreakpointDebugCtxMenuCommand : DebugCtxMenuCommand {
		public EnableDisableBreakpointDebugCtxMenuCommand()
			: base(DebugRoutedCommands.DisableBreakpoint) {
		}

		public override void Initialize(ContextMenuEntryContext context, MenuItem menuItem) {
			//TODO:
		}
	}

	[ExportContextMenuEntry(Icon = "CurrentLineToolBar", Header = "S_how Next Statement", InputGestureText = "Alt+Num *", Category = "Debug", Order = 230)]
	sealed class ShowNextStatementDebugCtxMenuCommand : DebugCtxMenuCommand {
		public ShowNextStatementDebugCtxMenuCommand()
			: base(DebugRoutedCommands.ShowNextStatement) {
		}
	}

	[ExportContextMenuEntry(Icon = "SetNextStatement", Header = "Set Ne_xt Statement", InputGestureText = "Ctrl+Shift+F10", Category = "Debug", Order = 240)]
	sealed class SetNextStatementDebugCtxMenuCommand : DebugCtxMenuCommand {
		public SetNextStatementDebugCtxMenuCommand()
			: base(DebugRoutedCommands.SetNextStatement) {
		}
	}

	abstract class DebugMainMenuCommand : CommandWrapper, IMainMenuCommand {
		readonly bool? mustBeDebugging;

		protected DebugMainMenuCommand(ICommand realCmd, bool? mustBeDebugging)
			: base(realCmd) {
			this.mustBeDebugging = mustBeDebugging;
		}

		public bool IsVisible {
			get { return mustBeDebugging == null || DebugManager.Instance.IsDebugging == mustBeDebugging; }
		}
	}

	[ExportMainMenuCommand(Menu = "_Debug", MenuIcon = "StartDebugging", MenuCategory = "Start", MenuHeader = "Debug an _Executable…", MenuOrder = 5000)]
	sealed class DebugAssemblyDebugMainMenuCommand : DebugMainMenuCommand {
		public DebugAssemblyDebugMainMenuCommand()
			: base(DebugRoutedCommands.DebugAssembly, false) {
		}
	}

	[ExportMainMenuCommand(Menu = "_Debug", MenuIcon = "Process", MenuCategory = "Start", MenuHeader = "Attach to _Process…", MenuInputGestureText = "Ctrl+Alt+P", MenuOrder = 5010)]
	sealed class AttachDebugMainMenuCommand : DebugMainMenuCommand {
		public AttachDebugMainMenuCommand()
			: base(DebugRoutedCommands.Attach, false) {
		}
	}

	[ExportMainMenuCommand(Menu = "_Debug", MenuIcon = "ContinueDebugging", MenuCategory = "Debug1", MenuHeader = "_Continue", MenuInputGestureText = "F5", MenuOrder = 5100)]
	sealed class ContinueDebugMainMenuCommand : DebugMainMenuCommand {
		public ContinueDebugMainMenuCommand()
			: base(DebugRoutedCommands.Continue, true) {
		}
	}

	[ExportMainMenuCommand(Menu = "_Debug", MenuIcon = "Break", MenuCategory = "Debug1", MenuHeader = "Brea_k", MenuInputGestureText = "Ctrl+Break", MenuOrder = 5110)]
	sealed class BreakDebugMainMenuCommand : DebugMainMenuCommand {
		public BreakDebugMainMenuCommand()
			: base(DebugRoutedCommands.Break, true) {
		}
	}

	[ExportMainMenuCommand(Menu = "_Debug", MenuIcon = "StopProcess", MenuCategory = "Debug1", MenuHeader = "Stop D_ebugging", MenuInputGestureText = "Shift+F5", MenuOrder = 5120)]
	sealed class StopDebugMainMenuCommand : DebugMainMenuCommand {
		public StopDebugMainMenuCommand()
			: base(DebugRoutedCommands.Stop, true) {
		}
	}

	[ExportMainMenuCommand(Menu = "_Debug", MenuCategory = "Debug1", MenuHeader = "_Detach", MenuOrder = 5130)]
	sealed class DetachDebugMainMenuCommand : DebugMainMenuCommand {
		public DetachDebugMainMenuCommand()
			: base(DebugRoutedCommands.Detach, true) {
		}
	}

	[ExportMainMenuCommand(Menu = "_Debug", MenuIcon = "RestartProcess", MenuCategory = "Debug1", MenuHeader = "_Restart", MenuInputGestureText = "Ctrl+Shift+F5", MenuOrder = 5140)]
	sealed class RestartDebugMainMenuCommand : DebugMainMenuCommand {
		public RestartDebugMainMenuCommand()
			: base(DebugRoutedCommands.Restart, true) {
		}
	}

	[ExportMainMenuCommand(Menu = "_Debug", MenuIcon = "StepInto", MenuCategory = "Debug2", MenuHeader = "Step _Into", MenuInputGestureText = "F11", MenuOrder = 5200)]
	sealed class StepIntoDebugMainMenuCommand : DebugMainMenuCommand {
		public StepIntoDebugMainMenuCommand()
			: base(DebugRoutedCommands.StepInto, true) {
		}
	}

	[ExportMainMenuCommand(Menu = "_Debug", MenuIcon = "StepOver", MenuCategory = "Debug2", MenuHeader = "Step _Over", MenuInputGestureText = "F10", MenuOrder = 5210)]
	sealed class StepOverDebugMainMenuCommand : DebugMainMenuCommand {
		public StepOverDebugMainMenuCommand()
			: base(DebugRoutedCommands.StepOver, true) {
		}
	}

	[ExportMainMenuCommand(Menu = "_Debug", MenuIcon = "StepOut", MenuCategory = "Debug2", MenuHeader = "Step Ou_t", MenuInputGestureText = "Shift+F11", MenuOrder = 5220)]
	sealed class StepOutDebugMainMenuCommand : DebugMainMenuCommand {
		public StepOutDebugMainMenuCommand()
			: base(DebugRoutedCommands.StepOut, true) {
		}
	}

	[ExportMainMenuCommand(Menu = "_Debug", MenuCategory = "Breakpoints", MenuHeader = "To_ggle Breakpoint", MenuInputGestureText = "F9", MenuOrder = 5300)]
	sealed class ToggleBreakpointDebugMainMenuCommand : DebugMainMenuCommand {
		public ToggleBreakpointDebugMainMenuCommand()
			: base(DebugRoutedCommands.ToggleBreakpoint, null) {
		}
	}

	[ExportMainMenuCommand(Menu = "_Debug", MenuIcon = "DeleteAllBreakpoints", MenuCategory = "Breakpoints", MenuHeader = "Delete _All Breakpoints", MenuInputGestureText = "Ctrl+Shift+F9", MenuOrder = 5310)]
	sealed class DeleteAllBreakpointsDebugMainMenuCommand : DebugMainMenuCommand {
		public DeleteAllBreakpointsDebugMainMenuCommand()
			: base(DebugRoutedCommands.DeleteAllBreakpoints, null) {
		}
	}
}

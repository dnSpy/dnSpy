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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Input;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.DbgUI {
	static class DebugMenuCommands {
		abstract class DebugMainMenuCommand : MenuItemBase {
			protected readonly Lazy<Debugger> debugger;
			readonly bool? mustBeDebugging;

			protected DebugMainMenuCommand(Lazy<Debugger> debugger, bool? mustBeDebugging) {
				this.debugger = debugger;
				this.mustBeDebugging = mustBeDebugging;
			}

			public override bool IsVisible(IMenuItemContext context) => mustBeDebugging == null || debugger.Value.IsDebugging == mustBeDebugging;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:DebugAssemblyCommand2", Icon = DsImagesAttribute.Run, InputGestureText = "res:ShortCutKeyF5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_START, Order = 0)]
		sealed class DebugAssemblyDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public DebugAssemblyDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, false) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.DebugProgram();
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StartWithoutDebuggingCommand", Icon = DsImagesAttribute.RunOutline, InputGestureText = "res:ShortCutKeyCtrlF5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_START, Order = 10)]
		sealed class StartWithoutDegbuggingDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public StartWithoutDegbuggingDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, false) {
			}

			public override void Execute(IMenuItemContext context) {
				//TODO:
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:AttachToProcessCommand", Icon = DsImagesAttribute.Process, InputGestureText = "res:ShortCutKeyCtrlAltP", Group = MenuConstants.GROUP_APP_MENU_DEBUG_START, Order = 30)]
		sealed class AttachDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public AttachDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, false) {
			}

			public override void Execute(IMenuItemContext context) {
				//TODO:
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:ContinueDebuggingCommand", Icon = DsImagesAttribute.Run, InputGestureText = "res:ShortCutKeyF5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 0)]
		sealed class ContinueDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public ContinueDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.Continue();
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:BreakCommand", Icon = DsImagesAttribute.Pause, InputGestureText = "res:ShortCutKeyCtrlBreak", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 10)]
		sealed class BreakDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public BreakDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.BreakAll();
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StopDebuggingCommand", Icon = DsImagesAttribute.Stop, InputGestureText = "res:ShortCutKeyShiftF5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 20)]
		sealed class StopDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public StopDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.Stop();
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:DetachCommand", Icon = DsImagesAttribute.Cancel, Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 30)]
		sealed class DetachDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public DetachDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.DetachAll();
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:RestartCommand", Icon = DsImagesAttribute.Restart, InputGestureText = "res:ShortCutKeyCtrlShiftF5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 40)]
		sealed class RestartDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public RestartDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.Restart();
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StepIntoCommand", Icon = DsImagesAttribute.StepInto, InputGestureText = "res:ShortCutKeyF11", Group = MenuConstants.GROUP_APP_MENU_DEBUG_STEP, Order = 0)]
		sealed class StepIntoDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public StepIntoDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.StepInto();
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StepOverCommand", Icon = DsImagesAttribute.StepOver, InputGestureText = "res:ShortCutKeyF10", Group = MenuConstants.GROUP_APP_MENU_DEBUG_STEP, Order = 10)]
		sealed class StepOverDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public StepOverDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.StepOver();
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StepOutCommand", Icon = DsImagesAttribute.StepOut, InputGestureText = "res:ShortCutKeyShiftF11", Group = MenuConstants.GROUP_APP_MENU_DEBUG_STEP, Order = 20)]
		sealed class StepOutDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public StepOutDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.StepOut();
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:ToggleBreakpointCommand", InputGestureText = "res:ShortCutKeyF9", Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 0)]
		sealed class ToggleBreakpointDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public ToggleBreakpointDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, null) {
			}

			public override void Execute(IMenuItemContext context) {
				//TODO:
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:DeleteAllBreakpointsCommand", Icon = DsImagesAttribute.ClearBreakpointGroup, InputGestureText = "res:ShortCutKeyCtrlShiftF9", Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 10)]
		sealed class DeleteAllBreakpointsDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public DeleteAllBreakpointsDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, null) {
			}

			public override void Execute(IMenuItemContext context) {
				//TODO:
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:EnableAllBreakpointsCommand", Icon = DsImagesAttribute.EnableAllBreakpoints, Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 20)]
		sealed class EnableAllBreakpointsDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public EnableAllBreakpointsDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, null) {
			}

			public override bool IsVisible(IMenuItemContext context) {
				return true;//TODO:
			}

			public override void Execute(IMenuItemContext context) {
				//TODO:
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:DisableAllBreakpointsCommand", Icon = DsImagesAttribute.DisableAllBreakpoints, Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 30)]
		sealed class DisableAllBreakpointsDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public DisableAllBreakpointsDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, null) {
			}

			public override bool IsVisible(IMenuItemContext context) {
				return true;//TODO:
			}

			public override void Execute(IMenuItemContext context) {
				//TODO:
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:BreakpointsCommand", Icon = DsImagesAttribute.BreakpointsWindow, InputGestureText = "res:ShortCutKeyCtrlAltB", Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 40)]
		sealed class BreakpointsWindowCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public BreakpointsWindowCommand(Lazy<Debugger> debugger)
				: base(debugger, null) {
			}

			public override void Execute(IMenuItemContext context) {
				//TODO:
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:LocalsCommand", Icon = DsImagesAttribute.LocalsWindow, InputGestureText = "res:ShortCutKeyAlt4", Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 0)]
		sealed class LocalsWindowCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public LocalsWindowCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) {
				//TODO:
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:CallStackCommand", Icon = DsImagesAttribute.CallStackWindow, InputGestureText = "res:ShortCutKeyCtrlAltC", Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 10)]
		sealed class CallStackWindowCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public CallStackWindowCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) {
				//TODO:
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:ThreadsCommand", Icon = DsImagesAttribute.Thread, InputGestureText = "res:ShortCutKeyCtrlAltH", Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 20)]
		sealed class ThreadsWindowCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public ThreadsWindowCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) {
				//TODO:
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:ModulesCommand", Icon = DsImagesAttribute.ModulesWindow, InputGestureText = "res:ShortCutKeyCtrlAltU", Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 30)]
		sealed class ModulesWindowCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public ModulesWindowCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) {
				//TODO:
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:ExceptionSettingsCommand", Icon = DsImagesAttribute.ExceptionSettings, InputGestureText = "res:ShortCutKeyCtrlAltE", Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 40)]
		sealed class ExceptionSettingsWindowCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public ExceptionSettingsWindowCommand(Lazy<Debugger> debugger)
				: base(debugger, null) {
			}

			public override void Execute(IMenuItemContext context) {
				//TODO:
			}
		}

		static class Constants {
			public const string SHOW_IN_MEMORY_WINDOW_GUID = "C9EF4AD5-21C6-4185-B5C7-7DCF2DFA7BCD";
			public const string GROUP_SHOW_IN_MEMORY_WINDOW = "0,6DE55384-1907-4D19-86F1-3C48A1846193";
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:MemoryWindowCommand", Icon = DsImagesAttribute.MemoryWindow, Guid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 50)]
		sealed class MemoryWindowCommand : MenuItemBase {
			readonly Lazy<Debugger> debugger;

			[ImportingConstructor]
			MemoryWindowCommand(Lazy<Debugger> debugger) => this.debugger = debugger;

			public override void Execute(IMenuItemContext context) { }
			public override bool IsVisible(IMenuItemContext context) => debugger.Value.IsDebugging;
		}

		sealed class CommandToMenuItem : MenuItemBase {
			readonly ICommand cmd;

			public CommandToMenuItem(ICommand cmd) => this.cmd = cmd;

			public override void Execute(IMenuItemContext context) => cmd.Execute(context);
			public override bool IsVisible(IMenuItemContext context) => cmd.CanExecute(context);
		}

		[ExportMenuItem(OwnerGuid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = Constants.GROUP_SHOW_IN_MEMORY_WINDOW, Order = 0)]
		sealed class SubMenuMemoryWindowCommand : MenuItemBase, IMenuItemProvider {
			static SubMenuMemoryWindowCommand() {
				subCmds = null;
				/*TODO:
				subCmds = new Tuple<IMenuItem, string, string>[DebugRoutedCommands.ShowMemoryCommands.Length];
				for (int i = 0; i < subCmds.Length; i++) {
					var inputGestureText = GetInputGestureText(i);
					var headerText = MemoryWindowsHelper.GetHeaderText(i);
					var cmd = new CommandToMenuItem(DebugRoutedCommands.ShowMemoryCommands[i]);
					subCmds[i] = Tuple.Create((IMenuItem)cmd, headerText, inputGestureText);
				}
				*/
			}

			static string GetInputGestureText(int i) {
				if (i == 0)
					return dnSpy_Debugger_Resources.ShortCutKeyAlt6;
				if (1 <= i && i <= 9)
					return string.Format(dnSpy_Debugger_Resources.ShortCutKeyCtrlShift_DIGIT, (i + 1) % 10);
				return string.Empty;
			}

			static readonly Tuple<IMenuItem, string, string>[] subCmds;

			public override void Execute(IMenuItemContext context) { }

			public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
				for (int i = 0; i < subCmds.Length; i++) {
					var info = subCmds[i];
					var attr = new ExportMenuItemAttribute { Header = info.Item2, Icon = DsImagesAttribute.MemoryWindow };
					if (!string.IsNullOrEmpty(info.Item3))
						attr.InputGestureText = info.Item3;
					yield return new CreatedMenuItem(attr, info.Item1);
				}
			}
		}
	}
}

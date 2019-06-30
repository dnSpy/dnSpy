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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows {
	[ExportAutoLoaded]
	sealed class ToolWindowsLoader : IAutoLoaded {
		[ImportingConstructor]
		ToolWindowsLoader(IWpfCommandService wpfCommandService, Lazy<ToolWindowsOperations> toolWindowsOperations) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);
			cmds.Add(new RelayCommand(a => toolWindowsOperations.Value.ShowCodeBreakpoints(), a => toolWindowsOperations.Value.CanShowCodeBreakpoints), ModifierKeys.Control | ModifierKeys.Alt, Key.B);
			cmds.Add(new RelayCommand(a => toolWindowsOperations.Value.ShowCodeBreakpoints(), a => toolWindowsOperations.Value.CanShowCodeBreakpoints), ModifierKeys.Alt, Key.F9);
			cmds.Add(new RelayCommand(a => toolWindowsOperations.Value.ShowCallStack(), a => toolWindowsOperations.Value.CanShowCallStack), ModifierKeys.Control | ModifierKeys.Alt, Key.C);
			cmds.Add(new RelayCommand(a => toolWindowsOperations.Value.ShowCallStack(), a => toolWindowsOperations.Value.CanShowCallStack), ModifierKeys.Alt, Key.D7);
			cmds.Add(new RelayCommand(a => toolWindowsOperations.Value.ShowCallStack(), a => toolWindowsOperations.Value.CanShowCallStack), ModifierKeys.Alt, Key.NumPad7);
			cmds.Add(new RelayCommand(a => toolWindowsOperations.Value.ShowThreads(), a => toolWindowsOperations.Value.CanShowThreads), ModifierKeys.Control | ModifierKeys.Alt, Key.H);
			cmds.Add(new RelayCommand(a => toolWindowsOperations.Value.ShowModules(), a => toolWindowsOperations.Value.CanShowModules), ModifierKeys.Control | ModifierKeys.Alt, Key.U);
			cmds.Add(new RelayCommand(a => toolWindowsOperations.Value.ShowExceptions(), a => toolWindowsOperations.Value.CanShowExceptions), ModifierKeys.Control | ModifierKeys.Alt, Key.E);
			cmds.Add(new RelayCommand(a => toolWindowsOperations.Value.ShowProcesses(), a => toolWindowsOperations.Value.CanShowProcesses), ModifierKeys.Control | ModifierKeys.Alt, Key.Z);
			cmds.Add(new RelayCommand(a => toolWindowsOperations.Value.ShowLocals(), a => toolWindowsOperations.Value.CanShowLocals), ModifierKeys.Alt, Key.D4);

			for (int i = 0; i < Memory.MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS && i < 10; i++) {
				int index = i;
				var cmd = new RelayCommand(a => toolWindowsOperations.Value.ShowMemory(index), a => toolWindowsOperations.Value.CanShowMemory(index));
				if (i == 0)
					cmds.Add(cmd, ModifierKeys.Alt, Key.D6);
				cmds.Add(cmd, ModifierKeys.Control | ModifierKeys.Shift, Key.D0 + (i + 1) % 10);
			}
		}
	}

	abstract class DebugToolWindowMainMenuCommand : MenuItemBase {
		protected readonly Lazy<ToolWindowsOperations> toolWindowsOperations;
		protected DebugToolWindowMainMenuCommand(Lazy<ToolWindowsOperations> toolWindowsOperations) => this.toolWindowsOperations = toolWindowsOperations;
		public override bool IsVisible(IMenuItemContext context) => IsEnabled(context);
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_WINDOWS_GUID, Header = "res:BreakpointsCommand", Icon = DsImagesAttribute.BreakpointsWindow, InputGestureText = "res:ShortCutKeyCtrlAltB", Group = MenuConstants.GROUP_APP_MENU_DEBUG_WINDOWS_SETTINGS, Order = 0)]
	sealed class BreakpointsWindowCommand : DebugToolWindowMainMenuCommand {
		[ImportingConstructor]
		public BreakpointsWindowCommand(Lazy<ToolWindowsOperations> toolWindowsOperations) : base(toolWindowsOperations) { }
		public override void Execute(IMenuItemContext context) => toolWindowsOperations.Value.ShowCodeBreakpoints();
		public override bool IsEnabled(IMenuItemContext context) => toolWindowsOperations.Value.CanShowCodeBreakpoints;
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_WINDOWS_GUID, Header = "res:ModuleBreakpointsCommand", Icon = DsImagesAttribute.ModulePublic, Group = MenuConstants.GROUP_APP_MENU_DEBUG_WINDOWS_SETTINGS, Order = 10)]
	sealed class ModuleBreakpointsWindowCommand : DebugToolWindowMainMenuCommand {
		[ImportingConstructor]
		public ModuleBreakpointsWindowCommand(Lazy<ToolWindowsOperations> toolWindowsOperations) : base(toolWindowsOperations) { }
		public override void Execute(IMenuItemContext context) => toolWindowsOperations.Value.ShowModuleBreakpoints();
		public override bool IsEnabled(IMenuItemContext context) => toolWindowsOperations.Value.CanShowModuleBreakpoints;
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_WINDOWS_GUID, Header = "res:ExceptionSettingsCommand", Icon = DsImagesAttribute.ExceptionSettings, InputGestureText = "res:ShortCutKeyCtrlAltE", Group = MenuConstants.GROUP_APP_MENU_DEBUG_WINDOWS_SETTINGS, Order = 20)]
	sealed class ExceptionSettingsWindowCommand : DebugToolWindowMainMenuCommand {
		[ImportingConstructor]
		public ExceptionSettingsWindowCommand(Lazy<ToolWindowsOperations> toolWindowsOperations) : base(toolWindowsOperations) { }
		public override void Execute(IMenuItemContext context) => toolWindowsOperations.Value.ShowExceptions();
		public override bool IsEnabled(IMenuItemContext context) => toolWindowsOperations.Value.CanShowExceptions;
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_WINDOWS_GUID, Header = "res:OutputCommand", Icon = DsImagesAttribute.Output, Group = MenuConstants.GROUP_APP_MENU_DEBUG_WINDOWS_SETTINGS, Order = 30)]
	sealed class OutputWindowCommand : DebugToolWindowMainMenuCommand {
		[ImportingConstructor]
		public OutputWindowCommand(Lazy<ToolWindowsOperations> toolWindowsOperations) : base(toolWindowsOperations) { }
		public override void Execute(IMenuItemContext context) => toolWindowsOperations.Value.ShowOutput();
		public override bool IsEnabled(IMenuItemContext context) => toolWindowsOperations.Value.CanShowOutput;
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_WINDOWS_GUID, Header = "res:WatchCommand", Icon = DsImagesAttribute.FileSystemWatcher, Guid = Constants.WATCH_GUID, Group = MenuConstants.GROUP_APP_MENU_DEBUG_WINDOWS_VALUES, Order = 0)]
	sealed class WatchWindowCommand : MenuItemBase {
		readonly Lazy<DbgManager> dbgManager;

		[ImportingConstructor]
		WatchWindowCommand(Lazy<DbgManager> dbgManager) => this.dbgManager = dbgManager;

		public override void Execute(IMenuItemContext context) { }
		public override bool IsVisible(IMenuItemContext context) => dbgManager.Value.IsDebugging;
	}

	[ExportMenuItem(OwnerGuid = Constants.WATCH_GUID, Group = MenuConstants.GROUP_APP_MENU_DEBUG_WINDOWS_WATCH_SUB, Order = 0)]
	sealed class SubMenuWatchWindowCommand : MenuItemBase, IMenuItemProvider {
		readonly (IMenuItem menuItem, string headerText, string inputGestureText)[] subCmds;

		[ImportingConstructor]
		SubMenuWatchWindowCommand(Lazy<ToolWindowsOperations> toolWindowsOperations) {
			subCmds = new(IMenuItem, string, string)[Watch.WatchWindowsHelper.NUMBER_OF_WATCH_WINDOWS];
			for (int i = 0; i < subCmds.Length; i++) {
				var inputGestureText = GetInputGestureText(i);
				var headerText = Watch.WatchWindowsHelper.GetHeaderText(i);
				var cmd = new WatchWindowNCommand(toolWindowsOperations, i);
				subCmds[i] = (cmd, headerText, inputGestureText);
			}
		}

		static string GetInputGestureText(int i) {
			if (0 <= i && i <= 9)
				return string.Format(dnSpy_Debugger_Resources.ShortCutKeyCtrlAltW_DIGIT, (i + 1) % 10);
			return string.Empty;
		}

		public override void Execute(IMenuItemContext context) { }

		public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
			for (int i = 0; i < subCmds.Length; i++) {
				var info = subCmds[i];
				var attr = new ExportMenuItemAttribute { Header = info.Item2, Icon = DsImagesAttribute.FileSystemWatcher };
				if (!string.IsNullOrEmpty(info.Item3))
					attr.InputGestureText = info.Item3;
				yield return new CreatedMenuItem(attr, info.Item1);
			}
		}
	}

	sealed class WatchWindowNCommand : DebugToolWindowMainMenuCommand {
		readonly int index;
		public WatchWindowNCommand(Lazy<ToolWindowsOperations> toolWindowsOperations, int index) : base(toolWindowsOperations) => this.index = index;
		public override void Execute(IMenuItemContext context) => toolWindowsOperations.Value.ShowWatch(index);
		public override bool IsEnabled(IMenuItemContext context) => toolWindowsOperations.Value.CanShowWatch(index);
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_WINDOWS_GUID, Header = "res:AutosCommand", Icon = DsImagesAttribute.AutosWindow, InputGestureText = "res:ShortCutKeyCtrlAltV_A", Group = MenuConstants.GROUP_APP_MENU_DEBUG_WINDOWS_VALUES, Order = 10)]
	sealed class AutosWindowCommand : DebugToolWindowMainMenuCommand {
		[ImportingConstructor]
		public AutosWindowCommand(Lazy<ToolWindowsOperations> toolWindowsOperations) : base(toolWindowsOperations) { }
		public override void Execute(IMenuItemContext context) => toolWindowsOperations.Value.ShowAutos();
		public override bool IsEnabled(IMenuItemContext context) => toolWindowsOperations.Value.CanShowAutos;
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_WINDOWS_GUID, Header = "res:LocalsCommand", Icon = DsImagesAttribute.LocalsWindow, InputGestureText = "res:ShortCutKeyAlt4", Group = MenuConstants.GROUP_APP_MENU_DEBUG_WINDOWS_VALUES, Order = 20)]
	sealed class LocalsWindowCommand : DebugToolWindowMainMenuCommand {
		[ImportingConstructor]
		public LocalsWindowCommand(Lazy<ToolWindowsOperations> toolWindowsOperations) : base(toolWindowsOperations) { }
		public override void Execute(IMenuItemContext context) => toolWindowsOperations.Value.ShowLocals();
		public override bool IsEnabled(IMenuItemContext context) => toolWindowsOperations.Value.CanShowLocals;
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_WINDOWS_GUID, Header = "res:CallStackCommand", Icon = DsImagesAttribute.CallStackWindow, InputGestureText = "res:ShortCutKeyCtrlAltC", Group = MenuConstants.GROUP_APP_MENU_DEBUG_WINDOWS_INFO, Order = 0)]
	sealed class CallStackWindowCommand : DebugToolWindowMainMenuCommand {
		[ImportingConstructor]
		public CallStackWindowCommand(Lazy<ToolWindowsOperations> toolWindowsOperations) : base(toolWindowsOperations) { }
		public override void Execute(IMenuItemContext context) => toolWindowsOperations.Value.ShowCallStack();
		public override bool IsEnabled(IMenuItemContext context) => toolWindowsOperations.Value.CanShowCallStack;
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_WINDOWS_GUID, Header = "res:ThreadsCommand", Icon = DsImagesAttribute.Thread, InputGestureText = "res:ShortCutKeyCtrlAltH", Group = MenuConstants.GROUP_APP_MENU_DEBUG_WINDOWS_INFO, Order = 10)]
	sealed class ThreadsWindowCommand : DebugToolWindowMainMenuCommand {
		[ImportingConstructor]
		public ThreadsWindowCommand(Lazy<ToolWindowsOperations> toolWindowsOperations) : base(toolWindowsOperations) { }
		public override void Execute(IMenuItemContext context) => toolWindowsOperations.Value.ShowThreads();
		public override bool IsEnabled(IMenuItemContext context) => toolWindowsOperations.Value.CanShowThreads;
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_WINDOWS_GUID, Header = "res:ModulesCommand", Icon = DsImagesAttribute.ModulesWindow, InputGestureText = "res:ShortCutKeyCtrlAltU", Group = MenuConstants.GROUP_APP_MENU_DEBUG_WINDOWS_INFO, Order = 20)]
	sealed class ModulesWindowCommand : DebugToolWindowMainMenuCommand {
		[ImportingConstructor]
		public ModulesWindowCommand(Lazy<ToolWindowsOperations> toolWindowsOperations) : base(toolWindowsOperations) { }
		public override void Execute(IMenuItemContext context) => toolWindowsOperations.Value.ShowModules();
		public override bool IsEnabled(IMenuItemContext context) => toolWindowsOperations.Value.CanShowModules;
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_WINDOWS_GUID, Header = "res:ProcessesCommand", Icon = DsImagesAttribute.Process, InputGestureText = "res:ShortCutKeyCtrlAltZ", Group = MenuConstants.GROUP_APP_MENU_DEBUG_WINDOWS_INFO, Order = 30)]
	sealed class ProcessesWindowCommand : DebugToolWindowMainMenuCommand {
		[ImportingConstructor]
		public ProcessesWindowCommand(Lazy<ToolWindowsOperations> toolWindowsOperations) : base(toolWindowsOperations) { }
		public override void Execute(IMenuItemContext context) => toolWindowsOperations.Value.ShowProcesses();
		public override bool IsEnabled(IMenuItemContext context) => toolWindowsOperations.Value.CanShowProcesses;
	}

	static class Constants {
		public const string WATCH_GUID = "ED461E7B-3254-4C26-8F39-F23C308644BD";
		public const string SHOW_IN_MEMORY_WINDOW_GUID = "C9EF4AD5-21C6-4185-B5C7-7DCF2DFA7BCD";
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:DebugWindows", Guid = MenuConstants.APP_MENU_DEBUG_WINDOWS_GUID, Group = MenuConstants.GROUP_APP_MENU_DEBUG_WINDOWS, Order = 0)]
	sealed class DebugMemoryWindowsCommand : MenuItemBase {
		public override void Execute(IMenuItemContext context) { }
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_WINDOWS_GUID, Header = "res:MemoryWindowCommand", Icon = DsImagesAttribute.MemoryWindow, Guid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = MenuConstants.GROUP_APP_MENU_DEBUG_WINDOWS_MEMORY, Order = 0)]
	sealed class MemoryWindowCommand : MenuItemBase {
		readonly Lazy<DbgManager> dbgManager;

		[ImportingConstructor]
		MemoryWindowCommand(Lazy<DbgManager> dbgManager) => this.dbgManager = dbgManager;

		public override void Execute(IMenuItemContext context) { }
		public override bool IsVisible(IMenuItemContext context) => dbgManager.Value.IsDebugging;
	}

	sealed class MemoryWindowNCommand : DebugToolWindowMainMenuCommand {
		readonly int index;
		public MemoryWindowNCommand(Lazy<ToolWindowsOperations> toolWindowsOperations, int index) : base(toolWindowsOperations) => this.index = index;
		public override void Execute(IMenuItemContext context) => toolWindowsOperations.Value.ShowMemory(index);
		public override bool IsEnabled(IMenuItemContext context) => toolWindowsOperations.Value.CanShowMemory(index);
	}

	[ExportMenuItem(OwnerGuid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = MenuConstants.GROUP_APP_MENU_DEBUG_WINDOWS_MEMORY_SUB, Order = 0)]
	sealed class SubMenuMemoryWindowCommand : MenuItemBase, IMenuItemProvider {
		readonly (IMenuItem menuItem, string headerText, string inputGestureText)[] subCmds;

		[ImportingConstructor]
		SubMenuMemoryWindowCommand(Lazy<ToolWindowsOperations> toolWindowsOperations) {
			subCmds = new(IMenuItem, string, string)[Memory.MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS];
			for (int i = 0; i < subCmds.Length; i++) {
				var inputGestureText = GetInputGestureText(i);
				var headerText = Memory.MemoryWindowsHelper.GetHeaderText(i);
				var cmd = new MemoryWindowNCommand(toolWindowsOperations, i);
				subCmds[i] = (cmd, headerText, inputGestureText);
			}
		}

		static string GetInputGestureText(int i) {
			if (i == 0)
				return dnSpy_Debugger_Resources.ShortCutKeyAlt6;
			if (1 <= i && i <= 9)
				return string.Format(dnSpy_Debugger_Resources.ShortCutKeyCtrlShift_DIGIT, (i + 1) % 10);
			return string.Empty;
		}

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

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

using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using dndbg.Engine;
using dnSpy.AvalonEdit;
using dnSpy.Contracts;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.ToolBars;
using dnSpy.Debugger.Breakpoints;
using dnSpy.Debugger.Memory;
using dnSpy.Shared.UI.Images;
using dnSpy.Shared.UI.Menus;
using dnSpy.Shared.UI.ToolBars;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.TextView;

namespace dnSpy.Debugger {
	abstract class DebugToolBarButtonCommand : ToolBarButtonCommand {
		internal static void OnLoaded() {
			DebugManager.Instance.OnProcessStateChanged += DebugManager_OnProcessStateChanged;
		}

		static void DebugManager_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			bool newIsDebugging = DebugManager.Instance.IsDebugging;
			if (newIsDebugging != prevIsDebugging) {
				prevIsDebugging = newIsDebugging;
				MainWindow.Instance.UpdateToolbar();
			}
		}
		static bool? prevIsDebugging = null;

		protected DebugToolBarButtonCommand(ICommand command)
			: base(command) {
		}

		public override bool IsVisible(IToolBarItemContext context) {
			return DebugManager.Instance.IsDebugging;
		}
	}

	[ExportToolBarButton(Icon = "StartDebugging", ToolTip = "Debug an Assembly (F5)", Header = "Start", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG, Order = 0)]
	sealed class DebugAssemblyToolbarCommand : DebugToolBarButtonCommand {
		public DebugAssemblyToolbarCommand()
			: base(DebugRoutedCommands.DebugAssembly) {
		}

		public override bool IsVisible(IToolBarItemContext context) {
			return !DebugManager.Instance.IsDebugging;
		}
	}

	[ExportToolBarButton(Icon = "ContinueDebugging", ToolTip = "Continue (F5)", Header = "Continue", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_CONTINUE, Order = 0)]
	sealed class ContinueDebugToolBarButtonCommand : DebugToolBarButtonCommand {
		public ContinueDebugToolBarButtonCommand()
			: base(DebugRoutedCommands.Continue) {
		}
	}

	[ExportToolBarButton(Icon = "Break", ToolTip = "Break (Ctrl+Break)", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_CONTINUE, Order = 10)]
	sealed class BreakDebugToolBarButtonCommand : DebugToolBarButtonCommand {
		public BreakDebugToolBarButtonCommand()
			: base(DebugRoutedCommands.Break) {
		}
	}

	[ExportToolBarButton(Icon = "StopProcess", ToolTip = "Stop Debugging (Shift+F5)", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_CONTINUE, Order = 20)]
	sealed class StopDebugToolBarButtonCommand : DebugToolBarButtonCommand {
		public StopDebugToolBarButtonCommand()
			: base(DebugRoutedCommands.Stop) {
		}
	}

	[ExportToolBarButton(Icon = "RestartProcess", ToolTip = "Restart (Ctrl+Shift+F5)", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_CONTINUE, Order = 30)]
	sealed class RestartDebugToolBarButtonCommand : DebugToolBarButtonCommand {
		public RestartDebugToolBarButtonCommand()
			: base(DebugRoutedCommands.Restart) {
		}
	}

	[ExportToolBarButton(Icon = "CurrentLineToolBar", ToolTip = "Show Next Statement (Alt+Num *)", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_STEP, Order = 0)]
	sealed class ShowNextStatementDebugToolBarButtonCommand : DebugToolBarButtonCommand {
		public ShowNextStatementDebugToolBarButtonCommand()
			: base(DebugRoutedCommands.ShowNextStatement) {
		}
	}

	[ExportToolBarButton(Icon = "StepInto", ToolTip = "Step Into (F11)", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_STEP, Order = 10)]
	sealed class StepIntoDebugToolBarButtonCommand : DebugToolBarButtonCommand {
		public StepIntoDebugToolBarButtonCommand()
			: base(DebugRoutedCommands.StepInto) {
		}
	}

	[ExportToolBarButton(Icon = "StepOver", ToolTip = "Step Over (F10)", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_STEP, Order = 20)]
	sealed class StepOverDebugToolBarButtonCommand : DebugToolBarButtonCommand {
		public StepOverDebugToolBarButtonCommand()
			: base(DebugRoutedCommands.StepOver) {
		}
	}

	[ExportToolBarButton(Icon = "StepOut", ToolTip = "Step Out (Shift+F11)", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_STEP, Order = 30)]
	sealed class StepOutDebugToolBarButtonCommand : DebugToolBarButtonCommand {
		public StepOutDebugToolBarButtonCommand()
			: base(DebugRoutedCommands.StepOut) {
		}
	}

	abstract class DebugCtxMenuCommand : MenuItemBase {
		readonly ICommand realCmd;

		protected DebugCtxMenuCommand(ICommand realCmd) {
			this.realCmd = realCmd;
		}

		public override void Execute(IMenuItemContext context) {
			realCmd.Execute(context);
		}

		public override bool IsVisible(IMenuItemContext context) {
			return IsValidElement(context.CreatorObject) && realCmd.CanExecute(context);
		}

		public override bool IsEnabled(IMenuItemContext context) {
			return true;
		}

		protected virtual bool IsValidElement(GuidObject element) {
			return element.Guid == new Guid(MenuConstants.GUIDOBJ_DECOMPILED_CODE_GUID);
		}
	}

	[ExportMenuItem(Header = "_Debug Assembly", Icon = "StartDebugging", InputGestureText = "F5", Group = MenuConstants.GROUP_CTX_CODE_DEBUG, Order = 0)]
	sealed class DebugAssemblyDebugCtxMenuCommand : DebugCtxMenuCommand {
		public DebugAssemblyDebugCtxMenuCommand()
			: base(DebugRoutedCommands.DebugCurrentAssembly) {
		}

		public override string GetHeader(IMenuItemContext context) {
			var asm = DebugManager.Instance.GetCurrentExecutableAssembly(context);
			if (asm == null)
				return null;
			return string.Format("_Debug {0}", UIUtils.EscapeMenuItemHeader(asm.ShortName));
		}

		protected override bool IsValidElement(GuidObject element) {
			return element.Guid == new Guid(MenuConstants.GUIDOBJ_DECOMPILED_CODE_GUID) ||
				element.Guid == new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID);
		}
	}

	[ExportMenuItem(Icon = "BreakpointMenu", InputGestureText = "F9", Group = MenuConstants.GROUP_CTX_CODE_DEBUG, Order = 10)]
	sealed class ToggleBreakpointDebugCtxMenuCommand : DebugCtxMenuCommand {
		public ToggleBreakpointDebugCtxMenuCommand()
			: base(DebugRoutedCommands.ToggleBreakpoint) {
		}

		public override string GetHeader(IMenuItemContext context) {
			int count;
			bool? enabled = BreakpointManager.Instance.GetAddRemoveBreakpointsInfo(out count);

			if (enabled == null)
				return "_Add Breakpoint";
			if (enabled.Value)
				return count == 1 ? "D_elete Breakpoint" : "D_elete Breakpoints";
			return count == 1 ? "_Enable Breakpoint" : "_Enable Breakpoints";
		}
	}

	[ExportMenuItem(InputGestureText = "Ctrl+F9", Group = MenuConstants.GROUP_CTX_CODE_DEBUG, Order = 20)]
	sealed class EnableDisableBreakpointDebugCtxMenuCommand : DebugCtxMenuCommand {
		public EnableDisableBreakpointDebugCtxMenuCommand()
			: base(DebugRoutedCommands.DisableBreakpoint) {
		}

		public override bool IsEnabled(IMenuItemContext context) {
			int count;
			bool enabled = BreakpointManager.Instance.GetEnableDisableBreakpointsInfo(out count);
			return IsMenuItemEnabledInternal(count);
		}

		public override string GetHeader(IMenuItemContext context) {
			int count;
			bool enabled = BreakpointManager.Instance.GetEnableDisableBreakpointsInfo(out count);
			return GetHeaderInternal(enabled, count);
		}

		public override string GetIcon(IMenuItemContext context) {
			return GetIconInternal();
		}

		internal static bool IsMenuItemEnabledInternal(int count) {
			return count > 0;
		}

		internal static string GetHeaderInternal(bool enabled, int count) {
			if (enabled)
				return count <= 1 ? "_Disable Breakpoint" : "_Disable Breakpoints";
			return count <= 1 ? "Enab_le Breakpoint" : "Enab_le Breakpoints";
		}

		internal static string GetIconInternal() {
			return "DisableEnableBreakpoint";
		}
	}

	[ExportMenuItem(Icon = "CurrentLineToolBar", Header = "S_how Next Statement", InputGestureText = "Alt+Num *", Group = MenuConstants.GROUP_CTX_CODE_DEBUG, Order = 30)]
	sealed class ShowNextStatementDebugCtxMenuCommand : DebugCtxMenuCommand {
		public ShowNextStatementDebugCtxMenuCommand()
			: base(DebugRoutedCommands.ShowNextStatement) {
		}
	}

	[ExportMenuItem(Icon = "SetNextStatement", Header = "Set Ne_xt Statement", InputGestureText = "Ctrl+Shift+F10", Group = MenuConstants.GROUP_CTX_CODE_DEBUG, Order = 40)]
	sealed class SetNextStatementDebugCtxMenuCommand : DebugCtxMenuCommand {
		public SetNextStatementDebugCtxMenuCommand()
			: base(DebugRoutedCommands.SetNextStatement) {
		}
	}

	abstract class DebugMainMenuCommand : MenuItemCommand {
		readonly bool? mustBeDebugging;

		protected DebugMainMenuCommand(ICommand realCmd, bool? mustBeDebugging)
			: base(realCmd) {
			this.mustBeDebugging = mustBeDebugging;
		}

		public override bool IsVisible(IMenuItemContext context) {
			return mustBeDebugging == null || DebugManager.Instance.IsDebugging == mustBeDebugging;
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "Debug an Assembl_y...", Icon = "StartDebugging", InputGestureText = "F5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_START, Order = 0)]
	sealed class DebugAssemblyDebugMainMenuCommand : DebugMainMenuCommand {
		public DebugAssemblyDebugMainMenuCommand()
			: base(DebugRoutedCommands.DebugAssembly, false) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "Start Wit_hout Debugging", Icon = "StartWithoutDebugging", InputGestureText = "Ctrl+F5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_START, Order = 10)]
	sealed class StartWithoutDegbuggingDebugMainMenuCommand : DebugMainMenuCommand {
		public StartWithoutDegbuggingDebugMainMenuCommand()
			: base(DebugRoutedCommands.StartWithoutDebugging, false) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "Debug a CoreCLR Assembl_y...", Icon = "StartDebugging", Group = MenuConstants.GROUP_APP_MENU_DEBUG_START, Order = 20)]
	sealed class DebugCoreCLRAssemblyDebugMainMenuCommand : DebugMainMenuCommand {
		public DebugCoreCLRAssemblyDebugMainMenuCommand()
			: base(DebugRoutedCommands.DebugCoreCLRAssembly, false) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "Attach to _Process...", Icon = "Process", InputGestureText = "Ctrl+Alt+P", Group = MenuConstants.GROUP_APP_MENU_DEBUG_START, Order = 30)]
	sealed class AttachDebugMainMenuCommand : DebugMainMenuCommand {
		public AttachDebugMainMenuCommand()
			: base(DebugRoutedCommands.Attach, false) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "_Continue", Icon = "ContinueDebugging", InputGestureText = "F5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 0)]
	sealed class ContinueDebugMainMenuCommand : DebugMainMenuCommand {
		public ContinueDebugMainMenuCommand()
			: base(DebugRoutedCommands.Continue, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "Brea_k", Icon = "Break", InputGestureText = "Ctrl+Break", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 10)]
	sealed class BreakDebugMainMenuCommand : DebugMainMenuCommand {
		public BreakDebugMainMenuCommand()
			: base(DebugRoutedCommands.Break, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "Stop D_ebugging", Icon = "StopProcess", InputGestureText = "Shift+F5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 20)]
	sealed class StopDebugMainMenuCommand : DebugMainMenuCommand {
		public StopDebugMainMenuCommand()
			: base(DebugRoutedCommands.Stop, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "_Detach", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 30)]
	sealed class DetachDebugMainMenuCommand : DebugMainMenuCommand {
		public DetachDebugMainMenuCommand()
			: base(DebugRoutedCommands.Detach, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "_Restart", Icon = "RestartProcess", InputGestureText = "Ctrl+Shift+F5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 40)]
	sealed class RestartDebugMainMenuCommand : DebugMainMenuCommand {
		public RestartDebugMainMenuCommand()
			: base(DebugRoutedCommands.Restart, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "Step _Into", Icon = "StepInto", InputGestureText = "F11", Group = MenuConstants.GROUP_APP_MENU_DEBUG_STEP, Order = 0)]
	sealed class StepIntoDebugMainMenuCommand : DebugMainMenuCommand {
		public StepIntoDebugMainMenuCommand()
			: base(DebugRoutedCommands.StepInto, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "Step _Over", Icon = "StepOver", InputGestureText = "F10", Group = MenuConstants.GROUP_APP_MENU_DEBUG_STEP, Order = 10)]
	sealed class StepOverDebugMainMenuCommand : DebugMainMenuCommand {
		public StepOverDebugMainMenuCommand()
			: base(DebugRoutedCommands.StepOver, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "Step Ou_t", Icon = "StepOut", InputGestureText = "Shift+F11", Group = MenuConstants.GROUP_APP_MENU_DEBUG_STEP, Order = 20)]
	sealed class StepOutDebugMainMenuCommand : DebugMainMenuCommand {
		public StepOutDebugMainMenuCommand()
			: base(DebugRoutedCommands.StepOut, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "To_ggle Breakpoint", InputGestureText = "F9", Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 0)]
	sealed class ToggleBreakpointDebugMainMenuCommand : DebugMainMenuCommand {
		public ToggleBreakpointDebugMainMenuCommand()
			: base(DebugRoutedCommands.ToggleBreakpoint, null) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "Delete _All Breakpoints", Icon = "DeleteAllBreakpoints", InputGestureText = "Ctrl+Shift+F9", Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 10)]
	sealed class DeleteAllBreakpointsDebugMainMenuCommand : DebugMainMenuCommand {
		public DeleteAllBreakpointsDebugMainMenuCommand()
			: base(DebugRoutedCommands.DeleteAllBreakpoints, null) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "Enable All Breakpoi_nts", Icon = "EnableAllBreakpoints", Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 20)]
	sealed class EnableAllBreakpointsDebugMainMenuCommand : DebugMainMenuCommand {
		public EnableAllBreakpointsDebugMainMenuCommand()
			: base(DebugRoutedCommands.EnableAllBreakpoints, null) {
		}

		public override bool IsVisible(IMenuItemContext context) {
			return DebugRoutedCommands.EnableAllBreakpoints.CanExecute(null, MainWindow.Instance);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "Disable All Breakpoi_nts", Icon = "DisableAllBreakpoints", Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 30)]
	sealed class DisableAllBreakpointsDebugMainMenuCommand : DebugMainMenuCommand {
		public DisableAllBreakpointsDebugMainMenuCommand()
			: base(DebugRoutedCommands.DisableAllBreakpoints, null) {
		}

		public override bool IsVisible(IMenuItemContext context) {
			return DebugRoutedCommands.DisableAllBreakpoints.CanExecute(null, MainWindow.Instance);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "_Breakpoints", Icon = "BreakpointsWindow", InputGestureText = "Ctrl+Alt+B", Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 40)]
	sealed class BreakpointsWindowCommand : DebugMainMenuCommand {
		public BreakpointsWindowCommand()
			: base(DebugRoutedCommands.ShowBreakpoints, null) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "_Locals", Icon = "LocalsWindow", InputGestureText = "Alt+4", Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 0)]
	sealed class LocalsWindowCommand : DebugMainMenuCommand {
		public LocalsWindowCommand()
			: base(DebugRoutedCommands.ShowLocals, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "Call _Stack", Icon = "CallStackWindow", InputGestureText = "Ctrl+Alt+C", Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 10)]
	sealed class CallStackWindowCommand : DebugMainMenuCommand {
		public CallStackWindowCommand()
			: base(DebugRoutedCommands.ShowCallStack, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "T_hreads", Icon = "Thread", InputGestureText = "Ctrl+Alt+H", Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 20)]
	sealed class ThreadsWindowCommand : DebugMainMenuCommand {
		public ThreadsWindowCommand()
			: base(DebugRoutedCommands.ShowThreads, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "Mod_ules", Icon = "ModulesWindow", InputGestureText = "Ctrl+Alt+U", Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 30)]
	sealed class ModulesWindowCommand : DebugMainMenuCommand {
		public ModulesWindowCommand()
			: base(DebugRoutedCommands.ShowModules, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "E_xception Settings", Icon = "ExceptionSettings", InputGestureText = "Ctrl+Alt+E", Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 40)]
	sealed class ExceptionSettingsWindowCommand : DebugMainMenuCommand {
		public ExceptionSettingsWindowCommand()
			: base(DebugRoutedCommands.ShowExceptions, null) {
		}
	}

	static class Constants {
		public const string SHOW_IN_MEMORY_WINDOW_GUID = "C9EF4AD5-21C6-4185-B5C7-7DCF2DFA7BCD";
		public const string GROUP_SHOW_IN_MEMORY_WINDOW = "0,6DE55384-1907-4D19-86F1-3C48A1846193";
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "_Memory", Icon = "MemoryWindow", Guid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 50)]
	sealed class MemoryWindowCommand : MenuItemBase {
		public override void Execute(IMenuItemContext context) {
		}

		public override bool IsVisible(IMenuItemContext context) {
			return DebugManager.Instance.IsDebugging;
		}
	}

	sealed class CommandToMenuItem : MenuItemBase {
		readonly ICommand cmd;

		public CommandToMenuItem(ICommand cmd) {
			this.cmd = cmd;
		}

		public override void Execute(IMenuItemContext context) {
			cmd.Execute(context);
		}

		public override bool IsVisible(IMenuItemContext context) {
			return cmd.CanExecute(context);
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = Constants.GROUP_SHOW_IN_MEMORY_WINDOW, Order = 0)]
	sealed class SubMenuMemoryWindowCommand : MenuItemBase, IMenuItemCreator {
		static SubMenuMemoryWindowCommand() {
			subCmds = new Tuple<IMenuItem, string, string>[DebugRoutedCommands.ShowMemoryCommands.Length];
			for (int i = 0; i < subCmds.Length; i++) {
				var inputGestureText = GetInputGestureText(i);
				var headerText = MemoryControlCreator.GetHeaderText(i);
				var cmd = new CommandToMenuItem(DebugRoutedCommands.ShowMemoryCommands[i]);
				subCmds[i] = Tuple.Create((IMenuItem)cmd, headerText, inputGestureText);
			}
		}

		static string GetInputGestureText(int i) {
			if (i == 0)
				return "Alt+6";
			if (1 <= i && i <= 9)
				return string.Format("Ctrl+Alt+{0}", (i + 1) % 10);
			return string.Empty;
		}

		static readonly Tuple<IMenuItem, string, string>[] subCmds;

		public override void Execute(IMenuItemContext context) {
		}

		public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
			for (int i = 0; i < subCmds.Length; i++) {
				var info = subCmds[i];
				var attr = new ExportMenuItemAttribute { Header = info.Item2, Icon = "MemoryWindow" };
				if (!string.IsNullOrEmpty(info.Item3))
					attr.InputGestureText = info.Item3;
				yield return new CreatedMenuItem(attr, info.Item1);
			}
		}
	}

	[ExportIconBarActionEntry(Icon = "BreakpointMenu", Category = "Debug")]
	sealed class BreakpointCommand : IIconBarActionEntry {
		public bool IsEnabled(DecompilerTextView textView) {
			return true;
		}

		public void Execute(DecompilerTextView textView, int line) {
			BreakpointManager.Instance.Toggle(textView, line);
		}
	}

	[ExportIconBarContextMenuEntry(Header = "D_elete Breakpoint", Icon = "BreakpointMenu", Category = "Debug", Order = 100)]
	sealed class DeleteBreakpointCommand : IIconBarContextMenuEntry {
		public bool IsVisible(IIconBarObject context) {
			return context is ILCodeBreakpoint;
		}

		public bool IsEnabled(IIconBarObject context) {
			return IsVisible(context);
		}

		public void Execute(IIconBarObject context) {
			var bpm = context as ILCodeBreakpoint;
			if (bpm != null)
				BreakpointManager.Instance.Remove(bpm);
		}
	}

	[ExportIconBarContextMenuEntry(InputGestureText = "Ctrl+F9", Category = "Debug", Order = 110)]
	sealed class EnableAndDisableBreakpointCommand : IIconBarContextMenuEntry2 {
		public bool IsVisible(IIconBarObject context) {
			return context is ILCodeBreakpoint;
		}

		public bool IsEnabled(IIconBarObject context) {
			return IsVisible(context);
		}

		public void Execute(IIconBarObject context) {
			var bpm = context as ILCodeBreakpoint;
			if (bpm != null)
				bpm.IsEnabled = !bpm.IsEnabled;
		}

		public void Initialize(IIconBarObject context, MenuItem menuItem) {
			var bpm = context as ILCodeBreakpoint;
			if (bpm != null) {
				menuItem.IsEnabled = EnableDisableBreakpointDebugCtxMenuCommand.IsMenuItemEnabledInternal(1);
				menuItem.Header = EnableDisableBreakpointDebugCtxMenuCommand.GetHeaderInternal(bpm.IsEnabled, 1);
				DnSpy.App.ImageManager.Add16x16Image(menuItem, GetType().Assembly, EnableDisableBreakpointDebugCtxMenuCommand.GetIconInternal(), true);
			}
		}
	}
}

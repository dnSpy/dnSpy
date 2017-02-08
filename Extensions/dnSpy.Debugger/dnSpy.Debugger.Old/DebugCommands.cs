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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Input;
using dndbg.Engine;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.ToolBars;
using dnSpy.Contracts.Utilities;
using dnSpy.Debugger.Breakpoints;
using dnSpy.Debugger.Memory;
using dnSpy.Debugger.Old.Properties;

namespace dnSpy.Debugger {
	[ExportAutoLoaded]
	sealed class RefreshToolBarCommand : IAutoLoaded {
		readonly ITheDebugger theDebugger;
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		RefreshToolBarCommand(ITheDebugger theDebugger, IAppWindow appWindow) {
			this.theDebugger = theDebugger;
			this.appWindow = appWindow;
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
		}

		void TheDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			bool newIsDebugging = theDebugger.IsDebugging;
			if (newIsDebugging != prevIsDebugging) {
				prevIsDebugging = newIsDebugging;
				appWindow.RefreshToolBar();
			}
		}
		bool? prevIsDebugging = null;
	}

	abstract class DebugToolBarButtonCommand : ToolBarButtonCommand {
		protected readonly Lazy<ITheDebugger> theDebugger;

		// Prevents ITheDebugger from being loaded since IsVisible will be called early. We're
		// never debugging when the program starts so there's no need to check IsDebugging.
		[ExportAutoLoaded]
		sealed class Loader : IAutoLoaded {
			[ImportingConstructor]
			Loader(IAppWindow appWindow) {
				initd = true;
			}
		}
		protected static bool initd;

		protected DebugToolBarButtonCommand(ICommand command, Lazy<ITheDebugger> theDebugger)
			: base(command) {
			this.theDebugger = theDebugger;
		}

		public override bool IsVisible(IToolBarItemContext context) => initd && theDebugger.Value.IsDebugging;
	}

	[ExportToolBarButton(Icon = DsImagesAttribute.Run, ToolTip = "res:ToolBarDebugAssemblyToolTip", Header = "res:ToolBarStartDebuggingButton", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG, Order = 0)]
	sealed class DebugAssemblyToolbarCommand : DebugToolBarButtonCommand {
		[ImportingConstructor]
		public DebugAssemblyToolbarCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.DebugAssembly, theDebugger) {
		}

		public override bool IsVisible(IToolBarItemContext context) => !initd || !theDebugger.Value.IsDebugging;
	}

	[ExportToolBarButton(Icon = DsImagesAttribute.Run, ToolTip = "res:ToolBarContinueDebuggingToolTip", Header = "res:ToolBarContinueDebuggingButton", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_CONTINUE, Order = 0)]
	sealed class ContinueDebugToolBarButtonCommand : DebugToolBarButtonCommand {
		[ImportingConstructor]
		public ContinueDebugToolBarButtonCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.Continue, theDebugger) {
		}
	}

	[ExportToolBarButton(Icon = DsImagesAttribute.Pause, ToolTip = "res:ToolBarBreakToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_CONTINUE, Order = 10)]
	sealed class BreakDebugToolBarButtonCommand : DebugToolBarButtonCommand {
		[ImportingConstructor]
		public BreakDebugToolBarButtonCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.Break, theDebugger) {
		}
	}

	[ExportToolBarButton(Icon = DsImagesAttribute.Stop, ToolTip = "res:ToolBarStopDebuggingToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_CONTINUE, Order = 20)]
	sealed class StopDebugToolBarButtonCommand : DebugToolBarButtonCommand {
		[ImportingConstructor]
		public StopDebugToolBarButtonCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.Stop, theDebugger) {
		}
	}

	[ExportToolBarButton(Icon = DsImagesAttribute.Restart, ToolTip = "res:ToolBarRestartToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_CONTINUE, Order = 30)]
	sealed class RestartDebugToolBarButtonCommand : DebugToolBarButtonCommand {
		[ImportingConstructor]
		public RestartDebugToolBarButtonCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.Restart, theDebugger) {
		}
	}

	[ExportToolBarButton(Icon = DsImagesAttribute.GoToNext, ToolTip = "res:ToolBarShowNextStatementToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_STEP, Order = 0)]
	sealed class ShowNextStatementDebugToolBarButtonCommand : DebugToolBarButtonCommand {
		[ImportingConstructor]
		public ShowNextStatementDebugToolBarButtonCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.ShowNextStatement, theDebugger) {
		}
	}

	[ExportToolBarButton(Icon = DsImagesAttribute.StepInto, ToolTip = "res:ToolBarStepIntoToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_STEP, Order = 10)]
	sealed class StepIntoDebugToolBarButtonCommand : DebugToolBarButtonCommand {
		[ImportingConstructor]
		public StepIntoDebugToolBarButtonCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.StepInto, theDebugger) {
		}
	}

	[ExportToolBarButton(Icon = DsImagesAttribute.StepOver, ToolTip = "res:ToolBarStepOverToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_STEP, Order = 20)]
	sealed class StepOverDebugToolBarButtonCommand : DebugToolBarButtonCommand {
		[ImportingConstructor]
		public StepOverDebugToolBarButtonCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.StepOver, theDebugger) {
		}
	}

	[ExportToolBarButton(Icon = DsImagesAttribute.StepOut, ToolTip = "res:ToolBarStepOutToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_STEP, Order = 30)]
	sealed class StepOutDebugToolBarButtonCommand : DebugToolBarButtonCommand {
		[ImportingConstructor]
		public StepOutDebugToolBarButtonCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.StepOut, theDebugger) {
		}
	}

	abstract class DebugCtxMenuCommand : MenuItemBase {
		readonly ICommand realCmd;

		protected DebugCtxMenuCommand(ICommand realCmd) {
			this.realCmd = realCmd;
		}

		public override void Execute(IMenuItemContext context) => realCmd.Execute(context);
		public override bool IsVisible(IMenuItemContext context) => IsValidElement(context.CreatorObject) && realCmd.CanExecute(context);
		public override bool IsEnabled(IMenuItemContext context) => true;
		protected virtual bool IsValidElement(GuidObject element) => element.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID);
	}

	[ExportMenuItem(Header = "res:DebugAssemblyCommand", Icon = DsImagesAttribute.Run, InputGestureText = "res:ShortCutKeyF5", Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUG, Order = 0)]
	sealed class DebugAssemblyDebugCtxMenuCommand : DebugCtxMenuCommand {
		readonly Lazy<IDebugService> debugService;

		[ImportingConstructor]
		public DebugAssemblyDebugCtxMenuCommand(Lazy<IDebugService> debugService)
			: base(DebugRoutedCommands.DebugCurrentAssembly) {
			this.debugService = debugService;
		}

		public override string GetHeader(IMenuItemContext context) {
			var asm = debugService.Value.GetCurrentExecutableAssembly(context);
			if (asm == null)
				return null;
			return string.Format(dnSpy_Debugger_Resources.DebugProgramX, UIUtilities.EscapeMenuItemHeader(asm.GetShortName()));
		}

		protected override bool IsValidElement(GuidObject element) =>
			element.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID) ||
			element.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID);
	}

	[ExportMenuItem(Icon = DsImagesAttribute.CheckDot, InputGestureText = "res:ShortCutKeyF9", Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUG, Order = 10)]
	sealed class ToggleBreakpointDebugCtxMenuCommand : DebugCtxMenuCommand {
		readonly Lazy<IBreakpointService> breakpointService;

		[ImportingConstructor]
		public ToggleBreakpointDebugCtxMenuCommand(Lazy<IBreakpointService> breakpointService)
			: base(DebugRoutedCommands.ToggleBreakpoint) {
			this.breakpointService = breakpointService;
		}

		public override string GetHeader(IMenuItemContext context) {
			int count;
			bool? enabled = breakpointService.Value.GetAddRemoveBreakpointsInfo(out count);

			if (enabled == null)
				return dnSpy_Debugger_Resources.AddBreakpointCommand;
			if (enabled.Value)
				return count == 1 ? dnSpy_Debugger_Resources.DeleteBreakpointCommand : dnSpy_Debugger_Resources.DeleteBreakpointsCommand;
			return count == 1 ? dnSpy_Debugger_Resources.EnableBreakpointCommand : dnSpy_Debugger_Resources.EnableBreakpointsCommand;
		}
	}

	[ExportMenuItem(InputGestureText = "res:ShortCutKeyCtrlF9", Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUG, Order = 20)]
	sealed class EnableDisableBreakpointDebugCtxMenuCommand : DebugCtxMenuCommand {
		readonly Lazy<IBreakpointService> breakpointService;

		[ImportingConstructor]
		public EnableDisableBreakpointDebugCtxMenuCommand(Lazy<IBreakpointService> breakpointService)
			: base(DebugRoutedCommands.DisableBreakpoint) {
			this.breakpointService = breakpointService;
		}

		public override bool IsEnabled(IMenuItemContext context) {
			int count;
			bool enabled = breakpointService.Value.GetEnableDisableBreakpointsInfo(out count);
			return IsMenuItemEnabledInternal(count);
		}

		public override string GetHeader(IMenuItemContext context) {
			int count;
			bool enabled = breakpointService.Value.GetEnableDisableBreakpointsInfo(out count);
			return GetHeaderInternal(enabled, count);
		}

		public override ImageReference? GetIcon(IMenuItemContext context) => GetIconInternal();
		internal static bool IsMenuItemEnabledInternal(int count) => count > 0;

		internal static string GetHeaderInternal(bool enabled, int count) {
			if (enabled)
				return count <= 1 ? dnSpy_Debugger_Resources.DisableBreakpointCommand2 : dnSpy_Debugger_Resources.DisableBreakpointsCommand2;
			return count <= 1 ? dnSpy_Debugger_Resources.EnableBreakpointCommand2 : dnSpy_Debugger_Resources.EnableBreakpointsCommand2;
		}

		internal static ImageReference? GetIconInternal() => DsImages.ToggleAllBreakpoints;
	}

	[ExportMenuItem(Icon = DsImagesAttribute.GoToNext, Header = "res:ShowNextStatementCommand", InputGestureText = "res:ShortCutAltAsterisk", Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUG, Order = 30)]
	sealed class ShowNextStatementDebugCtxMenuCommand : DebugCtxMenuCommand {
		public ShowNextStatementDebugCtxMenuCommand()
			: base(DebugRoutedCommands.ShowNextStatement) {
		}
	}

	[ExportMenuItem(Icon = DsImagesAttribute.GoToNextInList, Header = "res:SetNextStatementCommand", InputGestureText = "res:ShortCutKeyCtrlShiftF10", Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUG, Order = 40)]
	sealed class SetNextStatementDebugCtxMenuCommand : DebugCtxMenuCommand {
		public SetNextStatementDebugCtxMenuCommand()
			: base(DebugRoutedCommands.SetNextStatement) {
		}
	}

	abstract class DebugMainMenuCommand : MenuItemCommand {
		readonly Lazy<ITheDebugger> theDebugger;
		readonly bool? mustBeDebugging;

		protected DebugMainMenuCommand(ICommand realCmd, Lazy<ITheDebugger> theDebugger, bool? mustBeDebugging)
			: base(realCmd) {
			this.theDebugger = theDebugger;
			this.mustBeDebugging = mustBeDebugging;
		}

		public override bool IsVisible(IMenuItemContext context) => mustBeDebugging == null || theDebugger.Value.IsDebugging == mustBeDebugging;
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:DebugAssemblyCommand2", Icon = DsImagesAttribute.Run, InputGestureText = "res:ShortCutKeyF5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_START, Order = 0)]
	sealed class DebugAssemblyDebugMainMenuCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public DebugAssemblyDebugMainMenuCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.DebugAssembly, theDebugger, false) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StartWithoutDebuggingCommand", Icon = DsImagesAttribute.RunOutline, InputGestureText = "res:ShortCutKeyCtrlF5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_START, Order = 10)]
	sealed class StartWithoutDegbuggingDebugMainMenuCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public StartWithoutDegbuggingDebugMainMenuCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.StartWithoutDebugging, theDebugger, false) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:DebugCoreCLRAssemblyCommand", Icon = DsImagesAttribute.Run, Group = MenuConstants.GROUP_APP_MENU_DEBUG_START, Order = 20)]
	sealed class DebugCoreCLRAssemblyDebugMainMenuCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public DebugCoreCLRAssemblyDebugMainMenuCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.DebugCoreCLRAssembly, theDebugger, false) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:AttachToProcessCommand", Icon = DsImagesAttribute.Process, InputGestureText = "res:ShortCutKeyCtrlAltP", Group = MenuConstants.GROUP_APP_MENU_DEBUG_START, Order = 30)]
	sealed class AttachDebugMainMenuCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public AttachDebugMainMenuCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.Attach, theDebugger, false) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:ContinueDebuggingCommand", Icon = DsImagesAttribute.Run, InputGestureText = "res:ShortCutKeyF5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 0)]
	sealed class ContinueDebugMainMenuCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public ContinueDebugMainMenuCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.Continue, theDebugger, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:BreakCommand", Icon = DsImagesAttribute.Pause, InputGestureText = "res:ShortCutKeyCtrlBreak", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 10)]
	sealed class BreakDebugMainMenuCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public BreakDebugMainMenuCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.Break, theDebugger, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StopDebuggingCommand", Icon = DsImagesAttribute.Stop, InputGestureText = "res:ShortCutKeyShiftF5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 20)]
	sealed class StopDebugMainMenuCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public StopDebugMainMenuCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.Stop, theDebugger, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:DetachCommand", Icon = DsImagesAttribute.Cancel, Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 30)]
	sealed class DetachDebugMainMenuCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public DetachDebugMainMenuCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.Detach, theDebugger, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:RestartCommand", Icon = DsImagesAttribute.Restart, InputGestureText = "res:ShortCutKeyCtrlShiftF5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 40)]
	sealed class RestartDebugMainMenuCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public RestartDebugMainMenuCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.Restart, theDebugger, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StepIntoCommand", Icon = DsImagesAttribute.StepInto, InputGestureText = "res:ShortCutKeyF11", Group = MenuConstants.GROUP_APP_MENU_DEBUG_STEP, Order = 0)]
	sealed class StepIntoDebugMainMenuCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public StepIntoDebugMainMenuCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.StepInto, theDebugger, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StepOverCommand", Icon = DsImagesAttribute.StepOver, InputGestureText = "res:ShortCutKeyF10", Group = MenuConstants.GROUP_APP_MENU_DEBUG_STEP, Order = 10)]
	sealed class StepOverDebugMainMenuCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public StepOverDebugMainMenuCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.StepOver, theDebugger, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StepOutCommand", Icon = DsImagesAttribute.StepOut, InputGestureText = "res:ShortCutKeyShiftF11", Group = MenuConstants.GROUP_APP_MENU_DEBUG_STEP, Order = 20)]
	sealed class StepOutDebugMainMenuCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public StepOutDebugMainMenuCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.StepOut, theDebugger, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:ToggleBreakpointCommand", InputGestureText = "res:ShortCutKeyF9", Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 0)]
	sealed class ToggleBreakpointDebugMainMenuCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public ToggleBreakpointDebugMainMenuCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.ToggleBreakpoint, theDebugger, null) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:DeleteAllBreakpointsCommand", Icon = DsImagesAttribute.ClearBreakpointGroup, InputGestureText = "res:ShortCutKeyCtrlShiftF9", Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 10)]
	sealed class DeleteAllBreakpointsDebugMainMenuCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public DeleteAllBreakpointsDebugMainMenuCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.DeleteAllBreakpoints, theDebugger, null) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:EnableAllBreakpointsCommand", Icon = DsImagesAttribute.EnableAllBreakpoints, Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 20)]
	sealed class EnableAllBreakpointsDebugMainMenuCommand : DebugMainMenuCommand {
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		public EnableAllBreakpointsDebugMainMenuCommand(Lazy<ITheDebugger> theDebugger, IAppWindow appWindow)
			: base(DebugRoutedCommands.EnableAllBreakpoints, theDebugger, null) {
			this.appWindow = appWindow;
		}

		public override bool IsVisible(IMenuItemContext context) => DebugRoutedCommands.EnableAllBreakpoints.CanExecute(null, appWindow.MainWindow);
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:DisableAllBreakpointsCommand", Icon = DsImagesAttribute.DisableAllBreakpoints, Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 30)]
	sealed class DisableAllBreakpointsDebugMainMenuCommand : DebugMainMenuCommand {
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		public DisableAllBreakpointsDebugMainMenuCommand(Lazy<ITheDebugger> theDebugger, IAppWindow appWindow)
			: base(DebugRoutedCommands.DisableAllBreakpoints, theDebugger, null) {
			this.appWindow = appWindow;
		}

		public override bool IsVisible(IMenuItemContext context) => DebugRoutedCommands.DisableAllBreakpoints.CanExecute(null, appWindow.MainWindow);
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:BreakpointsCommand", Icon = DsImagesAttribute.BreakpointsWindow, InputGestureText = "res:ShortCutKeyCtrlAltB", Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 40)]
	sealed class BreakpointsWindowCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public BreakpointsWindowCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.ShowBreakpoints, theDebugger, null) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:LocalsCommand", Icon = DsImagesAttribute.LocalsWindow, InputGestureText = "res:ShortCutKeyAlt4", Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 0)]
	sealed class LocalsWindowCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public LocalsWindowCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.ShowLocals, theDebugger, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:CallStackCommand", Icon = DsImagesAttribute.CallStackWindow, InputGestureText = "res:ShortCutKeyCtrlAltC", Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 10)]
	sealed class CallStackWindowCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public CallStackWindowCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.ShowCallStack, theDebugger, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:ThreadsCommand", Icon = DsImagesAttribute.Thread, InputGestureText = "res:ShortCutKeyCtrlAltH", Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 20)]
	sealed class ThreadsWindowCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public ThreadsWindowCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.ShowThreads, theDebugger, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:ModulesCommand", Icon = DsImagesAttribute.ModulesWindow, InputGestureText = "res:ShortCutKeyCtrlAltU", Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 30)]
	sealed class ModulesWindowCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public ModulesWindowCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.ShowModules, theDebugger, true) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:ExceptionSettingsCommand", Icon = DsImagesAttribute.ExceptionSettings, InputGestureText = "res:ShortCutKeyCtrlAltE", Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 40)]
	sealed class ExceptionSettingsWindowCommand : DebugMainMenuCommand {
		[ImportingConstructor]
		public ExceptionSettingsWindowCommand(Lazy<ITheDebugger> theDebugger)
			: base(DebugRoutedCommands.ShowExceptions, theDebugger, null) {
		}
	}

	static class Constants {
		public const string SHOW_IN_MEMORY_WINDOW_GUID = "C9EF4AD5-21C6-4185-B5C7-7DCF2DFA7BCD";
		public const string GROUP_SHOW_IN_MEMORY_WINDOW = "0,6DE55384-1907-4D19-86F1-3C48A1846193";
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:MemoryWindowCommand", Icon = DsImagesAttribute.MemoryWindow, Guid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = MenuConstants.GROUP_APP_MENU_DEBUG_SHOW, Order = 50)]
	sealed class MemoryWindowCommand : MenuItemBase {
		readonly Lazy<ITheDebugger> theDebugger;

		[ImportingConstructor]
		MemoryWindowCommand(Lazy<ITheDebugger> theDebugger) {
			this.theDebugger = theDebugger;
		}

		public override void Execute(IMenuItemContext context) { }
		public override bool IsVisible(IMenuItemContext context) => theDebugger.Value.IsDebugging;
	}

	sealed class CommandToMenuItem : MenuItemBase {
		readonly ICommand cmd;

		public CommandToMenuItem(ICommand cmd) {
			this.cmd = cmd;
		}

		public override void Execute(IMenuItemContext context) => cmd.Execute(context);
		public override bool IsVisible(IMenuItemContext context) => cmd.CanExecute(context);
	}

	[ExportMenuItem(OwnerGuid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = Constants.GROUP_SHOW_IN_MEMORY_WINDOW, Order = 0)]
	sealed class SubMenuMemoryWindowCommand : MenuItemBase, IMenuItemProvider {
		static SubMenuMemoryWindowCommand() {
			subCmds = new Tuple<IMenuItem, string, string>[DebugRoutedCommands.ShowMemoryCommands.Length];
			for (int i = 0; i < subCmds.Length; i++) {
				var inputGestureText = GetInputGestureText(i);
				var headerText = MemoryWindowsHelper.GetHeaderText(i);
				var cmd = new CommandToMenuItem(DebugRoutedCommands.ShowMemoryCommands[i]);
				subCmds[i] = Tuple.Create((IMenuItem)cmd, headerText, inputGestureText);
			}
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

	abstract class GlyphMarginCommand : MenuItemBase<ILCodeBreakpoint> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected sealed override ILCodeBreakpoint CreateContext(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_GLYPHMARGIN_GUID))
				return null;
			return context.Find<ILCodeBreakpoint>();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.GLYPHMARGIN_GUID, Header = "res:DeleteBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = MenuConstants.GROUP_GLYPHMARGIN_DEBUG_BPS, Order = 0)]
	sealed class DeleteBreakpointCommand : GlyphMarginCommand {
		readonly Lazy<IBreakpointService> breakpointService;

		[ImportingConstructor]
		DeleteBreakpointCommand(Lazy<IBreakpointService> breakpointService) {
			this.breakpointService = breakpointService;
		}

		public override void Execute(ILCodeBreakpoint context) => breakpointService.Value.Remove(context);
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.GLYPHMARGIN_GUID, InputGestureText = "res:ShortCutKeyCtrlF9", Group = MenuConstants.GROUP_GLYPHMARGIN_DEBUG_BPS, Order = 10)]
	sealed class EnableAndDisableBreakpointCommand : GlyphMarginCommand {
		public override void Execute(ILCodeBreakpoint context) => context.IsEnabled = !context.IsEnabled;
		public override bool IsEnabled(ILCodeBreakpoint context) => EnableDisableBreakpointDebugCtxMenuCommand.IsMenuItemEnabledInternal(1);
		public override string GetHeader(ILCodeBreakpoint context) => EnableDisableBreakpointDebugCtxMenuCommand.GetHeaderInternal(context.IsEnabled, 1);
		public override ImageReference? GetIcon(ILCodeBreakpoint context) => EnableDisableBreakpointDebugCtxMenuCommand.GetIconInternal();
	}
}

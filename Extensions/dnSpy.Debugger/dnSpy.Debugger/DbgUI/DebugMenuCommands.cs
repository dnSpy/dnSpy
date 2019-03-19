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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings.Dialog;
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

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StartDebugging2", Icon = DsImagesAttribute.Run, Group = MenuConstants.GROUP_APP_MENU_DEBUG_START, Order = 0)]
		sealed class DebugAssemblyDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public DebugAssemblyDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, false) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.DebugProgram(pauseAtEntryPoint: false);
			public override bool IsVisible(IMenuItemContext context) => debugger.Value.CanDebugProgram;
			public override string GetInputGestureText(IMenuItemContext context) =>
				debugger.Value.IsDebugging ? null : dnSpy_Debugger_Resources.ShortCutKeyF5;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StartWithoutDebuggingCommand", Icon = DsImagesAttribute.RunOutline, InputGestureText = "res:ShortCutKeyCtrlF5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_START, Order = 10)]
		sealed class StartWithoutDebuggingDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public StartWithoutDebuggingDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, null) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.StartWithoutDebugging();
			public override bool IsEnabled(IMenuItemContext context) => debugger.Value.CanStartWithoutDebugging;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:AttachToProcessCommand", Icon = DsImagesAttribute.Process, InputGestureText = "res:ShortCutKeyCtrlAltP", Group = MenuConstants.GROUP_APP_MENU_DEBUG_START, Order = 30)]
		sealed class AttachDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public AttachDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, false) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.AttachProgram();
			public override bool IsEnabled(IMenuItemContext context) => debugger.Value.CanAttachProgram;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:ContinueDebuggingCommand", Icon = DsImagesAttribute.Run, InputGestureText = "res:ShortCutKeyF5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 0)]
		sealed class ContinueDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public ContinueDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.Continue();
			public override bool IsEnabled(IMenuItemContext context) => debugger.Value.CanContinue;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:BreakCommand", Icon = DsImagesAttribute.Pause, InputGestureText = "res:ShortCutKeyCtrlAltBreak", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 10)]
		sealed class BreakDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public BreakDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.BreakAll();
			public override bool IsEnabled(IMenuItemContext context) => debugger.Value.CanBreakAll;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StopDebuggingCommand", Icon = DsImagesAttribute.Stop, InputGestureText = "res:ShortCutKeyShiftF5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 20)]
		sealed class StopDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public StopDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.StopDebugging();
			public override bool IsEnabled(IMenuItemContext context) => debugger.Value.CanStopDebugging;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:DetachAllCommand", Icon = DsImagesAttribute.Cancel, Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 30)]
		sealed class DetachAllDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public DetachAllDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.DetachAll();
			public override bool IsEnabled(IMenuItemContext context) => debugger.Value.CanDetachAll;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:TerminateAllCommand", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 40)]
		sealed class TerminateAllDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public TerminateAllDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.TerminateAll();
			public override bool IsEnabled(IMenuItemContext context) => debugger.Value.CanTerminateAll;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:RestartCommand", Icon = DsImagesAttribute.Restart, InputGestureText = "res:ShortCutKeyCtrlShiftF5", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 50)]
		sealed class RestartDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public RestartDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.Restart();
			public override bool IsEnabled(IMenuItemContext context) => debugger.Value.CanRestart;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:AttachToProcessCommand", Icon = DsImagesAttribute.Process, InputGestureText = "res:ShortCutKeyCtrlAltP", Group = MenuConstants.GROUP_APP_MENU_DEBUG_CONTINUE, Order = 70)]
		sealed class Attach2DebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public Attach2DebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.AttachProgram();
			public override bool IsEnabled(IMenuItemContext context) => debugger.Value.CanAttachProgram;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StepIntoCurrentProcessCommand", Icon = DsImagesAttribute.StepInto, InputGestureText = "res:ShortCutKeyCtrlAltF11", Group = MenuConstants.GROUP_APP_MENU_DEBUG_STEP_CURRENTPROCESS, Order = 0)]
		sealed class StepIntoCurrentProcessDebugMainMenuCommand : DebugMainMenuCommand {
			readonly Lazy<DebuggerSettings> debuggerSettings;

			[ImportingConstructor]
			public StepIntoCurrentProcessDebugMainMenuCommand(Lazy<Debugger> debugger, Lazy<DebuggerSettings> debuggerSettings)
				: base(debugger, true) => this.debuggerSettings = debuggerSettings;

			public override void Execute(IMenuItemContext context) => debugger.Value.StepIntoCurrentProcess();
			public override bool IsEnabled(IMenuItemContext context) => debugger.Value.CanStepIntoCurrentProcess;
			public override bool IsVisible(IMenuItemContext context) => !debuggerSettings.Value.BreakAllProcesses && debugger.Value.IsDebugging;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StepOverCurrentProcessCommand", Icon = DsImagesAttribute.StepOver, InputGestureText = "res:ShortCutKeyCtrlAltF10", Group = MenuConstants.GROUP_APP_MENU_DEBUG_STEP_CURRENTPROCESS, Order = 10)]
		sealed class StepOverCurrentProcessDebugMainMenuCommand : DebugMainMenuCommand {
			readonly Lazy<DebuggerSettings> debuggerSettings;

			[ImportingConstructor]
			public StepOverCurrentProcessDebugMainMenuCommand(Lazy<Debugger> debugger, Lazy<DebuggerSettings> debuggerSettings)
				: base(debugger, true) => this.debuggerSettings = debuggerSettings;

			public override void Execute(IMenuItemContext context) => debugger.Value.StepOverCurrentProcess();
			public override bool IsEnabled(IMenuItemContext context) => debugger.Value.CanStepOverCurrentProcess;
			public override bool IsVisible(IMenuItemContext context) => !debuggerSettings.Value.BreakAllProcesses && debugger.Value.IsDebugging;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StepOutCurrentProcessCommand", Icon = DsImagesAttribute.StepOut, InputGestureText = "res:ShortCutKeyCtrlShiftAltF11", Group = MenuConstants.GROUP_APP_MENU_DEBUG_STEP_CURRENTPROCESS, Order = 20)]
		sealed class StepOutCurrentProcessDebugMainMenuCommand : DebugMainMenuCommand {
			readonly Lazy<DebuggerSettings> debuggerSettings;

			[ImportingConstructor]
			public StepOutCurrentProcessDebugMainMenuCommand(Lazy<Debugger> debugger, Lazy<DebuggerSettings> debuggerSettings)
				: base(debugger, true) => this.debuggerSettings = debuggerSettings;

			public override void Execute(IMenuItemContext context) => debugger.Value.StepOutCurrentProcess();
			public override bool IsEnabled(IMenuItemContext context) => debugger.Value.CanStepOutCurrentProcess;
			public override bool IsVisible(IMenuItemContext context) => !debuggerSettings.Value.BreakAllProcesses && debugger.Value.IsDebugging;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StepIntoCommand", Icon = DsImagesAttribute.StepInto, InputGestureText = "res:ShortCutKeyF11", Group = MenuConstants.GROUP_APP_MENU_DEBUG_STEP, Order = 0)]
		sealed class StepIntoDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public StepIntoDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.StepInto();
			public override bool IsEnabled(IMenuItemContext context) => debugger.Value.CanStepInto;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StepOverCommand", Icon = DsImagesAttribute.StepOver, InputGestureText = "res:ShortCutKeyF10", Group = MenuConstants.GROUP_APP_MENU_DEBUG_STEP, Order = 10)]
		sealed class StepOverDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public StepOverDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.StepOver();
			public override bool IsEnabled(IMenuItemContext context) => debugger.Value.CanStepOver;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:StepOutCommand", Icon = DsImagesAttribute.StepOut, InputGestureText = "res:ShortCutKeyShiftF11", Group = MenuConstants.GROUP_APP_MENU_DEBUG_STEP, Order = 20)]
		sealed class StepOutDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public StepOutDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, true) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.StepOut();
			public override bool IsEnabled(IMenuItemContext context) => debugger.Value.CanStepOut;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:ToggleBreakpointCommand", InputGestureText = "res:ShortCutKeyF9", Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 0)]
		sealed class ToggleBreakpointDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public ToggleBreakpointDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, null) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.ToggleCreateBreakpoint();
			public override bool IsEnabled(IMenuItemContext context) => debugger.Value.CanToggleCreateBreakpoint;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:DeleteAllBreakpointsCommand", Icon = DsImagesAttribute.ClearBreakpointGroup, InputGestureText = "res:ShortCutKeyCtrlShiftF9", Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 10)]
		sealed class DeleteAllBreakpointsDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public DeleteAllBreakpointsDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, null) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.DeleteAllBreakpointsAskUser();
			public override bool IsVisible(IMenuItemContext context) => debugger.Value.CanDeleteAllBreakpoints;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:EnableAllBreakpointsCommand", Icon = DsImagesAttribute.EnableAllBreakpoints, Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 20)]
		sealed class EnableAllBreakpointsDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public EnableAllBreakpointsDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, null) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.EnableAllBreakpoints();
			public override bool IsVisible(IMenuItemContext context) => debugger.Value.CanEnableAllBreakpoints;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:DisableAllBreakpointsCommand", Icon = DsImagesAttribute.DisableAllBreakpoints, Group = MenuConstants.GROUP_APP_MENU_DEBUG_BREAKPOINTS, Order = 30)]
		sealed class DisableAllBreakpointsDebugMainMenuCommand : DebugMainMenuCommand {
			[ImportingConstructor]
			public DisableAllBreakpointsDebugMainMenuCommand(Lazy<Debugger> debugger)
				: base(debugger, null) {
			}

			public override void Execute(IMenuItemContext context) => debugger.Value.DisableAllBreakpoints();
			public override bool IsVisible(IMenuItemContext context) => debugger.Value.CanDisableAllBreakpoints;
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_DEBUG_GUID, Header = "res:Options", Icon = DsImagesAttribute.Settings, Group = MenuConstants.GROUP_APP_MENU_DEBUG_OPTIONS, Order = 0)]
		sealed class OptionsDebugMainMenuCommand : DebugMainMenuCommand {
			readonly IAppSettingsService appSettingsService;

			[ImportingConstructor]
			public OptionsDebugMainMenuCommand(Lazy<Debugger> debugger, IAppSettingsService appSettingsService)
				: base(debugger, null) => this.appSettingsService = appSettingsService;

			public override void Execute(IMenuItemContext context) => appSettingsService.Show(Settings.DebuggerAppSettingsPage.PageGuid);
		}
	}
}

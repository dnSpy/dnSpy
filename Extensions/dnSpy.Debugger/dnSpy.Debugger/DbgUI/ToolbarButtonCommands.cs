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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.ToolBars;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.DbgUI {
	static class ToolbarButtonCommands {
		abstract class DebugToolBarButton : ToolBarButtonBase {
			// Prevents the debugger from being loaded since IsVisible will be called early
			[ExportDbgManagerStartListener]
			sealed class DbgManagerStartListener : IDbgManagerStartListener {
				public void OnStart(DbgManager dbgManager) => initd = true;
			}
			protected static bool initd;

			protected readonly Lazy<Debugger> debugger;

			protected DebugToolBarButton(Lazy<Debugger> debugger) => this.debugger = debugger;

			public override bool IsVisible(IToolBarItemContext context) => initd && debugger.Value.IsDebugging;
		}

		[ExportToolBarButton(Icon = DsImagesAttribute.Run, Header = "res:ToolBarStartDebuggingButton", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG, Order = 0)]
		sealed class DebugAssemblyToolbarCommand : DebugToolBarButton {
			[ImportingConstructor]
			public DebugAssemblyToolbarCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override bool IsVisible(IToolBarItemContext context) => !initd || !debugger.Value.IsDebugging;
			public override void Execute(IToolBarItemContext context) => debugger.Value.DebugProgram();
			public override string GetToolTip(IToolBarItemContext context) => string.Format(dnSpy_Debugger_Resources.ToolBarDebugAssemblyToolTip, dnSpy_Debugger_Resources.ShortCutKeyF5);
		}

		[ExportToolBarButton(Icon = DsImagesAttribute.Run, Header = "res:ToolBarContinueDebuggingButton", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_CONTINUE, Order = 0)]
		sealed class ContinueDebugToolBarButtonCommand : DebugToolBarButton {
			[ImportingConstructor]
			public ContinueDebugToolBarButtonCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(IToolBarItemContext context) => debugger.Value.Continue();
			public override bool IsEnabled(IToolBarItemContext context) => debugger.Value.CanContinue;
			public override string GetToolTip(IToolBarItemContext context) => string.Format(dnSpy_Debugger_Resources.ToolBarContinueDebuggingToolTip, dnSpy_Debugger_Resources.ShortCutKeyF5);
		}

		[ExportToolBarButton(Icon = DsImagesAttribute.Pause, Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_CONTINUE, Order = 10)]
		sealed class BreakDebugToolBarButtonCommand : DebugToolBarButton {
			[ImportingConstructor]
			public BreakDebugToolBarButtonCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(IToolBarItemContext context) => debugger.Value.BreakAll();
			public override bool IsEnabled(IToolBarItemContext context) => debugger.Value.CanBreakAll;
			public override string GetToolTip(IToolBarItemContext context) => string.Format(dnSpy_Debugger_Resources.ToolBarBreakAllToolTip, dnSpy_Debugger_Resources.ShortCutKeyCtrlAltBreak);
		}

		[ExportToolBarButton(Icon = DsImagesAttribute.Stop, Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_CONTINUE, Order = 20)]
		sealed class StopDebugToolBarButtonCommand : DebugToolBarButton {
			[ImportingConstructor]
			public StopDebugToolBarButtonCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(IToolBarItemContext context) => debugger.Value.StopDebugging();
			public override bool IsEnabled(IToolBarItemContext context) => debugger.Value.CanStopDebugging;
			public override string GetToolTip(IToolBarItemContext context) => string.Format(dnSpy_Debugger_Resources.ToolBarStopDebuggingToolTip, dnSpy_Debugger_Resources.ShortCutKeyShiftF5);
		}

		[ExportToolBarButton(Icon = DsImagesAttribute.Restart, Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_CONTINUE, Order = 30)]
		sealed class RestartDebugToolBarButtonCommand : DebugToolBarButton {
			[ImportingConstructor]
			public RestartDebugToolBarButtonCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(IToolBarItemContext context) => debugger.Value.Restart();
			public override bool IsEnabled(IToolBarItemContext context) => debugger.Value.CanRestart;
			public override string GetToolTip(IToolBarItemContext context) => string.Format(dnSpy_Debugger_Resources.ToolBarRestartToolTip, dnSpy_Debugger_Resources.ShortCutKeyCtrlShiftF5);
		}

		[ExportToolBarButton(Icon = DsImagesAttribute.GoToNext, Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_STEP, Order = 0)]
		sealed class ShowNextStatementDebugToolBarButtonCommand : DebugToolBarButton {
			[ImportingConstructor]
			public ShowNextStatementDebugToolBarButtonCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(IToolBarItemContext context) => debugger.Value.ShowNextStatement();
			public override bool IsEnabled(IToolBarItemContext context) => debugger.Value.CanShowNextStatement;
			public override string GetToolTip(IToolBarItemContext context) => string.Format(dnSpy_Debugger_Resources.ToolBarShowNextStatementToolTip, dnSpy_Debugger_Resources.ShortCutAltAsterisk);
		}

		[ExportToolBarButton(Icon = DsImagesAttribute.StepInto, Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_STEP, Order = 10)]
		sealed class StepIntoDebugToolBarButtonCommand : DebugToolBarButton {
			[ImportingConstructor]
			public StepIntoDebugToolBarButtonCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(IToolBarItemContext context) => debugger.Value.StepInto();
			public override bool IsEnabled(IToolBarItemContext context) => debugger.Value.CanStepInto;
			public override string GetToolTip(IToolBarItemContext context) => string.Format(dnSpy_Debugger_Resources.ToolBarStepIntoToolTip, dnSpy_Debugger_Resources.ShortCutKeyF11);
		}

		[ExportToolBarButton(Icon = DsImagesAttribute.StepOver, Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_STEP, Order = 20)]
		sealed class StepOverDebugToolBarButtonCommand : DebugToolBarButton {
			[ImportingConstructor]
			public StepOverDebugToolBarButtonCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(IToolBarItemContext context) => debugger.Value.StepOver();
			public override bool IsEnabled(IToolBarItemContext context) => debugger.Value.CanStepOver;
			public override string GetToolTip(IToolBarItemContext context) => string.Format(dnSpy_Debugger_Resources.ToolBarStepOverToolTip, dnSpy_Debugger_Resources.ShortCutKeyF10);
		}

		[ExportToolBarButton(Icon = DsImagesAttribute.StepOut, Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_STEP, Order = 30)]
		sealed class StepOutDebugToolBarButtonCommand : DebugToolBarButton {
			[ImportingConstructor]
			public StepOutDebugToolBarButtonCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(IToolBarItemContext context) => debugger.Value.StepOut();
			public override bool IsEnabled(IToolBarItemContext context) => debugger.Value.CanStepOut;
			public override string GetToolTip(IToolBarItemContext context) => string.Format(dnSpy_Debugger_Resources.ToolBarStepOutToolTip, dnSpy_Debugger_Resources.ShortCutKeyShiftF11);
		}
	}
}

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
			public override string GetToolTip(IToolBarItemContext context) => string.Format(dnSpy_Debugger_Resources.ToolBarDebugAssemblyToolTip, "F5");
		}

		[ExportToolBarButton(Icon = DsImagesAttribute.Run, ToolTip = "res:ToolBarContinueDebuggingToolTip", Header = "res:ToolBarContinueDebuggingButton", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_CONTINUE, Order = 0)]
		sealed class ContinueDebugToolBarButtonCommand : DebugToolBarButton {
			[ImportingConstructor]
			public ContinueDebugToolBarButtonCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(IToolBarItemContext context) => debugger.Value.Continue();
		}

		[ExportToolBarButton(Icon = DsImagesAttribute.Pause, ToolTip = "res:ToolBarBreakToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_CONTINUE, Order = 10)]
		sealed class BreakDebugToolBarButtonCommand : DebugToolBarButton {
			[ImportingConstructor]
			public BreakDebugToolBarButtonCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(IToolBarItemContext context) => debugger.Value.BreakAll();
		}

		[ExportToolBarButton(Icon = DsImagesAttribute.Stop, ToolTip = "res:ToolBarStopDebuggingToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_CONTINUE, Order = 20)]
		sealed class StopDebugToolBarButtonCommand : DebugToolBarButton {
			[ImportingConstructor]
			public StopDebugToolBarButtonCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(IToolBarItemContext context) => debugger.Value.Stop();
		}

		[ExportToolBarButton(Icon = DsImagesAttribute.Restart, ToolTip = "res:ToolBarRestartToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_CONTINUE, Order = 30)]
		sealed class RestartDebugToolBarButtonCommand : DebugToolBarButton {
			[ImportingConstructor]
			public RestartDebugToolBarButtonCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(IToolBarItemContext context) => debugger.Value.Restart();
		}

		[ExportToolBarButton(Icon = DsImagesAttribute.GoToNext, ToolTip = "res:ToolBarShowNextStatementToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_STEP, Order = 0)]
		sealed class ShowNextStatementDebugToolBarButtonCommand : DebugToolBarButton {
			[ImportingConstructor]
			public ShowNextStatementDebugToolBarButtonCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(IToolBarItemContext context) => debugger.Value.ShowNextStatement();
		}

		[ExportToolBarButton(Icon = DsImagesAttribute.StepInto, ToolTip = "res:ToolBarStepIntoToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_STEP, Order = 10)]
		sealed class StepIntoDebugToolBarButtonCommand : DebugToolBarButton {
			[ImportingConstructor]
			public StepIntoDebugToolBarButtonCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(IToolBarItemContext context) => debugger.Value.StepInto();
		}

		[ExportToolBarButton(Icon = DsImagesAttribute.StepOver, ToolTip = "res:ToolBarStepOverToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_STEP, Order = 20)]
		sealed class StepOverDebugToolBarButtonCommand : DebugToolBarButton {
			[ImportingConstructor]
			public StepOverDebugToolBarButtonCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(IToolBarItemContext context) => debugger.Value.StepOver();
		}

		[ExportToolBarButton(Icon = DsImagesAttribute.StepOut, ToolTip = "res:ToolBarStepOutToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_DEBUG_STEP, Order = 30)]
		sealed class StepOutDebugToolBarButtonCommand : DebugToolBarButton {
			[ImportingConstructor]
			public StepOutDebugToolBarButtonCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(IToolBarItemContext context) => debugger.Value.StepOut();
		}
	}
}

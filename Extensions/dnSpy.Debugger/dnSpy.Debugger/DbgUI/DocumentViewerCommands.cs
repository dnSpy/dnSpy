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
using System.IO;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Utilities;
using dnSpy.Debugger.Breakpoints.Code.TextEditor;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.DbgUI {
	static class DocumentViewerCommands {
		abstract class DocumentViewerCommand : MenuItemBase<DocumentViewerCommand.Context> {
			protected sealed override object CachedContextKey => ContextKey;
			static readonly object ContextKey = new object();

			internal sealed class Context { }

			protected readonly Lazy<Debugger> debugger;

			protected DocumentViewerCommand(Lazy<Debugger> debugger) => this.debugger = debugger;

			protected sealed override Context CreateContext(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
					return null;
				return new Context();
			}
		}

		[ExportMenuItem(Header = "res:StartDebuggingCommand", Icon = DsImagesAttribute.Run, InputGestureText = "res:ShortCutKeyF5", Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUG, Order = 0)]
		sealed class DebugProgramDocumentViewerCommand : DocumentViewerCommand {
			[ImportingConstructor]
			public DebugProgramDocumentViewerCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override string GetHeader(Context context) {
				var filename = debugger.Value.GetCurrentExecutableFilename();
				if (!File.Exists(filename))
					return null;
				return string.Format(dnSpy_Debugger_Resources.DebugProgramX, UIUtilities.EscapeMenuItemHeader(Path.GetFileName(filename)));
			}

			public override void Execute(Context context) => debugger.Value.DebugProgram(pauseAtEntryPoint: false);
			public override bool IsVisible(Context context) => debugger.Value.CanDebugProgram;
		}

		[ExportMenuItem(Icon = DsImagesAttribute.CheckDot, InputGestureText = "res:ShortCutKeyF9", Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUG, Order = 10)]
		sealed class ToggleCreateBreakpointDocumentViewerCommand : DocumentViewerCommand {
			[ImportingConstructor]
			public ToggleCreateBreakpointDocumentViewerCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override string GetHeader(Context context) {
				switch (debugger.Value.GetToggleCreateBreakpointKind()) {
				case ToggleCreateBreakpointKind.Add:	return dnSpy_Debugger_Resources.AddBreakpointCommand;
				case ToggleCreateBreakpointKind.Delete:	return dnSpy_Debugger_Resources.DeleteBreakpointCommand;
				case ToggleCreateBreakpointKind.Enable:	return dnSpy_Debugger_Resources.EnableBreakpointCommand;
				case ToggleCreateBreakpointKind.None:
				default:
					return null;
				}
			}

			public override void Execute(Context context) => debugger.Value.ToggleCreateBreakpoint();
			public override bool IsVisible(Context context) => debugger.Value.CanToggleCreateBreakpoint;
		}

		[ExportMenuItem(InputGestureText = "res:ShortCutKeyCtrlF9", Icon = DsImagesAttribute.ToggleAllBreakpoints, Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUG, Order = 20)]
		sealed class ToggleEnableBreakpointDocumentViewerCommand : DocumentViewerCommand {
			[ImportingConstructor]
			public ToggleEnableBreakpointDocumentViewerCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override string GetHeader(Context context) {
				switch (debugger.Value.GetToggleEnableBreakpointKind()) {
				case ToggleEnableBreakpointKind.Enable:	return dnSpy_Debugger_Resources.EnableBreakpointCommand2;
				case ToggleEnableBreakpointKind.Disable:return dnSpy_Debugger_Resources.DisableBreakpointCommand2;
				case ToggleEnableBreakpointKind.None:
				default:
					return null;
				}
			}

			public override void Execute(Context context) => debugger.Value.ToggleEnableBreakpoint();
			public override bool IsVisible(Context context) => debugger.Value.CanToggleEnableBreakpoint;
		}

		[ExportMenuItem(Icon = DsImagesAttribute.GoToNext, Header = "res:ShowNextStatementCommand", InputGestureText = "res:ShortCutAltAsterisk", Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUG, Order = 30)]
		sealed class ShowNextStatementDocumentViewerCommand : DocumentViewerCommand {
			[ImportingConstructor]
			public ShowNextStatementDocumentViewerCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(Context context) => debugger.Value.ShowNextStatement();
			public override bool IsVisible(Context context) => debugger.Value.CanShowNextStatement;
		}

		[ExportMenuItem(Icon = DsImagesAttribute.GoToNextInList, Header = "res:SetNextStatementCommand", InputGestureText = "res:ShortCutKeyCtrlShiftF10", Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUG, Order = 40)]
		sealed class SetNextStatementDocumentViewerCommand : DocumentViewerCommand {
			[ImportingConstructor]
			public SetNextStatementDocumentViewerCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(Context context) => debugger.Value.SetNextStatement();
			public override bool IsVisible(Context context) => debugger.Value.CanSetNextStatement;
		}

		[ExportMenuItem(Icon = DsImagesAttribute.DisassemblyWindow, Header = "res:GoToDisassemblyCommand2", Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUG, Order = 50)]
		sealed class GoToDisassemblyDocumentViewerCommand : DocumentViewerCommand {
			[ImportingConstructor]
			public GoToDisassemblyDocumentViewerCommand(Lazy<Debugger> debugger)
				: base(debugger) {
			}

			public override void Execute(Context context) => debugger.Value.GoToDisassembly();
			public override bool IsVisible(Context context) => debugger.Value.CanGoToDisassembly;
		}
	}
}

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
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.ToolWindows.Processes {
	[ExportAutoLoaded]
	sealed class ProcessesCommandsLoader : IAutoLoaded {
		[ImportingConstructor]
		ProcessesCommandsLoader(IWpfCommandService wpfCommandService, Lazy<IProcessesContent> processesContent) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_PROCESSES_LISTVIEW);
			cmds.Add(new RelayCommand(a => processesContent.Value.VM.Copy(), a => processesContent.Value.VM.CanCopy), ModifierKeys.Control, Key.C);
			cmds.Add(new RelayCommand(a => processesContent.Value.VM.AttachToProcess(), a => processesContent.Value.VM.CanAttachToProcess), ModifierKeys.Control | ModifierKeys.Alt, Key.P);
		}
	}

	sealed class ProcessesCtxMenuContext {
		public IProcessesVM VM { get; }
		public ProcessesCtxMenuContext(IProcessesVM vm) => VM = vm;
	}

	abstract class ProcessesCtxMenuCommand : MenuItemBase<ProcessesCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected readonly Lazy<IProcessesContent> processesContent;

		protected ProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent) => this.processesContent = processesContent;

		protected sealed override ProcessesCtxMenuContext CreateContext(IMenuItemContext context) {
			if (!(context.CreatorObject.Object is ListView))
				return null;
			if (context.CreatorObject.Object != processesContent.Value.ListView)
				return null;
			return Create();
		}

		ProcessesCtxMenuContext Create() => new ProcessesCtxMenuContext(processesContent.Value.VM);
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DBG_PROCESSES_COPY, Order = 0)]
	sealed class CopyProcessesCtxMenuCommand : ProcessesCtxMenuCommand {
		[ImportingConstructor]
		CopyProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent)
			: base(processesContent) {
		}

		public override void Execute(ProcessesCtxMenuContext context) => context.VM.Copy();
		public override bool IsEnabled(ProcessesCtxMenuContext context) => context.VM.CanCopy;
	}

	[ExportMenuItem(Header = "res:SelectAllCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_PROCESSES_COPY, Order = 10)]
	sealed class SelectAllProcessesCtxMenuCommand : ProcessesCtxMenuCommand {
		[ImportingConstructor]
		SelectAllProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent)
			: base(processesContent) {
		}

		public override void Execute(ProcessesCtxMenuContext context) => context.VM.SelectAll();
		public override bool IsEnabled(ProcessesCtxMenuContext context) => context.VM.CanSelectAll;
	}

	[ExportMenuItem(Header = "res:DetachProcessCommand", Icon = DsImagesAttribute.Cancel, Group = MenuConstants.GROUP_CTX_DBG_PROCESSES_OPERATIONS, Order = 0)]
	sealed class DetachProcessProcessesCtxMenuCommand : ProcessesCtxMenuCommand {
		[ImportingConstructor]
		DetachProcessProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent)
			: base(processesContent) {
		}

		public override void Execute(ProcessesCtxMenuContext context) => context.VM.DetachProcess();
		public override bool IsEnabled(ProcessesCtxMenuContext context) => context.VM.CanDetachProcess;
	}

	[ExportMenuItem(Header = "res:TerminateProcessCommand", Icon = DsImagesAttribute.TerminateProcess, Group = MenuConstants.GROUP_CTX_DBG_PROCESSES_OPERATIONS, Order = 10)]
	sealed class TerminateProcessProcessesCtxMenuCommand : ProcessesCtxMenuCommand {
		[ImportingConstructor]
		TerminateProcessProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent)
			: base(processesContent) {
		}

		public override void Execute(ProcessesCtxMenuContext context) => context.VM.TerminateProcess();
		public override bool IsEnabled(ProcessesCtxMenuContext context) => context.VM.CanTerminateProcess;
	}

	[ExportMenuItem(Header = "res:DetachWhenDebuggingStoppedCommand", Group = MenuConstants.GROUP_CTX_DBG_PROCESSES_OPTIONS, Order = 0)]
	sealed class ToggleDetachWhenDebuggingStoppedProcessesCtxMenuCommand : ProcessesCtxMenuCommand {
		[ImportingConstructor]
		ToggleDetachWhenDebuggingStoppedProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent)
			: base(processesContent) {
		}

		public override void Execute(ProcessesCtxMenuContext context) => context.VM.ToggleDetachWhenDebuggingStopped();
		public override bool IsEnabled(ProcessesCtxMenuContext context) => context.VM.CanToggleDetachWhenDebuggingStopped;
		public override bool IsChecked(ProcessesCtxMenuContext context) => context.VM.DetachWhenDebuggingStopped;
	}

	[ExportMenuItem(Header = "res:HexDisplayCommand", Group = MenuConstants.GROUP_CTX_DBG_PROCESSES_OPTIONS, Order = 10)]
	sealed class UseHexadecimalProcessesCtxMenuCommand : ProcessesCtxMenuCommand {
		[ImportingConstructor]
		UseHexadecimalProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent)
			: base(processesContent) {
		}

		public override void Execute(ProcessesCtxMenuContext context) => context.VM.ToggleUseHexadecimal();
		public override bool IsEnabled(ProcessesCtxMenuContext context) => context.VM.CanToggleUseHexadecimal;
		public override bool IsChecked(ProcessesCtxMenuContext context) => context.VM.UseHexadecimal;
	}

	[ExportMenuItem(Header = "res:AttachToProcessCommand", InputGestureText = "res:ShortCutKeyCtrlAltP", Icon = DsImagesAttribute.Process, Group = MenuConstants.GROUP_CTX_DBG_PROCESSES_OPERATIONS2, Order = 0)]
	sealed class AttachToProcessProcessesCtxMenuCommand : ProcessesCtxMenuCommand {
		[ImportingConstructor]
		AttachToProcessProcessesCtxMenuCommand(Lazy<IProcessesContent> processesContent)
			: base(processesContent) {
		}

		public override void Execute(ProcessesCtxMenuContext context) => context.VM.AttachToProcess();
		public override bool IsEnabled(ProcessesCtxMenuContext context) => context.VM.CanAttachToProcess;
	}
}

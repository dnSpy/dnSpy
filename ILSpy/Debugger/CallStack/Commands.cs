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
using System.Linq;
using System.Text;
using System.Windows;
using dndbg.Engine;
using dnSpy.MVVM;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger.CallStack {
	sealed class CallStackCtxMenuContext {
		public readonly CallStackVM VM;
		public readonly ICallStackFrameVM[] SelectedItems;

		public CallStackCtxMenuContext(CallStackVM vm, ICallStackFrameVM[] selItems) {
			this.VM = vm;
			this.SelectedItems = selItems;
		}
	}

	sealed class CallStackCtxMenuCommandProxy : ContextMenuEntryCommandProxy {
		public CallStackCtxMenuCommandProxy(CallStackCtxMenuCommand cmd)
			: base(cmd) {
		}

		protected override ContextMenuEntryContext CreateContext() {
			return ContextMenuEntryContext.Create(CallStackPaneCreator.CallStackControlInstance.listView);
		}
	}

	abstract class CallStackCtxMenuCommand : ContextMenuEntryBase<CallStackCtxMenuContext> {
		protected override CallStackCtxMenuContext CreateContext(ContextMenuEntryContext context) {
			if (DebugManager.Instance.ProcessState != DebuggerProcessState.Stopped)
				return null;
			var ui = CallStackPaneCreator.CallStackControlInstance;
			if (context.Element != ui.listView)
				return null;
			var vm = ui.DataContext as CallStackVM;
			if (vm == null)
				return null;

			var elems = ui.listView.SelectedItems.OfType<ICallStackFrameVM>().ToArray();
			Array.Sort(elems, (a, b) => a.Index.CompareTo(b.Index));

			return new CallStackCtxMenuContext(vm, elems);
		}
	}

	[ExportContextMenuEntry(Header = "Cop_y", Order = 100, Category = "CopyCS", Icon = "Copy", InputGestureText = "Ctrl+C")]
	sealed class CopyCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		protected override void Execute(CallStackCtxMenuContext context) {
			var sb = new StringBuilder();
			foreach (var item in context.SelectedItems) {
				sb.Append(item.IsCurrentFrame ? '>' : ' ');
				sb.Append('\t');
				sb.Append(item.Name);
				sb.AppendLine();
			}
			if (sb.Length > 0)
				Clipboard.SetText(sb.ToString());
		}

		protected override bool IsEnabled(CallStackCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[ExportContextMenuEntry(Header = "Select _All", Order = 110, Category = "CopyCS", Icon = "Select", InputGestureText = "Ctrl+A")]
	sealed class SelectAllCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		protected override void Execute(CallStackCtxMenuContext context) {
			CallStackPaneCreator.CallStackControlInstance.listView.SelectAll();
		}
	}

	[ExportContextMenuEntry(Header = "_Switch To Frame", Order = 210, Category = "Frame")]
	sealed class SwitchToFrameCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		internal static CallStackFrameVM GetFrame(CallStackCtxMenuContext context) {
			if (context.SelectedItems.Length != 1)
				return null;
			return context.SelectedItems[0] as CallStackFrameVM;
		}

		protected override void Execute(CallStackCtxMenuContext context) {
			Execute(GetFrame(context));
		}

		internal static void Execute(CallStackFrameVM vm) {
			if (vm != null) {
				StackFrameManager.Instance.SelectedFrame = vm.Index;
				FrameUtils.GoTo(vm.Frame);
			}
		}

		protected override bool IsEnabled(CallStackCtxMenuContext context) {
			return GetFrame(context) != null;
		}
	}

	[ExportContextMenuEntry(Header = "_Go To Source Code", Order = 220, Category = "Frame", Icon = "GoToSourceCode")]
	sealed class GoToSourceCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		protected override void Execute(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			if (vm != null)
				FrameUtils.GoToIL(vm.Frame);
		}

		protected override bool IsEnabled(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			return vm != null && FrameUtils.CanGoToIL(vm.Frame);
		}
	}

	[ExportContextMenuEntry(Header = "Go To _Disassembly", Order = 230, Category = "Frame", Icon = "DisassemblyWindow")]
	sealed class GoToDisassemblyCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		protected override void Execute(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			if (vm != null)
				FrameUtils.GoToDisasm(vm.Frame);
		}

		protected override bool IsEnabled(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			return vm != null && FrameUtils.CanGoToDisasm(vm.Frame);
		}
	}

	[ExportContextMenuEntry(Header = "Ru_n To Cursor", Order = 240, Category = "Frame", Icon = "Cursor", InputGestureText = "Ctrl+F10")]
	sealed class RunToCursorCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		protected override void Execute(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			if (vm != null)
				DebugManager.Instance.RunTo(vm.Frame);
		}

		protected override bool IsEnabled(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			return vm != null && DebugManager.Instance.CanRunTo(vm.Frame);
		}
	}
}

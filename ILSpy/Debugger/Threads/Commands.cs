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
using System.Windows;
using System.Windows.Controls;
using dndbg.Engine;
using dnSpy.Debugger.CallStack;
using dnSpy.MVVM;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger.Threads {
	sealed class ThreadsCtxMenuContext {
		public readonly ThreadsVM VM;
		public readonly ThreadVM[] SelectedItems;

		public ThreadsCtxMenuContext(ThreadsVM vm, ThreadVM[] selItems) {
			this.VM = vm;
			this.SelectedItems = selItems;
		}
	}

	sealed class ThreadsCtxMenuCommandProxy : ContextMenuEntryCommandProxy {
		public ThreadsCtxMenuCommandProxy(ThreadsCtxMenuCommand cmd)
			: base(cmd) {
		}

		protected override ContextMenuEntryContext CreateContext() {
			return ContextMenuEntryContext.Create(ThreadsControlCreator.ThreadsControlInstance.listView);
		}
	}

	abstract class ThreadsCtxMenuCommand : ContextMenuEntryBase<ThreadsCtxMenuContext> {
		protected override ThreadsCtxMenuContext CreateContext(ContextMenuEntryContext context) {
			if (DebugManager.Instance.ProcessState != DebuggerProcessState.Stopped)
				return null;
			var ui = ThreadsControlCreator.ThreadsControlInstance;
			if (context.Element != ui.listView)
				return null;
			var vm = ui.DataContext as ThreadsVM;
			if (vm == null)
				return null;

			var elems = ui.listView.SelectedItems.OfType<ThreadVM>().ToArray();
			Array.Sort(elems, (a, b) => a.Thread.IncrementedId.CompareTo(b.Thread.IncrementedId));

			return new ThreadsCtxMenuContext(vm, elems);
		}
	}

	[ExportContextMenuEntry(Header = "Cop_y", Order = 100, Category = "CopyTH", Icon = "Copy", InputGestureText = "Ctrl+C")]
	sealed class CopyCallThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		protected override void Execute(ThreadsCtxMenuContext context) {
			var output = new PlainTextOutput();
			foreach (var vm in context.SelectedItems) {
				var printer = new ThreadPrinter(output, DebuggerSettings.Instance.UseHexadecimal);
				printer.WriteCurrent(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteId(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteManagedId(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteCategory(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteName(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteLocation(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WritePriority(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteAffinityMask(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteSuspended(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteProcess(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteAppDomain(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteUserState(vm);
				output.WriteLine();
			}
			var s = output.ToString();
			if (s.Length > 0)
				Clipboard.SetText(s);
		}

		protected override bool IsEnabled(ThreadsCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[ExportContextMenuEntry(Header = "Select _All", Order = 110, Category = "CopyTH", Icon = "Select", InputGestureText = "Ctrl+A")]
	sealed class SelectAllThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		protected override void Execute(ThreadsCtxMenuContext context) {
			ThreadsControlCreator.ThreadsControlInstance.listView.SelectAll();
		}

		protected override bool IsEnabled(ThreadsCtxMenuContext context) {
			return ThreadsControlCreator.ThreadsControlInstance.listView.Items.Count > 0;
		}
	}

	[ExportContextMenuEntry(Header = "_Hexadecimal Display", Order = 200, Category = "THMiscOptions")]
	sealed class HexadecimalDisplayThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		protected override void Execute(ThreadsCtxMenuContext context) {
			DebuggerSettings.Instance.UseHexadecimal = !DebuggerSettings.Instance.UseHexadecimal;
		}

		protected override void Initialize(ThreadsCtxMenuContext context, MenuItem menuItem) {
			menuItem.IsChecked = DebuggerSettings.Instance.UseHexadecimal;
		}
	}

	[ExportContextMenuEntry(Header = "_Switch To Thread", Order = 300, Category = "TH1", InputGestureText = "Enter")]
	sealed class SwitchToThreadThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		protected override void Execute(ThreadsCtxMenuContext context) {
			GoTo(context, false);
		}

		protected override bool IsEnabled(ThreadsCtxMenuContext context) {
			return CanGoToThread(context);
		}

		internal static bool CanGoToThread(ThreadsCtxMenuContext context) {
			return context.SelectedItems.Length == 1;
		}

		internal static void GoTo(ThreadsCtxMenuContext context, bool newTab) {
			if (context.SelectedItems.Length == 0)
				return;
			GoTo(context.SelectedItems[0], newTab);
		}

		internal static void GoTo(ThreadVM vm, bool newTab) {
			if (vm == null)
				return;
			StackFrameManager.Instance.SelectedThread = vm.Thread;
			FrameUtils.GoTo(vm.Thread.AllFrames.FirstOrDefault(f => f.IsILFrame), newTab);
		}
	}

	[ExportContextMenuEntry(Header = "Switch To Thread (New _Tab)", Order = 310, Category = "TH1", InputGestureText = "Ctrl+Enter")]
	sealed class SwitchToThreadNewTabThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		protected override void Execute(ThreadsCtxMenuContext context) {
			SwitchToThreadThreadsCtxMenuCommand.GoTo(context, true);
		}

		protected override bool IsEnabled(ThreadsCtxMenuContext context) {
			return SwitchToThreadThreadsCtxMenuCommand.CanGoToThread(context);
		}
	}

	[ExportContextMenuEntry(Header = "Rename", Order = 320, Category = "TH1")]
	sealed class RenameThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		protected override void Execute(ThreadsCtxMenuContext context) {
			//TODO:
		}

		protected override bool IsEnabled(ThreadsCtxMenuContext context) {
			return false;//TODO:
		}
	}

	[ExportContextMenuEntry(Header = "_Freeze", Order = 330, Category = "TH1")]
	sealed class FreezeThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		protected override void Execute(ThreadsCtxMenuContext context) {
			foreach (var t in context.SelectedItems)
				t.IsSuspended = true;
		}

		protected override bool IsEnabled(ThreadsCtxMenuContext context) {
			return context.SelectedItems.Any(t => !t.IsSuspended);
		}
	}

	[ExportContextMenuEntry(Header = "_Thaw", Order = 340, Category = "TH1")]
	sealed class ThawThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		protected override void Execute(ThreadsCtxMenuContext context) {
			foreach (var t in context.SelectedItems)
				t.IsSuspended = false;
		}

		protected override bool IsEnabled(ThreadsCtxMenuContext context) {
			return context.SelectedItems.Any(t => t.IsSuspended);
		}
	}
}

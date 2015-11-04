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
using dndbg.Engine;
using dnSpy.Contracts.Menus;
using dnSpy.Debugger.CallStack;
using dnSpy.MVVM;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;

namespace dnSpy.Debugger.Threads {
	sealed class ThreadsCtxMenuContext {
		public readonly ThreadsVM VM;
		public readonly ThreadVM[] SelectedItems;

		public ThreadsCtxMenuContext(ThreadsVM vm, ThreadVM[] selItems) {
			this.VM = vm;
			this.SelectedItems = selItems;
		}
	}

	sealed class ThreadsCtxMenuCommandProxy : MenuItemCommandProxy<ThreadsCtxMenuContext> {
        public ThreadsCtxMenuCommandProxy(ThreadsCtxMenuCommand cmd)
			: base(cmd) {
		}

		protected override ThreadsCtxMenuContext CreateContext() {
			return ThreadsCtxMenuCommand.Create();
		}
	}

	abstract class ThreadsCtxMenuCommand : MenuItemBase<ThreadsCtxMenuContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected sealed override ThreadsCtxMenuContext CreateContext(IMenuItemContext context) {
			if (DebugManager.Instance.ProcessState != DebuggerProcessState.Stopped)
				return null;
			var ui = ThreadsControlCreator.ThreadsControlInstance;
			if (context.CreatorObject.Object != ui.listView)
				return null;
			return Create();
		}

		internal static ThreadsCtxMenuContext Create() {
			var ui = ThreadsControlCreator.ThreadsControlInstance;
			var vm = ui.DataContext as ThreadsVM;
			if (vm == null)
				return null;

			var elems = ui.listView.SelectedItems.OfType<ThreadVM>().ToArray();
			Array.Sort(elems, (a, b) => a.Thread.IncrementedId.CompareTo(b.Thread.IncrementedId));

			return new ThreadsCtxMenuContext(vm, elems);
		}
	}

	[ExportMenuItem(Header = "Cop_y", Icon = "Copy", InputGestureText = "Ctrl+C", Group = MenuConstants.GROUP_CTX_DBG_THREADS_COPY, Order = 0)]
	sealed class CopyCallThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		public override void Execute(ThreadsCtxMenuContext context) {
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

		public override bool IsEnabled(ThreadsCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[ExportMenuItem(Header = "Select _All", Icon = "Select", InputGestureText = "Ctrl+A", Group = MenuConstants.GROUP_CTX_DBG_THREADS_COPY, Order = 10)]
	sealed class SelectAllThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		public override void Execute(ThreadsCtxMenuContext context) {
			ThreadsControlCreator.ThreadsControlInstance.listView.SelectAll();
		}

		public override bool IsEnabled(ThreadsCtxMenuContext context) {
			return ThreadsControlCreator.ThreadsControlInstance.listView.Items.Count > 0;
		}
	}

	[ExportMenuItem(Header = "_Hexadecimal Display", Group = MenuConstants.GROUP_CTX_DBG_THREADS_HEXOPTS, Order = 0)]
	sealed class HexadecimalDisplayThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		public override void Execute(ThreadsCtxMenuContext context) {
			DebuggerSettings.Instance.UseHexadecimal = !DebuggerSettings.Instance.UseHexadecimal;
		}

		public override bool IsChecked(ThreadsCtxMenuContext context) {
			return DebuggerSettings.Instance.UseHexadecimal;
		}
	}

	[ExportMenuItem(Header = "_Switch To Thread", InputGestureText = "Enter", Group = MenuConstants.GROUP_CTX_DBG_THREADS_CMDS, Order = 0)]
	sealed class SwitchToThreadThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		public override void Execute(ThreadsCtxMenuContext context) {
			GoTo(context, false);
		}

		public override bool IsEnabled(ThreadsCtxMenuContext context) {
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

	[ExportMenuItem(Header = "Switch To Thread (New _Tab)", InputGestureText = "Ctrl+Enter", Group = MenuConstants.GROUP_CTX_DBG_THREADS_CMDS, Order = 10)]
	sealed class SwitchToThreadNewTabThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		public override void Execute(ThreadsCtxMenuContext context) {
			SwitchToThreadThreadsCtxMenuCommand.GoTo(context, true);
		}

		public override bool IsEnabled(ThreadsCtxMenuContext context) {
			return SwitchToThreadThreadsCtxMenuCommand.CanGoToThread(context);
		}
	}

	[ExportMenuItem(Header = "Rename", Group = MenuConstants.GROUP_CTX_DBG_THREADS_CMDS, Order = 20)]
	sealed class RenameThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		public override void Execute(ThreadsCtxMenuContext context) {
			//TODO:
		}

		public override bool IsEnabled(ThreadsCtxMenuContext context) {
			return false;//TODO:
		}
	}

	[ExportMenuItem(Header = "_Freeze", Group = MenuConstants.GROUP_CTX_DBG_THREADS_CMDS, Order = 30)]
	sealed class FreezeThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		public override void Execute(ThreadsCtxMenuContext context) {
			foreach (var t in context.SelectedItems)
				t.IsSuspended = true;
		}

		public override bool IsEnabled(ThreadsCtxMenuContext context) {
			return context.SelectedItems.Any(t => !t.IsSuspended);
		}
	}

	[ExportMenuItem(Header = "_Thaw", Group = MenuConstants.GROUP_CTX_DBG_THREADS_CMDS, Order = 40)]
	sealed class ThawThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		public override void Execute(ThreadsCtxMenuContext context) {
			foreach (var t in context.SelectedItems)
				t.IsSuspended = false;
		}

		public override bool IsEnabled(ThreadsCtxMenuContext context) {
			return context.SelectedItems.Any(t => t.IsSuspended);
		}
	}
}

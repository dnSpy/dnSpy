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
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dndbg.Engine;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Debugger.CallStack;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Highlighting;
using dnSpy.Shared.Menus;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.Threads {
	[ExportAutoLoaded]
	sealed class ThreadsContentCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		ThreadsContentCommandLoader(IWpfCommandManager wpfCommandManager, CopyCallThreadsCtxMenuCommand copyCmd, SwitchToThreadThreadsCtxMenuCommand switchCmd, SwitchToThreadNewTabThreadsCtxMenuCommand switchNewTabCmd) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_DEBUGGER_THREADS_LISTVIEW);
			cmds.Add(ApplicationCommands.Copy, new ThreadsCtxMenuCommandProxy(copyCmd));
			cmds.Add(new ThreadsCtxMenuCommandProxy(switchCmd), ModifierKeys.None, Key.Enter);
			cmds.Add(new ThreadsCtxMenuCommandProxy(switchNewTabCmd), ModifierKeys.Control, Key.Enter);
			cmds.Add(new ThreadsCtxMenuCommandProxy(switchNewTabCmd), ModifierKeys.Shift, Key.Enter);
		}
	}

	[ExportAutoLoaded]
	sealed class ThreadsCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		ThreadsCommandLoader(IWpfCommandManager wpfCommandManager, IMainToolWindowManager mainToolWindowManager) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);
			cmds.Add(DebugRoutedCommands.ShowThreads, new RelayCommand(a => mainToolWindowManager.Show(ThreadsToolWindowContent.THE_GUID)));
			cmds.Add(DebugRoutedCommands.ShowThreads, ModifierKeys.Control | ModifierKeys.Alt, Key.H);
		}
	}

	sealed class ThreadsCtxMenuContext {
		public readonly IThreadsVM VM;
		public readonly ThreadVM[] SelectedItems;

		public ThreadsCtxMenuContext(IThreadsVM vm, ThreadVM[] selItems) {
			this.VM = vm;
			this.SelectedItems = selItems;
		}
	}

	sealed class ThreadsCtxMenuCommandProxy : MenuItemCommandProxy<ThreadsCtxMenuContext> {
		readonly ThreadsCtxMenuCommand cmd;

		public ThreadsCtxMenuCommandProxy(ThreadsCtxMenuCommand cmd)
			: base(cmd) {
			this.cmd = cmd;
		}

		protected override ThreadsCtxMenuContext CreateContext() {
			return cmd.Create();
		}
	}

	abstract class ThreadsCtxMenuCommand : MenuItemBase<ThreadsCtxMenuContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected readonly Lazy<ITheDebugger> theDebugger;
		protected readonly Lazy<IThreadsContent> threadsContent;

		protected ThreadsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IThreadsContent> threadsContent) {
			this.theDebugger = theDebugger;
			this.threadsContent = threadsContent;
		}

		protected sealed override ThreadsCtxMenuContext CreateContext(IMenuItemContext context) {
			if (!(context.CreatorObject.Object is ListView))
				return null;
			if (theDebugger.Value.ProcessState != DebuggerProcessState.Paused)
				return null;
			if (context.CreatorObject.Object != threadsContent.Value.ListView)
				return null;
			return Create();
		}

		internal ThreadsCtxMenuContext Create() {
			var vm = threadsContent.Value.ThreadsVM;
			var elems = threadsContent.Value.ListView.SelectedItems.OfType<ThreadVM>().ToArray();
			Array.Sort(elems, (a, b) => a.Thread.UniqueId.CompareTo(b.Thread.UniqueId));

			return new ThreadsCtxMenuContext(vm, elems);
		}
	}

	[Export, ExportMenuItem(Header = "res:CopyCommand", Icon = "Copy", InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DBG_THREADS_COPY, Order = 0)]
	sealed class CopyCallThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		readonly IDebuggerSettings debuggerSettings;

		[ImportingConstructor]
		CopyCallThreadsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IThreadsContent> threadsContent, IDebuggerSettings debuggerSettings)
			: base(theDebugger, threadsContent) {
			this.debuggerSettings = debuggerSettings;
		}

		public override void Execute(ThreadsCtxMenuContext context) {
			var output = new NoSyntaxHighlightOutput();
			foreach (var vm in context.SelectedItems) {
				var printer = new ThreadPrinter(output, debuggerSettings.UseHexadecimal, theDebugger.Value.Debugger);
				printer.WriteCurrent(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteId(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteManagedId(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteCategory(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteName(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteLocation(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WritePriority(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteAffinityMask(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteSuspended(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteProcess(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteAppDomain(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteUserState(vm);
				output.WriteLine();
			}
			var s = output.ToString();
			if (s.Length > 0) {
				try {
					Clipboard.SetText(s);
				}
				catch (ExternalException) { }
			}
		}

		public override bool IsEnabled(ThreadsCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[ExportMenuItem(Header = "res:SelectAllCommand", Icon = "Select", InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_THREADS_COPY, Order = 10)]
	sealed class SelectAllThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		[ImportingConstructor]
		SelectAllThreadsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IThreadsContent> threadsContent)
			: base(theDebugger, threadsContent) {
		}

		public override void Execute(ThreadsCtxMenuContext context) {
			threadsContent.Value.ListView.SelectAll();
		}

		public override bool IsEnabled(ThreadsCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[ExportMenuItem(Header = "res:HexDisplayCommand", Group = MenuConstants.GROUP_CTX_DBG_THREADS_HEXOPTS, Order = 0)]
	sealed class HexadecimalDisplayThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		readonly DebuggerSettingsImpl debuggerSettings;

		[ImportingConstructor]
		HexadecimalDisplayThreadsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IThreadsContent> threadsContent, DebuggerSettingsImpl debuggerSettings)
			: base(theDebugger, threadsContent) {
			this.debuggerSettings = debuggerSettings;
		}

		public override void Execute(ThreadsCtxMenuContext context) {
			debuggerSettings.UseHexadecimal = !debuggerSettings.UseHexadecimal;
		}

		public override bool IsChecked(ThreadsCtxMenuContext context) {
			return debuggerSettings.UseHexadecimal;
		}
	}

	[Export, ExportMenuItem(Header = "res:SwitchToThreadCommand", InputGestureText = "res:ShortCutKeyEnter", Group = MenuConstants.GROUP_CTX_DBG_THREADS_CMDS, Order = 0)]
	sealed class SwitchToThreadThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		readonly Lazy<IStackFrameManager> stackFrameManager;
		readonly IFileTabManager fileTabManager;
		readonly Lazy<IModuleLoader> moduleLoader;

		[ImportingConstructor]
		SwitchToThreadThreadsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IThreadsContent> threadsContent, Lazy<IStackFrameManager> stackFrameManager, IFileTabManager fileTabManager, Lazy<IModuleLoader> moduleLoader)
			: base(theDebugger, threadsContent) {
			this.stackFrameManager = stackFrameManager;
			this.fileTabManager = fileTabManager;
			this.moduleLoader = moduleLoader;
		}

		public override void Execute(ThreadsCtxMenuContext context) {
			GoTo(stackFrameManager, fileTabManager, moduleLoader, context, false);
		}

		public override bool IsEnabled(ThreadsCtxMenuContext context) {
			return CanGoToThread(context);
		}

		internal static bool CanGoToThread(ThreadsCtxMenuContext context) {
			return context.SelectedItems.Length == 1;
		}

		internal static void GoTo(Lazy<IStackFrameManager> stackFrameManager, IFileTabManager fileTabManager, Lazy<IModuleLoader> moduleLoader, ThreadsCtxMenuContext context, bool newTab) {
			if (context.SelectedItems.Length == 0)
				return;
			GoTo(fileTabManager, moduleLoader.Value, stackFrameManager.Value, context.SelectedItems[0], newTab);
		}

		internal static void GoTo(IFileTabManager fileTabManager, IModuleLoader moduleLoader, IStackFrameManager stackFrameManager, ThreadVM vm, bool newTab) {
			if (vm == null)
				return;
			stackFrameManager.SelectedThread = vm.Thread;
			FrameUtils.GoTo(fileTabManager, moduleLoader, vm.Thread.AllFrames.FirstOrDefault(f => f.IsILFrame), newTab);
		}
	}

	[Export, ExportMenuItem(Header = "res:SwitchToThreadNewTabCommand", InputGestureText = "res:ShortCutKeyCtrlEnter", Group = MenuConstants.GROUP_CTX_DBG_THREADS_CMDS, Order = 10)]
	sealed class SwitchToThreadNewTabThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		readonly Lazy<IStackFrameManager> stackFrameManager;
		readonly IFileTabManager fileTabManager;
		readonly Lazy<IModuleLoader> moduleLoader;

		[ImportingConstructor]
		SwitchToThreadNewTabThreadsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IThreadsContent> threadsContent, Lazy<IStackFrameManager> stackFrameManager, IFileTabManager fileTabManager, Lazy<IModuleLoader> moduleLoader)
			: base(theDebugger, threadsContent) {
			this.stackFrameManager = stackFrameManager;
			this.fileTabManager = fileTabManager;
			this.moduleLoader = moduleLoader;
		}

		public override void Execute(ThreadsCtxMenuContext context) {
			SwitchToThreadThreadsCtxMenuCommand.GoTo(stackFrameManager, fileTabManager, moduleLoader, context, true);
		}

		public override bool IsEnabled(ThreadsCtxMenuContext context) {
			return SwitchToThreadThreadsCtxMenuCommand.CanGoToThread(context);
		}
	}

	[ExportMenuItem(Header = "res:RenameThreadCommand", Group = MenuConstants.GROUP_CTX_DBG_THREADS_CMDS, Order = 20)]
	sealed class RenameThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		[ImportingConstructor]
		RenameThreadsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IThreadsContent> threadsContent)
			: base(theDebugger, threadsContent) {
		}

		public override void Execute(ThreadsCtxMenuContext context) {
			//TODO:
		}

		public override bool IsEnabled(ThreadsCtxMenuContext context) {
			return false;//TODO:
		}
	}

	[ExportMenuItem(Header = "res:FreezeThreadCommand", Group = MenuConstants.GROUP_CTX_DBG_THREADS_CMDS, Order = 30)]
	sealed class FreezeThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		[ImportingConstructor]
		FreezeThreadsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IThreadsContent> threadsContent)
			: base(theDebugger, threadsContent) {
		}

		public override void Execute(ThreadsCtxMenuContext context) {
			foreach (var t in context.SelectedItems)
				t.IsSuspended = true;
		}

		public override bool IsEnabled(ThreadsCtxMenuContext context) {
			return context.SelectedItems.Any(t => !t.IsSuspended);
		}
	}

	[ExportMenuItem(Header = "res:ThawThreadCommand", Group = MenuConstants.GROUP_CTX_DBG_THREADS_CMDS, Order = 40)]
	sealed class ThawThreadsCtxMenuCommand : ThreadsCtxMenuCommand {
		[ImportingConstructor]
		ThawThreadsCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IThreadsContent> threadsContent)
			: base(theDebugger, threadsContent) {
		}

		public override void Execute(ThreadsCtxMenuContext context) {
			foreach (var t in context.SelectedItems)
				t.IsSuspended = false;
		}

		public override bool IsEnabled(ThreadsCtxMenuContext context) {
			return context.SelectedItems.Any(t => t.IsSuspended);
		}
	}
}

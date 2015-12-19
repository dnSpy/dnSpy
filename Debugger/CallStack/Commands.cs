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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dndbg.Engine;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Shared.UI.Menus;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Debugger.CallStack {
	[ExportAutoLoaded]
	sealed class CallStackContentCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CallStackContentCommandLoader(IWpfCommandManager wpfCommandManager, CopyCallStackCtxMenuCommand copyCmd, RunToCursorCallStackCtxMenuCommand runToCursorCmd, SwitchToFrameCallStackCtxMenuCommand switchToFrameCmd, SwitchToFrameNewTabCallStackCtxMenuCommand switchToFrameNewTabCmd) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_DEBUGGER_CALLSTACK_LISTVIEW);
			cmds.Add(ApplicationCommands.Copy, new CallStackCtxMenuCommandProxy(copyCmd));
			cmds.Add(new CallStackCtxMenuCommandProxy(runToCursorCmd), ModifierKeys.Control, Key.F10);
			cmds.Add(new CallStackCtxMenuCommandProxy(switchToFrameCmd), ModifierKeys.None, Key.Enter);
			cmds.Add(new CallStackCtxMenuCommandProxy(switchToFrameNewTabCmd), ModifierKeys.Control, Key.Enter);
			cmds.Add(new CallStackCtxMenuCommandProxy(switchToFrameNewTabCmd), ModifierKeys.Shift, Key.Enter);
		}
	}

	[ExportAutoLoaded]
	sealed class CallStackCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CallStackCommandLoader(IWpfCommandManager wpfCommandManager, IMainToolWindowManager mainToolWindowManager) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);

			cmds.Add(DebugRoutedCommands.ShowCallStack, new RelayCommand(a => mainToolWindowManager.Show(CallStackToolWindowContent.THE_GUID)));
			cmds.Add(DebugRoutedCommands.ShowCallStack, ModifierKeys.Control | ModifierKeys.Alt, Key.C);
			cmds.Add(DebugRoutedCommands.ShowCallStack, ModifierKeys.Alt, Key.D7);
			cmds.Add(DebugRoutedCommands.ShowCallStack, ModifierKeys.Alt, Key.NumPad7);
		}
	}

	sealed class CallStackCtxMenuContext {
		public readonly ICallStackVM VM;
		public readonly ICallStackFrameVM[] SelectedItems;

		public CallStackCtxMenuContext(ICallStackVM vm, ICallStackFrameVM[] selItems) {
			this.VM = vm;
			this.SelectedItems = selItems;
		}
	}

	sealed class CallStackCtxMenuCommandProxy : MenuItemCommandProxy<CallStackCtxMenuContext> {
		readonly CallStackCtxMenuCommand cmd;

		public CallStackCtxMenuCommandProxy(CallStackCtxMenuCommand cmd)
			: base(cmd) {
			this.cmd = cmd;
		}

		protected override CallStackCtxMenuContext CreateContext() {
			return cmd.Create();
		}
	}

	abstract class CallStackCtxMenuCommand : MenuItemBase<CallStackCtxMenuContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected readonly Lazy<ITheDebugger> theDebugger;
		protected readonly Lazy<ICallStackContent> callStackContent;

		protected CallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent) {
			this.theDebugger = theDebugger;
			this.callStackContent = callStackContent;
		}

		protected sealed override CallStackCtxMenuContext CreateContext(IMenuItemContext context) {
			if (!(context.CreatorObject.Object is ListView))
				return null;
			if (theDebugger.Value.ProcessState != DebuggerProcessState.Stopped)
				return null;
			if (context.CreatorObject.Object != callStackContent.Value.ListView)
				return null;
			return Create();
		}

		internal CallStackCtxMenuContext Create() {
			var vm = callStackContent.Value.CallStackVM;
			var elems = callStackContent.Value.ListView.SelectedItems.OfType<ICallStackFrameVM>().ToArray();
			Array.Sort(elems, (a, b) => a.Index.CompareTo(b.Index));

			return new CallStackCtxMenuContext(vm, elems);
		}
	}

	[Export, ExportMenuItem(Header = "Cop_y", Icon = "Copy", InputGestureText = "Ctrl+C", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_COPY, Order = 0)]
	sealed class CopyCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		CopyCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent)
			: base(theDebugger, callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) {
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

		public override bool IsEnabled(CallStackCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[ExportMenuItem(Header = "Select _All", Icon = "Select", InputGestureText = "Ctrl+A", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_COPY, Order = 10)]
	sealed class SelectAllCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		SelectAllCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent)
			: base(theDebugger, callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) {
			callStackContent.Value.ListView.SelectAll();
		}

		public override bool IsEnabled(CallStackCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[Export, ExportMenuItem(Header = "_Switch To Frame", InputGestureText = "Enter", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 0)]
	sealed class SwitchToFrameCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly Lazy<IStackFrameManager> stackFrameManager;
		readonly IFileTabManager fileTabManager;
		readonly Lazy<IModuleLoader> moduleLoader;

		[ImportingConstructor]
		SwitchToFrameCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, Lazy<IStackFrameManager> stackFrameManager, IFileTabManager fileTabManager, Lazy<IModuleLoader> moduleLoader)
			: base(theDebugger, callStackContent) {
			this.stackFrameManager = stackFrameManager;
			this.fileTabManager = fileTabManager;
			this.moduleLoader = moduleLoader;
		}

		internal static CallStackFrameVM GetFrame(CallStackCtxMenuContext context) {
			if (context.SelectedItems.Length != 1)
				return null;
			return context.SelectedItems[0] as CallStackFrameVM;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			Execute(stackFrameManager.Value, fileTabManager, moduleLoader.Value, GetFrame(context), false);
		}

		internal static void Execute(IStackFrameManager stackFrameManager, IFileTabManager fileTabManager, IModuleLoader moduleLoader, CallStackFrameVM vm, bool newTab) {
			if (vm != null) {
				stackFrameManager.SelectedFrameNumber = vm.Index;
				FrameUtils.GoTo(fileTabManager, moduleLoader, vm.Frame, newTab);
			}
		}

		public override bool IsEnabled(CallStackCtxMenuContext context) {
			return GetFrame(context) != null;
		}
	}

	[Export, ExportMenuItem(Header = "Switch To Frame (New _Tab)", InputGestureText = "Ctrl+Enter", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 10)]
	sealed class SwitchToFrameNewTabCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly Lazy<IStackFrameManager> stackFrameManager;
		readonly IFileTabManager fileTabManager;
		readonly Lazy<IModuleLoader> moduleLoader;

		[ImportingConstructor]
		SwitchToFrameNewTabCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, Lazy<IStackFrameManager> stackFrameManager, IFileTabManager fileTabManager, Lazy<IModuleLoader> moduleLoader)
			: base(theDebugger, callStackContent) {
			this.stackFrameManager = stackFrameManager;
			this.fileTabManager = fileTabManager;
			this.moduleLoader = moduleLoader;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			SwitchToFrameCallStackCtxMenuCommand.Execute(stackFrameManager.Value, fileTabManager, moduleLoader.Value, SwitchToFrameCallStackCtxMenuCommand.GetFrame(context), true);
		}

		public override bool IsEnabled(CallStackCtxMenuContext context) {
			return SwitchToFrameCallStackCtxMenuCommand.GetFrame(context) != null;
		}
	}

	[ExportMenuItem(Header = "_Go To Code", Icon = "GoToSourceCode", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 20)]
	sealed class GoToSourceCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly IFileTabManager fileTabManager;
		readonly Lazy<IModuleLoader> moduleLoader;

		[ImportingConstructor]
		GoToSourceCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, IFileTabManager fileTabManager, Lazy<IModuleLoader> moduleLoader)
			: base(theDebugger, callStackContent) {
			this.fileTabManager = fileTabManager;
			this.moduleLoader = moduleLoader;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			if (vm != null)
				FrameUtils.GoToIL(fileTabManager, moduleLoader.Value, vm.Frame, false);
		}

		public override bool IsEnabled(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			return vm != null && FrameUtils.CanGoToIL(vm.Frame);
		}
	}

	[ExportMenuItem(Header = "Go To _Disassembly", Icon = "DisassemblyWindow", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 30)]
	sealed class GoToDisassemblyCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		GoToDisassemblyCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent)
			: base(theDebugger, callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			if (vm != null)
				FrameUtils.GoToDisasm(vm.Frame);
		}

		public override bool IsEnabled(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			return vm != null && FrameUtils.CanGoToDisasm(vm.Frame);
		}
	}

	[Export, ExportMenuItem(Header = "Ru_n To Cursor", Icon = "Cursor", InputGestureText = "Ctrl+F10", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 40)]
	sealed class RunToCursorCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly Lazy<DebugManager> debugManager;

		[ImportingConstructor]
		RunToCursorCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, Lazy<DebugManager> debugManager)
			: base(theDebugger, callStackContent) {
			this.debugManager = debugManager;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			if (vm != null)
				debugManager.Value.RunTo(vm.Frame);
		}

		public override bool IsEnabled(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			return vm != null && debugManager.Value.CanRunTo(vm.Frame);
		}
	}

	[ExportMenuItem(Header = "_Hexadecimal Display", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_HEXOPTS, Order = 0)]
	sealed class HexadecimalDisplayCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly DebuggerSettingsImpl debuggerSettings;

		[ImportingConstructor]
		HexadecimalDisplayCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, DebuggerSettingsImpl debuggerSettings)
			: base(theDebugger, callStackContent) {
			this.debuggerSettings = debuggerSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			debuggerSettings.UseHexadecimal = !debuggerSettings.UseHexadecimal;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return debuggerSettings.UseHexadecimal;
		}
	}

	[ExportMenuItem(Header = "Show _Module Names", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 0)]
	sealed class ShowModuleNamesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowModuleNamesCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			callStackSettings.ShowModuleNames = !callStackSettings.ShowModuleNames;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return callStackSettings.ShowModuleNames;
		}
	}

	[ExportMenuItem(Header = "Show Parameter _Types", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 10)]
	sealed class ShowParameterTypesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowParameterTypesCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			callStackSettings.ShowParameterTypes = !callStackSettings.ShowParameterTypes;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return callStackSettings.ShowParameterTypes;
		}
	}

	[ExportMenuItem(Header = "Show _Parameter Names", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 20)]
	sealed class ShowParameterNamesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowParameterNamesCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			callStackSettings.ShowParameterNames = !callStackSettings.ShowParameterNames;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return callStackSettings.ShowParameterNames;
		}
	}

	[ExportMenuItem(Header = "Show Parameter _Values", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 30)]
	sealed class ShowParameterValuesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowParameterValuesCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			callStackSettings.ShowParameterValues = !callStackSettings.ShowParameterValues;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return callStackSettings.ShowParameterValues;
		}
	}

	[ExportMenuItem(Header = "Show IP", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 40)]
	sealed class ShowIPCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowIPCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			callStackSettings.ShowIP = !callStackSettings.ShowIP;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return callStackSettings.ShowIP;
		}
	}

	[ExportMenuItem(Header = "Show Owner Types", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 50)]
	sealed class ShowOwnerTypesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowOwnerTypesCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			callStackSettings.ShowOwnerTypes = !callStackSettings.ShowOwnerTypes;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return callStackSettings.ShowOwnerTypes;
		}
	}

	[ExportMenuItem(Header = "Show Namespaces", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 60)]
	sealed class ShowNamespacesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowNamespacesCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			callStackSettings.ShowNamespaces = !callStackSettings.ShowNamespaces;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return callStackSettings.ShowNamespaces;
		}
	}

	[ExportMenuItem(Header = "Show Return Types", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 70)]
	sealed class ShowReturnTypesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowReturnTypesCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			callStackSettings.ShowReturnTypes = !callStackSettings.ShowReturnTypes;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return callStackSettings.ShowReturnTypes;
		}
	}

	[ExportMenuItem(Header = "Show Type Keywords", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 80)]
	sealed class ShowTypeKeywordsCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowTypeKeywordsCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			callStackSettings.ShowTypeKeywords = !callStackSettings.ShowTypeKeywords;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return callStackSettings.ShowTypeKeywords;
		}
	}

	[ExportMenuItem(Header = "Show Tokens", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 90)]
	sealed class ShowTokensCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowTokensCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			callStackSettings.ShowTokens = !callStackSettings.ShowTokens;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return callStackSettings.ShowTokens;
		}
	}
}

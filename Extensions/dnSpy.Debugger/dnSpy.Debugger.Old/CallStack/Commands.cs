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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dndbg.Engine;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.ToolWindows.App;

namespace dnSpy.Debugger.CallStack {
	//[ExportAutoLoaded]
	sealed class CallStackContentCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CallStackContentCommandLoader(IWpfCommandService wpfCommandService, CopyCallStackCtxMenuCommand copyCmd, RunToCursorCallStackCtxMenuCommand runToCursorCmd, SwitchToFrameCallStackCtxMenuCommand switchToFrameCmd, SwitchToFrameNewTabCallStackCtxMenuCommand switchToFrameNewTabCmd) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_CALLSTACK_LISTVIEW);
			cmds.Add(ApplicationCommands.Copy, new CallStackCtxMenuCommandProxy(copyCmd));
			cmds.Add(new CallStackCtxMenuCommandProxy(runToCursorCmd), ModifierKeys.Control, Key.F10);
			cmds.Add(new CallStackCtxMenuCommandProxy(switchToFrameCmd), ModifierKeys.None, Key.Enter);
			cmds.Add(new CallStackCtxMenuCommandProxy(switchToFrameNewTabCmd), ModifierKeys.Control, Key.Enter);
			cmds.Add(new CallStackCtxMenuCommandProxy(switchToFrameNewTabCmd), ModifierKeys.Shift, Key.Enter);
		}
	}

	//[ExportAutoLoaded]
	sealed class CallStackCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CallStackCommandLoader(IWpfCommandService wpfCommandService, IDsToolWindowService toolWindowService) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);

			cmds.Add(DebugRoutedCommands.ShowCallStack, new RelayCommand(a => toolWindowService.Show(CallStackToolWindowContent.THE_GUID)));
			cmds.Add(DebugRoutedCommands.ShowCallStack, ModifierKeys.Control | ModifierKeys.Alt, Key.C);
			cmds.Add(DebugRoutedCommands.ShowCallStack, ModifierKeys.Alt, Key.D7);
			cmds.Add(DebugRoutedCommands.ShowCallStack, ModifierKeys.Alt, Key.NumPad7);
		}
	}

	sealed class CallStackCtxMenuContext {
		public ICallStackVM VM { get; }
		public ICallStackFrameVM[] SelectedItems { get; }

		public CallStackCtxMenuContext(ICallStackVM vm, ICallStackFrameVM[] selItems) {
			VM = vm;
			SelectedItems = selItems;
		}
	}

	sealed class CallStackCtxMenuCommandProxy : MenuItemCommandProxy<CallStackCtxMenuContext> {
		readonly CallStackCtxMenuCommand cmd;

		public CallStackCtxMenuCommandProxy(CallStackCtxMenuCommand cmd)
			: base(cmd) {
			this.cmd = cmd;
		}

		protected override CallStackCtxMenuContext CreateContext() => cmd.Create();
	}

	abstract class CallStackCtxMenuCommand : MenuItemBase<CallStackCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
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
			if (theDebugger.Value.ProcessState != DebuggerProcessState.Paused)
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

	//[Export, ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_COPY, Order = 0)]
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
			if (sb.Length > 0) {
				try {
					Clipboard.SetText(sb.ToString());
				}
				catch (ExternalException) { }
			}
		}

		public override bool IsEnabled(CallStackCtxMenuContext context) => context.SelectedItems.Length > 0;
	}

	//[ExportMenuItem(Header = "res:SelectAllCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_COPY, Order = 10)]
	sealed class SelectAllCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		[ImportingConstructor]
		SelectAllCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent)
			: base(theDebugger, callStackContent) {
		}

		public override void Execute(CallStackCtxMenuContext context) {
			callStackContent.Value.ListView.SelectAll();
		}

		public override bool IsEnabled(CallStackCtxMenuContext context) => context.SelectedItems.Length > 0;
	}

	//[Export, ExportMenuItem(Header = "res:SwitchToFrameCommand", InputGestureText = "res:ShortCutKeyEnter", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 0)]
	sealed class SwitchToFrameCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly Lazy<IStackFrameService> stackFrameService;
		readonly IDocumentTabService documentTabService;
		readonly Lazy<IModuleLoader> moduleLoader;
		readonly IModuleIdProvider moduleIdProvider;

		[ImportingConstructor]
		SwitchToFrameCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, Lazy<IStackFrameService> stackFrameService, IDocumentTabService documentTabService, Lazy<IModuleLoader> moduleLoader, IModuleIdProvider moduleIdProvider)
			: base(theDebugger, callStackContent) {
			this.stackFrameService = stackFrameService;
			this.documentTabService = documentTabService;
			this.moduleLoader = moduleLoader;
			this.moduleIdProvider = moduleIdProvider;
		}

		internal static CallStackFrameVM GetFrame(CallStackCtxMenuContext context) {
			if (context.SelectedItems.Length != 1)
				return null;
			return context.SelectedItems[0] as CallStackFrameVM;
		}

		public override void Execute(CallStackCtxMenuContext context) =>
			Execute(moduleIdProvider, stackFrameService.Value, documentTabService, moduleLoader.Value, GetFrame(context), false);

		internal static void Execute(IModuleIdProvider moduleIdProvider, IStackFrameService stackFrameService, IDocumentTabService documentTabService, IModuleLoader moduleLoader, CallStackFrameVM vm, bool newTab) {
			if (vm != null) {
				stackFrameService.SelectedFrameNumber = vm.Index;
				FrameUtils.GoTo(moduleIdProvider, documentTabService, moduleLoader, vm.Frame, newTab);
			}
		}

		public override bool IsEnabled(CallStackCtxMenuContext context) => GetFrame(context) != null;
	}

	//[Export, ExportMenuItem(Header = "res:SwitchToFrameNewTabCommand", InputGestureText = "res:ShortCutKeyCtrlEnter", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 10)]
	sealed class SwitchToFrameNewTabCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly Lazy<IStackFrameService> stackFrameService;
		readonly IDocumentTabService documentTabService;
		readonly Lazy<IModuleLoader> moduleLoader;
		readonly IModuleIdProvider moduleIdProvider;

		[ImportingConstructor]
		SwitchToFrameNewTabCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, Lazy<IStackFrameService> stackFrameService, IDocumentTabService documentTabService, Lazy<IModuleLoader> moduleLoader, IModuleIdProvider moduleIdProvider)
			: base(theDebugger, callStackContent) {
			this.stackFrameService = stackFrameService;
			this.documentTabService = documentTabService;
			this.moduleLoader = moduleLoader;
			this.moduleIdProvider = moduleIdProvider;
		}

		public override void Execute(CallStackCtxMenuContext context) =>
			SwitchToFrameCallStackCtxMenuCommand.Execute(moduleIdProvider, stackFrameService.Value, documentTabService, moduleLoader.Value, SwitchToFrameCallStackCtxMenuCommand.GetFrame(context), true);
		public override bool IsEnabled(CallStackCtxMenuContext context) => SwitchToFrameCallStackCtxMenuCommand.GetFrame(context) != null;
	}

	//[ExportMenuItem(Header = "res:GoToCodeCommand", Icon = DsImagesAttribute.GoToSourceCode, Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 20)]
	sealed class GoToSourceCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly IDocumentTabService documentTabService;
		readonly Lazy<IModuleLoader> moduleLoader;
		readonly IModuleIdProvider moduleIdProvider;

		[ImportingConstructor]
		GoToSourceCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, IDocumentTabService documentTabService, Lazy<IModuleLoader> moduleLoader, IModuleIdProvider moduleIdProvider)
			: base(theDebugger, callStackContent) {
			this.documentTabService = documentTabService;
			this.moduleLoader = moduleLoader;
			this.moduleIdProvider = moduleIdProvider;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			if (vm != null)
				FrameUtils.GoToIL(moduleIdProvider, documentTabService, moduleLoader.Value, vm.Frame, false);
		}

		public override bool IsEnabled(CallStackCtxMenuContext context) =>
			FrameUtils.CanGoToIL(SwitchToFrameCallStackCtxMenuCommand.GetFrame(context)?.Frame);
	}

	//[ExportMenuItem(Header = "res:GoToDisassemblyCommand2", Icon = DsImagesAttribute.DisassemblyWindow, Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 30)]
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

		public override bool IsEnabled(CallStackCtxMenuContext context) =>
			FrameUtils.CanGoToDisasm(SwitchToFrameCallStackCtxMenuCommand.GetFrame(context)?.Frame);
	}

	//[Export, ExportMenuItem(Header = "res:RunToCursorCommand", Icon = DsImagesAttribute.Cursor, InputGestureText = "res:ShortCutKeyCtrlF10", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 40)]
	sealed class RunToCursorCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly Lazy<IDebugService> debugService;

		[ImportingConstructor]
		RunToCursorCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, Lazy<IDebugService> debugService)
			: base(theDebugger, callStackContent) {
			this.debugService = debugService;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			if (vm != null)
				debugService.Value.RunTo(vm.Frame);
		}

		public override bool IsEnabled(CallStackCtxMenuContext context) =>
			debugService.Value.CanRunTo(SwitchToFrameCallStackCtxMenuCommand.GetFrame(context)?.Frame);
	}

	//[ExportMenuItem(Header = "res:HexDisplayCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_HEXOPTS, Order = 0)]
	sealed class HexadecimalDisplayCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly DebuggerSettingsImpl debuggerSettings;

		[ImportingConstructor]
		HexadecimalDisplayCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, DebuggerSettingsImpl debuggerSettings)
			: base(theDebugger, callStackContent) {
			this.debuggerSettings = debuggerSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) => debuggerSettings.UseHexadecimal = !debuggerSettings.UseHexadecimal;
		public override bool IsChecked(CallStackCtxMenuContext context) => debuggerSettings.UseHexadecimal;
	}

	//[ExportMenuItem(Header = "res:ShowModuleNamesCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 0)]
	sealed class ShowModuleNamesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowModuleNamesCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) => callStackSettings.ShowModuleNames = !callStackSettings.ShowModuleNames;
		public override bool IsChecked(CallStackCtxMenuContext context) => callStackSettings.ShowModuleNames;
	}

	//[ExportMenuItem(Header = "res:ShowParameterTypesCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 10)]
	sealed class ShowParameterTypesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowParameterTypesCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) => callStackSettings.ShowParameterTypes = !callStackSettings.ShowParameterTypes;
		public override bool IsChecked(CallStackCtxMenuContext context) => callStackSettings.ShowParameterTypes;
	}

	//[ExportMenuItem(Header = "res:ShowParameterNamesCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 20)]
	sealed class ShowParameterNamesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowParameterNamesCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) => callStackSettings.ShowParameterNames = !callStackSettings.ShowParameterNames;
		public override bool IsChecked(CallStackCtxMenuContext context) => callStackSettings.ShowParameterNames;
	}

	//[ExportMenuItem(Header = "res:ShowParameterValuesCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 30)]
	sealed class ShowParameterValuesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowParameterValuesCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) => callStackSettings.ShowParameterValues = !callStackSettings.ShowParameterValues;
		public override bool IsChecked(CallStackCtxMenuContext context) => callStackSettings.ShowParameterValues;
	}

	//[ExportMenuItem(Header = "res:ShowInstructionPointerCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 40)]
	sealed class ShowIPCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowIPCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) => callStackSettings.ShowIP = !callStackSettings.ShowIP;
		public override bool IsChecked(CallStackCtxMenuContext context) => callStackSettings.ShowIP;
	}

	//[ExportMenuItem(Header = "res:ShowOwnerTypesCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 50)]
	sealed class ShowOwnerTypesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowOwnerTypesCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) => callStackSettings.ShowOwnerTypes = !callStackSettings.ShowOwnerTypes;
		public override bool IsChecked(CallStackCtxMenuContext context) => callStackSettings.ShowOwnerTypes;
	}

	//[ExportMenuItem(Header = "res:ShowNamespacesCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 60)]
	sealed class ShowNamespacesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowNamespacesCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) => callStackSettings.ShowNamespaces = !callStackSettings.ShowNamespaces;
		public override bool IsChecked(CallStackCtxMenuContext context) => callStackSettings.ShowNamespaces;
	}

	//[ExportMenuItem(Header = "res:ShowReturnTypesCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 70)]
	sealed class ShowReturnTypesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowReturnTypesCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) => callStackSettings.ShowReturnTypes = !callStackSettings.ShowReturnTypes;
		public override bool IsChecked(CallStackCtxMenuContext context) => callStackSettings.ShowReturnTypes;
	}

	//[ExportMenuItem(Header = "res:ShowTypeKeywordsCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 80)]
	sealed class ShowTypeKeywordsCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowTypeKeywordsCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) => callStackSettings.ShowTypeKeywords = !callStackSettings.ShowTypeKeywords;
		public override bool IsChecked(CallStackCtxMenuContext context) => callStackSettings.ShowTypeKeywords;
	}

	//[ExportMenuItem(Header = "res:ShowTokensCommand", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 90)]
	sealed class ShowTokensCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		readonly CallStackSettingsImpl callStackSettings;

		[ImportingConstructor]
		ShowTokensCallStackCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<ICallStackContent> callStackContent, CallStackSettingsImpl callStackSettings)
			: base(theDebugger, callStackContent) {
			this.callStackSettings = callStackSettings;
		}

		public override void Execute(CallStackCtxMenuContext context) => callStackSettings.ShowTokens = !callStackSettings.ShowTokens;
		public override bool IsChecked(CallStackCtxMenuContext context) => callStackSettings.ShowTokens;
	}
}

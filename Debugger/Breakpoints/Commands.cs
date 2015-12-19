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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.Highlighting;
using dnSpy.Shared.UI.Menus;
using dnSpy.Shared.UI.MVVM;
using ICSharpCode.Decompiler;

namespace dnSpy.Debugger.Breakpoints {
	[ExportAutoLoaded]
	sealed class BreakpointsCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		BreakpointsCommandLoader(IWpfCommandManager wpfCommandManager, Lazy<BreakpointManager> breakpointManager, IMainToolWindowManager mainToolWindowManager) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);
			cmds.Add(DebugRoutedCommands.DeleteAllBreakpoints, (s, e) => breakpointManager.Value.ClearAskUser(), (s, e) => e.CanExecute = breakpointManager.Value.CanClear, ModifierKeys.Control | ModifierKeys.Shift, Key.F9);
			cmds.Add(DebugRoutedCommands.ToggleBreakpoint, (s, e) => breakpointManager.Value.ToggleBreakpoint(), (s, e) => e.CanExecute = breakpointManager.Value.CanToggleBreakpoint, ModifierKeys.None, Key.F9);
			cmds.Add(DebugRoutedCommands.DisableBreakpoint, (s, e) => breakpointManager.Value.DisableBreakpoint(), (s, e) => e.CanExecute = breakpointManager.Value.CanDisableBreakpoint, ModifierKeys.Control, Key.F9);
			cmds.Add(DebugRoutedCommands.DisableAllBreakpoints, (s, e) => breakpointManager.Value.DisableAllBreakpoints(), (s, e) => e.CanExecute = breakpointManager.Value.CanDisableAllBreakpoints);
			cmds.Add(DebugRoutedCommands.EnableAllBreakpoints, (s, e) => breakpointManager.Value.EnableAllBreakpoints(), (s, e) => e.CanExecute = breakpointManager.Value.CanEnableAllBreakpoints);

			cmds.Add(DebugRoutedCommands.ShowBreakpoints, new RelayCommand(a => mainToolWindowManager.Show(BreakpointsToolWindowContent.THE_GUID)));
			cmds.Add(DebugRoutedCommands.ShowBreakpoints, ModifierKeys.Control | ModifierKeys.Alt, Key.B);
			cmds.Add(DebugRoutedCommands.ShowBreakpoints, ModifierKeys.Alt, Key.F9);
		}
	}

	sealed class BreakpointCtxMenuContext {
		public readonly IBreakpointsVM VM;
		public readonly BreakpointVM[] SelectedItems;

		public BreakpointCtxMenuContext(IBreakpointsVM vm, BreakpointVM[] selItems) {
			this.VM = vm;
			this.SelectedItems = selItems;
		}
	}

	sealed class BreakpointCtxMenuCommandProxy : MenuItemCommandProxy<BreakpointCtxMenuContext> {
		readonly BreakpointCtxMenuCommand cmd;

		public BreakpointCtxMenuCommandProxy(BreakpointCtxMenuCommand cmd)
			: base(cmd) {
			this.cmd = cmd;
		}

		protected override BreakpointCtxMenuContext CreateContext() {
			return cmd.Create();
		}
	}

	abstract class BreakpointCtxMenuCommand : MenuItemBase<BreakpointCtxMenuContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected readonly Lazy<IBreakpointsContent> breakpointsContent;

		protected BreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent) {
			this.breakpointsContent = breakpointsContent;
		}

		protected sealed override BreakpointCtxMenuContext CreateContext(IMenuItemContext context) {
			if (!(context.CreatorObject.Object is ListView))
				return null;
			if (context.CreatorObject.Object != breakpointsContent.Value.ListView)
				return null;
			return Create();
		}

		internal BreakpointCtxMenuContext Create() {
			var listView = breakpointsContent.Value.ListView;
			var vm = breakpointsContent.Value.BreakpointsVM;

			var dict = new Dictionary<object, int>(listView.Items.Count);
			for (int i = 0; i < listView.Items.Count; i++)
				dict[listView.Items[i]] = i;
			var elems = listView.SelectedItems.OfType<BreakpointVM>().ToArray();
			Array.Sort(elems, (a, b) => dict[a].CompareTo(dict[b]));

			return new BreakpointCtxMenuContext(vm, elems);
		}
	}

	[Export, ExportMenuItem(Header = "Cop_y", Icon = "Copy", InputGestureText = "Ctrl+C", Group = MenuConstants.GROUP_CTX_DBG_BPS_COPY, Order = 0)]
	sealed class CopyBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly ILanguageManager languageManager;
		readonly IDebuggerSettings debuggerSettings;

		[ImportingConstructor]
		CopyBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, ILanguageManager languageManager, IDebuggerSettings debuggerSettings)
			: base(breakpointsContent) {
			this.languageManager = languageManager;
			this.debuggerSettings = debuggerSettings;
		}

		public override void Execute(BreakpointCtxMenuContext context) {
			var output = new NoSyntaxHighlightOutput();
			foreach (var vm in context.SelectedItems) {
				var printer = new BreakpointPrinter(output, debuggerSettings.UseHexadecimal, languageManager.SelectedLanguage, languageManager);
				printer.WriteName(vm);
				output.Write("\t", TextTokenType.Text);
				printer.WriteAssembly(vm);
				output.Write("\t", TextTokenType.Text);
				printer.WriteModule(vm);
				output.Write("\t", TextTokenType.Text);
				printer.WriteFile(vm);
				output.WriteLine();
			}
			var s = output.ToString();
			if (s.Length > 0)
				Clipboard.SetText(s);
		}

		public override bool IsEnabled(BreakpointCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[ExportMenuItem(Header = "Select _All", Icon = "Select", InputGestureText = "Ctrl+A", Group = MenuConstants.GROUP_CTX_DBG_BPS_COPY, Order = 10)]
	sealed class SelectAllBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		[ImportingConstructor]
		SelectAllBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent)
			: base(breakpointsContent) {
		}

		public override void Execute(BreakpointCtxMenuContext context) {
			breakpointsContent.Value.ListView.SelectAll();
		}

		public override bool IsEnabled(BreakpointCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[Export, ExportMenuItem(Header = "_Delete", Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_DBG_BPS_COPY, Order = 20)]
	sealed class DeleteBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		[ImportingConstructor]
		DeleteBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent)
			: base(breakpointsContent) {
		}

		public override bool IsEnabled(BreakpointCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}

		public override void Execute(BreakpointCtxMenuContext context) {
			context.VM.Remove(context.SelectedItems);
		}
	}

	[ExportMenuItem(Header = "Delete _All Breakpoints", Icon = "DeleteAllBreakpoints", InputGestureText = "Ctrl+Shift+F9", Group = MenuConstants.GROUP_CTX_DBG_BPS_COPY, Order = 30)]
	sealed class DeleteAllBPsBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		DeleteAllBPsBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, IAppWindow appWindow)
			: base(breakpointsContent) {
			this.appWindow = appWindow;
		}

		public override void Execute(BreakpointCtxMenuContext context) {
			DebugRoutedCommands.DeleteAllBreakpoints.Execute(null, appWindow.MainWindow);
		}

		public override bool IsEnabled(BreakpointCtxMenuContext context) {
			return DebugRoutedCommands.DeleteAllBreakpoints.CanExecute(null, appWindow.MainWindow);
		}
	}

	[ExportMenuItem(Header = "Enable All Breakpoi_nts", Icon = "EnableAllBreakpoints", Group = MenuConstants.GROUP_CTX_DBG_BPS_COPY, Order = 40)]
	sealed class EnableAllBPsBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		EnableAllBPsBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, IAppWindow appWindow)
			: base(breakpointsContent) {
			this.appWindow = appWindow;
		}

		public override void Execute(BreakpointCtxMenuContext context) {
			DebugRoutedCommands.EnableAllBreakpoints.Execute(null, appWindow.MainWindow);
		}

		public override bool IsVisible(BreakpointCtxMenuContext context) {
			return DebugRoutedCommands.EnableAllBreakpoints.CanExecute(null, appWindow.MainWindow);
		}
	}

	[ExportMenuItem(Header = "Disable All Breakpoi_nts", Icon = "DisableAllBreakpoints", Group = MenuConstants.GROUP_CTX_DBG_BPS_COPY, Order = 50)]
	sealed class DisableAllBPsBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		DisableAllBPsBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, IAppWindow appWindow)
			: base(breakpointsContent) {
			this.appWindow = appWindow;
		}

		public override void Execute(BreakpointCtxMenuContext context) {
			DebugRoutedCommands.DisableAllBreakpoints.Execute(null, appWindow.MainWindow);
		}

		public override bool IsVisible(BreakpointCtxMenuContext context) {
			return DebugRoutedCommands.DisableAllBreakpoints.CanExecute(null, appWindow.MainWindow);
		}
	}

	[Export, ExportMenuItem(Header = "_Go To Code", Icon = "GoToSourceCode", InputGestureText = "Enter", Group = MenuConstants.GROUP_CTX_DBG_BPS_CODE, Order = 0)]
	sealed class GoToSourceBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly Lazy<IModuleLoader> moduleLoader;
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		GoToSourceBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, Lazy<IModuleLoader> moduleLoader, IFileTabManager fileTabManager)
			: base(breakpointsContent) {
			this.moduleLoader = moduleLoader;
			this.fileTabManager = fileTabManager;
		}

		public override void Execute(BreakpointCtxMenuContext context) {
			if (context.SelectedItems.Length == 1)
				GoTo(fileTabManager, moduleLoader, context.SelectedItems[0], false);
		}

		internal static void GoTo(IFileTabManager fileTabManager, Lazy<IModuleLoader> moduleLoader, BreakpointVM vm, bool newTab) {
			if (vm == null)
				return;
			var ilbp = vm.Breakpoint as ILCodeBreakpoint;
			if (ilbp == null)
				return;
			DebugUtils.GoToIL(fileTabManager, moduleLoader.Value, ilbp.SerializedDnSpyToken.Module, ilbp.SerializedDnSpyToken.Token, ilbp.ILOffset, newTab);
		}

		public override bool IsEnabled(BreakpointCtxMenuContext context) {
			return context.SelectedItems.Length == 1 && context.SelectedItems[0].Breakpoint is ILCodeBreakpoint;
		}
	}

	[Export, ExportMenuItem(Header = "Go To Code (New _Tab)", Icon = "GoToSourceCode", InputGestureText = "Ctrl+Enter", Group = MenuConstants.GROUP_CTX_DBG_BPS_CODE, Order = 10)]
	sealed class GoToSourceNewTabBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly Lazy<IModuleLoader> moduleLoader;
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		GoToSourceNewTabBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, Lazy<IModuleLoader> moduleLoader, IFileTabManager fileTabManager)
			: base(breakpointsContent) {
			this.moduleLoader = moduleLoader;
			this.fileTabManager = fileTabManager;
		}

		public override void Execute(BreakpointCtxMenuContext context) {
			if (context.SelectedItems.Length == 1)
				GoToSourceBreakpointCtxMenuCommand.GoTo(fileTabManager, moduleLoader, context.SelectedItems[0], true);
		}

		public override bool IsEnabled(BreakpointCtxMenuContext context) {
			return context.SelectedItems.Length == 1 && context.SelectedItems[0].Breakpoint is ILCodeBreakpoint;
		}
	}

	[ExportMenuItem(Header = "Go To Disassembly", Icon = "DisassemblyWindow", Group = MenuConstants.GROUP_CTX_DBG_BPS_CODE, Order = 20)]
	sealed class GoToDisassemblyBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		[ImportingConstructor]
		GoToDisassemblyBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent)
			: base(breakpointsContent) {
		}

		public override void Execute(BreakpointCtxMenuContext context) {
			//TODO:
		}

		public override bool IsEnabled(BreakpointCtxMenuContext context) {
			return false;//TODO:
		}
	}

	[Export]
	sealed class ToggleEnableBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		[ImportingConstructor]
		ToggleEnableBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent)
			: base(breakpointsContent) {
		}

		public override void Execute(BreakpointCtxMenuContext context) {
			foreach (var bp in context.SelectedItems)
				bp.IsEnabled = !bp.IsEnabled;
		}
	}

	[ExportMenuItem(Header = "Show Tokens", Group = MenuConstants.GROUP_CTX_DBG_BPS_OPTS, Order = 0)]
	sealed class ShowTokensBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly BreakpointSettingsImpl breakpointSettings;

		[ImportingConstructor]
		ShowTokensBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, BreakpointSettingsImpl breakpointSettings)
			: base(breakpointsContent) {
			this.breakpointSettings = breakpointSettings;
		}

		public override void Execute(BreakpointCtxMenuContext context) {
			breakpointSettings.ShowTokens = !breakpointSettings.ShowTokens;
		}

		public override bool IsChecked(BreakpointCtxMenuContext context) {
			return breakpointSettings.ShowTokens;
		}
	}
}

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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.ToolWindows.App;

namespace dnSpy.Debugger.Breakpoints {
	//[ExportAutoLoaded]
	sealed class BreakpointsCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		BreakpointsCommandLoader(IWpfCommandService wpfCommandService, Lazy<BreakpointService> breakpointService, IDsToolWindowService toolWindowService) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);
			cmds.Add(DebugRoutedCommands.DeleteAllBreakpoints, (s, e) => breakpointService.Value.ClearAskUser(), (s, e) => e.CanExecute = breakpointService.Value.CanClear, ModifierKeys.Control | ModifierKeys.Shift, Key.F9);
			cmds.Add(DebugRoutedCommands.ToggleBreakpoint, (s, e) => breakpointService.Value.ToggleBreakpoint(), (s, e) => e.CanExecute = breakpointService.Value.CanToggleBreakpoint, ModifierKeys.None, Key.F9);
			cmds.Add(DebugRoutedCommands.DisableBreakpoint, (s, e) => breakpointService.Value.DisableBreakpoint(), (s, e) => e.CanExecute = breakpointService.Value.CanDisableBreakpoint, ModifierKeys.Control, Key.F9);
			cmds.Add(DebugRoutedCommands.DisableAllBreakpoints, (s, e) => breakpointService.Value.DisableAllBreakpoints(), (s, e) => e.CanExecute = breakpointService.Value.CanDisableAllBreakpoints);
			cmds.Add(DebugRoutedCommands.EnableAllBreakpoints, (s, e) => breakpointService.Value.EnableAllBreakpoints(), (s, e) => e.CanExecute = breakpointService.Value.CanEnableAllBreakpoints);

			cmds.Add(DebugRoutedCommands.ShowBreakpoints, new RelayCommand(a => toolWindowService.Show(BreakpointsToolWindowContent.THE_GUID)));
			cmds.Add(DebugRoutedCommands.ShowBreakpoints, ModifierKeys.Control | ModifierKeys.Alt, Key.B);
			cmds.Add(DebugRoutedCommands.ShowBreakpoints, ModifierKeys.Alt, Key.F9);
		}
	}

	sealed class BreakpointCtxMenuContext {
		public IBreakpointsVM VM { get; }
		public BreakpointVM[] SelectedItems { get; }

		public BreakpointCtxMenuContext(IBreakpointsVM vm, BreakpointVM[] selItems) {
			VM = vm;
			SelectedItems = selItems;
		}
	}

	sealed class BreakpointCtxMenuCommandProxy : MenuItemCommandProxy<BreakpointCtxMenuContext> {
		readonly BreakpointCtxMenuCommand cmd;

		public BreakpointCtxMenuCommandProxy(BreakpointCtxMenuCommand cmd)
			: base(cmd) {
			this.cmd = cmd;
		}

		protected override BreakpointCtxMenuContext CreateContext() => cmd.Create();
	}

	abstract class BreakpointCtxMenuCommand : MenuItemBase<BreakpointCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
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

	//[Export, ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DBG_BPS_COPY, Order = 0)]
	sealed class CopyBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly IDecompilerService decompilerService;
		readonly IDebuggerSettings debuggerSettings;

		[ImportingConstructor]
		CopyBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, IDecompilerService decompilerService, IDebuggerSettings debuggerSettings)
			: base(breakpointsContent) {
			this.decompilerService = decompilerService;
			this.debuggerSettings = debuggerSettings;
		}

		public override void Execute(BreakpointCtxMenuContext context) {
			var output = new StringBuilderTextColorOutput();
			foreach (var vm in context.SelectedItems) {
				var printer = new BreakpointPrinter(output, debuggerSettings.UseHexadecimal, decompilerService.Decompiler);
				printer.WriteName(vm);
				output.Write(BoxedTextColor.Text, "\t");
				printer.WriteAssembly(vm);
				output.Write(BoxedTextColor.Text, "\t");
				printer.WriteModule(vm);
				output.Write(BoxedTextColor.Text, "\t");
				printer.WriteFile(vm);
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

		public override bool IsEnabled(BreakpointCtxMenuContext context) => context.SelectedItems.Length > 0;
	}

	//[ExportMenuItem(Header = "res:SelectAllCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_BPS_COPY, Order = 10)]
	sealed class SelectAllBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		[ImportingConstructor]
		SelectAllBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent)
			: base(breakpointsContent) {
		}

		public override void Execute(BreakpointCtxMenuContext context) => breakpointsContent.Value.ListView.SelectAll();
		public override bool IsEnabled(BreakpointCtxMenuContext context) => context.SelectedItems.Length > 0;
	}

	//[Export, ExportMenuItem(Header = "res:DeleteCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:ShortCutKeyDelete", Group = MenuConstants.GROUP_CTX_DBG_BPS_COPY, Order = 20)]
	sealed class DeleteBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		[ImportingConstructor]
		DeleteBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent)
			: base(breakpointsContent) {
		}

		public override bool IsEnabled(BreakpointCtxMenuContext context) => context.SelectedItems.Length > 0;
		public override void Execute(BreakpointCtxMenuContext context) => context.VM.Remove(context.SelectedItems);
	}

	//[ExportMenuItem(Header = "res:DeleteAllBreakpointsCommand2", Icon = DsImagesAttribute.ClearBreakpointGroup, InputGestureText = "res:ShortCutKeyCtrlShiftF9", Group = MenuConstants.GROUP_CTX_DBG_BPS_COPY, Order = 30)]
	sealed class DeleteAllBPsBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		DeleteAllBPsBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, IAppWindow appWindow)
			: base(breakpointsContent) {
			this.appWindow = appWindow;
		}

		public override void Execute(BreakpointCtxMenuContext context) => DebugRoutedCommands.DeleteAllBreakpoints.Execute(null, appWindow.MainWindow);
		public override bool IsEnabled(BreakpointCtxMenuContext context) => DebugRoutedCommands.DeleteAllBreakpoints.CanExecute(null, appWindow.MainWindow);
	}

	//[ExportMenuItem(Header = "res:EnableAllBreakpointsCommand", Icon = DsImagesAttribute.EnableAllBreakpoints, Group = MenuConstants.GROUP_CTX_DBG_BPS_COPY, Order = 40)]
	sealed class EnableAllBPsBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		EnableAllBPsBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, IAppWindow appWindow)
			: base(breakpointsContent) {
			this.appWindow = appWindow;
		}

		public override void Execute(BreakpointCtxMenuContext context) => DebugRoutedCommands.EnableAllBreakpoints.Execute(null, appWindow.MainWindow);
		public override bool IsVisible(BreakpointCtxMenuContext context) => DebugRoutedCommands.EnableAllBreakpoints.CanExecute(null, appWindow.MainWindow);
	}

	//[ExportMenuItem(Header = "res:DisableAllBreakpointsCommand", Icon = DsImagesAttribute.DisableAllBreakpoints, Group = MenuConstants.GROUP_CTX_DBG_BPS_COPY, Order = 50)]
	sealed class DisableAllBPsBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		DisableAllBPsBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, IAppWindow appWindow)
			: base(breakpointsContent) {
			this.appWindow = appWindow;
		}

		public override void Execute(BreakpointCtxMenuContext context) => DebugRoutedCommands.DisableAllBreakpoints.Execute(null, appWindow.MainWindow);
		public override bool IsVisible(BreakpointCtxMenuContext context) => DebugRoutedCommands.DisableAllBreakpoints.CanExecute(null, appWindow.MainWindow);
	}

	//[Export, ExportMenuItem(Header = "res:GoToCodeCommand", Icon = DsImagesAttribute.GoToSourceCode, InputGestureText = "res:ShortCutKeyEnter", Group = MenuConstants.GROUP_CTX_DBG_BPS_CODE, Order = 0)]
	sealed class GoToSourceBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly Lazy<IModuleLoader> moduleLoader;
		readonly IDocumentTabService documentTabService;
		readonly IModuleIdProvider moduleIdProvider;

		[ImportingConstructor]
		GoToSourceBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, Lazy<IModuleLoader> moduleLoader, IDocumentTabService documentTabService, IModuleIdProvider moduleIdProvider)
			: base(breakpointsContent) {
			this.moduleLoader = moduleLoader;
			this.documentTabService = documentTabService;
			this.moduleIdProvider = moduleIdProvider;
		}

		public override void Execute(BreakpointCtxMenuContext context) {
			if (context.SelectedItems.Length == 1)
				GoTo(moduleIdProvider, documentTabService, moduleLoader, context.SelectedItems[0], false);
		}

		internal static void GoTo(IModuleIdProvider moduleIdProvider, IDocumentTabService documentTabService, Lazy<IModuleLoader> moduleLoader, BreakpointVM vm, bool newTab) {
			if (vm == null)
				return;
			var ilbp = vm.Breakpoint as ILCodeBreakpoint;
			if (ilbp == null)
				return;
			DebugUtils.GoToIL(moduleIdProvider, documentTabService, moduleLoader.Value, ilbp.MethodToken.Module, ilbp.MethodToken.Token, ilbp.ILOffset, newTab);
		}

		public override bool IsEnabled(BreakpointCtxMenuContext context) => context.SelectedItems.Length == 1 && context.SelectedItems[0].Breakpoint is ILCodeBreakpoint;
	}

	//[Export, ExportMenuItem(Header = "res:GoToCodeNewTabCommand", Icon = DsImagesAttribute.GoToSourceCode, InputGestureText = "res:ShortCutKeyCtrlEnter", Group = MenuConstants.GROUP_CTX_DBG_BPS_CODE, Order = 10)]
	sealed class GoToSourceNewTabBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly Lazy<IModuleLoader> moduleLoader;
		readonly IDocumentTabService documentTabService;
		readonly IModuleIdProvider moduleIdProvider;

		[ImportingConstructor]
		GoToSourceNewTabBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, Lazy<IModuleLoader> moduleLoader, IDocumentTabService documentTabService, IModuleIdProvider moduleIdProvider)
			: base(breakpointsContent) {
			this.moduleLoader = moduleLoader;
			this.documentTabService = documentTabService;
			this.moduleIdProvider = moduleIdProvider;
		}

		public override void Execute(BreakpointCtxMenuContext context) {
			if (context.SelectedItems.Length == 1)
				GoToSourceBreakpointCtxMenuCommand.GoTo(moduleIdProvider, documentTabService, moduleLoader, context.SelectedItems[0], true);
		}

		public override bool IsEnabled(BreakpointCtxMenuContext context) => context.SelectedItems.Length == 1 && context.SelectedItems[0].Breakpoint is ILCodeBreakpoint;
	}

	//[ExportMenuItem(Header = "res:GoToDisassemblyCommand", Icon = DsImagesAttribute.DisassemblyWindow, Group = MenuConstants.GROUP_CTX_DBG_BPS_CODE, Order = 20)]
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

	//[Export]
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

	//[ExportMenuItem(Header = "res:ShowTokensCommand", Group = MenuConstants.GROUP_CTX_DBG_BPS_OPTS, Order = 0)]
	sealed class ShowTokensBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly BreakpointSettingsImpl breakpointSettings;

		[ImportingConstructor]
		ShowTokensBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, BreakpointSettingsImpl breakpointSettings)
			: base(breakpointsContent) {
			this.breakpointSettings = breakpointSettings;
		}

		public override void Execute(BreakpointCtxMenuContext context) => breakpointSettings.ShowTokens = !breakpointSettings.ShowTokens;
		public override bool IsChecked(BreakpointCtxMenuContext context) => breakpointSettings.ShowTokens;
	}

	//[ExportMenuItem(Header = "res:ShowModuleNamesCommand", Group = MenuConstants.GROUP_CTX_DBG_BPS_OPTS, Order = 10)]
	sealed class ShowModuleNamesBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly BreakpointSettingsImpl breakpointSettings;

		[ImportingConstructor]
		ShowModuleNamesBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, BreakpointSettingsImpl breakpointSettings)
			: base(breakpointsContent) {
			this.breakpointSettings = breakpointSettings;
		}

		public override void Execute(BreakpointCtxMenuContext context) => breakpointSettings.ShowModuleNames = !breakpointSettings.ShowModuleNames;
		public override bool IsChecked(BreakpointCtxMenuContext context) => breakpointSettings.ShowModuleNames;
	}

	//[ExportMenuItem(Header = "res:ShowParameterTypesCommand2", Group = MenuConstants.GROUP_CTX_DBG_BPS_OPTS, Order = 20)]
	sealed class ShowParameterTypesBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly BreakpointSettingsImpl breakpointSettings;

		[ImportingConstructor]
		ShowParameterTypesBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, BreakpointSettingsImpl breakpointSettings)
			: base(breakpointsContent) {
			this.breakpointSettings = breakpointSettings;
		}

		public override void Execute(BreakpointCtxMenuContext context) => breakpointSettings.ShowParameterTypes = !breakpointSettings.ShowParameterTypes;
		public override bool IsChecked(BreakpointCtxMenuContext context) => breakpointSettings.ShowParameterTypes;
	}

	//[ExportMenuItem(Header = "res:ShowParameterNamesCommand", Group = MenuConstants.GROUP_CTX_DBG_BPS_OPTS, Order = 30)]
	sealed class ShowParameterNamesBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly BreakpointSettingsImpl breakpointSettings;

		[ImportingConstructor]
		ShowParameterNamesBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, BreakpointSettingsImpl breakpointSettings)
			: base(breakpointsContent) {
			this.breakpointSettings = breakpointSettings;
		}

		public override void Execute(BreakpointCtxMenuContext context) => breakpointSettings.ShowParameterNames = !breakpointSettings.ShowParameterNames;
		public override bool IsChecked(BreakpointCtxMenuContext context) => breakpointSettings.ShowParameterNames;
	}

	//[ExportMenuItem(Header = "res:ShowOwnerTypesCommand", Group = MenuConstants.GROUP_CTX_DBG_BPS_OPTS, Order = 40)]
	sealed class ShowOwnerTypesBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly BreakpointSettingsImpl breakpointSettings;

		[ImportingConstructor]
		ShowOwnerTypesBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, BreakpointSettingsImpl breakpointSettings)
			: base(breakpointsContent) {
			this.breakpointSettings = breakpointSettings;
		}

		public override void Execute(BreakpointCtxMenuContext context) => breakpointSettings.ShowOwnerTypes = !breakpointSettings.ShowOwnerTypes;
		public override bool IsChecked(BreakpointCtxMenuContext context) => breakpointSettings.ShowOwnerTypes;
	}

	//[ExportMenuItem(Header = "res:ShowNamespacesCommand", Group = MenuConstants.GROUP_CTX_DBG_BPS_OPTS, Order = 50)]
	sealed class ShowNamespacesBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly BreakpointSettingsImpl breakpointSettings;

		[ImportingConstructor]
		ShowNamespacesBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, BreakpointSettingsImpl breakpointSettings)
			: base(breakpointsContent) {
			this.breakpointSettings = breakpointSettings;
		}

		public override void Execute(BreakpointCtxMenuContext context) => breakpointSettings.ShowNamespaces = !breakpointSettings.ShowNamespaces;
		public override bool IsChecked(BreakpointCtxMenuContext context) => breakpointSettings.ShowNamespaces;
	}

	//[ExportMenuItem(Header = "res:ShowReturnTypesCommand", Group = MenuConstants.GROUP_CTX_DBG_BPS_OPTS, Order = 60)]
	sealed class ShowReturnTypesBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly BreakpointSettingsImpl breakpointSettings;

		[ImportingConstructor]
		ShowReturnTypesBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, BreakpointSettingsImpl breakpointSettings)
			: base(breakpointsContent) {
			this.breakpointSettings = breakpointSettings;
		}

		public override void Execute(BreakpointCtxMenuContext context) => breakpointSettings.ShowReturnTypes = !breakpointSettings.ShowReturnTypes;
		public override bool IsChecked(BreakpointCtxMenuContext context) => breakpointSettings.ShowReturnTypes;
	}

	//[ExportMenuItem(Header = "res:ShowTypeKeywordsCommand", Group = MenuConstants.GROUP_CTX_DBG_BPS_OPTS, Order = 70)]
	sealed class ShowTypeKeywordsBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		readonly BreakpointSettingsImpl breakpointSettings;

		[ImportingConstructor]
		ShowTypeKeywordsBreakpointCtxMenuCommand(Lazy<IBreakpointsContent> breakpointsContent, BreakpointSettingsImpl breakpointSettings)
			: base(breakpointsContent) {
			this.breakpointSettings = breakpointSettings;
		}

		public override void Execute(BreakpointCtxMenuContext context) => breakpointSettings.ShowTypeKeywords = !breakpointSettings.ShowTypeKeywords;
		public override bool IsChecked(BreakpointCtxMenuContext context) => breakpointSettings.ShowTypeKeywords;
	}
}

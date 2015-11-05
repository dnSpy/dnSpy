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
using System.Linq;
using System.Windows;
using dnSpy.Contracts.Menus;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.Menus;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger.Breakpoints {
	sealed class BreakpointCtxMenuContext {
		public readonly BreakpointsVM VM;
		public readonly BreakpointVM[] SelectedItems;

		public BreakpointCtxMenuContext(BreakpointsVM vm, BreakpointVM[] selItems) {
			this.VM = vm;
			this.SelectedItems = selItems;
		}
	}

	sealed class BreakpointCtxMenuCommandProxy : MenuItemCommandProxy<BreakpointCtxMenuContext> {
		public BreakpointCtxMenuCommandProxy(BreakpointCtxMenuCommand cmd)
			: base(cmd) {
		}

		protected override BreakpointCtxMenuContext CreateContext() {
			return BreakpointCtxMenuCommand.Create();
		}
	}

	abstract class BreakpointCtxMenuCommand : MenuItemBase<BreakpointCtxMenuContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected sealed override BreakpointCtxMenuContext CreateContext(IMenuItemContext context) {
			var ui = BreakpointsControlCreator.BreakpointsControlInstance;
			if (context.CreatorObject.Object != ui.listView)
				return null;
			return Create();
		}

		internal static BreakpointCtxMenuContext Create() {
			var ui = BreakpointsControlCreator.BreakpointsControlInstance;
			var vm = ui.DataContext as BreakpointsVM;
			if (vm == null)
				return null;

			var dict = new Dictionary<object, int>(ui.listView.Items.Count);
			for (int i = 0; i < ui.listView.Items.Count; i++)
				dict[ui.listView.Items[i]] = i;
			var elems = ui.listView.SelectedItems.OfType<BreakpointVM>().ToArray();
			Array.Sort(elems, (a, b) => dict[a].CompareTo(dict[b]));

			return new BreakpointCtxMenuContext(vm, elems);
		}
	}

	[ExportMenuItem(Header = "Cop_y", Icon = "Copy", InputGestureText = "Ctrl+C", Group = MenuConstants.GROUP_CTX_DBG_BPS_COPY, Order = 0)]
	sealed class CopyBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		public override void Execute(BreakpointCtxMenuContext context) {
			var output = new PlainTextOutput();
			foreach (var vm in context.SelectedItems) {
				var printer = new BreakpointPrinter(output, DebuggerSettings.Instance.UseHexadecimal);
				printer.WriteName(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteAssembly(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteModule(vm);
				output.Write('\t', TextTokenType.Text);
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
		public override void Execute(BreakpointCtxMenuContext context) {
			BreakpointsControlCreator.BreakpointsControlInstance.listView.SelectAll();
		}

		public override bool IsEnabled(BreakpointCtxMenuContext context) {
			return BreakpointsControlCreator.BreakpointsControlInstance.listView.Items.Count > 0;
		}
	}

	[ExportMenuItem(Header = "_Delete", Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_DBG_BPS_COPY, Order = 20)]
	sealed class DeleteBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		public override void Execute(BreakpointCtxMenuContext context) {
			context.VM.Remove(context.SelectedItems);
		}
	}

	[ExportMenuItem(Header = "Delete _All Breakpoints", Icon = "DeleteAllBreakpoints", InputGestureText = "Ctrl+Shift+F9", Group = MenuConstants.GROUP_CTX_DBG_BPS_COPY, Order = 30)]
	sealed class DeleteAllBPsBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		public override void Execute(BreakpointCtxMenuContext context) {
			DebugRoutedCommands.DeleteAllBreakpoints.Execute(null, MainWindow.Instance);
		}

		public override bool IsEnabled(BreakpointCtxMenuContext context) {
			return DebugRoutedCommands.DeleteAllBreakpoints.CanExecute(null, MainWindow.Instance);
		}
	}

	[ExportMenuItem(Header = "Enable All Breakpoi_nts", Icon = "EnableAllBreakpoints", Group = MenuConstants.GROUP_CTX_DBG_BPS_COPY, Order = 40)]
	sealed class EnableAllBPsBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		public override void Execute(BreakpointCtxMenuContext context) {
			DebugRoutedCommands.EnableAllBreakpoints.Execute(null, MainWindow.Instance);
		}

		public override bool IsVisible(BreakpointCtxMenuContext context) {
			return DebugRoutedCommands.EnableAllBreakpoints.CanExecute(null, MainWindow.Instance);
		}
	}

	[ExportMenuItem(Header = "Disable All Breakpoi_nts", Icon = "DisableAllBreakpoints", Group = MenuConstants.GROUP_CTX_DBG_BPS_COPY, Order = 50)]
	sealed class DisableAllBPsBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		public override void Execute(BreakpointCtxMenuContext context) {
			DebugRoutedCommands.DisableAllBreakpoints.Execute(null, MainWindow.Instance);
		}

		public override bool IsVisible(BreakpointCtxMenuContext context) {
			return DebugRoutedCommands.DisableAllBreakpoints.CanExecute(null, MainWindow.Instance);
		}
	}

	[ExportMenuItem(Header = "_Go To Code", Icon = "GoToSourceCode", InputGestureText = "Enter", Group = MenuConstants.GROUP_CTX_DBG_BPS_CODE, Order = 0)]
	sealed class GoToSourceBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		public override void Execute(BreakpointCtxMenuContext context) {
			if (context.SelectedItems.Length == 1)
				GoTo(context.SelectedItems[0], false);
		}

		internal static void GoTo(BreakpointVM vm, bool newTab) {
			if (vm == null)
				return;
			var ilbp = vm.Breakpoint as ILCodeBreakpoint;
			if (ilbp == null)
				return;
			DebugUtils.GoToIL(ilbp.SerializedDnSpyToken.Module, ilbp.SerializedDnSpyToken.Token, ilbp.ILOffset, newTab);
		}

		public override bool IsEnabled(BreakpointCtxMenuContext context) {
			return context.SelectedItems.Length == 1 && context.SelectedItems[0].Breakpoint is ILCodeBreakpoint;
		}
	}

	[ExportMenuItem(Header = "Go To Code (New _Tab)", Icon = "GoToSourceCode", InputGestureText = "Ctrl+Enter", Group = MenuConstants.GROUP_CTX_DBG_BPS_CODE, Order = 10)]
	sealed class GoToSourceNewTabBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		public override void Execute(BreakpointCtxMenuContext context) {
			if (context.SelectedItems.Length == 1)
				GoToSourceBreakpointCtxMenuCommand.GoTo(context.SelectedItems[0], true);
		}

		public override bool IsEnabled(BreakpointCtxMenuContext context) {
			return context.SelectedItems.Length == 1 && context.SelectedItems[0].Breakpoint is ILCodeBreakpoint;
		}
	}

	[ExportMenuItem(Header = "Go To Disassembly", Icon = "DisassemblyWindow", Group = MenuConstants.GROUP_CTX_DBG_BPS_CODE, Order = 20)]
	sealed class GoToDisassemblyBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		public override void Execute(BreakpointCtxMenuContext context) {
			//TODO:
		}

		public override bool IsEnabled(BreakpointCtxMenuContext context) {
			return false;//TODO:
		}
	}

	sealed class ToggleEnableBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		public override void Execute(BreakpointCtxMenuContext context) {
			foreach (var bp in context.SelectedItems)
				bp.IsEnabled = !bp.IsEnabled;
		}
	}

	[ExportMenuItem(Header = "Show Tokens", Group = MenuConstants.GROUP_CTX_DBG_BPS_OPTS, Order = 0)]
	sealed class ShowTokensBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		public override void Execute(BreakpointCtxMenuContext context) {
			BreakpointSettings.Instance.ShowTokens = !BreakpointSettings.Instance.ShowTokens;
		}

		public override bool IsChecked(BreakpointCtxMenuContext context) {
			return BreakpointSettings.Instance.ShowTokens;
		}
	}
}

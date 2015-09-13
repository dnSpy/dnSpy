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

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using dnSpy.MVVM;
using dnSpy.NRefactory;
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

	sealed class BreakpointCtxMenuCommandProxy : ContextMenuEntryCommandProxy {
		public BreakpointCtxMenuCommandProxy(BreakpointCtxMenuCommand cmd)
			: base(cmd) {
		}

		protected override ContextMenuEntryContext CreateContext() {
			return ContextMenuEntryContext.Create(BreakpointsControlCreator.BreakpointsControlInstance.listView);
		}
	}

	abstract class BreakpointCtxMenuCommand : ContextMenuEntryBase<BreakpointCtxMenuContext> {
		protected override BreakpointCtxMenuContext CreateContext(ContextMenuEntryContext context) {
			var ui = BreakpointsControlCreator.BreakpointsControlInstance;
			if (context.Element != ui.listView)
				return null;
			var vm = ui.DataContext as BreakpointsVM;
			if (vm == null)
				return null;

			var elems = ui.listView.SelectedItems.OfType<BreakpointVM>().ToArray();

			return new BreakpointCtxMenuContext(vm, elems);
		}
	}

	[ExportContextMenuEntry(Header = "Cop_y", Order = 100, Category = "CopyBP", Icon = "Copy", InputGestureText = "Ctrl+C")]
	sealed class CopyBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		protected override void Execute(BreakpointCtxMenuContext context) {
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

		protected override bool IsEnabled(BreakpointCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[ExportContextMenuEntry(Header = "Select _All", Order = 110, Category = "CopyBP", Icon = "Select", InputGestureText = "Ctrl+A")]
	sealed class SelectAllBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		protected override void Execute(BreakpointCtxMenuContext context) {
			BreakpointsControlCreator.BreakpointsControlInstance.listView.SelectAll();
		}
	}

	[ExportContextMenuEntry(Header = "_Delete", Order = 120, Category = "CopyBP", Icon = "Delete", InputGestureText = "Del")]
	sealed class DeleteBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		protected override void Execute(BreakpointCtxMenuContext context) {
			context.VM.Remove(context.SelectedItems);
		}
	}

	[ExportContextMenuEntry(Header = "Delete _All Breakpoints", Order = 130, Category = "CopyBP", Icon = "DeleteAllBreakpoints", InputGestureText = "Ctrl+Shift+F9")]
	sealed class DeleteAllBPsBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		protected override void Execute(BreakpointCtxMenuContext context) {
			DebugRoutedCommands.DeleteAllBreakpoints.Execute(null, MainWindow.Instance);
		}

		protected override bool IsEnabled(BreakpointCtxMenuContext context) {
			return DebugRoutedCommands.DeleteAllBreakpoints.CanExecute(null, MainWindow.Instance);
		}
	}

	[ExportContextMenuEntry(Header = "Enable All Breakpoi_nts", Order = 140, Category = "CopyBP")]
	sealed class EnableAllBPsBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		protected override void Execute(BreakpointCtxMenuContext context) {
			DebugRoutedCommands.EnableAllBreakpoints.Execute(null, MainWindow.Instance);
		}

		protected override bool IsVisible(BreakpointCtxMenuContext context) {
			return DebugRoutedCommands.EnableAllBreakpoints.CanExecute(null, MainWindow.Instance);
		}
	}

	[ExportContextMenuEntry(Header = "Disable All Breakpoi_nts", Order = 150, Category = "CopyBP")]
	sealed class DisableAllBPsBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		protected override void Execute(BreakpointCtxMenuContext context) {
			DebugRoutedCommands.DisableAllBreakpoints.Execute(null, MainWindow.Instance);
		}

		protected override bool IsVisible(BreakpointCtxMenuContext context) {
			return DebugRoutedCommands.DisableAllBreakpoints.CanExecute(null, MainWindow.Instance);
		}
	}

	[ExportContextMenuEntry(Header = "_Go To Code", Order = 200, Category = "CodeBP", Icon = "GoToSourceCode", InputGestureText = "Enter")]
	sealed class GoToSourceBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		protected override void Execute(BreakpointCtxMenuContext context) {
			if (context.SelectedItems.Length == 1)
				GoTo(context.SelectedItems[0], false);
		}

		internal static void GoTo(BreakpointVM vm, bool newTab) {
			if (vm == null)
				return;
			var ilbp = vm.Breakpoint as ILCodeBreakpoint;
			if (ilbp == null)
				return;
			DebugUtils.GoToIL(ilbp.Assembly, ilbp.MethodKey, ilbp.ILOffset, newTab);
		}

		protected override bool IsEnabled(BreakpointCtxMenuContext context) {
			return context.SelectedItems.Length == 1 && context.SelectedItems[0].Breakpoint is ILCodeBreakpoint;
		}
	}

	[ExportContextMenuEntry(Header = "Go To Code (New _Tab)", Order = 210, Category = "CodeBP", Icon = "GoToSourceCode", InputGestureText = "Ctrl+Enter")]
	sealed class GoToSourceNewTabBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		protected override void Execute(BreakpointCtxMenuContext context) {
			if (context.SelectedItems.Length == 1)
				GoToSourceBreakpointCtxMenuCommand.GoTo(context.SelectedItems[0], true);
		}

		protected override bool IsEnabled(BreakpointCtxMenuContext context) {
			return context.SelectedItems.Length == 1 && context.SelectedItems[0].Breakpoint is ILCodeBreakpoint;
		}
	}

	[ExportContextMenuEntry(Header = "Go To Disassembly", Order = 220, Category = "CodeBP", Icon = "DisassemblyWindow")]
	sealed class GoToDisassemblyBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		protected override void Execute(BreakpointCtxMenuContext context) {
			//TODO:
		}

		protected override bool IsVisible(BreakpointCtxMenuContext context) {
			return false;//TODO:
		}

		protected override bool IsEnabled(BreakpointCtxMenuContext context) {
			return context.SelectedItems.Length == 1;
		}
	}

	sealed class ToggleEnableBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		protected override void Execute(BreakpointCtxMenuContext context) {
			foreach (var bp in context.SelectedItems)
				bp.IsEnabled = !bp.IsEnabled;
		}
	}

	[ExportContextMenuEntry(Header = "Show Tokens", Order = 600, Category = "BPNameOptions")]
	sealed class ShowTokensBreakpointCtxMenuCommand : BreakpointCtxMenuCommand {
		protected override void Execute(BreakpointCtxMenuContext context) {
			BreakpointSettings.Instance.ShowTokens = !BreakpointSettings.Instance.ShowTokens;
		}

		protected override void Initialize(BreakpointCtxMenuContext context, MenuItem menuItem) {
			menuItem.IsChecked = BreakpointSettings.Instance.ShowTokens;
		}
	}
}

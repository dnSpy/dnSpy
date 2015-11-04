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
using dnSpy.Contracts.Menus;
using dnSpy.MVVM;

namespace dnSpy.Debugger.CallStack {
	sealed class CallStackCtxMenuContext {
		public readonly CallStackVM VM;
		public readonly ICallStackFrameVM[] SelectedItems;

		public CallStackCtxMenuContext(CallStackVM vm, ICallStackFrameVM[] selItems) {
			this.VM = vm;
			this.SelectedItems = selItems;
		}
	}

	sealed class CallStackCtxMenuCommandProxy : MenuItemCommandProxy<CallStackCtxMenuContext> {
		public CallStackCtxMenuCommandProxy(CallStackCtxMenuCommand cmd)
			: base(cmd) {
		}

		protected override CallStackCtxMenuContext CreateContext() {
			return CallStackCtxMenuCommand.Create();
		}
	}

	abstract class CallStackCtxMenuCommand : MenuItemBase<CallStackCtxMenuContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected sealed override CallStackCtxMenuContext CreateContext(IMenuItemContext context) {
			if (DebugManager.Instance.ProcessState != DebuggerProcessState.Stopped)
				return null;
			var ui = CallStackControlCreator.CallStackControlInstance;
			if (context.CreatorObject.Object != ui.listView)
				return null;
			return Create();
		}

		internal static CallStackCtxMenuContext Create() {
			var ui = CallStackControlCreator.CallStackControlInstance;
			var vm = ui.DataContext as CallStackVM;
			if (vm == null)
				return null;

			var elems = ui.listView.SelectedItems.OfType<ICallStackFrameVM>().ToArray();
			Array.Sort(elems, (a, b) => a.Index.CompareTo(b.Index));

			return new CallStackCtxMenuContext(vm, elems);
		}
	}

	[ExportMenuItem(Header = "Cop_y", Icon = "Copy", InputGestureText = "Ctrl+C", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_COPY, Order = 0)]
	sealed class CopyCallStackCtxMenuCommand : CallStackCtxMenuCommand {
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
		public override void Execute(CallStackCtxMenuContext context) {
			CallStackControlCreator.CallStackControlInstance.listView.SelectAll();
		}

		public override bool IsEnabled(CallStackCtxMenuContext context) {
			return CallStackControlCreator.CallStackControlInstance.listView.Items.Count > 0;
		}
	}

	[ExportMenuItem(Header = "_Switch To Frame", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 0)]
	sealed class SwitchToFrameCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		internal static CallStackFrameVM GetFrame(CallStackCtxMenuContext context) {
			if (context.SelectedItems.Length != 1)
				return null;
			return context.SelectedItems[0] as CallStackFrameVM;
		}

		public override void Execute(CallStackCtxMenuContext context) {
			Execute(GetFrame(context), false);
		}

		internal static void Execute(CallStackFrameVM vm, bool newTab) {
			if (vm != null) {
				StackFrameManager.Instance.SelectedFrameNumber = vm.Index;
				FrameUtils.GoTo(vm.Frame, newTab);
			}
		}

		public override bool IsEnabled(CallStackCtxMenuContext context) {
			return GetFrame(context) != null;
		}
	}

	[ExportMenuItem(Header = "_Go To Code", Icon = "GoToSourceCode", InputGestureText = "Enter", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 10)]
	sealed class GoToSourceCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		public override void Execute(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			if (vm != null)
				FrameUtils.GoToIL(vm.Frame, false);
		}

		public override bool IsEnabled(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			return vm != null && FrameUtils.CanGoToIL(vm.Frame);
		}
	}

	[ExportMenuItem(Header = "Go To Code (New _Tab)", Icon = "GoToSourceCode", InputGestureText = "Ctrl+Enter", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 20)]
	sealed class GoToSourceNewTabCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		public override void Execute(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			if (vm != null)
				FrameUtils.GoToIL(vm.Frame, true);
		}

		public override bool IsEnabled(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			return vm != null && FrameUtils.CanGoToIL(vm.Frame);
		}
	}

	[ExportMenuItem(Header = "Go To _Disassembly", Icon = "DisassemblyWindow", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 30)]
	sealed class GoToDisassemblyCallStackCtxMenuCommand : CallStackCtxMenuCommand {
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

	[ExportMenuItem(Header = "Ru_n To Cursor", Icon = "Cursor", InputGestureText = "Ctrl+F10", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_FRAME, Order = 40)]
	sealed class RunToCursorCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		public override void Execute(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			if (vm != null)
				DebugManager.Instance.RunTo(vm.Frame);
		}

		public override bool IsEnabled(CallStackCtxMenuContext context) {
			var vm = SwitchToFrameCallStackCtxMenuCommand.GetFrame(context);
			return vm != null && DebugManager.Instance.CanRunTo(vm.Frame);
		}
	}

	[ExportMenuItem(Header = "_Hexadecimal Display", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_HEXOPTS, Order = 0)]
	sealed class HexadecimalDisplayCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		public override void Execute(CallStackCtxMenuContext context) {
			DebuggerSettings.Instance.UseHexadecimal = !DebuggerSettings.Instance.UseHexadecimal;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return DebuggerSettings.Instance.UseHexadecimal;
		}
	}

	[ExportMenuItem(Header = "Show _Module Names", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 0)]
	sealed class ShowModuleNamesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		public override void Execute(CallStackCtxMenuContext context) {
			CallStackSettings.Instance.ShowModuleNames = !CallStackSettings.Instance.ShowModuleNames;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return CallStackSettings.Instance.ShowModuleNames;
		}
	}

	[ExportMenuItem(Header = "Show Parameter _Types", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 10)]
	sealed class ShowParameterTypesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		public override void Execute(CallStackCtxMenuContext context) {
			CallStackSettings.Instance.ShowParameterTypes = !CallStackSettings.Instance.ShowParameterTypes;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return CallStackSettings.Instance.ShowParameterTypes;
		}
	}

	[ExportMenuItem(Header = "Show _Parameter Names", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 20)]
	sealed class ShowParameterNamesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		public override void Execute(CallStackCtxMenuContext context) {
			CallStackSettings.Instance.ShowParameterNames = !CallStackSettings.Instance.ShowParameterNames;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return CallStackSettings.Instance.ShowParameterNames;
		}
	}

	[ExportMenuItem(Header = "Show Parameter _Values", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 30)]
	sealed class ShowParameterValuesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		public override void Execute(CallStackCtxMenuContext context) {
			CallStackSettings.Instance.ShowParameterValues = !CallStackSettings.Instance.ShowParameterValues;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return CallStackSettings.Instance.ShowParameterValues;
		}
	}

	[ExportMenuItem(Header = "Show IP", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 40)]
	sealed class ShowIPCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		public override void Execute(CallStackCtxMenuContext context) {
			CallStackSettings.Instance.ShowIP = !CallStackSettings.Instance.ShowIP;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return CallStackSettings.Instance.ShowIP;
		}
	}

	[ExportMenuItem(Header = "Show Owner Types", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 50)]
	sealed class ShowOwnerTypesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		public override void Execute(CallStackCtxMenuContext context) {
			CallStackSettings.Instance.ShowOwnerTypes = !CallStackSettings.Instance.ShowOwnerTypes;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return CallStackSettings.Instance.ShowOwnerTypes;
		}
	}

	[ExportMenuItem(Header = "Show Namespaces", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 60)]
	sealed class ShowNamespacesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		public override void Execute(CallStackCtxMenuContext context) {
			CallStackSettings.Instance.ShowNamespaces = !CallStackSettings.Instance.ShowNamespaces;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return CallStackSettings.Instance.ShowNamespaces;
		}
	}

	[ExportMenuItem(Header = "Show Return Types", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 70)]
	sealed class ShowReturnTypesCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		public override void Execute(CallStackCtxMenuContext context) {
			CallStackSettings.Instance.ShowReturnTypes = !CallStackSettings.Instance.ShowReturnTypes;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return CallStackSettings.Instance.ShowReturnTypes;
		}
	}

	[ExportMenuItem(Header = "Show Type Keywords", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 80)]
	sealed class ShowTypeKeywordsCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		public override void Execute(CallStackCtxMenuContext context) {
			CallStackSettings.Instance.ShowTypeKeywords = !CallStackSettings.Instance.ShowTypeKeywords;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return CallStackSettings.Instance.ShowTypeKeywords;
		}
	}

	[ExportMenuItem(Header = "Show Tokens", Group = MenuConstants.GROUP_CTX_DBG_CALLSTACK_OPTS, Order = 90)]
	sealed class ShowTokensCallStackCtxMenuCommand : CallStackCtxMenuCommand {
		public override void Execute(CallStackCtxMenuContext context) {
			CallStackSettings.Instance.ShowTokens = !CallStackSettings.Instance.ShowTokens;
		}

		public override bool IsChecked(CallStackCtxMenuContext context) {
			return CallStackSettings.Instance.ShowTokens;
		}
	}
}

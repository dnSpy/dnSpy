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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Menus;
using dnSpy.Debugger.Memory;
using dnSpy.MVVM;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.Menus;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger.Locals {
	sealed class LocalsCtxMenuContext {
		public readonly LocalsVM VM;
		public readonly ValueVM[] SelectedItems;

		public LocalsCtxMenuContext(LocalsVM vm, ValueVM[] selItems) {
			this.VM = vm;
			this.SelectedItems = selItems;
		}
	}

	sealed class LocalsCtxMenuCommandProxy : MenuItemCommandProxy<LocalsCtxMenuContext> {
		public LocalsCtxMenuCommandProxy(LocalsCtxMenuCommand cmd)
			: base(cmd) {
		}

		protected override LocalsCtxMenuContext CreateContext() {
			return LocalsCtxMenuCommand.Create();
		}
	}

	abstract class LocalsCtxMenuCommand : MenuItemBase<LocalsCtxMenuContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected sealed override LocalsCtxMenuContext CreateContext(IMenuItemContext context) {
			if (DebugManager.Instance.ProcessState != DebuggerProcessState.Stopped)
				return null;
			var ui = LocalsControlCreator.LocalsControlInstance;
			if (context.CreatorObject.Object != ui.treeView)
				return null;
			return Create();
		}

		internal static LocalsCtxMenuContext Create() {
			var ui = LocalsControlCreator.LocalsControlInstance;
			var vm = ui.DataContext as LocalsVM;
			if (vm == null)
				return null;

			var dict = new Dictionary<object, int>(ui.treeView.Items.Count);
			for (int i = 0; i < ui.treeView.Items.Count; i++)
				dict[ui.treeView.Items[i]] = i;
			var elems = ui.treeView.SelectedItems.OfType<ValueVM>().ToArray();
			Array.Sort(elems, (a, b) => dict[a].CompareTo(dict[b]));

			return new LocalsCtxMenuContext(vm, elems);
		}
	}

	sealed class ToggleCollapsedLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		public override void Execute(LocalsCtxMenuContext context) {
			var vm = GetValueVM(context);
			if (vm != null)
				vm.IsExpanded = !vm.IsExpanded;
		}

		static ValueVM GetValueVM(LocalsCtxMenuContext context) {
			if (context.SelectedItems.Length != 1)
				return null;
			var vm = context.SelectedItems[0];
			if (vm.LazyLoading)
				return vm;
			if (vm.Children.Count > 0)
				return vm;
			return null;
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) {
			return GetValueVM(context) != null;
		}
	}

	[ExportMenuItem(Header = "Cop_y", Icon = "Copy", InputGestureText = "Ctrl+C", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_COPY, Order = 0)]
	sealed class CopyLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		public override void Execute(LocalsCtxMenuContext context) {
			var output = new PlainTextOutput();
			foreach (var vm in context.SelectedItems) {
				//TODO: Break if it takes too long and the user cancels
				var printer = new ValuePrinter(output, DebuggerSettings.Instance.UseHexadecimal);
				printer.WriteExpander(vm);
				output.Write('\t', TextTokenType.Text);
				// Add an extra here to emulate VS output
				output.Write('\t', TextTokenType.Text);
				printer.WriteName(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteValue(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteType(vm);
				output.WriteLine();
			}
			var s = output.ToString();
			if (s.Length > 0)
				Clipboard.SetText(s);
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[ExportMenuItem(Header = "Select _All", Icon = "Select", InputGestureText = "Ctrl+A", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_COPY, Order = 10)]
	sealed class SelectAllLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		public override void Execute(LocalsCtxMenuContext context) {
			LocalsControlCreator.LocalsControlInstance.treeView.SelectAll();
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) {
			return LocalsControlCreator.LocalsControlInstance.treeView.Items.Count > 0;
		}
	}

	[ExportMenuItem(Header = "_Edit Value", InputGestureText = "F2", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_VALUES, Order = 0)]
	sealed class EditValueLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		public override void Execute(LocalsCtxMenuContext context) {
			if (IsEnabled(context))
				context.SelectedItems[0].IsEditingValue = true;
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) {
			return context.SelectedItems.Length == 1 &&
				context.SelectedItems[0].CanEdit;
		}
	}

	[ExportMenuItem(Header = "Copy Va_lue", InputGestureText = "Ctrl+Shift+C", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_VALUES, Order = 10)]
	sealed class CopyValueLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		public override void Execute(LocalsCtxMenuContext context) {
			var output = new PlainTextOutput();
			foreach (var vm in context.SelectedItems) {
				//TODO: Break if it takes too long and the user cancels
				var printer = new ValuePrinter(output, DebuggerSettings.Instance.UseHexadecimal);
				printer.WriteValue(vm);
				if (context.SelectedItems.Length > 1)
					output.WriteLine();
			}
			var s = output.ToString();
			if (s.Length > 0)
				Clipboard.SetText(s);
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[ExportMenuItem(Header = "Add _Watch", Icon = "Watch", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_VALUES, Order = 20)]
	sealed class AddWatchLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		public override void Execute(LocalsCtxMenuContext context) {
			//TODO:
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) {
			return false;//TODO:
		}
	}

	[ExportMenuItem(Header = "_Save...", Icon = "Save", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_VALUES, Order = 30)]
	sealed class SaveDataLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		public override void Execute(LocalsCtxMenuContext context) {
			var value = GetValue(context);
			if (value == null)
				return;

			var filename = new PickSaveFilename().GetFilename(string.Empty, "bin", null);
			if (string.IsNullOrEmpty(filename))
				return;

			byte[] data;
			int? dataIndex = null, dataSize = null;
			if (value.IsString) {
				var s = value.String;
				data = s == null ? null : Encoding.Unicode.GetBytes(s);
			}
			else if (value.IsArray) {
				if (value.ArrayCount == 0)
					data = new byte[0];
				else {
					var elemValue = value.GetElementAtPosition(0);
					ulong elemSize = elemValue == null ? 0 : elemValue.Size;
					ulong elemAddr = elemValue == null ? 0 : elemValue.Address;
					ulong addr = value.Address;
					ulong totalSize = elemSize * value.ArrayCount;
					if (elemAddr == 0 || elemAddr < addr || elemAddr - addr > int.MaxValue || totalSize > int.MaxValue) {
						MainWindow.Instance.ShowMessageBox("Could not get array data");
						return;
					}
					data = value.ReadGenericValue();
					dataIndex = (int)(elemAddr - addr);
					dataSize = (int)totalSize;
				}
			}
			else
				data = value.ReadGenericValue();
			if (data == null) {
				MainWindow.Instance.ShowMessageBox("Could not read any data");
				return;
			}

			try {
				if (dataIndex == null)
					dataIndex = 0;
				if (dataSize == null)
					dataSize = data.Length - dataIndex.Value;
				using (var file = File.Create(filename))
					file.Write(data, dataIndex.Value, dataSize.Value);
			}
			catch (Exception ex) {
				MainWindow.Instance.ShowMessageBox(string.Format("Error saving data to '{0}'\nERROR: {1}", filename, ex.Message));
				return;
			}
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) {
			return GetValue(context) != null;
		}

		internal static CorValue GetValue(LocalsCtxMenuContext context) {
			if (context.SelectedItems.Length != 1)
				return null;
			var nv = context.SelectedItems[0] as NormalValueVM;
			var value = nv == null ? null : nv.ReadOnlyCorValue;
			if (value == null)
				return null;
			for (int i = 0; i < 2; i++) {
				if (!value.IsReference)
					break;
				if (value.IsNull)
					return null;
				if (value.Type == CorElementType.Ptr || value.Type == CorElementType.FnPtr)
					return null;
				value = value.NeuterCheckDereferencedValue;
				if (value == null)
					return null;
			}
			if (value.IsReference)
				return null;
			if (value.IsBox) {
				value = value.BoxedValue;
				if (value == null)
					return null;
			}
			return value;
		}
	}

	static class Constants {
		public const string SHOW_IN_MEMORY_WINDOW_GUID = "10E1F865-8531-486F-86E2-071FB1B9E1B1";
		public const string GROUP_SHOW_IN_MEMORY_WINDOW = "0,CFAF7CC1-2289-436D-8EB6-C5F6E32DE253";
	}

	sealed class SimpleMenuItem : MenuItemBase {
		readonly Action<IMenuItemContext> action;

		public SimpleMenuItem(Action<IMenuItemContext> action) {
			this.action = action;
		}

		public override void Execute(IMenuItemContext context) {
			action(context);
		}
	}

	[ExportMenuItem(Header = "Show in Memory Window", Icon = "MemoryWindow", Guid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = MenuConstants.GROUP_CTX_DBG_LOCALS_VALUES, Order = 40)]
	sealed class ShowInMemoryXLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		public override void Execute(LocalsCtxMenuContext context) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = Constants.GROUP_SHOW_IN_MEMORY_WINDOW, Order = 0)]
	sealed class ShowInMemoryXLocalsSubCtxMenuCommand : LocalsCtxMenuCommand, IMenuItemCreator {
		public override void Execute(LocalsCtxMenuContext context) {
		}

		static ShowInMemoryXLocalsSubCtxMenuCommand() {
			subCmds = new Tuple<IMenuItem, string, string>[MemoryControlCreator.NUMBER_OF_MEMORY_WINDOWS];
			for (int i = 0; i < subCmds.Length; i++)
				subCmds[i] = Tuple.Create((IMenuItem)new ShowInMemoryWindowLocalsCtxMenuCommand(i + 1), MemoryControlCreator.GetHeaderText(i), MemoryControlCreator.GetCtrlInputGestureText(i));
		}

		static readonly Tuple<IMenuItem, string, string>[] subCmds;

		public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
			var ctx = CreateContext(context);
			Debug.Assert(ctx != null);
			if (ctx == null)
				yield break;

			for (int i = 0; i < subCmds.Length; i++) {
				var info = subCmds[i];
				var attr = new ExportMenuItemAttribute { Header = info.Item2, Icon = "MemoryWindow" };
				if (!string.IsNullOrEmpty(info.Item3))
					attr.InputGestureText = info.Item3;
				yield return new CreatedMenuItem(attr, info.Item1);
			}
		}
	}

	sealed class ShowInMemoryLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		public override void Execute(LocalsCtxMenuContext context) {
			var addrRange = ShowInMemoryWindowLocalsCtxMenuCommand.GetValue(context);
			if (addrRange != null)
				MemoryUtils.ShowInMemoryWindow(addrRange.Value.Address, addrRange.Value.Size);
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) {
			return ShowInMemoryWindowLocalsCtxMenuCommand.GetValue(context) != null;
		}
	}

	sealed class ShowInMemoryWindowLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		readonly int windowNumber;

		internal struct AddrRange {
			public ulong Address;
			public ulong Size;
			public AddrRange(ulong addr, ulong size) {
				this.Address = addr;
				this.Size = size;
			}
		}

		public ShowInMemoryWindowLocalsCtxMenuCommand(int windowNumber) {
			this.windowNumber = windowNumber;
		}

		public override void Execute(LocalsCtxMenuContext context) {
			var addrRange = GetValue(context);
			if (addrRange != null)
				MemoryUtils.ShowInMemoryWindow(windowNumber, addrRange.Value.Address, addrRange.Value.Size);
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) {
			return GetValue(context) != null;
		}

		internal static AddrRange? GetValue(LocalsCtxMenuContext context) {
			var value = SaveDataLocalsCtxMenuCommand.GetValue(context);

			if (value == null)
				return null;

			if (value.IsArray) {
				if (value.ArrayCount == 0)
					return new AddrRange(value.Address, 0);

				var elemValue = value.GetElementAtPosition(0);
				ulong elemSize = elemValue == null ? 0 : elemValue.Size;
				ulong elemAddr = elemValue == null ? 0 : elemValue.Address;
				ulong addr = value.Address;
				ulong totalSize = elemSize * value.ArrayCount;
				if (elemAddr == 0 || elemAddr < addr || elemAddr - addr > int.MaxValue || totalSize > int.MaxValue)
					return null;

				ulong dataIndex = elemAddr - addr;
				return new AddrRange(value.Address + dataIndex, totalSize);
			}

			return new AddrRange(value.Address, value.Size);
		}
	}

	[ExportMenuItem(Header = "_Hexadecimal Display", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_HEXOPTS, Order = 0)]
	sealed class HexadecimalDisplayLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		public override void Execute(LocalsCtxMenuContext context) {
			DebuggerSettings.Instance.UseHexadecimal = !DebuggerSettings.Instance.UseHexadecimal;
		}

		public override bool IsChecked(LocalsCtxMenuContext context) {
			return DebuggerSettings.Instance.UseHexadecimal;
		}
	}

	[ExportMenuItem(Header = "C_ollapse Parent", Icon = "OneLevelUp", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_TREE, Order = 0)]
	sealed class CollapseParentLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		public override void Execute(LocalsCtxMenuContext context) {
			var vm = GetLocalParent(context);
			if (vm != null)
				vm.IsExpanded = false;
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) {
			var p = GetLocalParent(context);
			return p != null && p.IsExpanded;
		}

		static ValueVM GetLocalParent(LocalsCtxMenuContext context) {
			if (context.SelectedItems.Length == 0)
				return null;
			return context.SelectedItems[0].Parent as ValueVM;
		}
	}

	[ExportMenuItem(Header = "E_xpand Children", Icon = "SuperTypesOpen", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_TREE, Order = 10)]
	sealed class ExpandChildrenLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		public override void Execute(LocalsCtxMenuContext context) {
			var vm = GetLocalParent(context);
			if (vm != null) {
				vm.IsExpanded = true;
				foreach (ValueVM child in vm.Children) {
					if (CanExpand(child))
						child.IsExpanded = true;
				}
			}
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) {
			var p = GetLocalParent(context);
			if (p == null)
				return false;
			if (!p.IsExpanded)
				return true;
			return p.Children.Any(c => CanExpand((ValueVM)c));
		}

		static bool CanExpand(ValueVM vm) {
			return vm != null && !vm.IsExpanded && (vm.LazyLoading || vm.Children.Count > 0);
		}

		static ValueVM GetLocalParent(LocalsCtxMenuContext context) {
			if (context.SelectedItems.Length == 0)
				return null;
			var vm = context.SelectedItems[0];
			if (vm.LazyLoading)
				return vm;
			return vm.Children.Count == 0 ? null : vm;
		}
	}

	[ExportMenuItem(Header = "_Collapse Children", Icon = "SuperTypes", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_TREE, Order = 20)]
	sealed class CollapseChildrenLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		public override void Execute(LocalsCtxMenuContext context) {
			var vm = GetLocalParent(context);
			if (vm != null) {
				foreach (var child in vm.Children)
					child.IsExpanded = false;
			}
		}

		public override bool IsEnabled(LocalsCtxMenuContext context) {
			var p = GetLocalParent(context);
			if (p == null)
				return false;
			if (!p.IsExpanded)
				return false;
			return p.Children.Any(c => c.IsExpanded);
		}

		static ValueVM GetLocalParent(LocalsCtxMenuContext context) {
			if (context.SelectedItems.Length == 0)
				return null;
			var vm = context.SelectedItems[0];
			return vm.Children.Count == 0 ? null : vm;
		}
	}

	[ExportMenuItem(Header = "Show Namespaces", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_OPTS, Order = 0)]
	sealed class ShowNamespacesLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		public override void Execute(LocalsCtxMenuContext context) {
			LocalsSettings.Instance.ShowNamespaces = !LocalsSettings.Instance.ShowNamespaces;
		}

		public override bool IsChecked(LocalsCtxMenuContext context) {
			return LocalsSettings.Instance.ShowNamespaces;
		}
	}

	[ExportMenuItem(Header = "Show Type Keywords", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_OPTS, Order = 10)]
	sealed class ShowTypeKeywordsLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		public override void Execute(LocalsCtxMenuContext context) {
			LocalsSettings.Instance.ShowTypeKeywords = !LocalsSettings.Instance.ShowTypeKeywords;
		}

		public override bool IsChecked(LocalsCtxMenuContext context) {
			return LocalsSettings.Instance.ShowTypeKeywords;
		}
	}

	[ExportMenuItem(Header = "Show Tokens", Group = MenuConstants.GROUP_CTX_DBG_LOCALS_OPTS, Order = 20)]
	sealed class ShowTokensLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		public override void Execute(LocalsCtxMenuContext context) {
			LocalsSettings.Instance.ShowTokens = !LocalsSettings.Instance.ShowTokens;
		}

		public override bool IsChecked(LocalsCtxMenuContext context) {
			return LocalsSettings.Instance.ShowTokens;
		}
	}
}

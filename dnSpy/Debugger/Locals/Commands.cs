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
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Debugger.Memory;
using dnSpy.Images;
using dnSpy.MVVM;
using dnSpy.NRefactory;
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

	sealed class LocalsCtxMenuCommandProxy : ContextMenuEntryCommandProxy {
		public LocalsCtxMenuCommandProxy(LocalsCtxMenuCommand cmd)
			: base(cmd) {
		}

		protected override ContextMenuEntryContext CreateContext() {
			return ContextMenuEntryContext.Create(LocalsControlCreator.LocalsControlInstance.treeView);
		}
	}

	abstract class LocalsCtxMenuCommand : ContextMenuEntryBase<LocalsCtxMenuContext> {
		protected override LocalsCtxMenuContext CreateContext(ContextMenuEntryContext context) {
			if (DebugManager.Instance.ProcessState != DebuggerProcessState.Stopped)
				return null;
			var ui = LocalsControlCreator.LocalsControlInstance;
			if (context.Element != ui.treeView)
				return null;
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
		protected override void Execute(LocalsCtxMenuContext context) {
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

		protected override bool IsEnabled(LocalsCtxMenuContext context) {
			return GetValueVM(context) != null;
		}
	}

	[ExportContextMenuEntry(Header = "Cop_y", Order = 100, Category = "CopyLOC", Icon = "Copy", InputGestureText = "Ctrl+C")]
	sealed class CopyLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		protected override void Execute(LocalsCtxMenuContext context) {
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

		protected override bool IsEnabled(LocalsCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[ExportContextMenuEntry(Header = "Select _All", Order = 110, Category = "CopyLOC", Icon = "Select", InputGestureText = "Ctrl+A")]
	sealed class SelectAllLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		protected override void Execute(LocalsCtxMenuContext context) {
			LocalsControlCreator.LocalsControlInstance.treeView.SelectAll();
		}

		protected override bool IsEnabled(LocalsCtxMenuContext context) {
			return LocalsControlCreator.LocalsControlInstance.treeView.Items.Count > 0;
		}
	}

	[ExportContextMenuEntry(Header = "_Edit Value", Order = 200, Category = "LOCValues", InputGestureText = "F2")]
	sealed class EditValueLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		protected override void Execute(LocalsCtxMenuContext context) {
			if (IsEnabled(context))
				context.SelectedItems[0].IsEditingValue = true;
		}

		protected override bool IsEnabled(LocalsCtxMenuContext context) {
			return context.SelectedItems.Length == 1 &&
				context.SelectedItems[0].CanEdit;
		}
	}

	[ExportContextMenuEntry(Header = "Copy Va_lue", Order = 210, Category = "LOCValues", InputGestureText = "Ctrl+Shift+C")]
	sealed class CopyValueLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		protected override void Execute(LocalsCtxMenuContext context) {
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

		protected override bool IsEnabled(LocalsCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[ExportContextMenuEntry(Header = "Add _Watch", Order = 220, Category = "LOCValues", Icon = "Watch")]
	sealed class AddWatchLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		protected override void Execute(LocalsCtxMenuContext context) {
			//TODO:
		}

		protected override bool IsEnabled(LocalsCtxMenuContext context) {
			return false;//TODO:
		}
	}

	[ExportContextMenuEntry(Header = "_Save...", Order = 220, Category = "LOCValues", Icon = "Save")]
	sealed class SaveDataLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		protected override void Execute(LocalsCtxMenuContext context) {
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

		protected override bool IsEnabled(LocalsCtxMenuContext context) {
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

	[ExportContextMenuEntry(Header = "Show in Memory Window", Order = 220, Category = "LOCValues", Icon = "MemoryWindow")]
	sealed class ShowInMemoryXLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		protected override void Execute(LocalsCtxMenuContext context) {
		}

		static ShowInMemoryXLocalsCtxMenuCommand() {
			subCmds = new Tuple<ICommand, string, string>[MemoryControlCreator.NUMBER_OF_MEMORY_WINDOWS];
			for (int i = 0; i < subCmds.Length; i++)
				subCmds[i] = Tuple.Create((ICommand)new LocalsCtxMenuCommandProxy(new ShowInMemoryWindowLocalsCtxMenuCommand(i + 1)), MemoryControlCreator.GetHeaderText(i), MemoryControlCreator.GetCtrlInputGestureText(i));
		}

		static readonly Tuple<ICommand, string, string>[] subCmds;

		protected override void Initialize(LocalsCtxMenuContext context, MenuItem menuItem) {
			foreach (var tuple in subCmds) {
				var mi = new MenuItem {
					Command = tuple.Item1,
					Header = tuple.Item2,
				};
				if (!string.IsNullOrEmpty(tuple.Item3))
					mi.InputGestureText = tuple.Item3;
				MainWindow.CreateMenuItemImage(mi, this, "MemoryWindow", BackgroundType.ContextMenuItem);
				menuItem.Items.Add(mi);
			}
		}
	}

	sealed class ShowInMemoryLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		protected override void Execute(LocalsCtxMenuContext context) {
			var addrRange = ShowInMemoryWindowLocalsCtxMenuCommand.GetValue(context);
			if (addrRange != null)
				MemoryUtils.ShowInMemoryWindow(addrRange.Value.Address, addrRange.Value.Size);
		}

		protected override bool IsEnabled(LocalsCtxMenuContext context) {
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

		protected override void Execute(LocalsCtxMenuContext context) {
			var addrRange = GetValue(context);
			if (addrRange != null)
				MemoryUtils.ShowInMemoryWindow(windowNumber, addrRange.Value.Address, addrRange.Value.Size);
		}

		protected override bool IsEnabled(LocalsCtxMenuContext context) {
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

	[ExportContextMenuEntry(Header = "_Hexadecimal Display", Order = 300, Category = "LOCMiscOptions")]
	sealed class HexadecimalDisplayLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		protected override void Execute(LocalsCtxMenuContext context) {
			DebuggerSettings.Instance.UseHexadecimal = !DebuggerSettings.Instance.UseHexadecimal;
		}

		protected override void Initialize(LocalsCtxMenuContext context, MenuItem menuItem) {
			menuItem.IsChecked = DebuggerSettings.Instance.UseHexadecimal;
		}
	}

	[ExportContextMenuEntry(Header = "C_ollapse Parent", Order = 300, Category = "LOCTree", Icon = "OneLevelUp")]
	sealed class CollapseParentLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		protected override void Execute(LocalsCtxMenuContext context) {
			var vm = GetLocalParent(context);
			if (vm != null)
				vm.IsExpanded = false;
		}

		protected override bool IsEnabled(LocalsCtxMenuContext context) {
			var p = GetLocalParent(context);
			return p != null && p.IsExpanded;
		}

		static ValueVM GetLocalParent(LocalsCtxMenuContext context) {
			if (context.SelectedItems.Length == 0)
				return null;
			return context.SelectedItems[0].Parent as ValueVM;
		}
	}

	[ExportContextMenuEntry(Header = "E_xpand Children", Order = 310, Category = "LOCTree", Icon = "SuperTypesOpen")]
	sealed class ExpandChildrenLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		protected override void Execute(LocalsCtxMenuContext context) {
			var vm = GetLocalParent(context);
			if (vm != null) {
				vm.IsExpanded = true;
				foreach (ValueVM child in vm.Children) {
					if (CanExpand(child))
						child.IsExpanded = true;
				}
			}
		}

		protected override bool IsEnabled(LocalsCtxMenuContext context) {
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

	[ExportContextMenuEntry(Header = "_Collapse Children", Order = 320, Category = "LOCTree", Icon = "SuperTypes")]
	sealed class CollapseChildrenLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		protected override void Execute(LocalsCtxMenuContext context) {
			var vm = GetLocalParent(context);
			if (vm != null) {
				foreach (var child in vm.Children)
					child.IsExpanded = false;
			}
		}

		protected override bool IsEnabled(LocalsCtxMenuContext context) {
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

	[ExportContextMenuEntry(Header = "Show Namespaces", Order = 570, Category = "LOCDispOptions")]
	sealed class ShowNamespacesLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		protected override void Execute(LocalsCtxMenuContext context) {
			LocalsSettings.Instance.ShowNamespaces = !LocalsSettings.Instance.ShowNamespaces;
		}

		protected override void Initialize(LocalsCtxMenuContext context, MenuItem menuItem) {
			menuItem.IsChecked = LocalsSettings.Instance.ShowNamespaces;
		}
	}

	[ExportContextMenuEntry(Header = "Show Type Keywords", Order = 590, Category = "LOCDispOptions")]
	sealed class ShowTypeKeywordsLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		protected override void Execute(LocalsCtxMenuContext context) {
			LocalsSettings.Instance.ShowTypeKeywords = !LocalsSettings.Instance.ShowTypeKeywords;
		}

		protected override void Initialize(LocalsCtxMenuContext context, MenuItem menuItem) {
			menuItem.IsChecked = LocalsSettings.Instance.ShowTypeKeywords;
		}
	}

	[ExportContextMenuEntry(Header = "Show Tokens", Order = 600, Category = "LOCDispOptions")]
	sealed class ShowTokensLocalsCtxMenuCommand : LocalsCtxMenuCommand {
		protected override void Execute(LocalsCtxMenuContext context) {
			LocalsSettings.Instance.ShowTokens = !LocalsSettings.Instance.ShowTokens;
		}

		protected override void Initialize(LocalsCtxMenuContext context, MenuItem menuItem) {
			menuItem.IsChecked = LocalsSettings.Instance.ShowTokens;
		}
	}
}

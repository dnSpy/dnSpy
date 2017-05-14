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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	[Export(typeof(VariablesWindowOperations))]
	sealed class VariablesWindowOperationsImpl : VariablesWindowOperations {
		readonly DebuggerSettings debuggerSettings;
		readonly DbgEvalFormatterSettings dbgEvalFormatterSettings;
		readonly Lazy<DbgLanguageService> dbgLanguageService;
		readonly Lazy<ToolWindows.Memory.MemoryWindowService> memoryWindowService;
		readonly Lazy<IPickSaveFilename> pickSaveFilename;
		readonly Lazy<IMessageBoxService> messageBoxService;

		[ImportingConstructor]
		VariablesWindowOperationsImpl(DebuggerSettings debuggerSettings, DbgEvalFormatterSettings dbgEvalFormatterSettings, Lazy<DbgLanguageService> dbgLanguageService, Lazy<ToolWindows.Memory.MemoryWindowService> memoryWindowService, Lazy<IPickSaveFilename> pickSaveFilename, Lazy<IMessageBoxService> messageBoxService) {
			this.debuggerSettings = debuggerSettings;
			this.dbgEvalFormatterSettings = dbgEvalFormatterSettings;
			this.dbgLanguageService = dbgLanguageService;
			this.memoryWindowService = memoryWindowService;
			this.pickSaveFilename = pickSaveFilename;
			this.messageBoxService = messageBoxService;
		}

		bool CanExecCommands(IValueNodesVM vm) {
			if (vm == null)
				return false;
			if (!vm.IsOpen)
				return false;
			if (vm.IsReadOnly)
				return false;

			return true;
		}

		ValueNodeImpl SelectedNode(IValueNodesVM vm) {
			var nodes = vm.TreeView.SelectedItems;
			return nodes.Length != 1 ? null : nodes[0] as ValueNodeImpl;
		}

		bool HasSelectedNodes(IValueNodesVM vm) => vm.TreeView.SelectedItems.OfType<ValueNodeImpl>().Any();

		IEnumerable<ValueNodeImpl> SelectedNodes(IValueNodesVM vm) {
			var dict = new Dictionary<TreeNodeData, int>();
			int index = 0;
			foreach (var data in vm.TreeView.Root.Descendants().Where(a => a.IsVisible).Select(a => a.Data))
				dict[data] = index++;
			return vm.TreeView.SelectedItems.OfType<ValueNodeImpl>().OrderBy(a => dict.TryGetValue(a, out var order) ? order : int.MaxValue);
		}

		public override bool CanCopy(IValueNodesVM vm) => CanExecCommands(vm) && HasSelectedNodes(vm);
		public override void Copy(IValueNodesVM vm) {
			if (!CanCopy(vm))
				return;

			//TODO: Show a progress dlg box and allow the user to cancel it if it's taking too long
			var output = new StringBuilderTextColorOutput();
			foreach (var node in SelectedNodes(vm)) {
				var formatter = node.Context.Formatter;
				formatter.WriteName(output, node);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteValue(output, node, out _);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteType(output, node);
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

		public override bool CanCopyValue(IValueNodesVM vm) => CanExecCommands(vm) && HasSelectedNodes(vm);
		public override void CopyValue(IValueNodesVM vm) {
			if (!CanCopyValue(vm))
				return;

			//TODO: Show a progress dlg box and allow the user to cancel it if it's taking too long
			var output = new StringBuilderTextColorOutput();
			int count = 0;
			foreach (var node in SelectedNodes(vm)) {
				if (count > 0)
					output.WriteLine();
				count++;

				node.Context.Formatter.WriteValue(output, node, out _);
			}
			if (count > 1)
				output.WriteLine();
			var s = output.ToString();
			if (count != 0) {
				try {
					Clipboard.SetText(s);
				}
				catch (ExternalException) { }
			}
		}

		public override bool CanSelectAll(IValueNodesVM vm) => CanExecCommands(vm) && vm.TreeView.Root.Children.Count > 0;
		public override void SelectAll(IValueNodesVM vm) {
			if (!CanSelectAll(vm))
				return;
			vm.TreeView.SelectAll();
		}

		public override bool CanEditValue(IValueNodesVM vm) => CanExecCommands(vm) && SelectedNode(vm)?.DebuggerValueNode.IsReadOnly == false;
		public override void EditValue(IValueNodesVM vm) {
			if (!CanEditValue(vm))
				return;
			var node = SelectedNode(vm);
			if (node == null || node.DebuggerValueNode.IsReadOnly)
				return;
			node.ClearEditingValueProperties();
			node.ValueEditableValue.IsEditingValue = true;
		}

		public override bool CanAddWatch(IValueNodesVM vm) => CanExecCommands(vm) && HasSelectedNodes(vm);
		public override void AddWatch(IValueNodesVM vm) {
			if (!CanAddWatch(vm))
				return;
			//TODO: Add 1+ expressions to the watch window
		}

		public override bool CanMakeObjectId(IValueNodesVM vm) => CanExecCommands(vm) && HasSelectedNodes(vm);
		public override void MakeObjectId(IValueNodesVM vm) {
			if (!CanMakeObjectId(vm))
				return;
			//TODO: Create 1+ obj ids
		}

		public override bool CanSave(IValueNodesVM vm) => CanExecCommands(vm) && SelectedNode(vm)?.DebuggerValueNode.Value.GetRawAddressValue(onlyDataAddress: true) != null;
		public override void Save(IValueNodesVM vm) {
			if (!CanSave(vm))
				return;
			Save(SelectedNode(vm)?.DebuggerValueNode, pickSaveFilename, messageBoxService);
		}

		static void Save(DbgValueNode valueNode, Lazy<IPickSaveFilename> pickSaveFilename, Lazy<IMessageBoxService> messageBoxService) {
			var value = valueNode?.Value;
			if (value == null)
				return;
			if (value.Runtime.IsClosed)
				return;
			var addr = value.GetRawAddressValue(onlyDataAddress: true);
			if (addr == null)
				return;

			var filename = pickSaveFilename.Value.GetFilename(string.Empty, "bin", null);
			if (string.IsNullOrEmpty(filename))
				return;

			try {
				using (var file = File.Create(filename)) {
					if (value.HasRawValue && value.RawValue is string s) {
						var utf8Data = Encoding.UTF8.GetBytes(s);
						file.Write(utf8Data, 0, utf8Data.Length);
					}
					else {
						const int BUF_LEN = 0x2000;
						var buf = new byte[(int)Math.Min(BUF_LEN, addr.Value.Length)];
						ulong currAddr = addr.Value.Address;
						ulong lenLeft = addr.Value.Length;
						while (lenLeft > 0) {
							int bytesToRead = (int)Math.Min((uint)buf.Length, lenLeft);
							value.Process.ReadMemory(currAddr, buf, 0, bytesToRead);
							file.Write(buf, 0, bytesToRead);
							currAddr += (uint)bytesToRead;
							lenLeft -= (uint)bytesToRead;
						}
					}
				}
			}
			catch (Exception ex) {
				messageBoxService.Value.Show(string.Format(dnSpy_Debugger_Resources.LocalsSave_Error_CouldNotSaveDataToFilename, filename, ex.Message));
				return;
			}
		}

		public override bool CanShowInMemoryWindow(IValueNodesVM vm) => CanExecCommands(vm) && SelectedNode(vm)?.DebuggerValueNode.Value.GetRawAddressValue(onlyDataAddress: true) != null;
		public override void ShowInMemoryWindow(IValueNodesVM vm) {
			if (!CanShowInMemoryWindow(vm))
				return;
			ShowInMemoryWindowCore(vm, null);
		}

		public override bool CanShowInMemoryWindow(IValueNodesVM vm, int windowIndex) => CanExecCommands(vm) && SelectedNode(vm)?.DebuggerValueNode.Value.GetRawAddressValue(onlyDataAddress: true) != null;
		public override void ShowInMemoryWindow(IValueNodesVM vm, int windowIndex) {
			if (!CanShowInMemoryWindow(vm, windowIndex))
				return;
			if ((uint)windowIndex >= (uint)ToolWindows.Memory.MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS)
				throw new ArgumentOutOfRangeException(nameof(windowIndex));
			ShowInMemoryWindowCore(vm, windowIndex);
		}

		void ShowInMemoryWindowCore(IValueNodesVM vm, int? windowIndex) {
			if (!CanExecCommands(vm))
				return;
			var node = SelectedNode(vm);
			if (node == null)
				return;
			var addr = node.DebuggerValueNode.Value.GetRawAddressValue(onlyDataAddress: true);
			if (addr == null)
				return;
			var process = node.DebuggerValueNode.Process;
			var start = new HexPosition(addr.Value.Address);
			var end = start + addr.Value.Length;
			Debug.Assert(end <= HexPosition.MaxEndPosition);
			if (end <= HexPosition.MaxEndPosition) {
				if (windowIndex != null)
					memoryWindowService.Value.Show(process.Id, HexSpan.FromBounds(start, end), windowIndex.Value);
				else
					memoryWindowService.Value.Show(process.Id, HexSpan.FromBounds(start, end));
			}
		}

		public override bool CanToggleExpanded(IValueNodesVM vm) => CanExecCommands(vm) && SelectedNode(vm) != null;
		public override void ToggleExpanded(IValueNodesVM vm) {
			if (!CanToggleExpanded(vm))
				return;
			var node = SelectedNode(vm);
			if (node == null)
				return;
			if (node.TreeNode.LazyLoading || node.TreeNode.Children.Count > 0)
				node.TreeNode.IsExpanded = !node.TreeNode.IsExpanded;
		}

		public override bool CanCollapseParent(IValueNodesVM vm) => CanExecCommands(vm) && SelectedNode(vm)?.Parent != null;
		public override void CollapseParent(IValueNodesVM vm) {
			if (!CanCollapseParent(vm))
				return;
			var parent = SelectedNode(vm)?.Parent;
			if (parent == null)
				return;
			parent.TreeNode.IsExpanded = false;
		}

		static bool CanExpand(ValueNodeImpl node) => node != null && !node.TreeNode.IsExpanded && (node.TreeNode.LazyLoading || node.TreeNode.Children.Count > 0);
		ValueNodeImpl GetExpandChildrenNode(IValueNodesVM vm) {
			if (!CanExecCommands(vm))
				return null;
			var node = SelectedNode(vm);
			if (node == null)
				return null;
			if (node.TreeNode.LazyLoading)
				return node;
			return node.TreeNode.Children.Count == 0 ? null : node;
		}
		public override bool CanExpandChildren(IValueNodesVM vm) {
			var node = GetExpandChildrenNode(vm);
			if (node == null)
				return false;
			if (!node.TreeNode.IsExpanded)
				return true;
			return node.TreeNode.Children.Any(c => CanExpand(c.Data as ValueNodeImpl));
		}
		public override void ExpandChildren(IValueNodesVM vm) {
			var node = GetExpandChildrenNode(vm);
			if (node == null)
				return;
			node.TreeNode.IsExpanded = true;
			foreach (var child in node.TreeNode.Children) {
				if (CanExpand(child.Data as ValueNodeImpl))
					child.IsExpanded = true;
			}
		}

		ValueNodeImpl GetCollapseChildrenNode(IValueNodesVM vm) {
			if (!CanExecCommands(vm))
				return null;
			var node = SelectedNode(vm);
			if (node == null)
				return null;
			return node.TreeNode.Children.Count == 0 ? null : node;
		}
		public override bool CanCollapseChildren(IValueNodesVM vm) {
			var node = GetCollapseChildrenNode(vm);
			if (node == null)
				return false;
			if (!node.TreeNode.IsExpanded)
				return false;
			return node.TreeNode.Children.Any(c => c.IsExpanded);
		}
		public override void CollapseChildren(IValueNodesVM vm) {
			var node = GetCollapseChildrenNode(vm);
			if (node == null)
				return;
			foreach (var child in node.TreeNode.Children)
				child.IsExpanded = false;
		}

		public override IList<DbgLanguage> GetLanguages(IValueNodesVM vm) {
			if (!CanExecCommands(vm))
				return Array.Empty<DbgLanguage>();
			var runtimeGuid= vm.RuntimeGuid;
			if (runtimeGuid == null)
				return Array.Empty<DbgLanguage>();
			return dbgLanguageService.Value.GetLanguages(runtimeGuid.Value);
		}

		public override DbgLanguage GetCurrentLanguage(IValueNodesVM vm) {
			if (!CanExecCommands(vm))
				return null;
			var runtimeGuid = vm.RuntimeGuid;
			if (runtimeGuid == null)
				return null;
			return dbgLanguageService.Value.GetCurrentLanguage(runtimeGuid.Value);
		}

		public override void SetCurrentLanguage(IValueNodesVM vm, DbgLanguage language) {
			if (!CanExecCommands(vm))
				return;
			var runtimeGuid = vm.RuntimeGuid;
			if (runtimeGuid == null)
				return;
			dbgLanguageService.Value.SetCurrentLanguage(runtimeGuid.Value, language);
		}

		public override bool CanToggleUseHexadecimal => true;
		public override void ToggleUseHexadecimal() => UseHexadecimal = !UseHexadecimal;
		public override bool UseHexadecimal {
			get => debuggerSettings.UseHexadecimal;
			set => debuggerSettings.UseHexadecimal = value;
		}

		public override bool ShowDeclaringTypes {
			get => dbgEvalFormatterSettings.ShowDeclaringTypes;
			set => dbgEvalFormatterSettings.ShowDeclaringTypes = value;
		}

		public override bool ShowNamespaces {
			get => dbgEvalFormatterSettings.ShowNamespaces;
			set => dbgEvalFormatterSettings.ShowNamespaces = value;
		}

		public override bool ShowIntrinsicTypeKeywords {
			get => dbgEvalFormatterSettings.ShowIntrinsicTypeKeywords;
			set => dbgEvalFormatterSettings.ShowIntrinsicTypeKeywords = value;
		}

		public override bool ShowTokens {
			get => dbgEvalFormatterSettings.ShowTokens;
			set => dbgEvalFormatterSettings.ShowTokens = value;
		}
	}
}

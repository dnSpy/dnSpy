/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.TreeView;
using dnSpy.Debugger.Evaluation.Watch;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	[Export(typeof(VariablesWindowOperations))]
	sealed class VariablesWindowOperationsImpl : VariablesWindowOperations {
		readonly DebuggerSettings debuggerSettings;
		readonly DbgEvalFormatterSettings dbgEvalFormatterSettings;
		readonly Lazy<DbgLanguageService> dbgLanguageService;
		readonly Lazy<DbgObjectIdService> dbgObjectIdService;
		readonly Lazy<WatchExpressionsService> watchExpressionsService;
		readonly Lazy<ToolWindows.Memory.MemoryWindowService> memoryWindowService;
		readonly Lazy<IPickSaveFilename> pickSaveFilename;
		readonly Lazy<IMessageBoxService> messageBoxService;

		[ImportingConstructor]
		VariablesWindowOperationsImpl(DebuggerSettings debuggerSettings, DbgEvalFormatterSettings dbgEvalFormatterSettings, Lazy<DbgLanguageService> dbgLanguageService, Lazy<DbgObjectIdService> dbgObjectIdService, Lazy<WatchExpressionsService> watchExpressionsService, Lazy<ToolWindows.Memory.MemoryWindowService> memoryWindowService, Lazy<IPickSaveFilename> pickSaveFilename, Lazy<IMessageBoxService> messageBoxService) {
			this.debuggerSettings = debuggerSettings;
			this.dbgEvalFormatterSettings = dbgEvalFormatterSettings;
			this.dbgLanguageService = dbgLanguageService;
			this.dbgObjectIdService = dbgObjectIdService;
			this.watchExpressionsService = watchExpressionsService;
			this.memoryWindowService = memoryWindowService;
			this.pickSaveFilename = pickSaveFilename;
			this.messageBoxService = messageBoxService;
		}

		bool CanExecCommands(IValueNodesVM vm) => CanExecCommands(vm, isWatch: false);

		bool CanExecCommands(IValueNodesVM vm, bool isWatch) {
			if (vm is null)
				return false;
			if (!vm.IsOpen)
				return false;
			if (!isWatch && vm.IsReadOnly)
				return false;

			return true;
		}

		ValueNodeImpl? SelectedNode(IValueNodesVM vm) {
			var nodes = vm.TreeView.SelectedItems;
			return nodes.Length != 1 ? null : nodes[0] as ValueNodeImpl;
		}

		bool HasSelectedNodes(IValueNodesVM vm) => vm.TreeView.SelectedItems.OfType<ValueNodeImpl>().Any(a => !a.IsEditNode);

		IEnumerable<ValueNodeImpl> SelectedNodes(IValueNodesVM vm) => vm.TreeView.SelectedItems.OfType<ValueNodeImpl>().Where(a => !a.IsEditNode);

		IEnumerable<ValueNodeImpl> SortedSelectedNodes(IValueNodesVM vm) {
			var dict = new Dictionary<TreeNodeData, int>();
			int index = 0;
			foreach (var data in vm.TreeView.Root.Descendants().Where(a => a.IsVisible).Select(a => a.Data))
				dict[data] = index++;
			return vm.TreeView.SelectedItems.OfType<ValueNodeImpl>().Where(a => !a.IsEditNode).OrderBy(a => dict.TryGetValue(a, out var order) ? order : int.MaxValue);
		}

		bool IsEmpty(IValueNodesVM vm) => !vm.TreeView.Root.DataChildren.OfType<ValueNodeImpl>().Any(a => !a.IsEditNode);

		static DbgValueNode? GetValueNode(RawNode? rawNode) => (rawNode as DebuggerValueRawNode)?.DebuggerValueNode;
		static DbgValue? GetValue(RawNode? rawNode) => GetValueNode(rawNode)?.Value;

		public override bool CanCopy(IValueNodesVM vm) => CanExecCommands(vm) && HasSelectedNodes(vm);
		public override void Copy(IValueNodesVM vm) {
			if (!CanCopy(vm))
				return;

			//TODO: Show a progress dlg box and allow the user to cancel it if it's taking too long
			var output = new DbgStringBuilderTextWriter();
			var expressions = new List<string>();
			foreach (var node in SortedSelectedNodes(vm)) {
				if (node.RawNode.CanEvaluateExpression)
					expressions.Add(node.RawNode.Expression);
				var formatter = node.Context.Formatter;
				formatter.WriteExpander(output, node);
				output.Write(DbgTextColor.Text, "\t");
				// Add an extra tab here to emulate VS output
				output.Write(DbgTextColor.Text, "\t");
				formatter.WriteName(output, node);
				output.Write(DbgTextColor.Text, "\t");
				formatter.WriteValueAndObjectId(output, node, out _);
				output.Write(DbgTextColor.Text, "\t");
				formatter.WriteType(output, node);
				output.WriteLine();
			}
			var s = output.ToString();
			if (s.Length > 0) {
				try {
					var dataObj = new DataObject();
					dataObj.SetText(s);
					dataObj.SetData(ClipboardFormats.VARIABLES_WINDOW_EXPRESSIONS, expressions.ToArray());
					Clipboard.SetDataObject(dataObj);
				}
				catch (ExternalException) { }
			}
		}

		public override bool CanCopyExpression(IValueNodesVM vm) => CanExecCommands(vm) && HasSelectedNodes(vm);
		public override void CopyExpression(IValueNodesVM vm) {
			if (!CanCopyExpression(vm))
				return;

			var output = new StringBuilder();
			int count = 0;
			foreach (var node in SortedSelectedNodes(vm)) {
				if (node.RawNode.CanEvaluateExpression) {
					if (count > 0)
						output.AppendLine();
					count++;
					output.Append(node.RawNode.Expression);
				}
			}
			if (count > 1)
				output.AppendLine();
			var s = output.ToString();
			if (s.Length > 0) {
				try {
					var dataObj = new DataObject();
					dataObj.SetText(s);
					Clipboard.SetDataObject(dataObj);
				}
				catch (ExternalException) { }
			}
		}

		public override bool CanCopyValue(IValueNodesVM vm) => CanExecCommands(vm) && HasSelectedNodes(vm);
		public override void CopyValue(IValueNodesVM vm) {
			if (!CanCopyValue(vm))
				return;

			//TODO: Show a progress dlg box and allow the user to cancel it if it's taking too long
			var output = new DbgStringBuilderTextWriter();
			int count = 0;
			foreach (var node in SortedSelectedNodes(vm)) {
				if (count > 0)
					output.WriteLine();
				count++;

				// Don't write the object id since the command is "copy value" (VS copies the object id, though)
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

		static bool HasClipboardExpressions() {
			try {
				return Clipboard.ContainsText() || Clipboard.ContainsData(ClipboardFormats.VARIABLES_WINDOW_EXPRESSIONS);
			}
			catch (ExternalException) { }
			return false;
		}

		static string[]? GetClipboardExpressions() {
			try {
				return Clipboard.GetData(ClipboardFormats.VARIABLES_WINDOW_EXPRESSIONS) as string[];
			}
			catch (ExternalException) { }
			return null;
		}

		static string? GetClipboardText() {
			try {
				return Clipboard.GetText();
			}
			catch (ExternalException) { }
			return null;
		}

		public override bool SupportsPaste(IValueNodesVM vm) => vm is not null && vm.CanAddRemoveExpressions;
		public override bool CanPaste(IValueNodesVM vm) => CanExecCommands(vm) && SupportsPaste(vm) && HasClipboardExpressions();
		public override void Paste(IValueNodesVM vm) {
			if (!CanPaste(vm))
				return;

			var expressions = GetClipboardExpressions();
			if (expressions is null) {
				var text = GetClipboardText();
				if (text is null)
					return;

				var list = new List<string>();
				using (var reader = new StringReader(text)) {
					for (;;) {
						var line = reader.ReadLine();
						if (line is null)
							break;
						var expr = GetExpression(line);
						if (expr.Length != 0)
							list.Add(expr);
					}
				}
				expressions = list.ToArray();
			}

			vm.AddExpressions(expressions);
		}

		static string GetExpression(string s) {
			s = s.Trim();
			int index = s.IndexOf('\t');
			if (index >= 0)
				s = s.Substring(0, index).Trim();
			return s;
		}

		public override bool SupportsDeleteWatch(IValueNodesVM vm) => vm is not null && vm.CanAddRemoveExpressions;
		public override bool CanDeleteWatch(IValueNodesVM vm) => CanExecCommands(vm, isWatch: true) && SupportsDeleteWatch(vm) && HasSelectedNodes(vm);
		public override void DeleteWatch(IValueNodesVM vm) {
			if (!CanDeleteWatch(vm))
				return;
			vm.DeleteExpressions(SelectedNodes(vm).Select(a => a.RootId).OfType<string>().ToArray());
		}

		public override bool SupportsClearAll(IValueNodesVM vm) => vm is not null && vm.CanAddRemoveExpressions;
		public override bool CanClearAll(IValueNodesVM vm) => CanExecCommands(vm) && SupportsClearAll(vm) && !IsEmpty(vm);
		public override void ClearAll(IValueNodesVM vm) {
			if (!CanClearAll(vm))
				return;
			vm.ClearAllExpressions();
		}

		public override bool CanSelectAll(IValueNodesVM vm) => CanExecCommands(vm) && vm.TreeView.Root.Children.Count > 0;
		public override void SelectAll(IValueNodesVM vm) {
			if (!CanSelectAll(vm))
				return;
			vm.TreeView.SelectAll();
		}

		public override bool CanEdit(IValueNodesVM vm) => (vm.CanAddRemoveExpressions && CanEditExpression(vm)) || CanEditValue(vm);

		public override void Edit(IValueNodesVM vm) {
			if (vm.CanAddRemoveExpressions && CanEditExpression(vm))
				EditExpression(vm);
			else if (CanEditValue(vm))
				EditValue(vm);
		}

		public override void Edit(IValueNodesVM vm, string text) {
			if (vm.CanAddRemoveExpressions && CanEditExpression(vm))
				EditExpression(vm, text);
			else if (CanEditValue(vm))
				EditValue(vm, text);
		}

		public override bool SupportsEditExpression(IValueNodesVM vm) => vm is not null && vm.CanAddRemoveExpressions;
		public override bool CanEditExpression(IValueNodesVM vm) => CanExecCommands(vm) && SupportsEditExpression(vm) && SelectedNode(vm)?.CanEditNameExpression() == true;
		public override void EditExpression(IValueNodesVM vm) {
			if (!CanEditExpression(vm))
				return;
			var node = SelectedNode(vm);
			if (node?.CanEditNameExpression() != true)
				return;
			node.ClearEditingValueProperties();
			node.NameEditableValue.IsEditingValue = true;
		}

		public override void EditExpression(IValueNodesVM vm, string text) {
			if (text is null)
				throw new ArgumentNullException(nameof(text));
			if (!CanEditExpression(vm))
				return;
			var node = SelectedNode(vm);
			if (node?.CanEditNameExpression() != true)
				return;
			node.ClearEditingValueProperties();
			node.Context.ExpressionToEdit = text;
			node.NameEditableValue.IsEditingValue = true;
		}

		public override bool CanEditValue(IValueNodesVM vm) => CanExecCommands(vm) && SelectedNode(vm)?.CanEditValue() == true;
		public override void EditValue(IValueNodesVM vm) {
			if (!CanEditValue(vm))
				return;
			var node = SelectedNode(vm);
			if (node is null || node.RawNode.IsReadOnly)
				return;
			node.ClearEditingValueProperties();
			node.ValueEditableValue.IsEditingValue = true;
		}

		public override void EditValue(IValueNodesVM vm, string text) {
			if (text is null)
				throw new ArgumentNullException(nameof(text));
			if (!CanEditValue(vm))
				return;
			var node = SelectedNode(vm);
			if (node is null || node.RawNode.IsReadOnly)
				return;
			node.ClearEditingValueProperties();
			node.Context.ExpressionToEdit = text;
			node.ValueEditableValue.IsEditingValue = true;
		}

		public override bool CanAddWatch(IValueNodesVM vm) => CanExecCommands(vm) && SelectedNodes(vm).Any(a => a.RawNode.CanEvaluateExpression);
		public override void AddWatch(IValueNodesVM vm) {
			if (!CanAddWatch(vm))
				return;
			var expressions = SortedSelectedNodes(vm).Where(a => a.RawNode.CanEvaluateExpression).Select(a => a.RawNode.Expression).Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();
			watchExpressionsService.Value.AddExpressions(expressions);
		}

		public override bool IsMakeObjectIdVisible(IValueNodesVM vm) {
			if (vm is null)
				return false;
			int removeCount = 0;
			foreach (var node in SelectedNodes(vm)) {
				if (node.RawNode is DebuggerValueRawNode rawNode && rawNode.DebuggerValueNode.Value is DbgValue value) {
					if (dbgObjectIdService.Value.CanCreateObjectId(value))
						return true;
					if (dbgObjectIdService.Value.GetObjectId(value) is not null)
						removeCount++;
				}
			}
			return removeCount == 0;
		}

		public override bool CanMakeObjectId(IValueNodesVM vm) => CanExecCommands(vm) && SelectedNodes(vm).Select(a => a.RawNode).OfType<DebuggerValueRawNode>().Any(a => a.DebuggerValueNode.Value is DbgValue value && dbgObjectIdService.Value.CanCreateObjectId(value));
		public override void MakeObjectId(IValueNodesVM vm) {
			if (!CanMakeObjectId(vm))
				return;
			var values = SortedSelectedNodes(vm).Select(a => a.RawNode).OfType<DebuggerValueRawNode>().Select(a => a.DebuggerValueNode.Value).OfType<DbgValue>().ToArray();
			dbgObjectIdService.Value.CreateObjectIds(values);
		}

		public override bool CanDeleteObjectId(IValueNodesVM vm) => CanExecCommands(vm) && SelectedNodes(vm).Select(a => a.RawNode).OfType<DebuggerValueRawNode>().Any(a => a.DebuggerValueNode.Value is DbgValue value && dbgObjectIdService.Value.GetObjectId(value) is not null);
		public override void DeleteObjectId(IValueNodesVM vm) {
			if (!CanDeleteObjectId(vm))
				return;
			var objectIds = SelectedNodes(vm).Select(a => a.RawNode).OfType<DebuggerValueRawNode>().Select(a => a.DebuggerValueNode.Value is DbgValue value ? dbgObjectIdService.Value.GetObjectId(value) : null).OfType<DbgObjectId>();
			dbgObjectIdService.Value.Remove(objectIds);
		}

		public override bool CanSave(IValueNodesVM vm) => CanExecCommands(vm) && (GetValue(SelectedNode(vm)?.RawNode)?.GetRawAddressValue(onlyDataAddress: true)?.Length ?? 0) != 0;
		public override void Save(IValueNodesVM vm) {
			if (!CanSave(vm))
				return;
			Save(GetValueNode(SelectedNode(vm)?.RawNode), pickSaveFilename, messageBoxService);
		}

		public override bool CanRefresh(IValueNodesVM vm) => CanExecCommands(vm);
		public override void Refresh(IValueNodesVM vm) {
			if (!CanRefresh(vm))
				return;
			vm.Refresh();
		}

		static void Save(DbgValueNode? valueNode, Lazy<IPickSaveFilename> pickSaveFilename, Lazy<IMessageBoxService> messageBoxService) {
			var value = valueNode?.Value;
			if (value is null || value.IsClosed)
				return;
			if (value.Runtime.IsClosed)
				return;
			var addr = value.GetRawAddressValue(onlyDataAddress: true);
			if (addr is null)
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

		public override bool CanShowInMemoryWindow(IValueNodesVM vm) => CanExecCommands(vm) && GetValue(SelectedNode(vm)?.RawNode)?.GetRawAddressValue(onlyDataAddress: true) is not null;
		public override void ShowInMemoryWindow(IValueNodesVM vm) {
			if (!CanShowInMemoryWindow(vm))
				return;
			ShowInMemoryWindowCore(vm, null);
		}

		public override bool CanShowInMemoryWindow(IValueNodesVM vm, int windowIndex) => CanExecCommands(vm) && GetValue(SelectedNode(vm)?.RawNode)?.GetRawAddressValue(onlyDataAddress: true) is not null;
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
			if (node is null)
				return;
			var valueNode = GetValueNode(node.RawNode);
			var addr = valueNode?.Value?.GetRawAddressValue(onlyDataAddress: true);
			if (addr is null)
				return;
			Debug2.Assert(valueNode is not null);
			var process = valueNode.Process;
			var start = new HexPosition(addr.Value.Address);
			var end = start + addr.Value.Length;
			Debug.Assert(end <= HexPosition.MaxEndPosition);
			if (end <= HexPosition.MaxEndPosition) {
				if (windowIndex is not null)
					memoryWindowService.Value.Show(process.Id, HexSpan.FromBounds(start, end), windowIndex.Value);
				else
					memoryWindowService.Value.Show(process.Id, HexSpan.FromBounds(start, end));
			}
		}

		public override bool CanToggleExpanded(IValueNodesVM vm) => CanExecCommands(vm) && SelectedNode(vm) is not null;
		public override void ToggleExpanded(IValueNodesVM vm) {
			if (!CanToggleExpanded(vm))
				return;
			var node = SelectedNode(vm);
			if (node is null)
				return;
			if (node.TreeNode.LazyLoading || node.TreeNode.Children.Count > 0)
				node.TreeNode.IsExpanded = !node.TreeNode.IsExpanded;
		}

		static ValueNode? GetParent(ValueNode? node) => node?.TreeNode.Parent?.Data as ValueNode;
		public override bool CanCollapseParent(IValueNodesVM vm) => CanExecCommands(vm) && GetParent(SelectedNode(vm)) is not null;
		public override void CollapseParent(IValueNodesVM vm) {
			if (!CanCollapseParent(vm))
				return;
			var parent = GetParent(SelectedNode(vm));
			if (parent is null)
				return;
			parent.TreeNode.IsExpanded = false;
		}

		static bool CanExpand(ValueNodeImpl? node) => node is not null && !node.TreeNode.IsExpanded && (node.TreeNode.LazyLoading || node.TreeNode.Children.Count > 0);
		ValueNodeImpl? GetExpandChildrenNode(IValueNodesVM vm) {
			if (!CanExecCommands(vm))
				return null;
			var node = SelectedNode(vm);
			if (node is null)
				return null;
			if (node.TreeNode.LazyLoading)
				return node;
			return node.TreeNode.Children.Count == 0 ? null : node;
		}
		public override bool CanExpandChildren(IValueNodesVM vm) {
			var node = GetExpandChildrenNode(vm);
			if (node is null)
				return false;
			if (!node.TreeNode.IsExpanded)
				return true;
			return node.TreeNode.Children.Any(c => CanExpand(c.Data as ValueNodeImpl));
		}
		public override void ExpandChildren(IValueNodesVM vm) {
			var node = GetExpandChildrenNode(vm);
			if (node is null)
				return;
			node.TreeNode.IsExpanded = true;
			foreach (var child in node.TreeNode.Children) {
				if (CanExpand(child.Data as ValueNodeImpl))
					child.IsExpanded = true;
			}
		}

		ValueNodeImpl? GetCollapseChildrenNode(IValueNodesVM vm) {
			if (!CanExecCommands(vm))
				return null;
			var node = SelectedNode(vm);
			if (node is null)
				return null;
			return node.TreeNode.Children.Count == 0 ? null : node;
		}
		public override bool CanCollapseChildren(IValueNodesVM vm) {
			var node = GetCollapseChildrenNode(vm);
			if (node is null)
				return false;
			if (!node.TreeNode.IsExpanded)
				return false;
			return node.TreeNode.Children.Any(c => c.IsExpanded);
		}
		public override void CollapseChildren(IValueNodesVM vm) {
			var node = GetCollapseChildrenNode(vm);
			if (node is null)
				return;
			foreach (var child in node.TreeNode.Children)
				child.IsExpanded = false;
		}

		public override IList<DbgLanguage> GetLanguages(IValueNodesVM vm) {
			if (!CanExecCommands(vm))
				return Array.Empty<DbgLanguage>();
			var runtimeKindGuid = vm.RuntimeKindGuid;
			if (runtimeKindGuid is null)
				return Array.Empty<DbgLanguage>();
			return dbgLanguageService.Value.GetLanguages(runtimeKindGuid.Value);
		}

		public override DbgLanguage? GetCurrentLanguage(IValueNodesVM vm) {
			if (!CanExecCommands(vm))
				return null;
			var runtimeKindGuid = vm.RuntimeKindGuid;
			if (runtimeKindGuid is null)
				return null;
			return dbgLanguageService.Value.GetCurrentLanguage(runtimeKindGuid.Value);
		}

		public override void SetCurrentLanguage(IValueNodesVM vm, DbgLanguage language) {
			if (!CanExecCommands(vm))
				return;
			var runtimeKindGuid = vm.RuntimeKindGuid;
			if (runtimeKindGuid is null)
				return;
			dbgLanguageService.Value.SetCurrentLanguage(runtimeKindGuid.Value, language);
		}

		public override bool CanToggleUseHexadecimal => true;
		public override void ToggleUseHexadecimal() => UseHexadecimal = !UseHexadecimal;
		public override bool UseHexadecimal {
			get => debuggerSettings.UseHexadecimal;
			set => debuggerSettings.UseHexadecimal = value;
		}

		public override bool CanToggleUseDigitSeparators => true;
		public override void ToggleUseDigitSeparators() => UseDigitSeparators = !UseDigitSeparators;
		public override bool UseDigitSeparators {
			get => debuggerSettings.UseDigitSeparators;
			set => debuggerSettings.UseDigitSeparators = value;
		}

		public override bool ShowOnlyPublicMembers {
			get => debuggerSettings.ShowOnlyPublicMembers;
			set => debuggerSettings.ShowOnlyPublicMembers = value;
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

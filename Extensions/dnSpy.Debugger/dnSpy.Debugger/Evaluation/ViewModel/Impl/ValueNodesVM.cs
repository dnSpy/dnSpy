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
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.TreeView;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.UI;
using dnSpy.Debugger.UI.Wpf;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	sealed class ValueNodesVM : ViewModelBase, IValueNodesVM, IEditValueNodeExpression {
		bool IValueNodesVM.IsOpen => isOpen;
		bool IValueNodesVM.IsReadOnly => valueNodesContext.IsWindowReadOnly;
		ITreeView IValueNodesVM.TreeView => treeView;
		Guid? IValueNodesVM.RuntimeKindGuid => valueNodesProvider.Language?.RuntimeKindGuid;
		VariablesWindowKind IValueNodesVM.VariablesWindowKind => variablesWindowKind;

		sealed class RootNode : TreeNodeData {
			public override Guid Guid => Guid.Empty;
			public override object? Text => null;
			public override object? ToolTip => null;
			public override ImageReference Icon => ImageReference.None;
			public override void OnRefreshUI() { }
		}

		enum SelectNodeKind {
			None,
			Open,
			Added,
		}

		readonly ValueNodesProvider valueNodesProvider;
		readonly VariablesWindowKind variablesWindowKind;
		readonly DebuggerSettings debuggerSettings;
		readonly DbgEvalFormatterSettings dbgEvalFormatterSettings;
		readonly DbgObjectIdService dbgObjectIdService;
		readonly ValueNodesContext valueNodesContext;
		readonly ITreeView treeView;
		readonly RootNode rootNode;
		bool isOpen;
		SelectNodeKind selectNodeKind;
		Guid? lastRuntimeKindGuid;
		DbgLanguage? lastLanguage;

		sealed class GuidObjectsProvider : IGuidObjectsProvider {
			readonly IValueNodesVM vm;
			public GuidObjectsProvider(IValueNodesVM vm) => this.vm = vm ?? throw new ArgumentNullException(nameof(vm));

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsProviderArgs args) {
				yield return new GuidObject(ValueNodesVMConstants.GUIDOBJ_VALUENODESVM_GUID, vm);
			}
		}

		internal event EventHandler? OnVariableChanged;

		public ValueNodesVM(UIDispatcher uiDispatcher, ValueNodesVMOptions options, ITreeViewService treeViewService, LanguageEditValueProviderFactory languageEditValueProviderFactory, DbgValueNodeImageReferenceService dbgValueNodeImageReferenceService, DebuggerSettings debuggerSettings, DbgEvalFormatterSettings dbgEvalFormatterSettings, DbgObjectIdService dbgObjectIdService, IClassificationFormatMapService classificationFormatMapService, ITextBlockContentInfoFactory textBlockContentInfoFactory, IMenuService menuService, IWpfCommandService wpfCommandService) {
			uiDispatcher.VerifyAccess();
			valueNodesProvider = options.NodesProvider;
			variablesWindowKind = options.VariablesWindowKind;
			this.debuggerSettings = debuggerSettings;
			this.dbgEvalFormatterSettings = dbgEvalFormatterSettings;
			this.dbgObjectIdService = dbgObjectIdService;
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			var formatCulture = CultureInfo.InvariantCulture;
			valueNodesContext = new ValueNodesContext(uiDispatcher, this, options.WindowContentType, options.NameColumnName, options.ValueColumnName, options.TypeColumnName, languageEditValueProviderFactory, dbgValueNodeImageReferenceService, new DbgValueNodeReaderImpl(EvaluateExpression), classificationFormatMap, textBlockContentInfoFactory, formatCulture, options.ShowMessageBox, OnValueNodeAssigned);
			valueNodesContext.Formatter.ObjectIdService = dbgObjectIdService;

			rootNode = new RootNode();
			var tvOptions = new TreeViewOptions {
				CanDragAndDrop = false,
				IsGridView = true,
				RootNode = rootNode,
			};
			treeView = treeViewService.Create(options.TreeViewGuid, tvOptions);

			menuService.InitializeContextMenu(treeView.UIObject, new Guid(MenuConstants.GUIDOBJ_VARIABLES_WINDOW_TREEVIEW_GUID), new GuidObjectsProvider(this));
			wpfCommandService.Add(options.VariablesWindowGuid, treeView.UIObject);
		}

		// UI thread
		void OnValueNodeAssigned(string? errorMessage, bool retry) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			if (errorMessage is not null)
				valueNodesContext.ShowMessageBox(errorMessage, ShowMessageBoxButtons.OK);
			if (!retry) {
				OnVariableChanged?.Invoke(this, EventArgs.Empty);
				RecreateRootChildren_UI();
			}
		}

		// UI thread
		void ValueNodesProvider_NodesChanged(object? sender, EventArgs e) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			RecreateRootChildren_UI();
		}

		// UI thread
		void ValueNodesProvider_IsReadOnlyChanged(object? sender, EventArgs e) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			valueNodesContext.IsWindowReadOnly = valueNodesProvider.IsReadOnly;
			RefreshThemeFields_UI();
		}

		// UI thread
		void ValueNodesProvider_LanguageChanged(object? sender, EventArgs e) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			valueNodesContext.NameEditValueProvider.Language = valueNodesProvider.Language;
			valueNodesContext.ValueEditValueProvider.Language = valueNodesProvider.Language;
			valueNodesContext.Formatter.Language = valueNodesProvider.Language;
		}

		// random thread
		void DbgObjectIdService_ObjectIdsChanged(object? sender, EventArgs e) {
			if (refreshNameFields)
				return;
			refreshNameFields = true;
			// Add an extra UI() so RecreateRootChildren_UI() gets called before RefreshNameFields_UI().
			// This way we avoid creating extra UI elements in the locals window.
			UI(() => UI(() => RefreshNameFields_UI()));
		}
		volatile bool refreshNameFields;

		// UI thread
		void RefreshNameFields_UI() {
			valueNodesContext.UIDispatcher.VerifyAccess();
			if (!refreshNameFields)
				return;
			refreshNameFields = false;
			const RefreshNodeOptions options = RefreshNodeOptions.RefreshValueControl;
			RefreshNodes(options);
		}

		// UI thread
		DbgValueNodeInfo EvaluateExpression(DbgEvaluationInfo evalInfo, string expression) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			var frame = valueNodesProvider.TryGetFrame();
			if (frame is null)
				return new DbgValueNodeInfo(expression, expression, dnSpy_Debugger_Resources.ErrorEvaluatingExpression, causesSideEffects: false);
			var res = evalInfo.Context.Language.ValueNodeFactory.Create(evalInfo, expression, valueNodesContext.ValueNodeEvaluationOptions,
				valueNodesContext.EvaluationOptions, evalInfo.Context.Language.ExpressionEvaluator.CreateExpressionEvaluatorState());
			return new DbgValueNodeInfo(res.ValueNode, res.CausesSideEffects);
		}

		// UI thread
		void RecreateRootChildrenDelay_UI() {
			valueNodesContext.UIDispatcher.VerifyAccess();
			valueNodesContext.UIDispatcher.UI(() => RecreateRootChildren_UI());
		}

		// UI thread
		internal void RefreshAllNodes_UI() {
			valueNodesContext.UIDispatcher.VerifyAccess();
			valueNodesProvider.RefreshAllNodes();
			RecreateRootChildren_UI();
		}

		// UI thread
		internal void RecreateRootChildren_UI() {
			valueNodesContext.UIDispatcher.VerifyAccess();
			refreshNameFields = false;
			Guid? runtimeKindGuid;
			DbgValueNodeInfo[] nodes;
			DbgEvaluationInfo? evalInfo;
			DbgLanguage? language;
			bool forceRecreateAllNodes;
			if (isOpen) {
				var nodeInfo = valueNodesProvider.GetNodes(valueNodesContext.EvaluationOptions, valueNodesContext.ValueNodeEvaluationOptions, valueNodesContext.ValueNodeFormatParameters.NameFormatterOptions);
				evalInfo = valueNodesProvider.TryGetEvaluationInfo();
				runtimeKindGuid = valueNodesProvider.Language?.RuntimeKindGuid ?? lastRuntimeKindGuid;
				// Frame got closed. Don't use the new nodes, we'll get new nodes using the new frame in a little while.
				if (nodeInfo.FrameClosed)
					return;
				nodes = nodeInfo.Nodes;
				language = valueNodesProvider.Language;
				forceRecreateAllNodes = nodeInfo.RecreateAllNodes;
			}
			else {
				evalInfo = null;
				nodes = Array.Empty<DbgValueNodeInfo>();
				runtimeKindGuid = null;
				language = null;
				forceRecreateAllNodes = false;
			}
			valueNodesContext.ValueNodeReader.SetEvaluationInfo(evalInfo);
			valueNodesContext.EvaluationInfo = evalInfo;

#if DEBUG
			var origEditNode = TryGetEditNode();
#endif
			RecreateRootChildrenCore_UI(nodes, runtimeKindGuid, language, forceRecreateAllNodes);
			VerifyChildren_UI(nodes);
#if DEBUG
			// PERF: make sure edit node was re-used
			Debug2.Assert(origEditNode is null || origEditNode == TryGetEditNode());
#endif

			if (selectNodeKind != SelectNodeKind.None) {
				ITreeNode? node;
				switch (selectNodeKind) {
				case SelectNodeKind.Open:
					node = rootNode.TreeNode.Children.FirstOrDefault();
					break;

				case SelectNodeKind.Added:
					node = rootNode.TreeNode.Children.LastOrDefault(a => !((ValueNodeImpl)a.Data).IsEditNode) ?? rootNode.TreeNode.Children.LastOrDefault();
					break;

				default: throw new InvalidOperationException();
				}
				selectNodeKind = SelectNodeKind.None;
				if (node is not null) {
					treeView.SelectItems(new[] { node.Data });
					treeView.ScrollIntoView();
				}
			}
		}

		// UI thread
		ValueNodeImpl? TryGetEditNode() {
			var children = rootNode.TreeNode.Children;
			if (children.Count == 0)
				return null;
			var node = (ValueNodeImpl)children[children.Count - 1].Data;
			return node.IsEditNode ? node : null;
		}

		// UI thread
		[Conditional("DEBUG")]
		void VerifyChildren_UI(DbgValueNodeInfo[] infos) {
			var children = rootNode.TreeNode.Children;
			Debug.Assert(children.Count == infos.Length + (valueNodesContext.EditValueNodeExpression.SupportsEditExpression ? 1 : 0));
			if (children.Count == infos.Length + (valueNodesContext.EditValueNodeExpression.SupportsEditExpression ? 1 : 0)) {
				for (int i = 0; i < infos.Length; i++) {
					var node = (ValueNodeImpl)children[i].Data;
					if (node.RawNode is DbgValueRawNode rootNode)
						Debug.Assert(rootNode.DebuggerValueNode == infos[i].Node);
					else if (infos[i].Node is not null && infos[i].CausesSideEffects && infos[i].Node!.HasError) {
					}
					else
						Debug2.Assert(infos[i].Node is null);
					Debug2.Assert(!valueNodesProvider.CanAddRemoveExpressions || infos[i].Id is not null, "Root IDs are required");
					Debug.Assert(infos[i].Id == node.RootId);
				}
			}
		}

		// UI thread
		void RecreateRootChildrenCore_UI(DbgValueNodeInfo[] infos, Guid? runtimeKindGuid, DbgLanguage? language, bool forceRecreateAllNodes) {
			valueNodesContext.UIDispatcher.VerifyAccess();

			bool recreateAllNodes = forceRecreateAllNodes || runtimeKindGuid != lastRuntimeKindGuid || language != lastLanguage;
			lastRuntimeKindGuid = runtimeKindGuid;
			lastLanguage = language;

			var children = rootNode.TreeNode.Children;
			int oldChildCount = children.Count;
			const int maxChildrenDiffCount = 20;

			if (recreateAllNodes || oldChildCount == 0 || infos.Length == 0 || Math.Abs(infos.Length - oldChildCount) > maxChildrenDiffCount) {
				SetNewRootChildren_UI(infos);
				return;
			}

			// PERF: Re-use as many nodes as possible so the UI is only updated when something changes.
			// Most of the time the node's UI elements don't change (same name, value, and type).
			// Recreating these elements is slow.

			var toOldIndex = new Dictionary<string, List<int>>(oldChildCount, StringComparer.Ordinal);
			for (int i = 0; i < oldChildCount; i++) {
				var node = (ValueNodeImpl)children[i].Data;
				if (node.IsEditNode)
					continue;
				var id = node.RootId ?? node.RawNode.Expression;
				if (!toOldIndex.TryGetValue(id, out var list))
					toOldIndex.Add(id, list = new List<int>(1));
				list.Add(i);
			}

			int currentNewIndex = 0;
			int updateIndex = 0;
			for (int currentOldIndex = 0; currentNewIndex < infos.Length;) {
				var (newIndex, oldIndex) = GetOldIndex(toOldIndex, infos, currentNewIndex, currentOldIndex);
				Debug.Assert((oldIndex < 0) == (newIndex < 0));
				bool lastIter = oldIndex < 0;
				if (lastIter) {
					newIndex = infos.Length;
					oldIndex = oldChildCount;

					// Check if all nodes were removed
					if (currentNewIndex == 0) {
						SetNewRootChildren_UI(infos);
						return;
					}
				}

				// Delete M nodes, create N nodes, but try to re-use min(M, N) nodes
				int deleteCount = oldIndex - currentOldIndex;
				for (;;) {
					if (currentNewIndex < newIndex) {
						var info = infos[currentNewIndex];
						ValueNodeImpl node;
						if (deleteCount > 0 && !(node = (ValueNodeImpl)children[updateIndex].Data).IsEditNode) {
							node.Reuse(info.Node, info.Id, info.Expression, info.ErrorMessage);
							deleteCount--;
						}
						else
							children.Insert(updateIndex, treeView.Create(new ValueNodeImpl(valueNodesContext, info.Node, info.Id, info.Expression, info.ErrorMessage)));
						currentNewIndex++;
						updateIndex++;
					}
					else if (deleteCount > 0) {
						if (!(deleteCount == 1 && ((ValueNodeImpl)children[updateIndex].Data).IsEditNode))
							children.RemoveAt(updateIndex);
						deleteCount--;
					}
					else
						break;
				}

				if (lastIter)
					break;
				Debug.Assert(updateIndex < children.Count);
				var reusedNode = (ValueNodeImpl)children[updateIndex++].Data;
				reusedNode.SetDebuggerValueNodeForRoot(infos[currentNewIndex++]);
				currentOldIndex = oldIndex + 1;
			}
			while (updateIndex < children.Count && !((ValueNodeImpl)children[updateIndex].Data).IsEditNode)
				children.RemoveAt(updateIndex);
			if (valueNodesContext.EditValueNodeExpression.SupportsEditExpression && TryGetEditNode() is null)
				children.Add(treeView.Create(ValueNodeImpl.CreateEditNode(valueNodesContext)));
		}

		static (int newIndex, int oldIndex) GetOldIndex(Dictionary<string, List<int>> dict, DbgValueNodeInfo[] newNodes, int newIndex, int minOldIndex) {
			for (; newIndex < newNodes.Length; newIndex++) {
				var info = newNodes[newIndex];
				if (dict.TryGetValue(info.Id ?? info.Node!.Expression, out var list)) {
					for (int i = 0; i < list.Count; i++) {
						int oldIndex = list[i];
						if (oldIndex >= minOldIndex)
							return (newIndex, oldIndex);
					}
					return (-1, -1);
				}
			}
			return (-1, -1);
		}

		// UI thread
		void SetNewRootChildren_UI(DbgValueNodeInfo[] infos) {
			valueNodesContext.UIDispatcher.VerifyAccess();

			// Always clear the selection. If there's lots of items (eg. locals) and we switch the language,
			// it's possible for the treeview to select a node which will get reformatted with the wrong
			// language (and it will throw an exception).
			// Another reason to clear it is that the treeview has bad perf when too many items are selected.
			treeView.SelectItems(Array.Empty<TreeNodeData>());

			if (valueNodesContext.EditValueNodeExpression.SupportsEditExpression) {
				var children = rootNode.TreeNode.Children;
				var editNode = TryGetEditNode();
				if (infos.Length == 0 && children.Count == 1 && editNode is not null)
					return;
				if (children.Count < 30) {
					while (children.Count > 0 && editNode != children[0].Data)
						children.RemoveAt(0);
					for (int i = 0; i < infos.Length; i++) {
						var info = infos[i];
						children.Insert(i, treeView.Create(new ValueNodeImpl(valueNodesContext, info.Node, info.Id, info.Expression, info.ErrorMessage)));
					}
					if (editNode is null)
						rootNode.TreeNode.AddChild(treeView.Create(ValueNodeImpl.CreateEditNode(valueNodesContext)));
				}
				else {
					children.Clear();
					foreach (var info in infos)
						rootNode.TreeNode.AddChild(treeView.Create(new ValueNodeImpl(valueNodesContext, info.Node, info.Id, info.Expression, info.ErrorMessage)));
					rootNode.TreeNode.AddChild(editNode?.TreeNode ?? treeView.Create(ValueNodeImpl.CreateEditNode(valueNodesContext)));
				}
			}
			else {
				rootNode.TreeNode.Children.Clear();
				foreach (var info in infos)
					rootNode.TreeNode.AddChild(treeView.Create(new ValueNodeImpl(valueNodesContext, info.Node, info.Id, info.Expression, info.ErrorMessage)));
			}
		}

		// UI thread
		void IValueNodesVM.Show() {
			valueNodesContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: true);
		}

		// UI thread
		void IValueNodesVM.Hide() {
			valueNodesContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: false);
		}

		// UI thread
		void InitializeDebugger_UI(bool enable) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			isOpen = enable;
			refreshNameFields = false;
			if (enable) {
				valueNodesContext.ClassificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
				dbgEvalFormatterSettings.PropertyChanged += DbgEvalFormatterSettings_PropertyChanged;
				valueNodesContext.UIVersion++;
				valueNodesContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				valueNodesContext.HighlightChangedVariables = debuggerSettings.HighlightChangedVariables;
				valueNodesContext.NameEditValueProvider.Language = valueNodesProvider.Language;
				valueNodesContext.ValueEditValueProvider.Language = valueNodesProvider.Language;
				valueNodesContext.Formatter.Language = valueNodesProvider.Language;
				UpdateFormatterOptions();
				UpdateEvaluationOptions();
				valueNodesContext.IsWindowReadOnly = valueNodesProvider.IsReadOnly;
				valueNodesProvider.NodesChanged += ValueNodesProvider_NodesChanged;
				valueNodesProvider.IsReadOnlyChanged += ValueNodesProvider_IsReadOnlyChanged;
				valueNodesProvider.LanguageChanged += ValueNodesProvider_LanguageChanged;
				lastRuntimeKindGuid = null;
				selectNodeKind = SelectNodeKind.Open;
				dbgObjectIdService.ObjectIdsChanged += DbgObjectIdService_ObjectIdsChanged;
			}
			else {
				valueNodesContext.ClassificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged -= DebuggerSettings_PropertyChanged;
				dbgEvalFormatterSettings.PropertyChanged -= DbgEvalFormatterSettings_PropertyChanged;
				valueNodesProvider.NodesChanged -= ValueNodesProvider_NodesChanged;
				valueNodesProvider.IsReadOnlyChanged -= ValueNodesProvider_IsReadOnlyChanged;
				valueNodesProvider.LanguageChanged -= ValueNodesProvider_LanguageChanged;
				dbgObjectIdService.ObjectIdsChanged -= DbgObjectIdService_ObjectIdsChanged;
				valueNodesContext.IsWindowReadOnly = true;
				lastRuntimeKindGuid = null;
				selectNodeKind = SelectNodeKind.None;
				valueNodesContext.NameEditValueProvider.Language = null;
				valueNodesContext.ValueEditValueProvider.Language = null;
				valueNodesContext.Formatter.Language = null;
			}
			RecreateRootChildren_UI();

			// IsWindowReadOnly changed
			RefreshThemeFields_UI();
		}

		// random thread
		void UI(Action callback) => valueNodesContext.UIDispatcher.UI(callback);

		// UI thread
		void ClassificationFormatMap_ClassificationFormatMappingChanged(object? sender, EventArgs e) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			valueNodesContext.UIVersion++;
			RefreshThemeFields_UI();
		}

		// random thread
		void DebuggerSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e) =>
			UI(() => DebuggerSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DebuggerSettings_PropertyChanged_UI(string? propertyName) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			switch (propertyName) {
			case nameof(DebuggerSettings.UseHexadecimal):
			case nameof(DebuggerSettings.UseDigitSeparators):
			case nameof(DebuggerSettings.UseStringConversionFunction):
				RefreshAllColumns_UI();
				break;

			case nameof(DebuggerSettings.SyntaxHighlight):
				valueNodesContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				RefreshThemeFields_UI();
				break;

			case nameof(DebuggerSettings.PropertyEvalAndFunctionCalls):
			case nameof(DebuggerSettings.ShowRawStructureOfObjects):
			case nameof(DebuggerSettings.HideCompilerGeneratedMembers):
			case nameof(DebuggerSettings.RespectHideMemberAttributes):
			case nameof(DebuggerSettings.HideDeprecatedError):
			case nameof(DebuggerSettings.ShowOnlyPublicMembers):
				UpdateFormatterOptions();
				UpdateEvaluationOptions();
				RecreateRootChildren_UI();
				break;

			case nameof(DebuggerSettings.HighlightChangedVariables):
				valueNodesContext.HighlightChangedVariables = debuggerSettings.HighlightChangedVariables;
				RefreshNodes(RefreshNodeOptions.RefreshValueControl);
				break;
			}
		}

		// random thread
		void DbgEvalFormatterSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e) =>
			UI(() => DbgEvalFormatterSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DbgEvalFormatterSettings_PropertyChanged_UI(string? propertyName) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			switch (propertyName) {
			case nameof(DbgEvalFormatterSettings.ShowNamespaces):
			case nameof(DbgEvalFormatterSettings.ShowIntrinsicTypeKeywords):
			case nameof(DbgEvalFormatterSettings.ShowTokens):
				RefreshAllColumns_UI();
				break;

			default:
				Debug.Fail($"Unknown property name: {propertyName}");
				break;
			}
		}

		// UI thread
		void RefreshThemeFields_UI() {
			valueNodesContext.UIDispatcher.VerifyAccess();
			const RefreshNodeOptions options =
				RefreshNodeOptions.RefreshNameControl |
				RefreshNodeOptions.RefreshValueControl |
				RefreshNodeOptions.RefreshTypeControl;
			RefreshNodes(options);
		}

		// UI thread
		void RefreshAllColumns_UI() {
			valueNodesContext.UIDispatcher.VerifyAccess();
			UpdateFormatterOptions();
			const RefreshNodeOptions options =
				RefreshNodeOptions.RefreshName |
				RefreshNodeOptions.RefreshNameControl |
				RefreshNodeOptions.RefreshValue |
				RefreshNodeOptions.RefreshValueControl |
				RefreshNodeOptions.RefreshType |
				RefreshNodeOptions.RefreshTypeControl;
			RefreshNodes(options);
		}

		// UI thread
		void RefreshNodes(RefreshNodeOptions options) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			valueNodesContext.RefreshNodeOptions = options;
			treeView.RefreshAllNodes();
		}

		// UI thread
		void UpdateFormatterOptions() {
			valueNodesContext.UIDispatcher.VerifyAccess();
			var valueFormatterOptions = GetValueFormatterOptions(isEdit: false);
			valueNodesContext.ValueNodeFormatParameters.NameFormatterOptions = valueFormatterOptions;
			valueNodesContext.ValueNodeFormatParameters.ValueFormatterOptions = valueFormatterOptions;
			valueNodesContext.ValueNodeFormatParameters.TypeFormatterOptions = valueFormatterOptions;
			valueNodesContext.ValueNodeFormatParameters.ValueFormatterTypeOptions = GetTypeFormatterOptions();
		}

		// UI thread
		void UpdateEvaluationOptions() {
			valueNodesContext.UIDispatcher.VerifyAccess();
			valueNodesContext.EvaluationOptions = GetEvaluationOptions();
			valueNodesContext.ValueNodeEvaluationOptions = GetValueNodeEvaluationOptions();
			valueNodesContext.ValueNodeReader.SetValueNodeEvaluationOptions(valueNodesContext.ValueNodeEvaluationOptions);
		}

		DbgValueFormatterOptions GetValueFormatterOptions(bool isEdit) {
			var options = DbgValueFormatterOptions.None;
			if (isEdit)
				options |= DbgValueFormatterOptions.Edit;
			if (!debuggerSettings.UseHexadecimal)
				options |= DbgValueFormatterOptions.Decimal;
			if (debuggerSettings.PropertyEvalAndFunctionCalls)
				options |= DbgValueFormatterOptions.FuncEval;
			if (debuggerSettings.UseStringConversionFunction && debuggerSettings.PropertyEvalAndFunctionCalls)
				options |= DbgValueFormatterOptions.ToString;
			if (debuggerSettings.UseDigitSeparators)
				options |= DbgValueFormatterOptions.DigitSeparators;
			if (debuggerSettings.FullString)
				options |= DbgValueFormatterOptions.FullString;
			if (dbgEvalFormatterSettings.ShowNamespaces)
				options |= DbgValueFormatterOptions.Namespaces;
			if (dbgEvalFormatterSettings.ShowIntrinsicTypeKeywords)
				options |= DbgValueFormatterOptions.IntrinsicTypeKeywords;
			if (dbgEvalFormatterSettings.ShowTokens)
				options |= DbgValueFormatterOptions.Tokens;
			return options;
		}

		DbgValueFormatterTypeOptions GetTypeFormatterOptions() {
			var options = DbgValueFormatterTypeOptions.None;
			if (!debuggerSettings.UseHexadecimal)
				options |= DbgValueFormatterTypeOptions.Decimal;
			if (debuggerSettings.UseDigitSeparators)
				options |= DbgValueFormatterTypeOptions.DigitSeparators;
			if (dbgEvalFormatterSettings.ShowNamespaces)
				options |= DbgValueFormatterTypeOptions.Namespaces;
			if (dbgEvalFormatterSettings.ShowIntrinsicTypeKeywords)
				options |= DbgValueFormatterTypeOptions.IntrinsicTypeKeywords;
			if (dbgEvalFormatterSettings.ShowTokens)
				options |= DbgValueFormatterTypeOptions.Tokens;
			return options;
		}

		DbgEvaluationOptions GetEvaluationOptions() {
			var options = DbgEvaluationOptions.Expression;
			if (!debuggerSettings.PropertyEvalAndFunctionCalls)
				options |= DbgEvaluationOptions.NoFuncEval;
			return options;
		}

		DbgValueNodeEvaluationOptions GetValueNodeEvaluationOptions() {
			var options = DbgValueNodeEvaluationOptions.None;
			if (!debuggerSettings.PropertyEvalAndFunctionCalls)
				options |= DbgValueNodeEvaluationOptions.NoFuncEval;
			if (debuggerSettings.ShowRawStructureOfObjects)
				options |= DbgValueNodeEvaluationOptions.RawView;
			if (debuggerSettings.HideCompilerGeneratedMembers)
				options |= DbgValueNodeEvaluationOptions.HideCompilerGeneratedMembers;
			if (debuggerSettings.RespectHideMemberAttributes)
				options |= DbgValueNodeEvaluationOptions.RespectHideMemberAttributes;
			if (debuggerSettings.HideDeprecatedError)
				options |= DbgValueNodeEvaluationOptions.HideDeprecatedError;
			if (debuggerSettings.ShowOnlyPublicMembers)
				options |= DbgValueNodeEvaluationOptions.PublicMembers;
			return options;
		}

		bool IValueNodesVM.CanAddRemoveExpressions {
			get {
				valueNodesContext.UIDispatcher.VerifyAccess();
				return valueNodesProvider.CanAddRemoveExpressions;
			}
		}

		void IValueNodesVM.DeleteExpressions(string[] ids) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			if (!valueNodesProvider.CanAddRemoveExpressions)
				throw new InvalidOperationException();
			valueNodesProvider.DeleteExpressions(ids);
			RecreateRootChildrenDelay_UI();
		}

		void IValueNodesVM.ClearAllExpressions() {
			valueNodesContext.UIDispatcher.VerifyAccess();
			if (!valueNodesProvider.CanAddRemoveExpressions)
				throw new InvalidOperationException();
			valueNodesProvider.ClearAllExpressions();
			RecreateRootChildrenDelay_UI();
		}

		void IValueNodesVM.EditExpression(string? id, string expression) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			if (!valueNodesProvider.CanAddRemoveExpressions)
				throw new InvalidOperationException();
			valueNodesProvider.EditExpression(id, expression);
			RecreateRootChildrenDelay_UI();
		}

		void IValueNodesVM.AddExpressions(string[] expressions, bool select) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			if (!valueNodesProvider.CanAddRemoveExpressions)
				throw new InvalidOperationException();
			valueNodesProvider.AddExpressions(expressions);
			selectNodeKind = select ? SelectNodeKind.Added : SelectNodeKind.None;
			RecreateRootChildrenDelay_UI();
		}

		void IValueNodesVM.Refresh() {
			valueNodesContext.UIDispatcher.VerifyAccess();
			if (!isOpen || valueNodesContext.IsWindowReadOnly)
				return;
			valueNodesProvider.RefreshAllNodes();
		}

		bool IEditValueNodeExpression.SupportsEditExpression => ((IValueNodesVM)this).CanAddRemoveExpressions;
		void IEditValueNodeExpression.EditExpression(string? id, string expression) => ((IValueNodesVM)this).EditExpression(id, expression);
		void IEditValueNodeExpression.AddExpressions(string[] expressions) => ((IValueNodesVM)this).AddExpressions(expressions);

		void IDisposable.Dispose() {
			valueNodesContext.UIDispatcher.VerifyAccess();
			treeView.Dispose();
		}
	}
}

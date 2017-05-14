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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.TreeView;
using dnSpy.Debugger.UI;
using dnSpy.Debugger.UI.Wpf;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	sealed class ValueNodesVM : ViewModelBase, IValueNodesVM {
		bool IValueNodesVM.IsOpen => isOpen;
		bool IValueNodesVM.IsReadOnly => isReadOnly;
		ITreeView IValueNodesVM.TreeView => treeView;
		Guid? IValueNodesVM.RuntimeGuid => valueNodesProvider.Language?.RuntimeGuid;

		sealed class RootNode : TreeNodeData {
			public override Guid Guid => Guid.Empty;
			public override object Text => null;
			public override object ToolTip => null;
			public override ImageReference Icon => ImageReference.None;
			public override void OnRefreshUI() { }
		}

		readonly ValueNodesProvider valueNodesProvider;
		readonly DebuggerSettings debuggerSettings;
		readonly DbgEvalFormatterSettings dbgEvalFormatterSettings;
		readonly ValueNodesContext valueNodesContext;
		readonly ITreeView treeView;
		readonly RootNode rootNode;
		bool isOpen;
		bool isReadOnly;

		sealed class GuidObjectsProvider : IGuidObjectsProvider {
			readonly IValueNodesVM vm;
			public GuidObjectsProvider(IValueNodesVM vm) => this.vm = vm ?? throw new ArgumentNullException(nameof(vm));

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsProviderArgs args) {
				yield return new GuidObject(ValueNodesVMConstants.GUIDOBJ_VALUENODESVM_GUID, vm);
			}
		}

		public ValueNodesVM(UIDispatcher uiDispatcher, ValueNodesVMOptions options, ITreeViewService treeViewService, LanguageEditValueProviderFactory languageEditValueProviderFactory, DbgValueNodeImageReferenceService dbgValueNodeImageReferenceService, DebuggerSettings debuggerSettings, DbgEvalFormatterSettings dbgEvalFormatterSettings, IClassificationFormatMapService classificationFormatMapService, ITextBlockContentInfoFactory textBlockContentInfoFactory, IMenuService menuService, IWpfCommandService wpfCommandService) {
			uiDispatcher.VerifyAccess();
			valueNodesProvider = options.NodesProvider;
			this.debuggerSettings = debuggerSettings;
			this.dbgEvalFormatterSettings = dbgEvalFormatterSettings;
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			valueNodesContext = new ValueNodesContext(uiDispatcher, options.WindowContentType, options.NameColumnName, options.ValueColumnName, options.TypeColumnName, languageEditValueProviderFactory, dbgValueNodeImageReferenceService, new DbgValueNodeReaderImpl(), classificationFormatMap, textBlockContentInfoFactory, options.ShowMessageBox);

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
		void ValueNodesProvider_NodesChanged(object sender, EventArgs e) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			RecreateRootChildren_UI();
		}

		// UI thread
		void ValueNodesProvider_IsReadOnlyChanged(object sender, EventArgs e) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			isReadOnly = valueNodesProvider.IsReadOnly;
		}

		// UI thread
		void ValueNodesProvider_LanguageChanged(object sender, EventArgs e) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			valueNodesContext.ValueEditValueProvider.Language = valueNodesProvider.Language;
		}

		// UI thread
		void RecreateRootChildren_UI() {
			valueNodesContext.UIDispatcher.VerifyAccess();
			var nodes = isOpen ? valueNodesProvider.GetNodes() : Array.Empty<DbgValueNodeInfo>();
			RecreateRootChildrenCore_UI(nodes);
			VerifyChildren_UI(nodes);
		}

		// UI thread
		[Conditional("DEBUG")]
		void VerifyChildren_UI(DbgValueNodeInfo[] infos) {
			var children = rootNode.TreeNode.Children;
			Debug.Assert(children.Count == infos.Length);
			if (children.Count == infos.Length) {
				for (int i = 0; i < infos.Length; i++) {
					var node = (ValueNodeImpl)children[i].Data;
					Debug.Assert(node.DebuggerValueNode == infos[i].Node);
				}
			}
		}

		// UI thread
		void RecreateRootChildrenCore_UI(DbgValueNodeInfo[] infos) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			if (infos.Length > 0)
				infos[0].Node.Runtime.CloseOnContinue(infos.Select(a => a.Node));

			if (infos.Length == 0 || rootNode.TreeNode.Children.Count == 0) {
				SetNewRootChildren_UI(infos);
				return;
			}

			// PERF: Re-use as many nodes as possible so the UI is only updated when something changes.
			// Most of the time the node's UI elements don't change (same name, value, and type).
			// Recreating these elements is slow.

			var children = rootNode.TreeNode.Children;
			int oldChildCount = children.Count;
			var toOldIndex = new Dictionary<string, List<int>>(oldChildCount, StringComparer.Ordinal);
			for (int i = 0; i < oldChildCount; i++) {
				var node = (ValueNodeImpl)children[i].Data;
				var id = node.RootId ?? node.DebuggerValueNode.Expression;
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

				int deleteCount = oldIndex - currentOldIndex;
				for (int i = deleteCount - 1; i >= 0; i--) {
					Debug.Assert(updateIndex + i < children.Count);
					children.RemoveAt(updateIndex + i);
				}

				for (; currentNewIndex < newIndex; currentNewIndex++) {
					Debug.Assert(updateIndex <= children.Count);
					var info = infos[currentNewIndex];
					children.Insert(updateIndex++, treeView.Create(new ValueNodeImpl(valueNodesContext, info.Node, info.Id)));
				}

				if (lastIter)
					break;
				Debug.Assert(updateIndex < children.Count);
				var reusedNode = (ValueNodeImpl)children[updateIndex++].Data;
				reusedNode.SetDebuggerValueNodeForRoot(infos[currentNewIndex++].Node);
				currentOldIndex = oldIndex + 1;
			}
			while (children.Count != updateIndex)
				children.RemoveAt(children.Count - 1);
		}

		static (int newIndex, int oldIndex) GetOldIndex(Dictionary<string, List<int>> dict, DbgValueNodeInfo[] newNodes, int newIndex, int minOldIndex) {
			for (; newIndex < newNodes.Length; newIndex++) {
				var info = newNodes[newIndex];
				if (dict.TryGetValue(info.Id ?? info.Node.Expression, out var list)) {
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
			rootNode.TreeNode.Children.Clear();
			foreach (var info in infos)
				rootNode.TreeNode.AddChild(treeView.Create(new ValueNodeImpl(valueNodesContext, info.Node, info.Id)));
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
			if (enable) {
				valueNodesContext.ClassificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
				dbgEvalFormatterSettings.PropertyChanged += DbgEvalFormatterSettings_PropertyChanged;
				valueNodesContext.UIVersion++;
				valueNodesContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				valueNodesContext.HighlightChangedVariables = debuggerSettings.HighlightChangedVariables;
				valueNodesContext.ValueEditValueProvider.Language = valueNodesProvider.Language;
				UpdateFormatterOptions();
				isReadOnly = valueNodesProvider.IsReadOnly;
				valueNodesProvider.NodesChanged += ValueNodesProvider_NodesChanged;
				valueNodesProvider.IsReadOnlyChanged += ValueNodesProvider_IsReadOnlyChanged;
				valueNodesProvider.LanguageChanged += ValueNodesProvider_LanguageChanged;
			}
			else {
				valueNodesContext.ClassificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged -= DebuggerSettings_PropertyChanged;
				dbgEvalFormatterSettings.PropertyChanged -= DbgEvalFormatterSettings_PropertyChanged;
				valueNodesProvider.NodesChanged -= ValueNodesProvider_NodesChanged;
				valueNodesProvider.IsReadOnlyChanged -= ValueNodesProvider_IsReadOnlyChanged;
				valueNodesProvider.LanguageChanged -= ValueNodesProvider_LanguageChanged;
				isReadOnly = true;
			}
			RecreateRootChildren_UI();
		}

		// random thread
		void UI(Action callback) => valueNodesContext.UIDispatcher.UI(callback);

		// UI thread
		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			valueNodesContext.UIVersion++;
			RefreshThemeFields_UI();
		}

		// random thread
		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => DebuggerSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DebuggerSettings_PropertyChanged_UI(string propertyName) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			switch (propertyName) {
			case nameof(DebuggerSettings.UseHexadecimal):
				RefreshHexFields_UI();
				break;

			case nameof(DebuggerSettings.SyntaxHighlight):
				valueNodesContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				RefreshThemeFields_UI();
				break;

			case nameof(DebuggerSettings.PropertyEvalAndFunctionCalls):
			case nameof(DebuggerSettings.UseStringConversionFunction):
				UpdateFormatterOptions();
				const RefreshNodeOptions options =
					RefreshNodeOptions.RefreshValue |
					RefreshNodeOptions.RefreshValueControl;
				RefreshNodes(options);
				break;

			case nameof(DebuggerSettings.HighlightChangedVariables):
				valueNodesContext.HighlightChangedVariables = debuggerSettings.HighlightChangedVariables;
				RefreshNodes(RefreshNodeOptions.RefreshValueControl);
				break;
			}
		}

		// random thread
		void DbgEvalFormatterSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => DbgEvalFormatterSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DbgEvalFormatterSettings_PropertyChanged_UI(string propertyName) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			switch (propertyName) {
			case nameof(DbgEvalFormatterSettings.ShowDeclaringTypes):
			case nameof(DbgEvalFormatterSettings.ShowNamespaces):
			case nameof(DbgEvalFormatterSettings.ShowIntrinsicTypeKeywords):
			case nameof(DbgEvalFormatterSettings.ShowTokens):
				UpdateFormatterOptions();
				const RefreshNodeOptions options =
					RefreshNodeOptions.RefreshValue |
					RefreshNodeOptions.RefreshValueControl |
					RefreshNodeOptions.RefreshType |
					RefreshNodeOptions.RefreshTypeControl;
				RefreshNodes(options);
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
		void RefreshHexFields_UI() {
			valueNodesContext.UIDispatcher.VerifyAccess();
			UpdateFormatterOptions();
			const RefreshNodeOptions options =
				RefreshNodeOptions.RefreshValue |
				RefreshNodeOptions.RefreshValueControl;
			RefreshNodes(options);
		}

		// UI thread
		void RefreshNodes(RefreshNodeOptions options) {
			valueNodesContext.UIDispatcher.VerifyAccess();
			valueNodesContext.RefreshNodeOptions = options;
			treeView.RefreshAllNodes();
		}

		void UpdateFormatterOptions() {
			valueNodesContext.UIDispatcher.VerifyAccess();
			valueNodesContext.ValueNodeFormatParameters.ValueFormatterOptions = GetValueFormatterOptions(isDisplay: true);
			valueNodesContext.ValueNodeFormatParameters.TypeFormatterOptions = GetTypeFormatterOptions();
		}

		DbgValueFormatterOptions GetValueFormatterOptions(bool isDisplay) {
			var flags = DbgValueFormatterOptions.None;
			if (isDisplay)
				flags |= DbgValueFormatterOptions.Display;
			if (!debuggerSettings.UseHexadecimal)
				flags |= DbgValueFormatterOptions.Decimal;
			if (debuggerSettings.PropertyEvalAndFunctionCalls)
				flags |= DbgValueFormatterOptions.FuncEval;
			if (debuggerSettings.UseStringConversionFunction)
				flags |= DbgValueFormatterOptions.ToString;
			if (dbgEvalFormatterSettings.ShowDeclaringTypes)
				flags |= DbgValueFormatterOptions.DeclaringTypes;
			if (dbgEvalFormatterSettings.ShowNamespaces)
				flags |= DbgValueFormatterOptions.Namespaces;
			if (dbgEvalFormatterSettings.ShowIntrinsicTypeKeywords)
				flags |= DbgValueFormatterOptions.IntrinsicTypeKeywords;
			if (dbgEvalFormatterSettings.ShowTokens)
				flags |= DbgValueFormatterOptions.Tokens;
			return flags;
		}

		DbgValueFormatterTypeOptions GetTypeFormatterOptions() {
			var flags = DbgValueFormatterTypeOptions.None;
			if (dbgEvalFormatterSettings.ShowDeclaringTypes)
				flags |= DbgValueFormatterTypeOptions.DeclaringTypes;
			if (dbgEvalFormatterSettings.ShowNamespaces)
				flags |= DbgValueFormatterTypeOptions.Namespaces;
			if (dbgEvalFormatterSettings.ShowIntrinsicTypeKeywords)
				flags |= DbgValueFormatterTypeOptions.IntrinsicTypeKeywords;
			if (dbgEvalFormatterSettings.ShowTokens)
				flags |= DbgValueFormatterTypeOptions.Tokens;
			return flags;
		}

		void IDisposable.Dispose() {
			valueNodesContext.UIDispatcher.VerifyAccess();
			treeView.Dispose();
		}
	}
}

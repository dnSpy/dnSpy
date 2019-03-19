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
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.TreeView;
using dnSpy.Documents.TreeView;

namespace dnSpy.Documents.Tabs {
	[Export, Export(typeof(IDocumentTabService))]
	sealed class DocumentTabService : IDocumentTabService {
		IDocumentTreeView IDocumentTabService.DocumentTreeView => DocumentTreeView;
		public DocumentTreeView DocumentTreeView { get; }
		public ITabGroupService TabGroupService { get; }

		IEnumerable<TabContentImpl> AllTabContentImpls {
			get {
				foreach (var g in TabGroupService.TabGroups) {
					foreach (TabContentImpl impl in g.TabContents)
						yield return impl;
				}
			}
		}

		ITabGroup SafeActiveTabGroup {
			get {
				var g = TabGroupService.ActiveTabGroup;
				if (g != null)
					return g;
				return TabGroupService.Create();
			}
		}

		TabContentImpl ActiveTabContentImpl => (TabContentImpl)TabGroupService.ActiveTabGroup?.ActiveTabContent;

		TabContentImpl SafeActiveTabContentImpl {
			get {
				var g = SafeActiveTabGroup;
				var impl = (TabContentImpl)g.ActiveTabContent;
				if (impl != null)
					return impl;
				return CreateNewTab(g);
			}
		}

		TabContentImpl CreateNewTab(ITabGroup tabGroup) {
			var impl = new TabContentImpl(this, documentTabUIContextLocatorProvider.Create(), referenceDocumentTabContentProviders, defaultDocumentTabContentProviders, referenceHandlers);
			tabGroup.Add(impl);
			return impl;
		}

		IDocumentTab IDocumentTabService.ActiveTab {
			get => ActiveTabContentImpl;
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				var impl = value as TabContentImpl;
				if (impl == null)
					throw new InvalidOperationException();
				var g = GetTabGroup(impl);
				if (g == null)
					throw new InvalidOperationException();
				g.ActiveTabContent = impl;
				TabGroupService.ActiveTabGroup = g;
			}
		}

		ITabGroup GetTabGroup(TabContentImpl impl) {
			foreach (var g in TabGroupService.TabGroups) {
				if (g.TabContents.Contains(impl))
					return g;
			}
			return null;
		}

		IDocumentTab IDocumentTabService.GetOrCreateActiveTab() => SafeActiveTabContentImpl;

		public IEnumerable<IDocumentTab> SortedTabs {
			get {
				var groups = TabGroupService.TabGroups.ToArray();
				if (groups.Length == 0)
					yield break;
				int startGroupIndex = Array.IndexOf(groups, TabGroupService.ActiveTabGroup);
				Debug.Assert(startGroupIndex >= 0);
				for (int i = 0; i < groups.Length; i++) {
					var g = groups[(startGroupIndex + i) % groups.Length];

					var contents = g.TabContents.Cast<TabContentImpl>().ToArray();
					if (contents.Length == 0)
						continue;
					int startContentIndex = Array.IndexOf(contents, g.ActiveTabContent);
					Debug.Assert(startContentIndex >= 0);
					for (int j = 0; j < contents.Length; j++) {
						var c = contents[(startContentIndex + j) % contents.Length];
						yield return c;
					}
				}
			}
		}

		public IEnumerable<IDocumentTab> VisibleFirstTabs {
			get {
				var hash = new HashSet<IDocumentTab>();
				foreach (var g in TabGroupService.TabGroups) {
					var c = (TabContentImpl)g.ActiveTabContent;
					if (c != null) {
						hash.Add(c);
						yield return c;
					}
				}
				foreach (var c in SortedTabs) {
					if (!hash.Contains(c))
						yield return c;
				}
			}
		}

		public IDocumentTabServiceSettings Settings { get; }

		readonly IDocumentTabUIContextLocatorProvider documentTabUIContextLocatorProvider;
		readonly ITabService tabService;
		readonly IDocumentTabContentFactoryService documentTabContentFactoryService;
		readonly IWpfFocusService wpfFocusService;
		readonly IDecompilationCache decompilationCache;
		readonly Lazy<IReferenceDocumentTabContentProvider, IReferenceDocumentTabContentProviderMetadata>[] referenceDocumentTabContentProviders;
		readonly Lazy<IDefaultDocumentTabContentProvider, IDefaultDocumentTabContentProviderMetadata>[] defaultDocumentTabContentProviders;
		readonly Lazy<IReferenceHandler, IReferenceHandlerMetadata>[] referenceHandlers;

		[ImportingConstructor]
		DocumentTabService(IDocumentTabUIContextLocatorProvider documentTabUIContextLocatorProvider, DocumentTreeView documentTreeView, ITabServiceProvider tabServiceProvider, IDocumentTabContentFactoryService documentTabContentFactoryService, IDocumentTabServiceSettings documentTabServiceSettings, IWpfFocusService wpfFocusService, IDecompilationCache decompilationCache, [ImportMany] IEnumerable<Lazy<IReferenceDocumentTabContentProvider, IReferenceDocumentTabContentProviderMetadata>> referenceDocumentTabContentProviders, [ImportMany] IEnumerable<Lazy<IDefaultDocumentTabContentProvider, IDefaultDocumentTabContentProviderMetadata>> defaultDocumentTabContentProviders, [ImportMany] IEnumerable<Lazy<IReferenceHandler, IReferenceHandlerMetadata>> referenceHandlers) {
			Settings = documentTabServiceSettings;
			this.documentTabUIContextLocatorProvider = documentTabUIContextLocatorProvider;
			this.documentTabContentFactoryService = documentTabContentFactoryService;
			this.wpfFocusService = wpfFocusService;
			this.decompilationCache = decompilationCache;
			this.referenceDocumentTabContentProviders = referenceDocumentTabContentProviders.OrderBy(a => a.Metadata.Order).ToArray();
			this.defaultDocumentTabContentProviders = defaultDocumentTabContentProviders.OrderBy(a => a.Metadata.Order).ToArray();
			this.referenceHandlers = referenceHandlers.OrderBy(a => a.Metadata.Order).ToArray();
			var tvElem = documentTreeView.TreeView.UIObject;
			tvElem.IsVisibleChanged += TreeView_IsVisibleChanged;
			isTreeViewVisible = tvElem.IsVisible;
			DocumentTreeView = documentTreeView;
			DocumentTreeView.DocumentService.CollectionChanged += DocumentService_CollectionChanged;
			DocumentTreeView.SelectionChanged += DocumentTreeView_SelectionChanged;
			DocumentTreeView.NodesTextChanged += DocumentTreeView_NodesTextChanged;
			DocumentTreeView.NodeActivated += DocumentTreeView_NodeActivated;
			DocumentTreeView.TreeView.NodeRemoved += TreeView_NodeRemoved;
			tabService = tabServiceProvider.Create();
			TabGroupService = tabService.Create(new TabGroupServiceOptions(MenuConstants.GUIDOBJ_DOCUMENTS_TABCONTROL_GUID));
			TabGroupService.TabSelectionChanged += TabGroupService_TabSelectionChanged;
			TabGroupService.TabGroupSelectionChanged += TabGroupService_TabGroupSelectionChanged;
		}

		void TreeView_NodeRemoved(object sender, TreeViewNodeRemovedEventArgs e) {
			if (!e.Removed)
				return;

			var documentNode = e.Node as DsDocumentNode;
			if (documentNode == null)
				return;
			OnNodeRemoved(documentNode);
		}

		void OnNodeRemoved(DsDocumentNode node) {
			var hash = GetSelfAndDsDocumentNodeChildren(node);
			foreach (TabContentImpl tab in VisibleFirstTabs)
				tab.OnNodesRemoved(hash, () => CreateTabContent(Array.Empty<DocumentTreeNodeData>()));
			decompilationCache.Clear(new HashSet<IDsDocument>(hash.Select(a => a.Document)));
		}

		static HashSet<DsDocumentNode> GetSelfAndDsDocumentNodeChildren(DsDocumentNode node, HashSet<DsDocumentNode> hash = null) {
			if (hash == null)
				hash = new HashSet<DsDocumentNode>();
			hash.Add(node);
			foreach (var c in node.TreeNode.DataChildren) {
				if (c is DsDocumentNode documentNode)
					GetSelfAndDsDocumentNodeChildren(documentNode, hash);
			}
			return hash;
		}

		public event EventHandler<NotifyDocumentCollectionChangedEventArgs> DocumentCollectionChanged;
		bool disable_DocumentCollectionChanged = false;
		void DocumentService_CollectionChanged(object sender, NotifyDocumentCollectionChangedEventArgs e) => CallDocumentCollectionChanged(e);
		void CallDocumentCollectionChanged(NotifyDocumentCollectionChangedEventArgs e) {
			if (disable_DocumentCollectionChanged)
				return;
			DocumentCollectionChanged?.Invoke(this, e);
		}

		void TabGroupService_TabGroupSelectionChanged(object sender, TabGroupSelectedEventArgs e) {
			if (e.Unselected != null) {
				var impl = (TabContentImpl)e.Unselected.ActiveTabContent;
				if (impl != null)
					impl.OnUnselected();
			}
			if (e.Selected != null) {
				var impl = (TabContentImpl)e.Selected.ActiveTabContent;
				if (impl != null) {
					impl.OnSelected();
					OnNewTabContentShown(impl);
				}
			}
		}

		void TabGroupService_TabSelectionChanged(object sender, TabSelectedEventArgs e) {
			if (e.Unselected != null) {
				var impl = (TabContentImpl)e.Unselected;
				impl.OnUnselected();
			}
			if (e.Selected != null) {
				Debug.Assert(e.TabGroup.ActiveTabContent == e.Selected);
				e.TabGroup.SetFocus(e.Selected);
				var impl = (TabContentImpl)e.Selected;
				impl.OnSelected();
				OnNewTabContentShown(impl);
			}
		}

		void DocumentTreeView_NodeActivated(object sender, DocumentTreeNodeActivatedEventArgs e) {
			e.Handled = true;

			if (e.Node is AssemblyReferenceNode asmRefNode) {
				var asm = DocumentTreeView.DocumentService.Resolve(asmRefNode.AssemblyRef, asmRefNode.GetModule());
				Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
					var asmNode = DocumentTreeView.FindNode(asm);
					if (asmNode != null)
						DocumentTreeView.TreeView.SelectItems(new[] { asmNode });
				}));
				return;
			}

			if (e.Node is DerivedTypeNode derivedTypeNode) {
				var td = derivedTypeNode.TypeDef;
				Debug.Assert(td != null);
				Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
					var typeNode = DocumentTreeView.FindNode(td);
					if (typeNode != null)
						DocumentTreeView.TreeView.SelectItems(new[] { typeNode });
				}));
				return;
			}

			if (e.Node is BaseTypeNode baseTypeNode) {
				var tdr = baseTypeNode.TypeDefOrRef;
				Debug.Assert(tdr != null);
				var td = tdr?.ScopeType.ResolveTypeDef();
				Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
					var typeNode = DocumentTreeView.FindNode(td);
					if (typeNode != null)
						DocumentTreeView.TreeView.SelectItems(new[] { typeNode });
				}));
				return;
			}

			var tab = ActiveTabContentImpl;
			if (tab == null)
				return;
			SetFocus(tab);
		}

		void DocumentTreeView_NodesTextChanged(object sender, EventArgs e) {
			foreach (var impl in AllTabContentImpls)
				impl.UpdateTitleAndToolTip();
		}

		void DocumentTreeView_SelectionChanged(object sender, TreeViewSelectionChangedEventArgs e) {
			if (disableSelectionChangedEventCounter > 0)
				return;
			var nodes = ((IDocumentTreeView)sender).TreeView.TopLevelSelection.OfType<DocumentTreeNodeData>().ToArray();

			// Prevent a new empty tab from opening when closing the last tab
			if (nodes.Length == 0 && ActiveTabContentImpl == null)
				return;

			// When the treeview selects nodes it will unselect everything and then select the new
			// nodes. We're not interested in the empty selection since it shouldn't be recorded in
			// the navigation history. If we get an empty selection, it could be because of the
			// treeview or it's because the user unselected everything. Show the empty nodes with
			// a slight delay so we can cancel it if the real selection immediately follows.
			// Reproduce: Select a node, then collapse its parent to unselect the node and select
			// the parent. Or open a new file.
			if (nodes.Length != 0 || inEmptySelectionHack) {
				ignoreEmptySelection = true;
				ShowNodes(nodes);
			}
			else {
				inEmptySelectionHack = true;
				ignoreEmptySelection = false;
				Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => {
					var old = ignoreEmptySelection;
					inEmptySelectionHack = false;
					ignoreEmptySelection = false;
					if (!old)
						ShowNodes(nodes);
				}));
			}
		}
		int disableSelectionChangedEventCounter = 0;
		bool inEmptySelectionHack;
		bool ignoreEmptySelection;

		void ShowNodes(DocumentTreeNodeData[] nodes) {
			var tabContent = CreateTabContent(nodes);
			disableSelectTreeNodes++;
			try {
				SafeActiveTabContentImpl.Show(tabContent, null, null);
			}
			finally {
				disableSelectTreeNodes--;
			}
		}

		public DocumentTabContent TryCreateContent(DocumentTreeNodeData[] nodes) => documentTabContentFactoryService.CreateTabContent(nodes);

		DocumentTabContent CreateTabContent(DocumentTreeNodeData[] nodes) {
			var content = TryCreateContent(nodes);
			Debug.Assert(content != null);
			return content ?? new NullDocumentTabContent();
		}

		internal void Add(ITabGroup group, DocumentTabContent tabContent, object uiState, Action<ShowTabContentEventArgs> onShown) {
			Debug.Assert(TabGroupService.TabGroups.Contains(group));
			var tab = OpenEmptyTab(group);
			tab.Show(tabContent, uiState, onShown);
		}

		public IDocumentTab OpenEmptyTab() => OpenEmptyTab(SafeActiveTabGroup);

		IDocumentTab OpenEmptyTab(ITabGroup g) {
			var impl = CreateNewTab(g);
			g.ActiveTabContent = impl;
			return impl;
		}

		int disableSelectTreeNodes;
		internal void OnNewTabContentShown(IDocumentTab documentTab) {
			if (documentTab == null)
				return;
			if (!isTreeViewVisible)
				return;
			if (!tabsLoaded)
				return;
			if (disableSelectTreeNodes > 0)
				return;
			if (documentTab != ActiveTabContentImpl)
				return;
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				if (documentTab == ActiveTabContentImpl)
					OnNewTabContentShownDelay(documentTab);
			}));
		}

		void OnNewTabContentShownDelay(IDocumentTab documentTab) {
			var newNodes = documentTab.Content.Nodes.ToArray();
			if (Equals(DocumentTreeView.TreeView.SelectedItems, newNodes))
				return;

			// The treeview steals the focus so remember the current focused element. Don't restore
			// the focus if it's a node in the treeview.
			var focusedElem = Keyboard.FocusedElement;
			if (DocumentTreeView.TreeView.UIObject.IsKeyboardFocusWithin)
				focusedElem = null;
			bool tabGroupHasFocus = TabGroupService.TabGroups.Any(a => a.IsKeyboardFocusWithin);

			disableSelectionChangedEventCounter++;
			try {
				DocumentTreeView.TreeView.SelectItems(newNodes);
			}
			finally {
				disableSelectionChangedEventCounter--;
			}

			if (focusedElem != null && Keyboard.FocusedElement != focusedElem) {
				if (tabGroupHasFocus) {
					var tab = ActiveTabContentImpl;
					Debug.Assert(tab != null);
					if (tab != null)
						tab.TrySetFocus();
				}
				else
					wpfFocusService.Focus(focusedElem);
			}
		}

		bool isTreeViewVisible = true;
		void TreeView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			if ((bool)e.NewValue) {
				isTreeViewVisible = true;
				OnNewTabContentShown(ActiveTabContentImpl);
			}
			else
				isTreeViewVisible = false;
		}

		static bool Equals(TreeNodeData[] a, TreeNodeData[] b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		public void SetFocus(IDocumentTab tab) {
			if (tab == null)
				throw new ArgumentNullException(nameof(tab));
			var impl = tab as TabContentImpl;
			if (impl == null)
				throw new InvalidOperationException();
			var g = GetTabGroup(impl);
			if (g == null)
				throw new InvalidOperationException();
			g.SetFocus(impl);
		}

		public void Refresh(IEnumerable<IDocumentTab> tabs) {
			if (tabs == null)
				throw new ArgumentNullException(nameof(tabs));
			foreach (var tab in tabs.ToArray()) {
				var impl = tab as TabContentImpl;
				if (impl == null)
					throw new InvalidOperationException();
				impl.Refresh();
			}
		}

		public bool Owns(ITabGroup tabGroup) => TabGroupService.TabGroups.Contains(tabGroup);

		internal void OnTabsLoaded() {
			Debug.Assert(!tabsLoaded);
			tabsLoaded = true;
			var impl = ActiveTabContentImpl;
			if (impl != null) {
				impl.OnTabsLoaded();
				OnNewTabContentShown(impl);
			}
		}
		bool tabsLoaded = false;

		public void Close(IDocumentTab tab) {
			if (tab == null)
				throw new ArgumentNullException(nameof(tab));
			var impl = tab as TabContentImpl;
			if (impl == null)
				throw new InvalidOperationException();
			var g = GetTabGroup(impl);
			if (g == null)
				throw new InvalidOperationException();
			g.Close(impl);
		}

		public IDocumentTab TryGetDocumentTab(ITabContent content) {
			var impl = content as TabContentImpl;
			if (impl == null)
				return null;
			return GetTabGroup(impl) == null ? null : impl;
		}

		public void CloseAll() {
			foreach (var impl in AllTabContentImpls.ToArray())
				Close(impl);
			DocumentTreeView.TreeView.SelectItems(Array.Empty<TreeNodeData>());
		}

		internal void OnRemoved(TabContentImpl impl) {
			if (ActiveTabContentImpl == null)
				DocumentTreeView.TreeView.SelectItems(Array.Empty<TreeNodeData>());
		}

		public void Refresh<T>() where T : DocumentTreeNodeData => Refresh(a => a is T);

		public void Refresh(Predicate<DocumentTreeNodeData> pred) {
			var nodes = new List<DocumentTreeNodeData>(DocumentTreeView.TreeView.Root.Data.Descendants().OfType<DocumentTreeNodeData>().Where(a => pred(a)));
			var hash = new HashSet<DsDocumentNode>();
			foreach (var node in nodes) {
				var n = node.GetAncestorOrSelf<DsDocumentNode>();
				if (n == null)
					continue;
				hash.Add(n);
			}
			if (hash.Count == 0)
				return;
			decompilationCache.Clear(new HashSet<IDsDocument>(hash.Select(a => a.Document)));

			var tabs = new List<IDocumentTab>();
			foreach (var tab in VisibleFirstTabs) {
				bool refresh = tab.Content.Nodes.Any(a => hash.Contains(a.GetAncestorOrSelf<DsDocumentNode>()));
				if (refresh)
					tabs.Add(tab);
			}
			Refresh(tabs);
		}

		HashSet<IDsDocument> GetModifiedDocuments(IDsDocument document) {
			var documentsHash = new HashSet<IDsDocument>();
			documentsHash.Add(document);
			var node = DocumentTreeView.FindNode(document);
			if (node is ModuleDocumentNode) {
				if (node.Document.AssemblyDef != null && node.Document.AssemblyDef.ManifestModule == node.Document.ModuleDef) {
					var asmNode = node.GetAssemblyNode();
					Debug.Assert(asmNode != null);
					if (asmNode != null)
						documentsHash.Add(asmNode.Document);
				}
			}
			else if (node is AssemblyDocumentNode) {
				node.TreeNode.EnsureChildrenLoaded();
				var manifestModNode = node.TreeNode.DataChildren.FirstOrDefault() as ModuleDocumentNode;
				Debug.Assert(manifestModNode != null);
				if (manifestModNode != null)
					documentsHash.Add(manifestModNode.Document);
			}
			return documentsHash;
		}

		public void RefreshModifiedDocument(IDsDocument document) {
			var documentsHash = GetModifiedDocuments(document);
			decompilationCache.Clear(documentsHash);

			var tabs = new List<IDocumentTab>();
			foreach (var tab in VisibleFirstTabs) {
				if (MustRefresh(tab, documentsHash))
					tabs.Add(tab);
			}
			if (tabs.Count > 0)
				Refresh(tabs);

			DocumentModified?.Invoke(this, new DocumentModifiedEventArgs(documentsHash.ToArray()));
		}
		public event EventHandler<DocumentModifiedEventArgs> DocumentModified;

		bool MustRefresh(IDocumentTab tab, IEnumerable<IDsDocument> documents) {
			var modules = new HashSet<IDsDocument>(documents);
			if (InModifiedModuleHelper.IsInModifiedModule(modules, tab.Content.Nodes))
				return true;
			var documentViewer = tab.TryGetDocumentViewer();
			if (documentViewer != null && InModifiedModuleHelper.IsInModifiedModule(DocumentTreeView.DocumentService, modules, documentViewer.Content.ReferenceCollection.Select(a => a.Data.Reference)))
				return true;

			return false;
		}

		public void FollowReference(object @ref, bool newTab, bool setFocus, Action<ShowTabContentEventArgs> onShown) {
			if (@ref == null)
				return;

			IDocumentTab tab = ActiveTabContentImpl;
			var sourceTab = tab;
			if (tab == null)
				tab = SafeActiveTabContentImpl;
			else if (newTab) {
				var g = TabGroupService.ActiveTabGroup;
				Debug.Assert(g != null);
				if (g == null)
					return;
				tab = OpenEmptyTab(g);
			}
			tab.FollowReference(@ref, sourceTab == null ? null : sourceTab.Content, onShown);
			if (setFocus)
				SetFocus(tab);
		}

		sealed class ReloadAllHelper : IDisposable {
			readonly DocumentTabService documentTabService;
			readonly HashSet<IDsDocument> originalDocuments;
			readonly bool old_disable_DocumentCollectionChanged;

			public ReloadAllHelper(DocumentTabService documentTabService) {
				this.documentTabService = documentTabService;
				originalDocuments = new HashSet<IDsDocument>(documentTabService.DocumentTreeView.DocumentService.GetDocuments(), new DsDocumentComparer());
				old_disable_DocumentCollectionChanged = documentTabService.disable_DocumentCollectionChanged;
				documentTabService.disable_DocumentCollectionChanged = true;
			}

			public void Dispose() {
				foreach (var document in documentTabService.DocumentTreeView.DocumentService.GetDocuments())
					originalDocuments.Remove(document);
				var removedDocuments = originalDocuments.ToArray();
				// Documents are added with a delay to the TV. Make sure our code executes after all
				// of the pending events.
				documentTabService.DocumentTreeView.AddAction(() => {
					documentTabService.disable_DocumentCollectionChanged = old_disable_DocumentCollectionChanged;
					if (removedDocuments.Length > 0)
						documentTabService.CallDocumentCollectionChanged(NotifyDocumentCollectionChangedEventArgs.CreateRemove(removedDocuments, null));
				});
			}

			sealed class DsDocumentComparer : IEqualityComparer<IDsDocument> {
				public bool Equals(IDsDocument x, IDsDocument y) {
					if (x == y)
						return true;

					var fx = x.SerializedDocument;
					var fy = y.SerializedDocument;
					if (fx == null || fy == null)
						return false;

					return Equals(fx.Value, fy.Value);
				}

				public int GetHashCode(IDsDocument obj) {
					var f = obj.SerializedDocument;
					return f == null ? 0 : GetHashCode(f.Value);
				}

				static bool Equals(DsDocumentInfo x, DsDocumentInfo y) => StringComparer.Ordinal.Equals(x.Name, y.Name) && x.Type.Equals(y.Type);
				static int GetHashCode(DsDocumentInfo obj) => obj.Name.GetHashCode() ^ obj.Type.GetHashCode();
			}
		}

		internal IDisposable OnReloadAll() => new ReloadAllHelper(this);
	}
}

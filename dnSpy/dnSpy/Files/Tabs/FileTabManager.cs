/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.TreeView;
using dnSpy.Files.TreeView;

namespace dnSpy.Files.Tabs {
	[Export, Export(typeof(IFileTabManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class FileTabManager : IFileTabManager {
		IFileTreeView IFileTabManager.FileTreeView {
			get { return fileTreeView; }
		}
		public FileTreeView FileTreeView {
			get { return fileTreeView; }
		}
		readonly FileTreeView fileTreeView;

		public ITabGroupManager TabGroupManager {
			get { return tabGroupManager; }
		}
		readonly ITabGroupManager tabGroupManager;

		IEnumerable<TabContentImpl> AllTabContentImpls {
			get {
				foreach (var g in TabGroupManager.TabGroups) {
					foreach (TabContentImpl impl in g.TabContents)
						yield return impl;
				}
			}
		}

		ITabGroup SafeActiveTabGroup {
			get {
				var g = TabGroupManager.ActiveTabGroup;
				if (g != null)
					return g;
				return TabGroupManager.Create();
			}
		}

		TabContentImpl ActiveTabContentImpl {
			get {
				var g = TabGroupManager.ActiveTabGroup;
				return g == null ? null : (TabContentImpl)g.ActiveTabContent;
			}
		}

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
			var impl = new TabContentImpl(this, fileTabUIContextLocatorCreator.Create(), refFactories);
			tabGroup.Add(impl);
			return impl;
		}

		IFileTab IFileTabManager.ActiveTab {
			get { return ActiveTabContentImpl; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				var impl = value as TabContentImpl;
				if (impl == null)
					throw new InvalidOperationException();
				var g = GetTabGroup(impl);
				if (g == null)
					throw new InvalidOperationException();
				g.ActiveTabContent = impl;
				TabGroupManager.ActiveTabGroup = g;
			}
		}

		ITabGroup GetTabGroup(TabContentImpl impl) {
			foreach (var g in TabGroupManager.TabGroups) {
				if (g.TabContents.Contains(impl))
					return g;
			}
			return null;
		}

		IFileTab IFileTabManager.GetOrCreateActiveTab() {
			return SafeActiveTabContentImpl;
		}

		public IEnumerable<IFileTab> SortedTabs {
			get {
				var groups = TabGroupManager.TabGroups.ToArray();
				if (groups.Length == 0)
					yield break;
				int startGroupIndex = Array.IndexOf(groups, TabGroupManager.ActiveTabGroup);
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

		public IEnumerable<IFileTab> VisibleFirstTabs {
			get {
				var hash = new HashSet<IFileTab>();
				foreach (var g in TabGroupManager.TabGroups) {
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

		public IFileTabManagerSettings Settings {
			get { return fileTabManagerSettings; }
		}
		readonly IFileTabManagerSettings fileTabManagerSettings;

		readonly IFileTabUIContextLocatorCreator fileTabUIContextLocatorCreator;
		readonly ITabManager tabManager;
		readonly IFileTabContentFactoryManager fileTabContentFactoryManager;
		readonly IWpfFocusManager wpfFocusManager;
		readonly IDecompilationCache decompilationCache;
		readonly Lazy<IReferenceFileTabContentCreator, IReferenceFileTabContentCreatorMetadata>[] refFactories;

		[ImportingConstructor]
		FileTabManager(IFileTabUIContextLocatorCreator fileTabUIContextLocatorCreator, FileTreeView fileTreeView, ITabManagerCreator tabManagerCreator, IFileTabContentFactoryManager fileTabContentFactoryManager, IFileTabManagerSettings fileTabManagerSettings, IWpfFocusManager wpfFocusManager, IDecompilationCache decompilationCache, [ImportMany] IEnumerable<Lazy<IReferenceFileTabContentCreator, IReferenceFileTabContentCreatorMetadata>> mefRefFactories) {
			this.fileTabManagerSettings = fileTabManagerSettings;
			this.fileTabUIContextLocatorCreator = fileTabUIContextLocatorCreator;
			this.fileTabContentFactoryManager = fileTabContentFactoryManager;
			this.wpfFocusManager = wpfFocusManager;
			this.decompilationCache = decompilationCache;
			this.refFactories = mefRefFactories.OrderBy(a => a.Metadata.Order).ToArray();
			var tvElem = fileTreeView.TreeView.UIObject as UIElement;
			Debug.Assert(tvElem != null);
			if (tvElem != null) {
				tvElem.IsVisibleChanged += TreeView_IsVisibleChanged;
				isTreeViewVisible = tvElem.IsVisible;
			}
			this.fileTreeView = fileTreeView;
			this.fileTreeView.FileManager.CollectionChanged += FileManager_CollectionChanged;
			this.fileTreeView.SelectionChanged += FileTreeView_SelectionChanged;
			this.fileTreeView.NodesTextChanged += FileTreeView_NodesTextChanged;
			this.fileTreeView.NodeActivated += FileTreeView_NodeActivated;
			this.fileTreeView.TreeView.NodeRemoved += TreeView_NodeRemoved;
			this.tabManager = tabManagerCreator.Create();
			this.tabGroupManager = this.tabManager.Create(new TabGroupManagerOptions(MenuConstants.GUIDOBJ_FILES_TABCONTROL_GUID));
			this.tabGroupManager.TabSelectionChanged += TabGroupManager_TabSelectionChanged;
			this.tabGroupManager.TabGroupSelectionChanged += TabGroupManager_TabGroupSelectionChanged;
		}

		void TreeView_NodeRemoved(object sender, TVNodeRemovedEventArgs e) {
			if (!e.Removed)
				return;

			var fileNode = e.Node as IDnSpyFileNode;
			if (fileNode == null)
				return;
			OnNodeRemoved(fileNode);
		}

		void OnNodeRemoved(IDnSpyFileNode node) {
			var hash = GetSelfAndDnSpyFileNodeChildren(node);
			foreach (TabContentImpl tab in VisibleFirstTabs)
				tab.OnNodesRemoved(hash, () => this.CreateTabContent(new IFileTreeNodeData[0]));
			decompilationCache.Clear(new HashSet<IDnSpyFile>(hash.Select(a => a.DnSpyFile)));
		}

		static HashSet<IDnSpyFileNode> GetSelfAndDnSpyFileNodeChildren(IDnSpyFileNode node, HashSet<IDnSpyFileNode> hash = null) {
			if (hash == null)
				hash = new HashSet<IDnSpyFileNode>();
			hash.Add(node);
			foreach (var c in node.TreeNode.DataChildren) {
				var fileNode = c as IDnSpyFileNode;
				if (fileNode != null)
					GetSelfAndDnSpyFileNodeChildren(fileNode, hash);
			}
			return hash;
		}

		public event EventHandler<NotifyFileCollectionChangedEventArgs> FileCollectionChanged;
		bool disable_FileCollectionChanged = false;
		void FileManager_CollectionChanged(object sender, NotifyFileCollectionChangedEventArgs e) {
			CallFileCollectionChanged(e);
		}
		void CallFileCollectionChanged(NotifyFileCollectionChangedEventArgs e) {
			if (disable_FileCollectionChanged)
				return;
			if (FileCollectionChanged != null)
				FileCollectionChanged(this, e);
		}

		void TabGroupManager_TabGroupSelectionChanged(object sender, TabGroupSelectedEventArgs e) {
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

		void TabGroupManager_TabSelectionChanged(object sender, TabSelectedEventArgs e) {
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

		void FileTreeView_NodeActivated(object sender, FileTreeNodeActivatedEventArgs e) {
			e.Handled = true;

			var asmRefNode = e.Node as IAssemblyReferenceNode;
			if (asmRefNode != null) {
				var asm = fileTreeView.FileManager.Resolve(asmRefNode.AssemblyRef, asmRefNode.GetModule());
				Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
					var asmNode = fileTreeView.FindNode(asm);
					if (asmNode != null)
						fileTreeView.TreeView.SelectItems(new ITreeNodeData[] { asmNode });
				}));
				return;
			}

			var derivedTypeNode = e.Node as IDerivedTypeNode;
			if (derivedTypeNode != null) {
				var td = derivedTypeNode.TypeDef;
				Debug.Assert(td != null);
				Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
					var typeNode = fileTreeView.FindNode(td);
					if (typeNode != null)
						fileTreeView.TreeView.SelectItems(new ITreeNodeData[] { typeNode });
				}));
				return;
			}

			var baseTypeNode = e.Node as IBaseTypeNode;
			if (baseTypeNode != null) {
				var tdr = baseTypeNode.TypeDefOrRef;
				Debug.Assert(tdr != null);
				var td = tdr?.ScopeType.ResolveTypeDef();
				Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
					var typeNode = fileTreeView.FindNode(td);
					if (typeNode != null)
						fileTreeView.TreeView.SelectItems(new ITreeNodeData[] { typeNode });
				}));
				return;
			}

			var tab = ActiveTabContentImpl;
			if (tab == null)
				return;
			SetFocus(tab);
		}

		void FileTreeView_NodesTextChanged(object sender, EventArgs e) {
			foreach (var impl in AllTabContentImpls)
				impl.UpdateTitleAndToolTip();
		}

		void FileTreeView_SelectionChanged(object sender, TVSelectionChangedEventArgs e) {
			if (disableSelectionChangedEventCounter > 0)
				return;
			var nodes = ((IFileTreeView)sender).TreeView.TopLevelSelection.OfType<IFileTreeNodeData>().ToArray();

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

		void ShowNodes(IFileTreeNodeData[] nodes) {
			var tabContent = CreateTabContent(nodes);
			disableSelectTreeNodes++;
			try {
				SafeActiveTabContentImpl.Show(tabContent, null, null);
			}
			finally {
				disableSelectTreeNodes--;
			}
		}

		public IFileTabContent TryCreateContent(IFileTreeNodeData[] nodes) {
			return fileTabContentFactoryManager.CreateTabContent(nodes);
		}

		IFileTabContent CreateTabContent(IFileTreeNodeData[] nodes) {
			var content = TryCreateContent(nodes);
			Debug.Assert(content != null);
			return content ?? new NullFileTabContent();
		}

		internal void Add(ITabGroup group, IFileTabContent tabContent, object serializedUI, Action<ShowTabContentEventArgs> onShown) {
			Debug.Assert(TabGroupManager.TabGroups.Contains(group));
			var tab = OpenEmptyTab(group);
			tab.Show(tabContent, serializedUI, onShown);
		}

		public IFileTab OpenEmptyTab() {
			return OpenEmptyTab(SafeActiveTabGroup);
		}

		IFileTab OpenEmptyTab(ITabGroup g) {
			var impl = CreateNewTab(g);
			g.ActiveTabContent = impl;
			return impl;
		}

		int disableSelectTreeNodes;
		internal void OnNewTabContentShown(IFileTab fileTab) {
			if (fileTab == null)
				return;
			if (!isTreeViewVisible)
				return;
			if (!tabsLoaded)
				return;
			if (disableSelectTreeNodes > 0)
				return;
			if (fileTab != ActiveTabContentImpl)
				return;
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				if (fileTab == ActiveTabContentImpl)
					OnNewTabContentShownDelay(fileTab);
			}));
		}

		void OnNewTabContentShownDelay(IFileTab fileTab) {
			var newNodes = fileTab.Content.Nodes.ToArray();
			if (Equals(fileTreeView.TreeView.SelectedItems, newNodes))
				return;

			// The treeview steals the focus so remember the current focused element. Don't restore
			// the focus if it's a node in the treeview.
			var focusedElem = Keyboard.FocusedElement;
			if (((UIElement)fileTreeView.TreeView.UIObject).IsKeyboardFocusWithin)
				focusedElem = null;
			bool tabGroupHasFocus = tabGroupManager.TabGroups.Any(a => a.IsKeyboardFocusWithin);

			disableSelectionChangedEventCounter++;
			try {
				fileTreeView.TreeView.SelectItems(newNodes);
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
					wpfFocusManager.Focus(focusedElem);
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

		static bool Equals(ITreeNodeData[] a, ITreeNodeData[] b) {
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

		public void SetFocus(IFileTab tab) {
			if (tab == null)
				throw new ArgumentNullException();
			var impl = tab as TabContentImpl;
			if (impl == null)
				throw new InvalidOperationException();
			var g = GetTabGroup(impl);
			if (g == null)
				throw new InvalidOperationException();
			g.SetFocus(impl);
		}

		public void Refresh(IEnumerable<IFileTab> tabs) {
			if (tabs == null)
				throw new ArgumentNullException();
			foreach (var tab in tabs.ToArray()) {
				var impl = tab as TabContentImpl;
				if (impl == null)
					throw new InvalidOperationException();
				impl.Refresh();
			}
		}

		public bool Owns(ITabGroup tabGroup) {
			return tabGroupManager.TabGroups.Contains(tabGroup);
		}

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

		public void Close(IFileTab tab) {
			if (tab == null)
				throw new ArgumentNullException();
			var impl = tab as TabContentImpl;
			if (impl == null)
				throw new InvalidOperationException();
			var g = GetTabGroup(impl);
			if (g == null)
				throw new InvalidOperationException();
			g.Close(impl);
		}

		public IFileTab TryGetFileTab(ITabContent content) {
			var impl = content as TabContentImpl;
			if (impl == null)
				return null;
			return GetTabGroup(impl) == null ? null : impl;
		}

		public void CloseAll() {
			foreach (var impl in AllTabContentImpls.ToArray())
				Close(impl);
			fileTreeView.TreeView.SelectItems(new ITreeNodeData[0]);
		}

		internal void OnRemoved(TabContentImpl impl) {
			if (ActiveTabContentImpl == null)
				fileTreeView.TreeView.SelectItems(new ITreeNodeData[0]);
		}

		public void Refresh<T>() where T : IFileTreeNodeData {
			Refresh(a => a is T);
		}

		public void Refresh(Predicate<IFileTreeNodeData> pred) {
			var nodes = new List<IFileTreeNodeData>(FileTreeView.TreeView.Root.Data.Descendants().OfType<IFileTreeNodeData>().Where(a => pred(a)));
			var hash = new HashSet<IDnSpyFileNode>();
			foreach (var node in nodes) {
				var n = node.GetAncestorOrSelf<IDnSpyFileNode>();
				if (n == null)
					continue;
				hash.Add(n);
			}
			if (hash.Count == 0)
				return;
			decompilationCache.Clear(new HashSet<IDnSpyFile>(hash.Select(a => a.DnSpyFile)));

			var tabs = new List<IFileTab>();
			foreach (var tab in VisibleFirstTabs) {
				bool refresh = tab.Content.Nodes.Any(a => hash.Contains(a.GetAncestorOrSelf<IDnSpyFileNode>()));
				if (refresh)
					tabs.Add(tab);
			}
			Refresh(tabs);
		}

		HashSet<IDnSpyFile> GetModifiedFiles(IDnSpyFile file) {
			var fileHash = new HashSet<IDnSpyFile>();
			fileHash.Add(file);
			var node = fileTreeView.FindNode(file);
			if (node is IModuleFileNode) {
				if (node.DnSpyFile.AssemblyDef != null && node.DnSpyFile.AssemblyDef.ManifestModule == node.DnSpyFile.ModuleDef) {
					var asmNode = node.GetAssemblyNode();
					Debug.Assert(asmNode != null);
					if (asmNode != null)
						fileHash.Add(asmNode.DnSpyFile);
				}
			}
			else if (node is IAssemblyFileNode) {
				node.TreeNode.EnsureChildrenLoaded();
				var manifestModNode = node.TreeNode.DataChildren.FirstOrDefault() as IModuleFileNode;
				Debug.Assert(manifestModNode != null);
				if (manifestModNode != null)
					fileHash.Add(manifestModNode.DnSpyFile);
			}
			return fileHash;
		}

		public void RefreshModifiedFile(IDnSpyFile file) {
			var fileHash = GetModifiedFiles(file);
			decompilationCache.Clear(fileHash);

			var tabs = new List<IFileTab>();
			foreach (var tab in VisibleFirstTabs) {
				if (MustRefresh(tab, fileHash))
					tabs.Add(tab);
			}
			if (tabs.Count > 0)
				Refresh(tabs);

			if (FileModified != null)
				FileModified(this, new FileModifiedEventArgs(fileHash.ToArray()));
		}
		public event EventHandler<FileModifiedEventArgs> FileModified;

		bool MustRefresh(IFileTab tab, IEnumerable<IDnSpyFile> files) {
			var modules = new HashSet<IDnSpyFile>(files);
			if (InModifiedModuleHelper.IsInModifiedModule(modules, tab.Content.Nodes))
				return true;
			var uiContext = tab.TryGetTextEditorUIContext();
			if (uiContext != null && InModifiedModuleHelper.IsInModifiedModule(FileTreeView.FileManager, modules, uiContext.References))
				return true;

			return false;
		}

		public void FollowReference(object @ref, bool newTab, bool setFocus, Action<ShowTabContentEventArgs> onShown) {
			if (@ref == null)
				return;

			IFileTab tab = ActiveTabContentImpl;
			var sourceTab = tab;
			if (tab == null)
				tab = SafeActiveTabContentImpl;
			else if (newTab) {
				var g = TabGroupManager.ActiveTabGroup;
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
			readonly FileTabManager fileTabManager;
			readonly HashSet<IDnSpyFile> originalFiles;
			readonly bool old_disable_FileCollectionChanged;

			public ReloadAllHelper(FileTabManager fileTabManager) {
				this.fileTabManager = fileTabManager;
				this.originalFiles = new HashSet<IDnSpyFile>(fileTabManager.FileTreeView.FileManager.GetFiles(), new DnSpyFileComparer());
				this.old_disable_FileCollectionChanged = fileTabManager.disable_FileCollectionChanged;
				fileTabManager.disable_FileCollectionChanged = true;
			}

			public void Dispose() {
				foreach (var file in fileTabManager.FileTreeView.FileManager.GetFiles())
					originalFiles.Remove(file);
				var removedFiles = originalFiles.ToArray();
				// Files are added with a delay to the TV. Make sure our code executes after all
				// of the pending events.
				fileTabManager.fileTreeView.AddAction(() => {
					fileTabManager.disable_FileCollectionChanged = old_disable_FileCollectionChanged;
					if (removedFiles.Length > 0)
						fileTabManager.CallFileCollectionChanged(NotifyFileCollectionChangedEventArgs.CreateRemove(removedFiles, null));
				});
			}

			sealed class DnSpyFileComparer : IEqualityComparer<IDnSpyFile> {
				public bool Equals(IDnSpyFile x, IDnSpyFile y) {
					if (x == y)
						return true;

					var fx = x.SerializedFile;
					var fy = y.SerializedFile;
					if (fx == null || fy == null)
						return false;

					return Equals(fx.Value, fy.Value);
				}

				public int GetHashCode(IDnSpyFile obj) {
					var f = obj.SerializedFile;
					return f == null ? 0 : GetHashCode(f.Value);
				}

				static bool Equals(DnSpyFileInfo x, DnSpyFileInfo y) {
					return StringComparer.Ordinal.Equals(x.Name, y.Name) &&
							x.Type.Equals(y.Type);
				}

				static int GetHashCode(DnSpyFileInfo obj) {
					return obj.Name.GetHashCode() ^ obj.Type.GetHashCode();
				}
			}
		}

		internal IDisposable OnReloadAll() {
			return new ReloadAllHelper(this);
		}
	}
}

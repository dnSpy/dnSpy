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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.TreeView;
using dnSpy.Files.TreeView;

namespace dnSpy.Files.Tabs {
	[Export, Export(typeof(IFileTabManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class FileTabManager : IFileTabManager {
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
		}

		IFileTab IFileTabManager.GetOrCreateActiveTab() {
			return SafeActiveTabContentImpl;
		}

		readonly IFileTabUIContextLocatorCreator fileTabUIContextLocatorCreator;

		readonly ITabManager tabManager;
		readonly Lazy<IFileTabContentFactory, IFileTabContentFactoryMetadata>[] tabContentFactories;
		readonly Lazy<IReferenceFileTabContentCreator, IReferenceFileTabContentCreatorMetadata>[] refFactories;

		[ImportingConstructor]
		FileTabManager(IFileTabUIContextLocatorCreator fileTabUIContextLocatorCreator, FileTreeView fileTreeView, ITabManagerCreator tabManagerCreator, [ImportMany] IEnumerable<Lazy<IFileTabContentFactory, IFileTabContentFactoryMetadata>> mefTabContentFactories, [ImportMany] IEnumerable<Lazy<IReferenceFileTabContentCreator, IReferenceFileTabContentCreatorMetadata>> mefRefFactories) {
			this.fileTabUIContextLocatorCreator = fileTabUIContextLocatorCreator;
			this.tabContentFactories = mefTabContentFactories.OrderBy(a => a.Metadata.Order).ToArray();
			Debug.Assert(tabContentFactories.Length > 0);
			this.refFactories = mefRefFactories.OrderBy(a => a.Metadata.Order).ToArray();
			this.fileTreeView = fileTreeView;
			this.fileTreeView.TreeView.SelectionChanged += TreeView_SelectionChanged;
			this.fileTreeView.NodesTextChanged += FileTreeView_NodesTextChanged;
			this.tabManager = tabManagerCreator.Create();
			this.tabGroupManager = this.tabManager.Create(new Guid(MenuConstants.GUIDOBJ_FILES_TABCONTROL_GUID));
		}

		void FileTreeView_NodesTextChanged(object sender, EventArgs e) {
			foreach (var impl in AllTabContentImpls)
				impl.UpdateTitleAndToolTip();
		}

		void TreeView_SelectionChanged(object sender, TVSelectionChangedEventArgs e) {
			if (disableSelectionChangedEventCounter > 0)
				return;
			ShowNodes(((ITreeView)sender).SelectedItems.OfType<IFileTreeNodeData>().ToArray());
		}
		int disableSelectionChangedEventCounter = 0;

		void ShowNodes(IFileTreeNodeData[] nodes) {
			var tabContent = CreateTabContent(nodes);
			SafeActiveTabContentImpl.Show(tabContent);
		}

		IFileTabContent CreateTabContent(IFileTreeNodeData[] nodes) {
			var context = new FileTabContentFactoryContext(nodes);
			foreach (var factory in tabContentFactories) {
				var tabContent = factory.Value.Create(context);
				if (tabContent != null)
					return tabContent;
			}
			throw new InvalidOperationException();
		}

		public IFileTab OpenEmptyTab() {
			var g = SafeActiveTabGroup;
			var impl = CreateNewTab(g);
			g.ActiveTabContent = impl;
			return impl;
		}

		internal void OnNewTabContentShown(IFileTab fileTab) {
			if (fileTab != ActiveTabContentImpl)
				return;
			disableSelectionChangedEventCounter++;
			try {
				fileTreeView.TreeView.SelectItems(fileTab.FileTabContent.Nodes);
			}
			finally {
				disableSelectionChangedEventCounter--;
			}
		}
	}
}

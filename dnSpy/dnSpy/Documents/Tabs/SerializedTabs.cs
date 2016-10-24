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
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.TreeView;
using dnSpy.Controls;
using dnSpy.Settings;
using dnSpy.Tabs;

namespace dnSpy.Documents.Tabs {
	sealed class SerializedTabGroupWindow {
		public const string MAIN_NAME = "Main";
		const string NAME_ATTR = "name";
		const string INDEX_ATTR = "index";
		const string ISHORIZONTAL_ATTR = "is-horizontal";
		const string TABGROUP_SECTION = "TabGroup";
		const string STACKEDCONTENTSTATE_SECTION = "StackedContent";

		public string Name { get; }
		public int Index { get; }
		public bool IsHorizontal { get; }
		public List<SerializedTabGroup> TabGroups { get; }
		public StackedContentState StackedContentState { get; }

		SerializedTabGroupWindow(string name, int index, bool isHorizontal, StackedContentState stackedContentState) {
			this.Name = name;
			this.Index = index;
			this.IsHorizontal = isHorizontal;
			this.TabGroups = new List<SerializedTabGroup>();
			this.StackedContentState = stackedContentState;
		}

		public static SerializedTabGroupWindow Load(ISettingsSection section) {
			var name = section.Attribute<string>(NAME_ATTR) ?? MAIN_NAME;
			int index = section.Attribute<int?>(INDEX_ATTR) ?? -1;
			bool isHorizontal = section.Attribute<bool?>(ISHORIZONTAL_ATTR) ?? false;
			var stackedContentState = StackedContentStateSerializer.TryDeserialize(section.GetOrCreateSection(STACKEDCONTENTSTATE_SECTION));
			var tgw = new SerializedTabGroupWindow(name, index, isHorizontal, stackedContentState);

			foreach (var tgSection in section.SectionsWithName(TABGROUP_SECTION))
				tgw.TabGroups.Add(SerializedTabGroup.Load(tgSection));

			return tgw;
		}

		public void Save(ISettingsSection section) {
			section.Attribute(NAME_ATTR, Name);
			section.Attribute(INDEX_ATTR, Index);
			section.Attribute(ISHORIZONTAL_ATTR, IsHorizontal);

			if (StackedContentState != null)
				StackedContentStateSerializer.Serialize(section.GetOrCreateSection(STACKEDCONTENTSTATE_SECTION), StackedContentState);

			foreach (var stg in TabGroups)
				stg.Save(section.CreateSection(TABGROUP_SECTION));
		}

		public static SerializedTabGroupWindow Create(IDocumentTabContentFactoryService factory, ITabGroupService tabGroupService, string name) {
			int index = tabGroupService.TabGroups.ToList().IndexOf(tabGroupService.ActiveTabGroup);
			var stackedContentState = ((TabGroupService)tabGroupService).StackedContentState;
			var tgw = new SerializedTabGroupWindow(name, index, tabGroupService.IsHorizontal, stackedContentState);

			foreach (var g in tabGroupService.TabGroups)
				tgw.TabGroups.Add(SerializedTabGroup.Create(factory, g));

			return tgw;
		}

		public IEnumerable<object> Restore(DocumentTabService documentTabService, IDocumentTabContentFactoryService documentTabContentFactoryService, ITabGroupService mgr) {
			mgr.IsHorizontal = IsHorizontal;
			for (int i = 0; i < TabGroups.Count; i++) {
				var stg = TabGroups[i];
				var g = i == 0 ? mgr.ActiveTabGroup ?? mgr.Create() : mgr.Create();
				yield return null;
				foreach (var o in stg.Restore(documentTabService, documentTabContentFactoryService, g))
					yield return o;
			}

			if (StackedContentState != null)
				((TabGroupService)mgr).StackedContentState = StackedContentState;

			var ary = mgr.TabGroups.ToArray();
			if ((uint)Index < (uint)ary.Length)
				mgr.ActiveTabGroup = ary[Index];
			yield return null;
		}
	}

	sealed class SerializedTabGroup {
		const string INDEX_ATTR = "index";
		const string TAB_SECTION = "Tab";

		public int Index { get; }
		public List<SerializedTab> Tabs { get; }

		SerializedTabGroup(int index) {
			this.Index = index;
			this.Tabs = new List<SerializedTab>();
		}

		public static SerializedTabGroup Load(ISettingsSection section) {
			int index = section.Attribute<int?>(INDEX_ATTR) ?? -1;
			var tg = new SerializedTabGroup(index);

			foreach (var tabSection in section.SectionsWithName(TAB_SECTION)) {
				var tab = SerializedTab.TryLoad(tabSection);
				if (tab != null)
					tg.Tabs.Add(tab);
			}

			return tg;
		}

		public void Save(ISettingsSection section) {
			section.Attribute(INDEX_ATTR, Index);

			foreach (var st in Tabs)
				st.Save(section.CreateSection(TAB_SECTION));
		}

		public static SerializedTabGroup Create(IDocumentTabContentFactoryService documentTabContentFactoryService, ITabGroup g) {
			int index = g.TabContents.ToList().IndexOf(g.ActiveTabContent);
			var tg = new SerializedTabGroup(index);

			foreach (IDocumentTab tab in g.TabContents) {
				var t = SerializedTab.TryCreate(documentTabContentFactoryService, tab);
				if (t != null)
					tg.Tabs.Add(t);
			}

			return tg;
		}

		public IEnumerable<object> Restore(DocumentTabService documentTabService, IDocumentTabContentFactoryService documentTabContentFactoryService, ITabGroup g) {
			foreach (var st in Tabs) {
				foreach (var o in st.TryRestore(documentTabService, documentTabContentFactoryService, g))
					yield return o;
			}
			var ary = g.TabContents.ToArray();
			if ((uint)Index < (uint)ary.Length)
				g.ActiveTabContent = ary[Index];
			yield return null;
		}
	}

	sealed class SerializedTab {
		const string CONTENT_SECTION = "Content";
		const string UI_SECTION = "UI";
		const string TAB_UI_SECTION = "TabUI";
		const string PATH_SECTION = "Path";
		const string CONTENT_GUID_ATTR = "_g_";
		const string AUTOLOADED_SECTION = "File";

		public ISettingsSection Content { get; }
		public ISettingsSection TabUI { get; }
		public ISettingsSection UI { get; }
		public List<SerializedPath> Paths { get; }
		public List<DsDocumentInfo> AutoLoadedDocuments { get; }

		SerializedTab(ISettingsSection content, ISettingsSection tabContentUI, ISettingsSection ui, List<SerializedPath> paths, List<DsDocumentInfo> autoLoadedDocuments) {
			this.Content = content;
			this.TabUI = tabContentUI;
			this.UI = ui;
			this.Paths = paths;
			this.AutoLoadedDocuments = autoLoadedDocuments;
		}

		public static SerializedTab TryLoad(ISettingsSection section) {
			var contentSect = section.TryGetSection(CONTENT_SECTION);
			if (contentSect == null || contentSect.Attribute<Guid?>(CONTENT_GUID_ATTR) == null)
				return null;
			var uiSect = section.TryGetSection(UI_SECTION);
			if (uiSect == null)
				return null;
			var tabUISect = section.TryGetSection(TAB_UI_SECTION);
			if (tabUISect == null)
				return null;

			var paths = new List<SerializedPath>();
			foreach (var pathSection in section.SectionsWithName(PATH_SECTION))
				paths.Add(SerializedPath.Load(pathSection));

			var autoLoadedDocuments = new List<DsDocumentInfo>();
			foreach (var sect in section.SectionsWithName(AUTOLOADED_SECTION)) {
				var info = DsDocumentInfoSerializer.TryLoad(sect);
				if (info != null)
					autoLoadedDocuments.Add(info.Value);
			}

			return new SerializedTab(contentSect, tabUISect, uiSect, paths, autoLoadedDocuments);
		}

		public void Save(ISettingsSection section) {
			Debug.Assert(Content.Attribute<Guid?>(CONTENT_GUID_ATTR) != null);
			section.CreateSection(CONTENT_SECTION).CopyFrom(Content);
			section.CreateSection(UI_SECTION).CopyFrom(UI);
			section.CreateSection(TAB_UI_SECTION).CopyFrom(TabUI);
			foreach (var path in Paths)
				path.Save(section.CreateSection(PATH_SECTION));
			foreach (var f in AutoLoadedDocuments)
				DsDocumentInfoSerializer.Save(section.CreateSection(AUTOLOADED_SECTION), f);
		}

		public static SerializedTab TryCreate(IDocumentTabContentFactoryService documentTabContentFactoryService, IDocumentTab tab) {
			var contentSect = new SettingsSection(CONTENT_SECTION);
			var guid = documentTabContentFactoryService.Serialize(tab.Content, contentSect);
			if (guid == null)
				return null;
			contentSect.Attribute(CONTENT_GUID_ATTR, guid.Value);

			var uiSect = new SettingsSection(UI_SECTION);
			tab.UIContext.SerializeUIState(uiSect, tab.UIContext.CreateUIState());

			var tabUISect = new SettingsSection(TAB_UI_SECTION);
			tab.SerializeUI(tabUISect);

			var paths = new List<SerializedPath>();
			foreach (var node in tab.Content.Nodes)
				paths.Add(SerializedPath.Create(node));

			var autoLoadedDocuments = new List<DsDocumentInfo>();
			foreach (var f in GetAutoLoadedDocuments(tab.Content.Nodes))
				autoLoadedDocuments.Add(f);

			return new SerializedTab(contentSect, tabUISect, uiSect, paths, autoLoadedDocuments);
		}

		static IEnumerable<DsDocumentInfo> GetAutoLoadedDocuments(IEnumerable<IDocumentTreeNodeData> nodes) {
			var hash = new HashSet<ITreeNodeData>();
			foreach (var node in nodes) {
				var document = node.GetTopNode();
				if (document == null || hash.Contains(document))
					continue;
				hash.Add(document);
				if (document.Document.IsAutoLoaded) {
					var info = document.Document.SerializedDocument;
					if (info != null)
						yield return info.Value;
				}
			}
		}

		sealed class GetNodesContext {
			public IDocumentTreeNodeData[] Nodes;
		}

		public IEnumerable<object> TryRestore(DocumentTabService documentTabService, IDocumentTabContentFactoryService documentTabContentFactoryService, ITabGroup g) {
			var guid = Content.Attribute<Guid?>(CONTENT_GUID_ATTR);
			if (guid == null)
				yield break;
			var ctx = new GetNodesContext();
			foreach (var o in GetNodes(ctx, documentTabService.DocumentTreeView))
				yield return o;
			if (ctx.Nodes == null)
				yield break;
			var tabContent = documentTabContentFactoryService.Deserialize(guid.Value, Content, ctx.Nodes);
			yield return null;
			if (tabContent == null)
				yield break;
			documentTabService.Add(g, tabContent, null, (Action<ShowTabContentEventArgs>)(a => {
				if (a.Success) {
					var uiContext = tabContent.DocumentTab.UIContext;
					tabContent.DocumentTab.DeserializeUI((ISettingsSection)this.TabUI);
					var obj = uiContext.DeserializeUIState(UI);
					uiContext.RestoreUIState(obj);
				}
			}));
			yield return null;
		}

		IEnumerable<object> GetNodes(GetNodesContext ctx, IDocumentTreeView documentTreeView) {
			var list = new List<IDocumentTreeNodeData>();
			var root = documentTreeView.TreeView.Root;
			var findCtx = new FindNodeContext();
			foreach (var path in Paths) {
				foreach (var o in path.FindNode(findCtx, root))
					yield return o;
				if (findCtx.Node == null)
					yield break;
				list.Add(findCtx.Node);
			}
			ctx.Nodes = list.ToArray();
		}
	}

	sealed class FindNodeContext {
		public IDocumentTreeNodeData Node;
	}

	sealed class SerializedPath {
		const string NAME_SECTION = "Name";

		public List<NodePathName> Names { get; }

		SerializedPath() {
			this.Names = new List<NodePathName>();
		}

		public static SerializedPath Load(ISettingsSection pathSection) {
			var path = new SerializedPath();

			foreach (var sect in pathSection.SectionsWithName(NAME_SECTION))
				path.Names.Add(NodePathNameSerializer.Load(sect));

			return path;
		}

		public void Save(ISettingsSection section) {
			foreach (var name in Names)
				NodePathNameSerializer.Save(section.CreateSection(NAME_SECTION), name);
		}

		public static SerializedPath Create(IDocumentTreeNodeData node) {
			var path = new SerializedPath();

			while (node != null && node.TreeNode.Parent != null) {
				path.Names.Add(node.NodePathName);
				var parent = node.TreeNode.Parent;
				node = parent.Data as IDocumentTreeNodeData;
			}
			path.Names.Reverse();

			return path;
		}

		public IEnumerable<object> FindNode(FindNodeContext ctx, ITreeNode root) {
			var node = root;
			foreach (var name in Names) {
				node.EnsureChildrenLoaded();
				yield return null;
				var tmp = node.DataChildren.OfType<IDocumentTreeNodeData>().FirstOrDefault(a => a.NodePathName.Equals(name));
				if (tmp == null)
					yield break;
				node = tmp.TreeNode;
			}
			ctx.Node = node == root ? null : node.Data as IDocumentTreeNodeData;
		}
	}

	static class NodePathNameSerializer {
		const string ID_ATTR = "id";
		const string NAME_ATTR = "name";

		public static NodePathName Load(ISettingsSection sect) {
			var id = sect.Attribute<Guid?>(ID_ATTR) ?? Guid.Empty;
			var name = sect.Attribute<string>(NAME_ATTR);
			return new NodePathName(id, name);
		}

		public static void Save(ISettingsSection section, NodePathName name) {
			section.Attribute(ID_ATTR, name.Guid);
			if (!string.IsNullOrEmpty(name.Name))
				section.Attribute(NAME_ATTR, name.Name);
		}
	}
}

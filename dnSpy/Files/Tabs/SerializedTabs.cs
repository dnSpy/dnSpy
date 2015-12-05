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
using System.Linq;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.TreeView;
using dnSpy.Controls;
using dnSpy.Settings;
using dnSpy.Tabs;

namespace dnSpy.Files.Tabs {
	sealed class SerializedTabGroupWindow {
		public const string MAIN_NAME = "Main";
		const string NAME_ATTR = "name";
		const string INDEX_ATTR = "index";
		const string ISHORIZONTAL_ATTR = "is-horizontal";
		const string TABGROUP_SECTION = "TabGroup";
		const string STACKEDCONTENTSTATE_SECTION = "StackedContent";

		public string Name {
			get { return name; }
		}
		readonly string name;

		public int Index {
			get { return index; }
		}
		readonly int index;

		public bool IsHorizontal {
			get { return isHorizontal; }
		}
		readonly bool isHorizontal;

		public List<SerializedTabGroup> TabGroups {
			get { return tabGroups; }
		}
		readonly List<SerializedTabGroup> tabGroups;

		public StackedContentState StackedContentState {
			get { return stackedContentState; }
		}
		readonly StackedContentState stackedContentState;

		SerializedTabGroupWindow(string name, int index, bool isHorizontal, StackedContentState stackedContentState) {
			this.name = name;
			this.index = index;
			this.isHorizontal = isHorizontal;
			this.tabGroups = new List<SerializedTabGroup>();
			this.stackedContentState = stackedContentState;
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

		public static SerializedTabGroupWindow Create(IFileTabContentFactoryManager creator, ITabGroupManager tabGroupManager, string name) {
			int index = tabGroupManager.TabGroups.ToList().IndexOf(tabGroupManager.ActiveTabGroup);
			var stackedContentState = ((TabGroupManager)tabGroupManager).StackedContentState;
			var tgw = new SerializedTabGroupWindow(name, index, tabGroupManager.IsHorizontal, stackedContentState);

			foreach (var g in tabGroupManager.TabGroups)
				tgw.TabGroups.Add(SerializedTabGroup.Create(creator, g));

			return tgw;
		}

		public IEnumerable<object> Restore(FileTabManager fileTabManager, IFileTabContentFactoryManager creator, ITabGroupManager mgr) {
			mgr.IsHorizontal = IsHorizontal;
			for (int i = 0; i < TabGroups.Count; i++) {
				var stg = TabGroups[i];
				var g = i == 0 ? mgr.ActiveTabGroup ?? mgr.Create() : mgr.Create();
				yield return null;
				foreach (var o in stg.Restore(fileTabManager, creator, g))
					yield return o;
			}

			if (StackedContentState != null)
				((TabGroupManager)mgr).StackedContentState = StackedContentState;

			var ary = mgr.TabGroups.ToArray();
			if ((uint)Index < (uint)ary.Length)
				mgr.ActiveTabGroup = ary[Index];
			yield return null;
		}
	}

	sealed class SerializedTabGroup {
		const string INDEX_ATTR = "index";
		const string TAB_SECTION = "Tab";

		public int Index {
			get { return index; }
		}
		readonly int index;

		public List<SerializedTab> Tabs {
			get { return tabs; }
		}
		readonly List<SerializedTab> tabs;

		SerializedTabGroup(int index) {
			this.index = index;
			this.tabs = new List<SerializedTab>();
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

		public static SerializedTabGroup Create(IFileTabContentFactoryManager creator, ITabGroup g) {
			int index = g.TabContents.ToList().IndexOf(g.ActiveTabContent);
			var tg = new SerializedTabGroup(index);

			foreach (IFileTab tab in g.TabContents) {
				var t = SerializedTab.TryCreate(creator, tab);
				if (t != null)
					tg.Tabs.Add(t);
			}

			return tg;
		}

		public IEnumerable<object> Restore(FileTabManager fileTabManager, IFileTabContentFactoryManager creator, ITabGroup g) {
			foreach (var st in Tabs) {
				foreach (var o in st.TryRestore(fileTabManager, creator, g))
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

		public ISettingsSection Content {
			get { return content; }
		}
		readonly ISettingsSection content;

		public ISettingsSection TabUI {
			get { return tabContentUI; }
		}
		ISettingsSection tabContentUI;

		public ISettingsSection UI {
			get { return ui; }
		}
		readonly ISettingsSection ui;

		public List<SerializedPath> Paths {
			get { return paths; }
		}
		readonly List<SerializedPath> paths;

		public List<DnSpyFileInfo> AutoLoadedFiles {
			get { return autoLoadedFiles; }
		}
		readonly List<DnSpyFileInfo> autoLoadedFiles;

		SerializedTab(ISettingsSection content, ISettingsSection tabContentUI, ISettingsSection ui, List<SerializedPath> paths, List<DnSpyFileInfo> autoLoadedFiles) {
			this.content = content;
			this.tabContentUI = tabContentUI;
			this.ui = ui;
			this.paths = paths;
			this.autoLoadedFiles = autoLoadedFiles;
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

			var autoLoadedFiles = new List<DnSpyFileInfo>();
			foreach (var sect in section.SectionsWithName(AUTOLOADED_SECTION)) {
				var info = DnSpyFileInfoSerializer.TryLoad(sect);
				if (info != null)
					autoLoadedFiles.Add(info.Value);
			}

			return new SerializedTab(contentSect, tabUISect, uiSect, paths, autoLoadedFiles);
		}

		public void Save(ISettingsSection section) {
			Debug.Assert(Content.Attribute<Guid?>(CONTENT_GUID_ATTR) != null);
			section.CreateSection(CONTENT_SECTION).CopyFrom(Content);
			section.CreateSection(UI_SECTION).CopyFrom(UI);
			section.CreateSection(TAB_UI_SECTION).CopyFrom(TabUI);
			foreach (var path in Paths)
				path.Save(section.CreateSection(PATH_SECTION));
			foreach (var f in AutoLoadedFiles)
				DnSpyFileInfoSerializer.Save(section.CreateSection(AUTOLOADED_SECTION), f);
		}

		public static SerializedTab TryCreate(IFileTabContentFactoryManager creator, IFileTab tab) {
			var contentSect = new SettingsSection(CONTENT_SECTION);
			var guid = creator.Serialize(tab.Content, contentSect);
			if (guid == null)
				return null;
			contentSect.Attribute(CONTENT_GUID_ATTR, guid.Value);

			var uiSect = new SettingsSection(UI_SECTION);
			tab.UIContext.SaveSerialized(uiSect, tab.UIContext.Serialize());

			var tabUISect = new SettingsSection(TAB_UI_SECTION);
			tab.SerializeUI(tabUISect);

			var paths = new List<SerializedPath>();
			foreach (var node in tab.Content.Nodes)
				paths.Add(SerializedPath.Create(node));

			var autoLoadedFiles = new List<DnSpyFileInfo>();
			foreach (var f in GetAutoLoadedFiles(tab.Content.Nodes))
				autoLoadedFiles.Add(f);

			return new SerializedTab(contentSect, tabUISect, uiSect, paths, autoLoadedFiles);
		}

		static IEnumerable<DnSpyFileInfo> GetAutoLoadedFiles(IEnumerable<IFileTreeNodeData> nodes) {
			var hash = new HashSet<ITreeNodeData>();
			foreach (var node in nodes) {
				var file = node.GetTopNode();
				if (file == null || hash.Contains(file))
					continue;
				hash.Add(file);
				if (file.DnSpyFile.IsAutoLoaded) {
					var info = file.DnSpyFile.SerializedFile;
					if (info != null)
						yield return info.Value;
				}
			}
		}

		sealed class GetNodesContext {
			public IFileTreeNodeData[] Nodes;
		}

		public IEnumerable<object> TryRestore(FileTabManager fileTabManager, IFileTabContentFactoryManager creator, ITabGroup g) {
			var guid = Content.Attribute<Guid?>(CONTENT_GUID_ATTR);
			if (guid == null)
				yield break;
			var ctx = new GetNodesContext();
			foreach (var o in GetNodes(ctx, fileTabManager.FileTreeView))
				yield return o;
			if (ctx.Nodes == null)
				yield break;
			var tabContent = creator.Deserialize(guid.Value, Content, ctx.Nodes);
			yield return null;
			if (tabContent == null)
				yield break;
			fileTabManager.Add(g, tabContent, null, a => {
				if (a.Success) {
					var uiContext = tabContent.FileTab.UIContext;
					tabContent.FileTab.DeserializeUI(TabUI);
					var obj = uiContext.CreateSerialized(UI);
					uiContext.Deserialize(obj);
				}
			});
			yield return null;
		}

		IEnumerable<object> GetNodes(GetNodesContext ctx, IFileTreeView fileTreeView) {
			var list = new List<IFileTreeNodeData>();
			var root = fileTreeView.TreeView.Root;
			var findCtx = new FindNodeContext();
			foreach (var path in paths) {
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
		public IFileTreeNodeData Node;
	}

	sealed class SerializedPath {
		const string NAME_SECTION = "Name";

		public List<NodePathName> Names {
			get { return names; }
		}
		readonly List<NodePathName> names;

		SerializedPath() {
			this.names = new List<NodePathName>();
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

		public static SerializedPath Create(IFileTreeNodeData node) {
			var path = new SerializedPath();

			while (node != null && node.TreeNode.Parent != null) {
				path.Names.Add(node.NodePathName);
				var parent = node.TreeNode.Parent;
				node = parent.Data as IFileTreeNodeData;
			}
			path.Names.Reverse();

			return path;
		}

		public IEnumerable<object> FindNode(FindNodeContext ctx, ITreeNode root) {
			var node = root;
			foreach (var name in Names) {
				node.EnsureChildrenLoaded();
				yield return null;
				var tmp = node.DataChildren.OfType<IFileTreeNodeData>().FirstOrDefault(a => a.NodePathName.Equals(name));
				if (tmp == null)
					yield break;
				node = tmp.TreeNode;
			}
			ctx.Node = node == root ? null : node.Data as IFileTreeNodeData;
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

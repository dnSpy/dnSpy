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
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Controls;
using dnSpy.Events;
using dnSpy.Tabs;

namespace dnSpy.ToolWindows {
	sealed class ToolWindowGroupManager : IToolWindowGroupManager, IStackedContentChild {
		readonly ITabGroupManager tabGroupManager;

		public event EventHandler<ToolWindowSelectedEventArgs> TabSelectionChanged {
			add { tabSelectionChanged.Add(value); }
			remove { tabSelectionChanged.Remove(value); }
		}
		readonly WeakEventList<ToolWindowSelectedEventArgs> tabSelectionChanged;

		public event EventHandler<ToolWindowGroupSelectedEventArgs> TabGroupSelectionChanged {
			add { tabGroupSelectionChanged.Add(value); }
			remove { tabGroupSelectionChanged.Remove(value); }
		}
		readonly WeakEventList<ToolWindowGroupSelectedEventArgs> tabGroupSelectionChanged;

		public event EventHandler<ToolWindowGroupCollectionChangedEventArgs> TabGroupCollectionChanged {
			add { toolWindowGroupCollectionChanged.Add(value); }
			remove { toolWindowGroupCollectionChanged.Remove(value); }
		}
		readonly WeakEventList<ToolWindowGroupCollectionChangedEventArgs> toolWindowGroupCollectionChanged;

		public object UIObject => tabGroupManager.UIObject;

		public IEnumerable<IToolWindowGroup> TabGroups => tabGroupManager.TabGroups.Select(a => GetToolWindowGroup(a));

		public IToolWindowGroup ActiveTabGroup {
			get { return GetToolWindowGroup(tabGroupManager.ActiveTabGroup); }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				var tg = GetTabGroup(value);
				if (tg == null)
					throw new InvalidOperationException();
				tabGroupManager.ActiveTabGroup = tg;
			}
		}

		public bool IsHorizontal {
			get { return tabGroupManager.IsHorizontal; }
			set { tabGroupManager.IsHorizontal = value; }
		}

		public StackedContentState StackedContentState {
			get { return ((TabGroupManager)tabGroupManager).StackedContentState; }
			set { ((TabGroupManager)tabGroupManager).StackedContentState = value; }
		}

		ITabGroup GetTabGroup(IToolWindowGroup g) => this.tabGroupManager.TabGroups.FirstOrDefault(a => a.Tag == g);

		public ToolWindowGroupManager(ITabGroupManager tabGroupManager) {
			this.tabGroupManager = tabGroupManager;
			this.tabSelectionChanged = new WeakEventList<ToolWindowSelectedEventArgs>();
			this.tabGroupSelectionChanged = new WeakEventList<ToolWindowGroupSelectedEventArgs>();
			this.toolWindowGroupCollectionChanged = new WeakEventList<ToolWindowGroupCollectionChangedEventArgs>();

			this.tabGroupManager.TabSelectionChanged += TabGroupManager_TabSelectionChanged;
			this.tabGroupManager.TabGroupSelectionChanged += TabGroupManager_TabGroupSelectionChanged;
			this.tabGroupManager.TabGroupCollectionChanged += TabGroupManager_TabGroupCollectionChanged;
		}

		internal IToolWindowGroup GetToolWindowGroup(ITabGroup tabGroup) => ToolWindowGroup.GetToolWindowGroup(tabGroup);
		static IToolWindowContent GetToolWindowContent(ITabContent selected) => ((TabContentImpl)selected)?.Content;

		void TabGroupManager_TabSelectionChanged(object sender, TabSelectedEventArgs e) {
			if (e.Selected != null) {
				Debug.Assert(e.TabGroup.ActiveTabContent == e.Selected);
				e.TabGroup.SetFocus(e.Selected);
			}
			tabSelectionChanged.Raise(this, new ToolWindowSelectedEventArgs(GetToolWindowGroup(e.TabGroup), GetToolWindowContent(e.Selected), GetToolWindowContent(e.Unselected)));
		}

		void TabGroupManager_TabGroupSelectionChanged(object sender, TabGroupSelectedEventArgs e) =>
			tabGroupSelectionChanged.Raise(this, new ToolWindowGroupSelectedEventArgs(GetToolWindowGroup(e.Selected), GetToolWindowGroup(e.Unselected)));
		void TabGroupManager_TabGroupCollectionChanged(object sender, TabGroupCollectionChangedEventArgs e) =>
			toolWindowGroupCollectionChanged.Raise(this, new ToolWindowGroupCollectionChangedEventArgs(e.Added, GetToolWindowGroup(e.TabGroup)));
		public IToolWindowGroup Create() => new ToolWindowGroup(this, tabGroupManager.Create());

		public void Close(IToolWindowGroup group) {
			if (group == null)
				throw new ArgumentNullException(nameof(group));
			var impl = group as ToolWindowGroup;
			if (impl == null)
				throw new InvalidOperationException();
			tabGroupManager.Close(impl.TabGroup);
		}

		public bool CloseAllTabsCanExecute => tabGroupManager.ActiveTabGroup != null && tabGroupManager.ActiveTabGroup.TabContents.Count() > 1 && tabGroupManager.CloseAllTabsCanExecute;
		public void CloseAllTabs() => tabGroupManager.CloseAllTabs();
		public bool NewHorizontalTabGroupCanExecute => tabGroupManager.NewHorizontalTabGroupCanExecute;
		public void NewHorizontalTabGroup() => tabGroupManager.NewHorizontalTabGroup(a => new ToolWindowGroup(this, a));
		public bool NewVerticalTabGroupCanExecute => tabGroupManager.NewVerticalTabGroupCanExecute;
		public void NewVerticalTabGroup() => tabGroupManager.NewVerticalTabGroup(a => new ToolWindowGroup(this, a));
		public bool MoveToNextTabGroupCanExecute => tabGroupManager.MoveToNextTabGroupCanExecute;
		public void MoveToNextTabGroup() => tabGroupManager.MoveToNextTabGroup();
		public bool MoveToPreviousTabGroupCanExecute => tabGroupManager.MoveToPreviousTabGroupCanExecute;
		public void MoveToPreviousTabGroup() => tabGroupManager.MoveToPreviousTabGroup();
		public bool MoveAllToNextTabGroupCanExecute => tabGroupManager.MoveAllToNextTabGroupCanExecute;
		public void MoveAllToNextTabGroup() => tabGroupManager.MoveAllToNextTabGroup();
		public bool MoveAllToPreviousTabGroupCanExecute => tabGroupManager.MoveAllToPreviousTabGroupCanExecute;
		public void MoveAllToPreviousTabGroup() => tabGroupManager.MoveAllToPreviousTabGroup();
		public bool CloseTabGroupCanExecute => tabGroupManager.CloseTabGroupCanExecute;
		public void CloseTabGroup() => tabGroupManager.CloseTabGroup();
		public bool CloseAllTabGroupsButThisCanExecute => tabGroupManager.CloseAllTabGroupsButThisCanExecute;
		public void CloseAllTabGroupsButThis() => tabGroupManager.CloseAllTabGroupsButThis();
		public bool MoveTabGroupAfterNextTabGroupCanExecute => tabGroupManager.MoveTabGroupAfterNextTabGroupCanExecute;
		public void MoveTabGroupAfterNextTabGroup() => tabGroupManager.MoveTabGroupAfterNextTabGroup();
		public bool MoveTabGroupBeforePreviousTabGroupCanExecute => tabGroupManager.MoveTabGroupBeforePreviousTabGroupCanExecute;
		public void MoveTabGroupBeforePreviousTabGroup() => tabGroupManager.MoveTabGroupBeforePreviousTabGroup();
		public bool MergeAllTabGroupsCanExecute => tabGroupManager.MergeAllTabGroupsCanExecute;
		public void MergeAllTabGroups() => tabGroupManager.MergeAllTabGroups();
		public bool UseVerticalTabGroupsCanExecute => tabGroupManager.UseVerticalTabGroupsCanExecute;
		public void UseVerticalTabGroups() => tabGroupManager.UseVerticalTabGroups();
		public bool UseHorizontalTabGroupsCanExecute => tabGroupManager.UseHorizontalTabGroupsCanExecute;
		public void UseHorizontalTabGroups() => tabGroupManager.UseHorizontalTabGroups();
	}
}

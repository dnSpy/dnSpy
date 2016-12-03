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
	sealed class ToolWindowGroupService : IToolWindowGroupService, IStackedContentChild {
		readonly ITabGroupService tabGroupService;

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

		public object UIObject => tabGroupService.UIObject;

		public IEnumerable<IToolWindowGroup> TabGroups => tabGroupService.TabGroups.Select(a => GetToolWindowGroup(a));

		public IToolWindowGroup ActiveTabGroup {
			get { return GetToolWindowGroup(tabGroupService.ActiveTabGroup); }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				var tg = GetTabGroup(value);
				if (tg == null)
					throw new InvalidOperationException();
				tabGroupService.ActiveTabGroup = tg;
			}
		}

		public bool IsHorizontal {
			get { return tabGroupService.IsHorizontal; }
			set { tabGroupService.IsHorizontal = value; }
		}

		public StackedContentState StackedContentState {
			get { return ((TabGroupService)tabGroupService).StackedContentState; }
			set { ((TabGroupService)tabGroupService).StackedContentState = value; }
		}

		ITabGroup GetTabGroup(IToolWindowGroup g) => tabGroupService.TabGroups.FirstOrDefault(a => a.Tag == g);

		public ToolWindowGroupService(ITabGroupService tabGroupService) {
			this.tabGroupService = tabGroupService;
			tabSelectionChanged = new WeakEventList<ToolWindowSelectedEventArgs>();
			tabGroupSelectionChanged = new WeakEventList<ToolWindowGroupSelectedEventArgs>();
			toolWindowGroupCollectionChanged = new WeakEventList<ToolWindowGroupCollectionChangedEventArgs>();

			this.tabGroupService.TabSelectionChanged += TabGroupService_TabSelectionChanged;
			this.tabGroupService.TabGroupSelectionChanged += TabGroupService_TabGroupSelectionChanged;
			this.tabGroupService.TabGroupCollectionChanged += TabGroupService_TabGroupCollectionChanged;
		}

		internal IToolWindowGroup GetToolWindowGroup(ITabGroup tabGroup) => ToolWindowGroup.GetToolWindowGroup(tabGroup);
		static ToolWindowContent GetToolWindowContent(ITabContent selected) => ((TabContentImpl)selected)?.Content;

		void TabGroupService_TabSelectionChanged(object sender, TabSelectedEventArgs e) {
			if (e.Selected != null) {
				Debug.Assert(e.TabGroup.ActiveTabContent == e.Selected);
				e.TabGroup.SetFocus(e.Selected);
			}
			tabSelectionChanged.Raise(this, new ToolWindowSelectedEventArgs(GetToolWindowGroup(e.TabGroup), GetToolWindowContent(e.Selected), GetToolWindowContent(e.Unselected)));
		}

		void TabGroupService_TabGroupSelectionChanged(object sender, TabGroupSelectedEventArgs e) =>
			tabGroupSelectionChanged.Raise(this, new ToolWindowGroupSelectedEventArgs(GetToolWindowGroup(e.Selected), GetToolWindowGroup(e.Unselected)));
		void TabGroupService_TabGroupCollectionChanged(object sender, TabGroupCollectionChangedEventArgs e) =>
			toolWindowGroupCollectionChanged.Raise(this, new ToolWindowGroupCollectionChangedEventArgs(e.Added, GetToolWindowGroup(e.TabGroup)));
		public IToolWindowGroup Create() => new ToolWindowGroup(this, tabGroupService.Create());

		public void Close(IToolWindowGroup group) {
			if (group == null)
				throw new ArgumentNullException(nameof(group));
			var impl = group as ToolWindowGroup;
			if (impl == null)
				throw new InvalidOperationException();
			tabGroupService.Close(impl.TabGroup);
		}

		public bool CloseAllTabsCanExecute => tabGroupService.ActiveTabGroup != null && tabGroupService.ActiveTabGroup.TabContents.Count() > 1 && tabGroupService.CloseAllTabsCanExecute;
		public void CloseAllTabs() => tabGroupService.CloseAllTabs();
		public bool NewHorizontalTabGroupCanExecute => tabGroupService.NewHorizontalTabGroupCanExecute;
		public void NewHorizontalTabGroup() => tabGroupService.NewHorizontalTabGroup(a => new ToolWindowGroup(this, a));
		public bool NewVerticalTabGroupCanExecute => tabGroupService.NewVerticalTabGroupCanExecute;
		public void NewVerticalTabGroup() => tabGroupService.NewVerticalTabGroup(a => new ToolWindowGroup(this, a));
		public bool MoveToNextTabGroupCanExecute => tabGroupService.MoveToNextTabGroupCanExecute;
		public void MoveToNextTabGroup() => tabGroupService.MoveToNextTabGroup();
		public bool MoveToPreviousTabGroupCanExecute => tabGroupService.MoveToPreviousTabGroupCanExecute;
		public void MoveToPreviousTabGroup() => tabGroupService.MoveToPreviousTabGroup();
		public bool MoveAllToNextTabGroupCanExecute => tabGroupService.MoveAllToNextTabGroupCanExecute;
		public void MoveAllToNextTabGroup() => tabGroupService.MoveAllToNextTabGroup();
		public bool MoveAllToPreviousTabGroupCanExecute => tabGroupService.MoveAllToPreviousTabGroupCanExecute;
		public void MoveAllToPreviousTabGroup() => tabGroupService.MoveAllToPreviousTabGroup();
		public bool CloseTabGroupCanExecute => tabGroupService.CloseTabGroupCanExecute;
		public void CloseTabGroup() => tabGroupService.CloseTabGroup();
		public bool CloseAllTabGroupsButThisCanExecute => tabGroupService.CloseAllTabGroupsButThisCanExecute;
		public void CloseAllTabGroupsButThis() => tabGroupService.CloseAllTabGroupsButThis();
		public bool MoveTabGroupAfterNextTabGroupCanExecute => tabGroupService.MoveTabGroupAfterNextTabGroupCanExecute;
		public void MoveTabGroupAfterNextTabGroup() => tabGroupService.MoveTabGroupAfterNextTabGroup();
		public bool MoveTabGroupBeforePreviousTabGroupCanExecute => tabGroupService.MoveTabGroupBeforePreviousTabGroupCanExecute;
		public void MoveTabGroupBeforePreviousTabGroup() => tabGroupService.MoveTabGroupBeforePreviousTabGroup();
		public bool MergeAllTabGroupsCanExecute => tabGroupService.MergeAllTabGroupsCanExecute;
		public void MergeAllTabGroups() => tabGroupService.MergeAllTabGroups();
		public bool UseVerticalTabGroupsCanExecute => tabGroupService.UseVerticalTabGroupsCanExecute;
		public void UseVerticalTabGroups() => tabGroupService.UseVerticalTabGroups();
		public bool UseHorizontalTabGroupsCanExecute => tabGroupService.UseHorizontalTabGroupsCanExecute;
		public void UseHorizontalTabGroups() => tabGroupService.UseHorizontalTabGroups();
	}
}

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
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Tabs;
using dnSpy.Controls;
using dnSpy.Events;

namespace dnSpy.Tabs {
	sealed class TabGroupManager : ITabGroupManager, IStackedContentChild {
		internal int ActiveIndex {
			get { return _activeIndex; }
			private set { _activeIndex = value; }
		}
		int _activeIndex;

		public object Tag { get; set; }

		public ITabGroup ActiveTabGroup {
			get { return ActiveIndex < 0 ? null : stackedContent[ActiveIndex]; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				var g = value as TabGroup;
				if (g == null)
					throw new InvalidOperationException();
				SetActive(g);
			}
		}

		public IEnumerable<ITabGroup> TabGroups {
			get { return stackedContent.Children.AsEnumerable(); }
		}

		public object UIObject {
			get { return stackedContent.UIObject; }
		}

		public bool IsHorizontal {
			get { return !stackedContent.IsHorizontal; }
			set { stackedContent.IsHorizontal = !value; }
		}

		public event EventHandler<TabSelectedEventArgs> TabSelectionChanged {
			add { tabSelectionChanged.Add(value); }
			remove { tabSelectionChanged.Remove(value); }
		}
		readonly WeakEventList<TabSelectedEventArgs> tabSelectionChanged;

		public event EventHandler<TabGroupSelectedEventArgs> TabGroupSelectionChanged {
			add { tabGroupSelectionChanged.Add(value); }
			remove { tabGroupSelectionChanged.Remove(value); }
		}
		readonly WeakEventList<TabGroupSelectedEventArgs> tabGroupSelectionChanged;

		public event EventHandler<TabGroupCollectionChangedEventArgs> TabGroupCollectionChanged {
			add { tabGroupCollectionChanged.Add(value); }
			remove { tabGroupCollectionChanged.Remove(value); }
		}
		readonly WeakEventList<TabGroupCollectionChangedEventArgs> tabGroupCollectionChanged;

		public StackedContentState StackedContentState {
			get { return stackedContent.State; }
			set { stackedContent.State = value; }
		}

		ITabManager ITabGroupManager.TabManager {
			get { return tabManager; }
		}
		public TabManager TabManager {
			get { return tabManager; }
		}
		readonly TabManager tabManager;

		readonly StackedContent<TabGroup> stackedContent;
		readonly IMenuManager menuManager;
		readonly IWpfFocusManager wpfFocusManager;
		readonly TabGroupManagerOptions options;

		public TabGroupManager(TabManager tabManager, IMenuManager menuManager, IWpfFocusManager wpfFocusManager, TabGroupManagerOptions options) {
			this.options = (options ?? new TabGroupManagerOptions()).Clone();
			this.stackedContent = new StackedContent<TabGroup>();
			this.tabSelectionChanged = new WeakEventList<TabSelectedEventArgs>();
			this.tabGroupSelectionChanged = new WeakEventList<TabGroupSelectedEventArgs>();
			this.tabGroupCollectionChanged = new WeakEventList<TabGroupCollectionChangedEventArgs>();
			this.tabManager = tabManager;
			this.menuManager = menuManager;
			this.wpfFocusManager = wpfFocusManager;
			this._activeIndex = -1;
		}

		internal void OnThemeChanged() {
			foreach (var g in stackedContent.Children)
				g.OnThemeChanged();
		}

		ITabGroup ITabGroupManager.Create() {
			return Create(stackedContent.Count, null);
		}

		TabGroup Create(int index, Action<ITabGroup> onCreated) {
			var tg = new TabGroup(this, menuManager, wpfFocusManager, options);
			stackedContent.AddChild(tg, null, index);
			if (ActiveIndex < 0)
				ActiveIndex = index;
			if (onCreated != null)
				onCreated(tg);
			tabGroupCollectionChanged.Raise(this, new TabGroupCollectionChangedEventArgs(true, tg));
			return tg;
		}

		internal void SetActive(TabGroup tabGroup) {
			tabManager.SetActive(this);

			if (tabGroup == ActiveTabGroup)
				return;
			int newIndex = stackedContent.IndexOf(tabGroup);
			if (newIndex < 0)
				throw new InvalidOperationException();
			var oldTabGroup = ActiveTabGroup;
			ActiveIndex = newIndex;
			tabGroupSelectionChanged.Raise(this, new TabGroupSelectedEventArgs(ActiveTabGroup, oldTabGroup));
		}

		internal void Remove(TabGroup tabGroup) {
			int index = stackedContent.IndexOf(tabGroup);
			if (index < 0)
				return;
			if (stackedContent.Count == 1) {
				ActiveIndex = -1;
				stackedContent.Remove(tabGroup);
			}
			else {
				if (ActiveIndex == index) {
					int newIndex = index == 0 ? index + 1 : index - 1;
					SetActive(stackedContent[newIndex]);
					ActiveIndex = newIndex;
				}
				var current = stackedContent[ActiveIndex];
				stackedContent.Remove(tabGroup);
				ActiveIndex = stackedContent.IndexOf(current);
				Debug.Assert(ActiveIndex >= 0);
			}
			tabGroupCollectionChanged.Raise(this, new TabGroupCollectionChangedEventArgs(false, tabGroup));
		}

		internal bool SetActiveTab(TabItemImpl tabItem) {
			if (!stackedContent.Contains(tabItem.Owner))
				return false;
			if (!tabItem.Owner.SetActiveTab(tabItem))
				return false;
			SetActive(tabItem.Owner);
			return true;
		}

		internal void OnSelectionChanged(TabGroup tabGroup, TabItemImpl selected, TabItemImpl unselected) {
			tabSelectionChanged.Raise(this, new TabSelectedEventArgs(tabGroup, selected == null ? null : selected.TabContent, unselected == null ? null : unselected.TabContent));
		}

		public bool NewHorizontalTabGroupCanExecute {
			get {
				return ActiveIndex >= 0 &&
					(stackedContent.Count == 1 || IsHorizontal) &&
					stackedContent[ActiveIndex].Count > 1;
			}
		}

		public void NewHorizontalTabGroup(Action<ITabGroup> onCreated = null) {
			if (!NewHorizontalTabGroupCanExecute)
				return;
			AddNewTabGroup(true, onCreated);
		}

		public bool NewVerticalTabGroupCanExecute {
			get {
				return ActiveIndex >= 0 &&
					(stackedContent.Count == 1 || !IsHorizontal) &&
					stackedContent[ActiveIndex].Count > 1;
			}
		}

		public void NewVerticalTabGroup(Action<ITabGroup> onCreated = null) {
			if (!NewVerticalTabGroupCanExecute)
				return;
			AddNewTabGroup(false, onCreated);
		}

		void AddNewTabGroup(bool horizontal, Action<ITabGroup> onCreated) {
			Debug.Assert(stackedContent.Count == 1 || IsHorizontal == horizontal);

			var newTabGroup = Create(ActiveIndex + 1, onCreated);

			IsHorizontal = horizontal;

			Move(newTabGroup, stackedContent[ActiveIndex], stackedContent[ActiveIndex].ActiveTabItemImpl);
			SetActive(newTabGroup);
		}

		void Move(TabGroup dstTabGroup, TabGroup srcTabGroup, TabItemImpl srcTabItemImpl, int insertIndex = 0) {
			Debug.Assert(stackedContent.Contains(dstTabGroup));
			Debug.Assert(stackedContent.Contains(srcTabGroup));
			Debug.Assert(srcTabGroup.Contains(srcTabItemImpl));
			if (srcTabGroup.MoveToAndSelect(dstTabGroup, srcTabItemImpl, insertIndex))
				SetActive(dstTabGroup);
		}

		public bool MoveToNextTabGroupCanExecute {
			get {
				return ActiveIndex >= 0 &&
					ActiveIndex + 1 < stackedContent.Count &&
					stackedContent[ActiveIndex].ActiveTabItemImpl != null;
			}
		}

		public void MoveToNextTabGroup() {
			if (!MoveToNextTabGroupCanExecute)
				return;
			Move(stackedContent[ActiveIndex + 1], stackedContent[ActiveIndex], stackedContent[ActiveIndex].ActiveTabItemImpl);
		}

		public bool MoveToPreviousTabGroupCanExecute {
			get {
				return ActiveIndex > 0 &&
					stackedContent[ActiveIndex].ActiveTabItemImpl != null;
			}
		}

		public void MoveToPreviousTabGroup() {
			if (!MoveToPreviousTabGroupCanExecute)
				return;
			Move(stackedContent[ActiveIndex - 1], stackedContent[ActiveIndex], stackedContent[ActiveIndex].ActiveTabItemImpl);
		}

		public bool MoveAllToNextTabGroupCanExecute {
			get {
				return ActiveIndex >= 0 &&
					ActiveIndex + 1 < stackedContent.Count &&
					stackedContent[ActiveIndex].Count > 1;
			}
		}

		public void MoveAllToNextTabGroup() {
			if (!MoveAllToNextTabGroupCanExecute)
				return;
			MoveAllToOtherTabGroup(stackedContent[ActiveIndex + 1], stackedContent[ActiveIndex]);
		}

		public bool MoveAllToPreviousTabGroupCanExecute {
			get {
				return ActiveIndex > 0 &&
					stackedContent[ActiveIndex].Count > 1;
			}
		}

		public void MoveAllToPreviousTabGroup() {
			if (!MoveToPreviousTabGroupCanExecute)
				return;
			MoveAllToOtherTabGroup(stackedContent[ActiveIndex - 1], stackedContent[ActiveIndex]);
		}

		void MoveAllToOtherTabGroup(TabGroup dst, TabGroup src) {
			var activeTab = src.ActiveTabItemImpl;
			Merge(dst, src, 0);
			dst.SetSelectedTab(activeTab);
			SetActive(dst);
		}

		public bool CloseAllTabsCanExecute {
			get {
				foreach (var tabGroup in stackedContent.Children) {
					if (tabGroup.CloseAllTabsCanExecute)
						return true;
				}
				return false;
			}
		}

		public void CloseAllTabs() {
			if (!CloseAllTabsCanExecute)
				return;
			foreach (var tabGroup in stackedContent.Children)
				tabGroup.CloseAllTabs();
		}

		void Merge(TabGroup dstTabGroup, TabGroup srcTabGroup, int insertIndex) {
			if (dstTabGroup == srcTabGroup)
				return;
			if (insertIndex < 0 || insertIndex > dstTabGroup.Count)
				insertIndex = dstTabGroup.Count;
			foreach (var srcTabItemImpl in srcTabGroup.AllTabItemImpls.ToArray())
				srcTabGroup.MoveTo(dstTabGroup, srcTabItemImpl, insertIndex++);
		}

		public bool MergeAllTabGroupsCanExecute {
			get { return ActiveIndex >= 0 && stackedContent.Count > 1; }
		}

		public void MergeAllTabGroups() {
			if (!MergeAllTabGroupsCanExecute)
				return;
			var dstTabGroup = stackedContent[ActiveIndex];
			foreach (var tabGroup in stackedContent.Children) {
				if (tabGroup == dstTabGroup)
					continue;
				Merge(dstTabGroup, tabGroup, -1);
			}
		}

		public bool UseVerticalTabGroupsCanExecute {
			get { return stackedContent.Count > 1 && IsHorizontal; }
		}

		public void UseVerticalTabGroups() {
			if (!UseVerticalTabGroupsCanExecute)
				return;
			IsHorizontal = false;
		}

		public bool UseHorizontalTabGroupsCanExecute {
			get { return stackedContent.Count > 1 && !IsHorizontal; }
		}

		public void UseHorizontalTabGroups() {
			if (!UseHorizontalTabGroupsCanExecute)
				return;
			IsHorizontal = true;
		}

		public bool CloseTabGroupCanExecute {
			get { return ActiveIndex >= 0 && stackedContent.Count > 1; }
		}

		public void CloseTabGroup() {
			if (!CloseTabGroupCanExecute)
				return;
			stackedContent[ActiveIndex].CloseAllTabs();
		}

		public bool CloseAllTabGroupsButThisCanExecute {
			get { return ActiveIndex >= 0 && stackedContent.Count > 1; }
		}

		public void CloseAllTabGroupsButThis() {
			if (!CloseAllTabGroupsButThisCanExecute)
				return;
			var activeTabGroup = stackedContent[ActiveIndex];
			foreach (var tabGroup in stackedContent.Children) {
				if (activeTabGroup == tabGroup)
					continue;
				tabGroup.CloseAllTabs();
			}
		}

		public bool MoveTabGroupAfterNextTabGroupCanExecute {
			get { return ActiveIndex >= 0 && ActiveIndex + 1 < stackedContent.Count; }
		}

		public void MoveTabGroupAfterNextTabGroup() {
			if (!MoveTabGroupAfterNextTabGroupCanExecute)
				return;
			SwapTabGroups(ActiveIndex, ActiveIndex + 1);
		}

		public bool MoveTabGroupBeforePreviousTabGroupCanExecute {
			get { return ActiveIndex > 0; }
		}

		public void MoveTabGroupBeforePreviousTabGroup() {
			if (!MoveTabGroupBeforePreviousTabGroupCanExecute)
				return;
			SwapTabGroups(ActiveIndex - 1, ActiveIndex);
		}

		void SwapTabGroups(int index1, int index2) {
			stackedContent.SwapChildren(index1, index2);

			if (ActiveIndex == index1)
				ActiveIndex = index2;
			else if (ActiveIndex == index2)
				ActiveIndex = index1;
		}

		public void Close(ITabGroup tabGroup) {
			if (tabGroup == null)
				throw new ArgumentNullException();
			var impl = tabGroup as TabGroup;
			if (impl == null)
				throw new InvalidOperationException();
			impl.CloseAllTabs();
		}
	}
}

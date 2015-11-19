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
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Tabs;
using dnSpy.Controls;

namespace dnSpy.Tabs {
	sealed class TabGroupManager : ITabGroupManager {
		internal int ActiveIndex {
			get { return _activeIndex; }
			private set { _activeIndex = value; }
		}
		int _activeIndex;

		public ITabGroup ActiveTabItem {
			get { return ActiveIndex < 0 ? null : stackedContent[ActiveIndex]; }
		}

		public IEnumerable<ITabGroup> TabGroups {
			get { return stackedContent.Children.AsEnumerable(); }
		}

		public object UIObject {
			get { return stackedContent.UIObject; }
		}

		public bool IsHorizontal {
			get { return stackedContent.IsHorizontal; }
			set { stackedContent.IsHorizontal = value; }
		}

		readonly StackedContent<TabGroup> stackedContent;
		readonly TabManager tabManager;
		readonly IMenuManager menuManager;
		readonly Guid tabGroupGuid;

		public TabGroupManager(TabManager tabManager, IMenuManager menuManager, Guid tabGroupGuid) {
			this.stackedContent = new StackedContent<TabGroup>();
			this.tabManager = tabManager;
			this.menuManager = menuManager;
			this.tabGroupGuid = tabGroupGuid;
		}

		internal void OnThemeChanged() {
			foreach (var g in stackedContent.Children)
				g.OnThemeChanged();
		}

		ITabGroup ITabGroupManager.Create() {
			return Create();
		}

		TabGroup Create() {
			return Create(stackedContent.Count);
		}

		TabGroup Create(int index) {
			var tg = new TabGroup(this, menuManager, tabGroupGuid);
			stackedContent.AddChild(tg, null, index);
			return tg;
		}

		internal void SetActive(TabGroup tabGroup) {
			tabManager.SetActive(this);

			if (tabGroup == ActiveTabItem)
				return;
			int newIndex = stackedContent.IndexOf(tabGroup);
			if (newIndex < 0)
				throw new InvalidOperationException();
			ActiveIndex = newIndex;
		}

		internal void Remove(TabGroup tabGroup) {
			int index = stackedContent.IndexOf(tabGroup);
			Debug.Assert(index >= 0);
			if (index < 0)
				return;
			if (stackedContent.Count == 1)
				return;

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

		internal bool SetActiveTab(TabItemImpl tabItem) {
			if (!stackedContent.Contains(tabItem.Owner))
				return false;
			if (!tabItem.Owner.SetActiveTab(tabItem))
				return false;
			SetActive(tabItem.Owner);
			return true;
		}

		bool NewHorizontalTabGroupCanExecute() {
			return (stackedContent.Count == 1 || IsHorizontal) &&
				stackedContent[ActiveIndex].Count > 1;
		}

		void NewHorizontalTabGroup() {
			if (!NewHorizontalTabGroupCanExecute())
				return;
			AddNewTabGroup(true);
		}

		bool NewVerticalTabGroupCanExecute() {
			return (stackedContent.Count == 1 || !IsHorizontal) &&
				stackedContent[ActiveIndex].Count > 1;
		}

		void NewVerticalTabGroup() {
			if (!NewVerticalTabGroupCanExecute())
				return;
			AddNewTabGroup(false);
		}

		void AddNewTabGroup(bool horizontal) {
			Debug.Assert(stackedContent.Count == 1 || IsHorizontal == horizontal);

			var newTabGroup = Create(ActiveIndex + 1);

			IsHorizontal = horizontal;

			Move(newTabGroup, stackedContent[ActiveIndex], stackedContent[ActiveIndex].ActiveTabItem);
			SetActive(newTabGroup);
		}

		void Move(TabGroup dstTabGroup, TabGroup srcTabGroup, TabItemImpl srcTabState, int insertIndex = 0) {
			Debug.Assert(stackedContent.Contains(dstTabGroup));
			Debug.Assert(stackedContent.Contains(srcTabGroup));
			Debug.Assert(srcTabGroup.TabControl.Items.Contains(srcTabState));
			if (srcTabGroup.MoveToAndSelect(dstTabGroup, srcTabState, insertIndex))
				SetActive(dstTabGroup);
		}

		bool MoveToNextTabGroupCanExecute() {
			return ActiveIndex + 1 < stackedContent.Count &&
				stackedContent[ActiveIndex].ActiveTabItem != null;
		}

		void MoveToNextTabGroup() {
			if (!MoveToNextTabGroupCanExecute())
				return;
			Move(stackedContent[ActiveIndex + 1], stackedContent[ActiveIndex], stackedContent[ActiveIndex].ActiveTabItem);
		}

		bool MoveToPreviousTabGroupCanExecute() {
			return ActiveIndex != 0 &&
				stackedContent[ActiveIndex].ActiveTabItem != null;
		}

		void MoveToPreviousTabGroup() {
			if (!MoveToPreviousTabGroupCanExecute())
				return;
			Move(stackedContent[ActiveIndex - 1], stackedContent[ActiveIndex], stackedContent[ActiveIndex].ActiveTabItem);
		}

		bool MoveAllToNextTabGroupCanExecute() {
			return ActiveIndex + 1 < stackedContent.Count &&
				stackedContent[ActiveIndex].Count > 1;
		}

		void MoveAllToNextTabGroup() {
			if (!MoveAllToNextTabGroupCanExecute())
				return;
			MoveAllToOtherTabGroup(stackedContent[ActiveIndex + 1], stackedContent[ActiveIndex]);
		}

		bool MoveAllToPreviousTabGroupCanExecute() {
			return ActiveIndex != 0 &&
				stackedContent[ActiveIndex].Count > 1;
		}

		void MoveAllToPreviousTabGroup() {
			if (!MoveToPreviousTabGroupCanExecute())
				return;
			MoveAllToOtherTabGroup(stackedContent[ActiveIndex - 1], stackedContent[ActiveIndex]);
		}

		void MoveAllToOtherTabGroup(TabGroup dst, TabGroup src) {
			var activeTab = src.ActiveTabItem;
			Merge(dst, src, 0);
			dst.SetSelectedTab(activeTab);
			SetActive(dst);
		}

		bool CloseAllTabsCanExecute() {
			foreach (var tabGroup in stackedContent.Children) {
				if (tabGroup.CloseAllTabsCanExecute())
					return true;
			}
			return false;
		}

		void CloseAllTabs() {
			if (!CloseAllTabsCanExecute())
				return;
			foreach (var tabGroup in stackedContent.Children)
				tabGroup.CloseAllTabs();
		}

		void Merge(TabGroup dstTabGroup, TabGroup srcTabGroup, int insertIndex) {
			if (dstTabGroup == srcTabGroup)
				return;
			if (insertIndex < 0 || insertIndex > dstTabGroup.Count)
				insertIndex = dstTabGroup.Count;
			foreach (var srcTabState in srcTabGroup.AllTabItemImpls.ToArray())
				srcTabGroup.MoveTo(dstTabGroup, srcTabState, insertIndex++);
		}

		bool MergeAllTabGroupsCanExecute() {
			return stackedContent.Count > 1;
		}

		void MergeAllTabGroups() {
			if (!MergeAllTabGroupsCanExecute())
				return;
			var dstTabGroup = stackedContent[ActiveIndex];
			foreach (var tabGroup in stackedContent.Children) {
				if (tabGroup == dstTabGroup)
					continue;
				Merge(dstTabGroup, tabGroup, -1);
			}
		}

		bool UseVerticalTabGroupsCanExecute() {
			return stackedContent.Count > 1 && IsHorizontal;
		}

		void UseVerticalTabGroups() {
			if (!UseVerticalTabGroupsCanExecute())
				return;
			IsHorizontal = false;
		}

		bool UseHorizontalTabGroupsCanExecute() {
			return stackedContent.Count > 1 && !IsHorizontal;
		}

		void UseHorizontalTabGroups() {
			if (!UseHorizontalTabGroupsCanExecute())
				return;
			IsHorizontal = true;
		}

		bool CloseTabGroupCanExecute() {
			return stackedContent.Count > 1;
		}

		void CloseTabGroup() {
			if (!CloseTabGroupCanExecute())
				return;
			stackedContent[ActiveIndex].CloseAllTabs();
		}

		bool CloseAllTabGroupsButThisCanExecute() {
			return stackedContent.Count > 1;
		}

		void CloseAllTabGroupsButThis() {
			if (!CloseAllTabGroupsButThisCanExecute())
				return;
			var activeTabGroup = stackedContent[ActiveIndex];
			foreach (var tabGroup in stackedContent.Children) {
				if (activeTabGroup == tabGroup)
					continue;
				tabGroup.CloseAllTabs();
			}
		}

		bool MoveTabGroupAfterNextTabGroupCanExecute() {
			return ActiveIndex + 1 < stackedContent.Count;
		}

		void MoveTabGroupAfterNextTabGroup() {
			if (!MoveTabGroupAfterNextTabGroupCanExecute())
				return;
			SwapTabGroups(ActiveIndex, ActiveIndex + 1);
		}

		bool MoveTabGroupBeforePreviousTabGroupCanExecute() {
			return ActiveIndex != 0;
		}

		void MoveTabGroupBeforePreviousTabGroup() {
			if (!MoveTabGroupBeforePreviousTabGroupCanExecute())
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
	}
}

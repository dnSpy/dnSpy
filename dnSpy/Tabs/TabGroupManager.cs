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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Tabs;

namespace dnSpy.Tabs {
	sealed class TabGroupManager : ITabGroupManager {
		internal int ActiveIndex {
			get { return _activeIndex; }
			private set { _activeIndex = value; }
		}
		int _activeIndex;

		public ITabGroup ActiveTabItem {
			get { return ActiveIndex < 0 ? null : tabGroups[ActiveIndex]; }
		}

		public IEnumerable<ITabGroup> TabGroups {
			get { return tabGroups.AsEnumerable(); }
		}
		readonly List<TabGroup> tabGroups;

		public object UIObject {
			get { return grid; }
		}
		Grid grid;

		public bool IsHorizontal {
			get { return isHorizontal; }
			set {
				if (isHorizontal != value) {
					isHorizontal = value;
					UpdateGrid();
				}
			}
		}
		bool isHorizontal;

		readonly TabManager tabManager;
		readonly IMenuManager menuManager;
		readonly Guid tabGroupGuid;

		public TabGroupManager(TabManager tabManager, IMenuManager menuManager, Guid tabGroupGuid) {
			this.tabManager = tabManager;
			this.menuManager = menuManager;
			this.tabGroupGuid = tabGroupGuid;
			this.tabGroups = new List<TabGroup>();
			this.grid = new Grid();
			this.grid.SetResourceReference(FrameworkElement.StyleProperty, "TabGroupsGridStyle");
			this.isHorizontal = true;
			UpdateGrid();
		}

		internal void OnThemeChanged() {
			foreach (var g in tabGroups)
				g.OnThemeChanged();
		}

		ITabGroup ITabGroupManager.Create() {
			return Create();
		}

		TabGroup Create() {
			var tg = Create(tabGroups.Count);
			UpdateGrid();
			return tg;
		}

		TabGroup Create(int index) {
			var tg = new TabGroup(this, menuManager, tabGroupGuid);
			tabGroups.Insert(index, tg);
			return tg;
		}

		void UpdateGrid() {
			UpdateGrid(IsHorizontal);
		}

		void UpdateGrid(bool horizontal) {
			grid.Children.Clear();
			grid.ColumnDefinitions.Clear();
			grid.RowDefinitions.Clear();

			// Make sure the horizontal grid splitters can resize the content
			double d = 0.0001;
			if (!horizontal) {
				grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
				int rowCol = 0;
				foreach (var tabGroup in tabGroups) {
					if (grid.Children.Count > 0) {
						var gridSplitter = new GridSplitter();
						Panel.SetZIndex(gridSplitter, 1);
						grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(3, GridUnitType.Pixel) });
						gridSplitter.SetValue(Grid.RowProperty, rowCol);
						gridSplitter.Margin = new Thickness(0, -5, 0, -5);
						gridSplitter.BorderThickness = new Thickness(0, 5, 0, 5);
						gridSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
						gridSplitter.VerticalAlignment = VerticalAlignment.Center;
						gridSplitter.Focusable = false;
						gridSplitter.BorderBrush = Brushes.Transparent;
						grid.Children.Add(gridSplitter);
						rowCol++;
					}

					grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1 - d, GridUnitType.Star) });
					tabGroup.TabControl.SetValue(Grid.RowProperty, rowCol);
					tabGroup.TabControl.ClearValue(Grid.ColumnProperty);
					grid.Children.Add(tabGroup.TabControl);
					rowCol++;
					d = -d;
				}
			}
			else {
				grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
				int rowCol = 0;
				foreach (var tabGroup in tabGroups) {
					if (grid.Children.Count > 0) {
						var gridSplitter = new GridSplitter();
						Panel.SetZIndex(gridSplitter, 1);
						grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(3, GridUnitType.Pixel) });
						gridSplitter.SetValue(Grid.ColumnProperty, rowCol);
						gridSplitter.Margin = new Thickness(-5, 0, -5, 0);
						gridSplitter.BorderThickness = new Thickness(5, 0, 5, 0);
						gridSplitter.HorizontalAlignment = HorizontalAlignment.Center;
						gridSplitter.VerticalAlignment = VerticalAlignment.Stretch;
						gridSplitter.Focusable = false;
						gridSplitter.BorderBrush = Brushes.Transparent;
						grid.Children.Add(gridSplitter);
						rowCol++;
					}

					grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1 - d, GridUnitType.Star) });
					tabGroup.TabControl.ClearValue(Grid.RowProperty);
					tabGroup.TabControl.SetValue(Grid.ColumnProperty, rowCol);
					grid.Children.Add(tabGroup.TabControl);
					rowCol++;
					d = -d;
				}
			}
		}

		internal void SetActive(TabGroup tabGroup) {
			tabManager.SetActive(this);

			if (tabGroup == ActiveTabItem)
				return;
			int newIndex = tabGroups.IndexOf(tabGroup);
			if (newIndex < 0)
				throw new InvalidOperationException();
			ActiveIndex = newIndex;
		}

		internal void Remove(TabGroup tabGroup) {
			int index = tabGroups.IndexOf(tabGroup);
			Debug.Assert(index >= 0);
			if (index < 0)
				return;
			if (tabGroups.Count == 1)
				return;

			if (ActiveIndex == index) {
				int newIndex = index == 0 ? index + 1 : index - 1;
				SetActive(tabGroups[newIndex]);
				ActiveIndex = newIndex;
			}
			var current = tabGroups[ActiveIndex];
			tabGroups.Remove(tabGroup);
			ActiveIndex = tabGroups.IndexOf(current);
			Debug.Assert(ActiveIndex >= 0);
			UpdateGrid();
		}

		internal bool SetActiveTab(TabItemImpl tabItem) {
			if (!tabGroups.Contains(tabItem.Owner))
				return false;
			if (!tabItem.Owner.SetActiveTab(tabItem))
				return false;
			SetActive(tabItem.Owner);
			return true;
		}

		bool NewHorizontalTabGroupCanExecute() {
			return (tabGroups.Count == 1 || IsHorizontal) &&
				tabGroups[ActiveIndex].Count > 1;
		}

		void NewHorizontalTabGroup() {
			if (!NewHorizontalTabGroupCanExecute())
				return;
			AddNewTabGroup(true);
		}

		bool NewVerticalTabGroupCanExecute() {
			return (tabGroups.Count == 1 || !IsHorizontal) &&
				tabGroups[ActiveIndex].Count > 1;
		}

		void NewVerticalTabGroup() {
			if (!NewVerticalTabGroupCanExecute())
				return;
			AddNewTabGroup(false);
		}

		void AddNewTabGroup(bool horizontal) {
			Debug.Assert(tabGroups.Count == 1 || IsHorizontal == horizontal);

			var newTabGroup = Create(ActiveIndex + 1);

			UpdateGrid(horizontal);
			IsHorizontal = horizontal;

			Move(newTabGroup, tabGroups[ActiveIndex], tabGroups[ActiveIndex].ActiveTabItem);
			SetActive(newTabGroup);
		}

		void Move(TabGroup dstTabGroup, TabGroup srcTabGroup, TabItemImpl srcTabState, int insertIndex = 0) {
			Debug.Assert(tabGroups.Contains(dstTabGroup));
			Debug.Assert(tabGroups.Contains(srcTabGroup));
			Debug.Assert(srcTabGroup.TabControl.Items.Contains(srcTabState));
			if (srcTabGroup.MoveToAndSelect(dstTabGroup, srcTabState, insertIndex))
				SetActive(dstTabGroup);
		}

		bool MoveToNextTabGroupCanExecute() {
			return ActiveIndex + 1 < tabGroups.Count &&
				tabGroups[ActiveIndex].ActiveTabItem != null;
		}

		void MoveToNextTabGroup() {
			if (!MoveToNextTabGroupCanExecute())
				return;
			Move(tabGroups[ActiveIndex + 1], tabGroups[ActiveIndex], tabGroups[ActiveIndex].ActiveTabItem);
		}

		bool MoveToPreviousTabGroupCanExecute() {
			return ActiveIndex != 0 &&
				tabGroups[ActiveIndex].ActiveTabItem != null;
		}

		void MoveToPreviousTabGroup() {
			if (!MoveToPreviousTabGroupCanExecute())
				return;
			Move(tabGroups[ActiveIndex - 1], tabGroups[ActiveIndex], tabGroups[ActiveIndex].ActiveTabItem);
		}

		bool MoveAllToNextTabGroupCanExecute() {
			return ActiveIndex + 1 < tabGroups.Count &&
				tabGroups[ActiveIndex].Count > 1;
		}

		void MoveAllToNextTabGroup() {
			if (!MoveAllToNextTabGroupCanExecute())
				return;
			MoveAllToOtherTabGroup(tabGroups[ActiveIndex + 1], tabGroups[ActiveIndex]);
		}

		bool MoveAllToPreviousTabGroupCanExecute() {
			return ActiveIndex != 0 &&
				tabGroups[ActiveIndex].Count > 1;
		}

		void MoveAllToPreviousTabGroup() {
			if (!MoveToPreviousTabGroupCanExecute())
				return;
			MoveAllToOtherTabGroup(tabGroups[ActiveIndex - 1], tabGroups[ActiveIndex]);
		}

		void MoveAllToOtherTabGroup(TabGroup dst, TabGroup src) {
			var activeTab = src.ActiveTabItem;
			Merge(dst, src, 0);
			dst.SetSelectedTab(activeTab);
			SetActive(dst);
		}

		bool CloseAllTabsCanExecute() {
			foreach (var tabGroup in tabGroups) {
				if (tabGroup.CloseAllTabsCanExecute())
					return true;
			}
			return false;
		}

		void CloseAllTabs() {
			if (!CloseAllTabsCanExecute())
				return;
			foreach (var tabGroup in tabGroups.ToArray())
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
			return tabGroups.Count > 1;
		}

		void MergeAllTabGroups() {
			if (!MergeAllTabGroupsCanExecute())
				return;
			var dstTabGroup = tabGroups[ActiveIndex];
			foreach (var tabGroup in tabGroups.ToArray()) {
				if (tabGroup == dstTabGroup)
					continue;
				Merge(dstTabGroup, tabGroup, -1);
			}
		}

		bool UseVerticalTabGroupsCanExecute() {
			return tabGroups.Count > 1 && IsHorizontal;
		}

		void UseVerticalTabGroups() {
			if (!UseVerticalTabGroupsCanExecute())
				return;
			IsHorizontal = false;
		}

		bool UseHorizontalTabGroupsCanExecute() {
			return tabGroups.Count > 1 && !IsHorizontal;
		}

		void UseHorizontalTabGroups() {
			if (!UseHorizontalTabGroupsCanExecute())
				return;
			IsHorizontal = true;
		}

		bool CloseTabGroupCanExecute() {
			return tabGroups.Count > 1;
		}

		void CloseTabGroup() {
			if (!CloseTabGroupCanExecute())
				return;
			tabGroups[ActiveIndex].CloseAllTabs();
		}

		bool CloseAllTabGroupsButThisCanExecute() {
			return tabGroups.Count > 1;
		}

		void CloseAllTabGroupsButThis() {
			if (!CloseAllTabGroupsButThisCanExecute())
				return;
			var activeTabGroup = tabGroups[ActiveIndex];
			foreach (var tabGroup in tabGroups.ToArray()) {
				if (activeTabGroup == tabGroup)
					continue;
				tabGroup.CloseAllTabs();
			}
		}

		bool MoveTabGroupAfterNextTabGroupCanExecute() {
			return ActiveIndex + 1 < tabGroups.Count;
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
			var tmp1 = tabGroups[index1];
			tabGroups[index1] = tabGroups[index2];
			tabGroups[index2] = tmp1;

			if (ActiveIndex == index1)
				ActiveIndex = index2;
			else if (ActiveIndex == index2)
				ActiveIndex = index1;

			UpdateGrid();
		}
	}
}

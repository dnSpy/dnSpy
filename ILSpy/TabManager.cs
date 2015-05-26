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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ICSharpCode.ILSpy.TextView;

namespace ICSharpCode.ILSpy
{
	public enum TabManagerAddType
	{
		Add,
		Remove,
		Attach,
		Detach,
	}

	public enum TabManagerState
	{
		Empty,
		Active,
		Inactive,
	}

	abstract class TabManagerBase
	{
		internal abstract void Close(object tabState);
		internal abstract void OnThemeChanged();
	}

	sealed class TabManager<TState> : TabManagerBase, INotifyPropertyChanged where TState : TabState
	{
		readonly TabControl tabControl;

		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string propName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}

		public bool IsActive {
			get { return isActive; }
			internal set {
				if (isActive != value) {
					isActive = value;
					foreach (var tabState in AllTabStates)
						tabState.IsActive = IsActive;
					OnPropertyChanged("IsActive");
					OnPropertyChanged("TabManagerState");
				}
			}
		}
		bool isActive;

		public TabManagerState TabManagerState {
			get {
				if (Count == 0)
					return TabManagerState.Empty;
				return IsActive ? TabManagerState.Active : TabManagerState.Inactive;
			}
		}

		public TabControl TabControl {
			get { return tabControl; }
		}

		public int Count {
			get { return tabControl.Items.Count; }
		}

		public int ActiveIndex {
			get { return tabControl.SelectedIndex; }
		}

		internal TState ActiveTabState {
			get {
				int index = tabControl.SelectedIndex == -1 ? 0 : tabControl.SelectedIndex;
				if (index >= tabControl.Items.Count)
					return null;
				var item = tabControl.Items[index] as TabItem;
				return item == null ? null : (TState)item.DataContext;
			}
		}

		public IEnumerable<TState> AllTabStates {
			get {
				foreach (var item in tabControl.Items) {
					var tabItem = item as TabItem;
					if (tabItem == null)
						continue;
					Debug.Assert(tabItem.DataContext is TState);
					yield return (TState)tabItem.DataContext;
				}
			}
		}

		readonly Action<TabManager<TState>, TState, TState> OnSelectionChanged;
		readonly Action<TabManager<TState>, TabManagerAddType, TState> OnAddRemoveTabState;
		readonly TabGroupsManager<TState> tabGroupsManager;

		public TabManager(TabGroupsManager<TState> tabGroupsManager, TabControl tabControl, Action<TabManager<TState>, TState, TState> onSelectionChanged, Action<TabManager<TState>, TabManagerAddType, TState> onAddRemoveTabState)
		{
			this.tabGroupsManager = tabGroupsManager;
			this.tabControl = tabControl;
			tabControl.DataContext = this;
			this.tabControl.SelectionChanged += tabControl_SelectionChanged;
			this.OnSelectionChanged = onSelectionChanged;
			this.OnAddRemoveTabState = onAddRemoveTabState;
		}

		internal override void OnThemeChanged()
		{
			// A color is calculated from TabManagerState so make sure it's recalculated
			OnPropertyChanged("TabManagerState");
		}

		internal override void Close(object ts)
		{
			CloseTab((TState)ts);
		}

		void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender != tabControl || e.Source != tabControl)
				return;
			Debug.Assert(e.RemovedItems.Count <= 1);
			Debug.Assert(e.AddedItems.Count <= 1);

			var oldState = e.RemovedItems.Count >= 1 ? (TState)((TabItem)e.RemovedItems[0]).DataContext : null;
			var newState = e.AddedItems.Count >= 1 ? (TState)((TabItem)e.AddedItems[0]).DataContext : null;

			foreach (var item in tabControl.Items) {
				var tabItem = (TabItem)item;
				Debug.Assert(tabItem != null && tabItem.DataContext is TState);
				var tabState = (TState)tabItem.DataContext;
				tabState.IsSelected = tabState == newState;
			}

			OnSelectionChanged(this, oldState, newState);
		}

		internal TState AddNewTabState(TState tabState)
		{
			tabState.Owner = this;
			tabState.TabItem.AllowDrop = true;
			AddEvents(tabState);

			UpdateState(tabState);
			tabControl.Items.Add(tabState.TabItem);
			if (tabControl.Items.Count == 1)
				OnPropertyChanged("TabManagerState");
			OnAddRemoveTabState(this, TabManagerAddType.Add, tabState);
			return tabState;
		}

		void AddEvents(TState tabState)
		{
			tabState.TabItem.MouseRightButtonDown += tabItem_MouseRightButtonDown;
			tabState.TabItem.PreviewMouseDown += tabItem_PreviewMouseDown;
			tabState.TabItem.DragOver += tabItem_DragOver;
			tabState.TabItem.Drop += tabItem_Drop;
			tabState.TabItem.AddHandler(UIElement.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(tabItem_GotKeyboardFocus), true);
		}

		void tabItem_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			tabGroupsManager.SetActive(this);
		}

		void RemoveEvents(TState tabState)
		{
			tabState.TabItem.MouseRightButtonDown -= tabItem_MouseRightButtonDown;
			tabState.TabItem.PreviewMouseDown -= tabItem_PreviewMouseDown;
			tabState.TabItem.DragOver -= tabItem_DragOver;
			tabState.TabItem.Drop -= tabItem_Drop;
			tabState.TabItem.RemoveHandler(UIElement.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(tabItem_GotKeyboardFocus));
		}

		TState GetTabState(object o)
		{
			var tabItem = o as TabItem;
			if (tabItem == null)
				return null;
			var tabState = tabItem.DataContext as TState;
			if (tabState == null || tabControl.Items.IndexOf(tabState.TabItem) < 0)
				return null;
			return tabState;
		}

		void tabItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			var tabState = GetTabState(sender);
			if (tabState == null)
				return;
			tabControl.SelectedItem = tabState.TabItem;
		}

		void tabItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			var tabState = GetTabState(sender);
			if (tabState == null)
				return;
			var tabItem = tabState.TabItem;

			// We get notified whenever the mouse is down in TabItem.Header and TabItem.Contents
			var mousePos = e.GetPosition(tabItem);
			if (mousePos.X >= tabItem.ActualWidth || mousePos.Y >= tabItem.ActualHeight)
				return;

			var tabControl = tabItem.Parent as TabControl;
			if (tabControl == null)
				return;

			if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) {
				tabGroupsManager.SetActive(this);
				tabControl.SelectedItem = tabItem;//TODO: Doesn't work immediately!
			}

			if (Keyboard.Modifiers == ModifierKeys.None && e.LeftButton == MouseButtonState.Pressed) {
				// HACK: The Close button won't work if we start the drag and drop operation
				if (IsTabButton(tabItem, e.OriginalSource))
					return;
				DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.Move);
			}
		}

		static bool IsTabButton(TabItem tabItem, object o)
		{
			var depo = o as DependencyObject;
			while ((depo is Visual || depo is Visual3D) && !(depo is ButtonBase) && depo != tabItem)
				depo = VisualTreeHelper.GetParent(depo);
			return depo is ButtonBase;
		}

		bool GetInfo(object sender, DragEventArgs e, out TabItem source, out TabItem target, out TabControl tabControlSource, out TabControl tabControlTarget, out TState tabStateSource, out TState tabStateTarget, out TabManager<TState> tabManagerSource, out TabManager<TState> tabManagerTarget, bool canBeSame)
		{
			source = target = null;
			tabControlSource = tabControlTarget = null;
			tabStateSource = tabStateTarget = null;
			tabManagerSource = tabManagerTarget = null;

			if (!e.Data.GetDataPresent(typeof(TabItem)))
				return false;

			target = sender as TabItem;
			source = (TabItem)e.Data.GetData(typeof(TabItem));
			if (target == null || source == null || (!canBeSame && target == source))
				return false;
			tabControlTarget = target.Parent as TabControl;
			if (tabControlTarget == null)
				return false;
			tabControlSource = source.Parent as TabControl;
			if (tabControlSource == null)
				return false;

			tabManagerTarget = tabControlTarget.DataContext as TabManager<TState>;
			tabManagerSource = tabControlSource.DataContext as TabManager<TState>;
			if (tabManagerTarget == null || tabManagerSource == null)
				return false;
			if (tabManagerTarget.tabGroupsManager != tabManagerSource.tabGroupsManager)
				return false;
			if (tabManagerTarget.tabGroupsManager != this.tabGroupsManager)
				return false;

			tabStateSource = source.DataContext as TState;
			tabStateTarget = target.DataContext as TState;
			if (tabStateSource == null || tabStateTarget == null)
				return false;

			return true;
		}

		void tabItem_DragOver(object sender, DragEventArgs e)
		{
			var tabState = GetTabState(sender);
			if (tabState == null)
				return;
			bool canDrag = false;

			TabItem source, target;
			TabControl tabControlSource, tabControlTarget;
			TState tabStateSource, tabStateTarget;
			TabManager<TState> tabManagerSource, tabManagerTarget;
			if (GetInfo(sender, e, out source, out target, out tabControlSource, out tabControlTarget, out tabStateSource, out tabStateTarget, out tabManagerSource, out tabManagerTarget, true))
				canDrag = true;

			e.Effects = canDrag ? DragDropEffects.Move : DragDropEffects.None;
			e.Handled = true;
		}

		void tabItem_Drop(object sender, DragEventArgs e)
		{
			TabItem source, target;
			TabControl tabControlSource, tabControlTarget;
			TState tabStateSource, tabStateTarget;
			TabManager<TState> tabManagerSource, tabManagerTarget;
			if (!GetInfo(sender, e, out source, out target, out tabControlSource, out tabControlTarget, out tabStateSource, out tabStateTarget, out tabManagerSource, out tabManagerTarget, false))
				return;

			if (tabManagerSource.MoveToAndSelect(tabManagerTarget, tabStateSource, tabStateTarget))
				tabManagerTarget.tabGroupsManager.SetActive(tabManagerTarget);
		}

		TState AttachTabState(TState tabState, int insertIndex)
		{
			tabState.Owner = this;
			AddEvents(tabState);
			if (insertIndex < 0 || insertIndex > TabControl.Items.Count)
				insertIndex = TabControl.Items.Count;
			UpdateState(tabState);
			tabControl.Items.Insert(insertIndex, tabState.TabItem);
			if (tabControl.Items.Count == 1)
				OnPropertyChanged("TabManagerState");
			OnAddRemoveTabState(this, TabManagerAddType.Attach, tabState);
			return tabState;
		}

		void UpdateState(TState tabState)
		{
			Debug.Assert(tabControl.Items.IndexOf(tabState) < 0);
			tabState.IsSelected = false;	// It's not inserted so can't be selected
			tabState.IsActive = IsActive;
		}

		public void SetSelectedIndex(int index)
		{
			int selectedIndex = unchecked((uint)index) < (uint)tabControl.Items.Count ?
						index : tabControl.Items.Count == 0 ? -1 : 0;
			tabControl.SelectedIndex = selectedIndex;
		}

		public void SetSelectedTab(TabState tabState)
		{
			tabControl.SelectedItem = tabState.TabItem;
		}

		// This method is only executed when the text editor does NOT have keyboard focus
		void SelectTab(int index)
		{
			if (tabControl.Items.Count == 0)
				return;
			if (index < 0)
				index += tabControl.Items.Count;
			index = index % tabControl.Items.Count;
			tabControl.SelectedIndex = index;
		}

		public void SelectNextTab()
		{
			SelectTab(tabControl.SelectedIndex + 1);
		}

		public bool SelectNextTabCanExecute()
		{
			return tabControl.Items.Count > 1;
		}

		public void SelectPreviousTab()
		{
			SelectTab(tabControl.SelectedIndex - 1);
		}

		public bool SelectPreviousTabCanExecute()
		{
			return tabControl.Items.Count > 1;
		}

		public void CloseActiveTab()
		{
			RemoveTabState(ActiveTabState);
		}

		public bool CloseActiveTabCanExecute()
		{
			return ActiveTabState != null;
		}

		public void CloseTab(TState tabState)
		{
			RemoveTabState(tabState);
		}

		public void CloseAllButActiveTab()
		{
			var activeTab = ActiveTabState;
			if (activeTab == null)
				return;
			foreach (var tabState in AllTabStates.ToArray()) {
				if (tabState != activeTab)
					RemoveTabState(tabState);
			}
		}

		public bool CloseAllButActiveTabCanExecute()
		{
			return tabControl.Items.Count > 1;
		}

		internal bool CloseAllTabsCanExecute()
		{
			return tabControl.Items.Count > 0;
		}

		internal void CloseAllTabs()
		{
			foreach (var tabState in AllTabStates.ToArray())
				RemoveTabState(tabState);
		}

		public void RemoveAllTabStates()
		{
			var allTabStates = AllTabStates.ToArray();
			tabControl.Items.Clear();
			foreach (var tabState in allTabStates)
				RemoveTabStateInternal(tabState);
			NotifyIfEmtpy();
		}

		public void RemoveTabState(TState tabState)
		{
			if (tabState == null)
				return;
			Debug.Assert(tabControl.Items.Contains(tabState.TabItem));
			DetachNoEvents(tabState);
			RemoveTabStateInternal(tabState);
			NotifyIfEmtpy();
		}

		void DetachNoEvents(TState tabState)
		{
			if (tabState == null)
				return;
			int index = tabControl.Items.IndexOf(tabState.TabItem);
			Debug.Assert(index >= 0);
			if (index < 0)
				return;

			if (tabControl.Items.Count == 1) {
				tabControl.Items.RemoveAt(index);
				tabControl.SelectedIndex = -1;
			}
			else if (index == 0) {
				tabControl.SelectedIndex = index + 1;
				tabControl.Items.RemoveAt(index);
			}
			else {
				tabControl.SelectedIndex = index - 1;
				tabControl.Items.RemoveAt(index);
			}
		}

		public void DetachTabState(TState tabState)
		{
			DetachNoEvents(tabState);
			RemoveEvents(tabState);
			OnAddRemoveTabState(this, TabManagerAddType.Detach, tabState);
			NotifyIfEmtpy();
		}

		void RemoveTabStateInternal(TState tabState)
		{
			RemoveEvents(tabState);
			OnAddRemoveTabState(this, TabManagerAddType.Remove, tabState);
			tabState.Dispose();
		}

		void NotifyIfEmtpy()
		{
			if (tabControl.Items.Count == 0) {
				OnPropertyChanged("TabManagerState");
				tabGroupsManager.Remove(this);
			}
		}

		public bool MoveToAndSelect(TabManager<TState> dstTabManager, TState srcTabState, TState insertBeforeThis)
		{
			bool res = MoveTo(dstTabManager, srcTabState, insertBeforeThis);
			if (res)
				dstTabManager.SetSelectedTab(srcTabState);
			return res;
		}

		public bool MoveToAndSelect(TabManager<TState> dstTabManager, TState srcTabState, int insertIndex)
		{
			bool res = MoveTo(dstTabManager, srcTabState, insertIndex);
			if (res)
				dstTabManager.SetSelectedTab(srcTabState);
			return res;
		}

		public bool MoveTo(TabManager<TState> dstTabManager, TState srcTabState, TState insertBeforeThis)
		{
			if (insertBeforeThis != null) {
				Debug.Assert(dstTabManager.tabControl.Items.Contains(insertBeforeThis.TabItem));
				return MoveTo(dstTabManager, srcTabState, dstTabManager.tabControl.Items.IndexOf(insertBeforeThis.TabItem));
			}
			else
				return MoveTo(dstTabManager, srcTabState, -1);
		}

		public bool MoveTo(TabManager<TState> dstTabManager, TState srcTabState, int insertIndex)
		{
			Debug.Assert(this.TabControl.Items.Contains(srcTabState.TabItem));
			if (srcTabState == null)
				return false;

			DetachTabState(srcTabState);
			dstTabManager.AttachTabState(srcTabState, insertIndex);
			return true;
		}

		public bool SetActiveTab(TState tabState)
		{
			if (tabState == null || !this.TabControl.Items.Contains(tabState.TabItem))
				return false;
			this.TabControl.SelectedItem = tabState.TabItem;
			return true;
		}
	}
}

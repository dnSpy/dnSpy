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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Tabs;
using dnSpy.Controls;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Tabs {
	enum TabGroupState {
		Empty,
		Active,
		Inactive,
	}

	sealed class TabGroup : ViewModelBase, ITabGroup, IStackedContentChild {
		public IEnumerable<ITabContent> TabContents {
			get { return AllTabItemImpls.Select(a => a.TabContent); }
		}

		internal IEnumerable<TabItemImpl> AllTabItemImpls {
			get { return tabControl.Items.Cast<TabItemImpl>(); }
		}

		public bool IsActive {
			get { return isActive; }
			internal set {
				if (isActive != value) {
					isActive = value;
					foreach (var tabItem in AllTabItemImpls)
						tabItem.IsActive = IsActive;
					OnPropertyChanged("IsActive");
					OnPropertyChanged("TabGroupState");
				}
			}
		}
		bool isActive;

		public TabGroupState TabGroupState {
			get {
				if (Count == 0)
					return TabGroupState.Empty;
				return IsActive ? TabGroupState.Active : TabGroupState.Inactive;
			}
		}

		public bool HasOpenedDoc {
			get { return Count != 0; }
		}

		internal TabControl TabControl {
			get { return tabControl; }
		}
		readonly TabControl tabControl;

		internal int Count {
			get { return tabControl.Items.Count; }
		}

		internal TabItemImpl ActiveTabItem {
			get {
				int index = tabControl.SelectedIndex == -1 ? 0 : tabControl.SelectedIndex;
				if (index >= tabControl.Items.Count)
					return null;
				return (TabItemImpl)tabControl.Items[index];
			}
		}

		internal Guid Guid {
			get { return tabGroupGuid; }
		}

		object IStackedContentChild.UIObject {
			get { return tabControl; }
		}

		IStackedContent IStackedContentChild.StackedContent { get; set; }

		readonly Guid tabGroupGuid;

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly TabGroup tabGroup;

			public GuidObjectsCreator(TabGroup tabGroup) {
				this.tabGroup = tabGroup;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObject creatorObject, bool openedFromKeyboard) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_FILES_TABGROUP_GUID, tabGroup);
			}
		}

		readonly TabGroupManager tabGroupManager;

		public TabGroup(TabGroupManager tabGroupManager, IMenuManager menuManager, Guid tabGroupGuid) {
			this.tabGroupManager = tabGroupManager;
			this.tabGroupGuid = tabGroupGuid;
			this.tabControl = new TabControl();
			this.tabControl.DataContext = this;
			this.tabControl.SetResourceReference(FrameworkElement.StyleProperty, "FileTabGroupTabControl");
			menuManager.InitializeContextMenu(this.tabControl, tabGroupGuid, new GuidObjectsCreator(this));
		}

		internal void OnThemeChanged() {
			OnStylePropChange();
		}

		void OnStylePropChange() {
			OnPropertyChanged("TabManagerState");
			OnPropertyChanged("HasOpenedDoc");
		}

		void AddEvents(TabItemImpl impl) {
			impl.MouseRightButtonDown += tabItem_MouseRightButtonDown;
			impl.PreviewMouseDown += tabItem_PreviewMouseDown;
			impl.DragOver += tabItem_DragOver;
			impl.Drop += tabItem_Drop;
			impl.AddHandler(UIElement.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(tabItem_GotKeyboardFocus), true);
			impl.AddHandler(UIElement.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(tabItem_LostKeyboardFocus), true);
		}

		void RemoveEvents(TabItemImpl impl) {
			impl.MouseRightButtonDown -= tabItem_MouseRightButtonDown;
			impl.PreviewMouseDown -= tabItem_PreviewMouseDown;
			impl.DragOver -= tabItem_DragOver;
			impl.Drop -= tabItem_Drop;
			impl.RemoveHandler(UIElement.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(tabItem_GotKeyboardFocus));
			impl.RemoveHandler(UIElement.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(tabItem_LostKeyboardFocus));
		}

		void tabItem_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
			tabGroupManager.SetActive(this);
			IsActive = true;
		}

		void tabItem_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
			IsActive = false;
		}

		TabItemImpl GetTabItemImpl(object o) {
			var tabItem = o as TabItemImpl;
			if (tabItem == null)
				return null;
			if (tabControl.Items.IndexOf(tabItem) < 0)
				return null;
			return tabItem;
		}

		void tabItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
			var tabItem = GetTabItemImpl(sender);
			if (tabItem == null)
				return;
			tabControl.SelectedItem = tabItem;
		}

		void tabItem_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
			var tabItem = GetTabItemImpl(sender);
			if (tabItem == null)
				return;

			// We get notified whenever the mouse is down in TabItem.Header and TabItem.Contents
			var mousePos = e.GetPosition(tabItem);
			if (mousePos.X >= tabItem.ActualWidth || mousePos.Y >= tabItem.ActualHeight)
				return;

			var tabControl = tabItem.Parent as TabControl;
			if (tabControl == null)
				return;

			if (tabControl.SelectedItem == tabItem)
				tabItem.FocusContent();

			// HACK: The Close button won't work if we start the drag and drop operation
			bool isTabButtonPressed = IsTabButton(tabItem, e.OriginalSource);

			if (!isTabButtonPressed && (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)) {
				tabGroupManager.SetActive(this);
				tabControl.SelectedItem = tabItem;
			}

			if (!isTabButtonPressed && (Keyboard.Modifiers == ModifierKeys.None && e.LeftButton == MouseButtonState.Pressed)) {
				// Don't call DoDragDrop() immediately because it takes ownership of the mouse and
				// prevents the pressed TabItem from becoming selected. When we don't want to drag
				// a tab, the TabItem gets selected first when we release the mouse pointer instead
				// of instantly when we press it. It makes the program feel slow.
				tabControl.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate {
					// Make sure it's still the active TabItem
					if (tabControl.SelectedItem == tabItem) {
						try {
							DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.Move);
						}
						catch (COMException) { // Rarely happens
						}
					}
				}));
			}
		}

		static bool IsTabButton(TabItem tabItem, object o) {
			return GetItem<ButtonBase>(tabItem, o) != null;
		}

		public static T GetItem<T>(DependencyObject view, object o) where T : class {
			var depo = o as DependencyObject;
			while (depo != null && !(depo is T) && depo != view)
				depo = GetParent(depo);
			return depo as T;
		}

		static DependencyObject GetParent(DependencyObject depo) {
			if (depo is Visual || depo is Visual3D)
				return VisualTreeHelper.GetParent(depo);
			else if (depo is FrameworkContentElement)
				return ((FrameworkContentElement)depo).Parent;
			return null;
		}

		bool GetInfo(object sender, DragEventArgs e,
					out TabItemImpl tabItemSource, out TabItemImpl tabItemTarget,
					out TabGroup tabGroupSource, out TabGroup tabGroupTarget,
					bool canBeSame) {
			tabItemSource = tabItemTarget = null;
			tabGroupSource = tabGroupTarget = null;

			if (!e.Data.GetDataPresent(typeof(TabItemImpl)))
				return false;

			tabItemTarget = sender as TabItemImpl;
			tabItemSource = (TabItemImpl)e.Data.GetData(typeof(TabItemImpl));
			if (tabItemTarget == null || tabItemSource == null || (!canBeSame && tabItemTarget == tabItemSource))
				return false;
			var tabControlTarget = tabItemTarget.Parent as TabControl;
			if (tabControlTarget == null)
				return false;
			var tabControlSource = tabItemSource.Parent as TabControl;
			if (tabControlSource == null)
				return false;

			tabGroupTarget = tabControlTarget.DataContext as TabGroup;
			tabGroupSource = tabControlSource.DataContext as TabGroup;
			if (tabGroupTarget == null || tabGroupSource == null)
				return false;
			if (tabGroupTarget.tabGroupManager != tabGroupSource.tabGroupManager)
				return false;
			if (tabGroupTarget.tabGroupManager != this.tabGroupManager)
				return false;

			return true;
		}

		void tabItem_DragOver(object sender, DragEventArgs e) {
			var tabItem = GetTabItemImpl(sender);
			if (tabItem == null)
				return;
			bool canDrag = false;

			TabItemImpl tabItemSource, tabItemTarget;
			TabGroup tabGroupSource, tabGroupTarget;
			if (GetInfo(sender, e, out tabItemSource, out tabItemTarget, out tabGroupSource, out tabGroupTarget, true))
				canDrag = true;

			e.Effects = canDrag ? DragDropEffects.Move : DragDropEffects.None;
			e.Handled = true;
		}

		void tabItem_Drop(object sender, DragEventArgs e) {
			TabItemImpl tabItemSource, tabItemTarget;
			TabGroup tabGroupSource, tabGroupTarget;
			if (!GetInfo(sender, e, out tabItemSource, out tabItemTarget, out tabGroupSource, out tabGroupTarget, false))
				return;

			if (tabGroupSource.MoveToAndSelect(tabGroupTarget, tabItemSource, tabItemTarget))
				tabGroupTarget.tabGroupManager.SetActive(tabGroupTarget);
		}

		public void Add(ITabContent content) {
			if (content == null)
				throw new ArgumentNullException();
			var impl = new TabItemImpl(this, content);
			AddEvents(impl);
			content.OnVisibilityChanged(TabContentVisibilityEvent.Added);
			tabControl.Items.Add(impl);
			if (tabControl.Items.Count == 1)
				OnStylePropChange();
		}

		int IndexOf(ITabContent content) {
			if (content == null)
				throw new ArgumentNullException();
			for (int i = 0; i < tabControl.Items.Count; i++) {
				var ti = (TabItemImpl)tabControl.Items[i];
				if (ti.TabContent == content)
					return i;
			}
			Debug.Fail(string.Format("Couldn't find {0}", content));
			return -1;
		}

		public void Remove(ITabContent content) {
			int index = IndexOf(content);
			if (index >= 0)
				RemoveTabItem((TabItemImpl)tabControl.Items[index]);
		}

		void NotifyIfEmtpy() {
			if (tabControl.Items.Count == 0) {
				OnStylePropChange();
				tabGroupManager.Remove(this);
			}
		}

		internal void SetSelectedTab(TabItemImpl tabItem) {
			tabControl.SelectedItem = tabItem;
		}

		public bool MoveToAndSelect(TabGroup dstTabGroup, TabItemImpl srcTabItem, TabItemImpl insertBeforeThis) {
			bool res = MoveTo(dstTabGroup, srcTabItem, insertBeforeThis);
			if (res)
				dstTabGroup.SetSelectedTab(srcTabItem);
			return res;
		}

		public bool MoveToAndSelect(TabGroup dstTabGroup, TabItemImpl srcTabItem, int insertIndex) {
			bool res = MoveTo(dstTabGroup, srcTabItem, insertIndex);
			if (res)
				dstTabGroup.SetSelectedTab(srcTabItem);
			return res;
		}

		public bool MoveTo(TabGroup dstTabGroup, TabItemImpl srcTabItem, TabItemImpl insertBeforeThis) {
			if (insertBeforeThis != null) {
				Debug.Assert(dstTabGroup.tabControl.Items.Contains(insertBeforeThis));
				return MoveTo(dstTabGroup, srcTabItem, dstTabGroup.tabControl.Items.IndexOf(insertBeforeThis));
			}
			else
				return MoveTo(dstTabGroup, srcTabItem, -1);
		}

		public bool MoveTo(TabGroup dstTabGroup, TabItemImpl srcTabItem, int insertIndex) {
			Debug.Assert(this.TabControl.Items.Contains(srcTabItem));
			if (srcTabItem == null)
				return false;

			DetachTabItem(srcTabItem);
			dstTabGroup.AttachTabItem(srcTabItem, insertIndex);

			if (srcTabItem.IsKeyboardFocusWithin) {
				tabGroupManager.SetActiveTab(srcTabItem);
				this.IsActive = false;
				dstTabGroup.IsActive = true;
			}

			return true;
		}

		TabItemImpl AttachTabItem(TabItemImpl tabItem, int insertIndex) {
			tabItem.Owner = this;
			AddEvents(tabItem);
			if (insertIndex < 0 || insertIndex > TabControl.Items.Count)
				insertIndex = TabControl.Items.Count;
			UpdateState(tabItem);
			tabControl.Items.Insert(insertIndex, tabItem);
			if (tabControl.Items.Count == 1)
				OnStylePropChange();
			return tabItem;
		}

		void UpdateState(TabItemImpl tabItem) {
			Debug.Assert(tabControl.Items.IndexOf(tabItem) < 0);
			tabItem.IsSelected = false;		// It's not inserted so can't be selected
			tabItem.IsActive = IsActive;
		}

		void DetachNoEvents(TabItemImpl tabItem) {
			if (tabItem == null)
				return;
			int index = tabControl.Items.IndexOf(tabItem);
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

		void DetachTabItem(TabItemImpl tabItem) {
			DetachNoEvents(tabItem);
			RemoveEvents(tabItem);
			NotifyIfEmtpy();
		}

		internal bool SetActiveTab(TabItemImpl tabItem) {
			if (tabItem == null || !this.TabControl.Items.Contains(tabItem))
				return false;
			this.TabControl.SelectedItem = tabItem;
			return true;
		}

		internal void Close(TabItemImpl impl) {
			Remove(impl.TabContent);
		}

		// This method is only executed when the text editor does NOT have keyboard focus
		void SelectTab(int index) {
			if (tabControl.Items.Count == 0)
				return;
			if (index < 0)
				index += tabControl.Items.Count;
			index = index % tabControl.Items.Count;
			tabControl.SelectedIndex = index;
		}

		void SelectNextTab() {
			SelectTab(tabControl.SelectedIndex + 1);
		}

		bool SelectNextTabCanExecute() {
			return tabControl.Items.Count > 1;
		}

		void SelectPreviousTab() {
			SelectTab(tabControl.SelectedIndex - 1);
		}

		bool SelectPreviousTabCanExecute() {
			return tabControl.Items.Count > 1;
		}

		void CloseActiveTab() {
			RemoveTabItem(ActiveTabItem);
		}

		bool CloseActiveTabCanExecute() {
			return ActiveTabItem != null;
		}

		void CloseAllButActiveTab() {
			var activeTab = ActiveTabItem;
			if (activeTab == null)
				return;
			foreach (var tabItem in AllTabItemImpls.ToArray()) {
				if (tabItem != activeTab)
					RemoveTabItem(tabItem);
			}
		}

		bool CloseAllButActiveTabCanExecute() {
			return tabControl.Items.Count > 1;
		}

		internal bool CloseAllTabsCanExecute() {
			return tabControl.Items.Count > 0;
		}

		internal void CloseAllTabs() {
			foreach (var tabItem in AllTabItemImpls.ToArray())
				RemoveTabItem(tabItem);
		}

		void RemoveTabItem(TabItemImpl tabItem) {
			if (tabItem == null)
				return;
			Debug.Assert(tabControl.Items.Contains(tabItem));
			DetachNoEvents(tabItem);
			RemoveEvents(tabItem);
			tabItem.TabContent.OnVisibilityChanged(TabContentVisibilityEvent.Removed);
			tabItem.Dispose();
			NotifyIfEmtpy();
		}
	}
}

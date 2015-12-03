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
using System.Windows.Threading;
using dnSpy.Contracts.Controls;
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

		internal int Count {
			get { return tabControl.Items.Count; }
		}

		public bool IsKeyboardFocusWithin {
			get { return tabControl.IsKeyboardFocusWithin; }
		}

		public ITabContent ActiveTabContent {
			get {
				var act = ActiveTabItemImpl;
				return act == null ? null : act.TabContent;
			}
			set {
				if (value == null)
					throw new ArgumentNullException();
				var impl = GetTabItemImpl(value);
				if (impl == null)
					throw new InvalidOperationException();
				tabControl.SelectedItem = impl;
			}
		}

		public void SetFocus(ITabContent content) {
			if (content == null)
				throw new ArgumentNullException();
			var impl = GetTabItemImpl(content);
			if (impl == null)
				throw new InvalidOperationException();
			tabControl.SelectedItem = impl;
			tabGroupManager.SetActive(this);
			SetFocus2(impl.TabContent);
		}

		void SetFocus2(ITabContent content) {
			var fel = content.FocusedElement;
			if (fel == null)
				fel = content.UIObject as IInputElement;
			var sv = fel as ScrollViewer;
			if (sv != null)
				fel = sv.Content as IInputElement ?? fel;
			if (fel == null || !fel.Focusable)
				return;

			var uiel = fel as UIElement;
			if (uiel != null && !uiel.IsVisible)
				new SetFocusWhenVisible(this, content, uiel);
			else
				SetFocusNoChecks(fel);
		}

		void SetFocusNoChecks(IInputElement uiel) {
			Debug.Assert(uiel != null && uiel.Focusable);
			if (uiel == null)
				return;
			wpfFocusManager.Focus(uiel);
		}

		bool IsActiveTab(ITabContent content) {
			var impl = GetTabItemImpl(content);
			if (impl == null)
				return false;
			if (impl != ActiveTabItemImpl)
				return false;
			if (TabGroupManager.ActiveTabGroup != this)
				return false;

			return true;
		}

		sealed class SetFocusWhenVisible {
			readonly TabGroup tabGroup;
			readonly ITabContent content;
			readonly UIElement uiElem;

			public SetFocusWhenVisible(TabGroup tabGroup, ITabContent content, UIElement uiElem) {
				this.tabGroup = tabGroup;
				this.content = content;
				this.uiElem = uiElem;
				uiElem.IsVisibleChanged += uiElem_IsVisibleChanged;
			}

			void uiElem_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
				uiElem.IsVisibleChanged -= uiElem_IsVisibleChanged;
				if (tabGroup.IsActiveTab(content))
					tabGroup.SetFocusNoChecks(uiElem);
			}
		}

		TabItemImpl GetTabItemImpl(ITabContent content) {
			foreach (TabItemImpl impl in tabControl.Items) {
				if (impl.TabContent == content)
					return impl;
			}
			return null;
		}

		internal TabItemImpl ActiveTabItemImpl {
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

		public ITabGroupManager TabGroupManager {
			get { return tabGroupManager; }
		}
		readonly TabGroupManager tabGroupManager;

		object IStackedContentChild.UIObject {
			get { return tabControl; }
		}

		IStackedContent IStackedContentChild.StackedContent { get; set; }

		readonly Guid tabGroupGuid;
		readonly TabControl tabControl;
		readonly IWpfFocusManager wpfFocusManager;

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			readonly TabGroup tabGroup;

			public GuidObjectsCreator(TabGroup tabGroup) {
				this.tabGroup = tabGroup;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObject creatorObject, bool openedFromKeyboard) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_FILES_TABGROUP_GUID, tabGroup);
			}
		}

		public TabGroup(TabGroupManager tabGroupManager, IMenuManager menuManager, IWpfFocusManager wpfFocusManager, Guid tabGroupGuid) {
			this.tabGroupManager = tabGroupManager;
			this.wpfFocusManager = wpfFocusManager;
			this.tabGroupGuid = tabGroupGuid;
			this.tabControl = new TabControl();
			this.tabControl.DataContext = this;
			this.tabControl.SetResourceReference(FrameworkElement.StyleProperty, "FileTabGroupTabControl");
			this.tabControl.SelectionChanged += TabControl_SelectionChanged;
			menuManager.InitializeContextMenu(this.tabControl, tabGroupGuid, new GuidObjectsCreator(this));
		}

		void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (sender != tabControl || e.Source != tabControl)
				return;
			Debug.Assert(e.RemovedItems.Count <= 1);
			Debug.Assert(e.AddedItems.Count <= 1);

			TabItemImpl selected = null, unselected = null;
			if (e.RemovedItems.Count >= 1) {
				unselected = e.RemovedItems[0] as TabItemImpl;
				if (unselected == null)
					return;
			}
			if (e.AddedItems.Count >= 1) {
				selected = e.AddedItems[0] as TabItemImpl;
				if (selected == null)
					return;
			}

			tabGroupManager.SetActive(this);
			tabGroupManager.OnSelectionChanged(this, selected, unselected);
		}

		internal bool Contains(TabItemImpl impl) {
			return tabControl.Items.Contains(impl);
		}

		internal void OnThemeChanged() {
			OnStylePropChange();
		}

		void OnStylePropChange() {
			OnPropertyChanged("TabGroupState");
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
			var tabItem = GetTabItemImpl(sender);
			if (tabItem != null)
				tabItem.IsActive = true;
			IsActive = true;
		}

		void tabItem_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
			var tabItem = GetTabItemImpl(sender);
			if (tabItem != null)
				tabItem.IsActive = false;
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
				SetFocus2(tabItem.TabContent);

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
				depo = UIUtils.GetParent(depo);
			return depo as T;
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
			UpdateState(impl);
			AddToTabControl(impl, tabControl.Items.Count);
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
			Debug.Assert(Contains(srcTabItem));
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
			if (insertIndex < 0 || insertIndex > tabControl.Items.Count)
				insertIndex = tabControl.Items.Count;
			UpdateState(tabItem);
			AddToTabControl(tabItem, insertIndex);
			return tabItem;
		}

		void AddToTabControl(TabItemImpl tabItem, int insertIndex) {
			tabControl.Items.Insert(insertIndex, tabItem);
			if (tabControl.Items.Count == 1) {
				tabControl.SelectedItem = tabItem;
				OnStylePropChange();
			}
		}

		void UpdateState(TabItemImpl tabItem) {
			Debug.Assert(tabControl.Items.IndexOf(tabItem) < 0);
			tabItem.IsSelected = false;		// It's not inserted so can't be selected
			tabItem.IsActive = IsActive && tabControl.IsKeyboardFocusWithin;
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
			if (tabItem == null || !Contains(tabItem))
				return false;
			this.tabControl.SelectedItem = tabItem;
			return true;
		}

		void ITabGroup.Close(ITabContent content) {
			if (content == null)
				throw new ArgumentNullException();
			var impl = GetTabItemImpl(content);
			if (impl == null)
				throw new InvalidOperationException();
			Close(impl);
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

		public void SelectNextTab() {
			SelectTab(tabControl.SelectedIndex + 1);
		}

		public bool SelectNextTabCanExecute {
			get { return tabControl.Items.Count > 1; }
		}

		public void SelectPreviousTab() {
			SelectTab(tabControl.SelectedIndex - 1);
		}

		public bool SelectPreviousTabCanExecute {
			get { return tabControl.Items.Count > 1; }
		}

		public void CloseActiveTab() {
			RemoveTabItem(ActiveTabItemImpl);
		}

		public bool CloseActiveTabCanExecute {
			get { return ActiveTabItemImpl != null; }
		}

		public void CloseAllButActiveTab() {
			var activeTab = ActiveTabItemImpl;
			if (activeTab == null)
				return;
			foreach (var tabItem in AllTabItemImpls.ToArray()) {
				if (tabItem != activeTab)
					RemoveTabItem(tabItem);
			}
		}

		public bool CloseAllButActiveTabCanExecute {
			get { return tabControl.Items.Count > 1; }
		}

		public bool CloseAllTabsCanExecute {
			get { return tabControl.Items.Count > 0; }
		}

		public void CloseAllTabs() {
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

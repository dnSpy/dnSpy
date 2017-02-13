/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Tabs;
using dnSpy.Controls;
using dnSpy.Events;

namespace dnSpy.Tabs {
	enum TabGroupState {
		Empty,
		Active,
		Inactive,
	}

	sealed class TabGroup : ViewModelBase, ITabGroup, IStackedContentChild {
		public object Tag { get; set; }

		public event EventHandler<TabContentAttachedEventArgs> TabContentAttached {
			add { tabContentAttached.Add(value); }
			remove { tabContentAttached.Remove(value); }
		}
		readonly WeakEventList<TabContentAttachedEventArgs> tabContentAttached;

		public IEnumerable<ITabContent> TabContents => AllTabItemImpls.Select(a => a.TabContent);
		internal IEnumerable<TabItemImpl> AllTabItemImpls => tabControl.Items.Cast<TabItemImpl>();

		public bool IsActive {
			get { return isActive; }
			internal set {
				if (isActive != value) {
					isActive = value;
					foreach (var tabItem in AllTabItemImpls)
						tabItem.IsActive = IsActive;
					OnPropertyChanged(nameof(IsActive));
					OnPropertyChanged(nameof(TabGroupState));
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

		public bool HasOpenedDoc => Count != 0;
		internal int Count => tabControl.Items.Count;
		public bool IsKeyboardFocusWithin => tabControl.IsKeyboardFocusWithin;

		public ITabContent ActiveTabContent {
			get {
				var act = ActiveTabItemImpl;
				return act == null ? null : act.TabContent;
			}
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				var impl = GetTabItemImpl(value);
				tabControl.SelectedItem = impl ?? throw new InvalidOperationException();
			}
		}

		public void SetFocus(ITabContent content) {
			if (content == null)
				throw new ArgumentNullException(nameof(content));
			var impl = GetTabItemImpl(content);
			tabControl.SelectedItem = impl ?? throw new InvalidOperationException();
			tabGroupService.SetActive(this);
			SetFocus2(impl.TabContent);
		}

		void SetFocus2(ITabContent content) {
			var fel = content.FocusedElement;
			if (fel == null)
				fel = content.UIObject as IInputElement;
			var sv = fel as ScrollViewer;
			if (sv != null)
				fel = sv.Content as IInputElement ?? fel;

			var focusable = content as IFocusable;
			if (focusable != null && focusable.CanFocus) {
				var uiel = fel as UIElement;
				if (uiel != null && !uiel.IsVisible)
					new SetFocusWhenVisible(this, content, uiel, () => {
						if (wpfFocusService.CanFocus)
							focusable.Focus();
					});
				else {
					if (wpfFocusService.CanFocus)
						focusable.Focus();
				}
			}
			else {
				if (fel == null || !fel.Focusable)
					return;

				var uiel = fel as UIElement;
				if (uiel != null && !uiel.IsVisible)
					new SetFocusWhenVisible(this, content, uiel, () => SetFocusNoChecks(fel));
				else
					SetFocusNoChecks(fel);
			}
		}

		void SetFocusNoChecks(IInputElement uiel) {
			Debug.Assert(uiel != null && uiel.Focusable);
			if (uiel == null)
				return;
			wpfFocusService.Focus(uiel);
		}

		bool IsActiveTab(ITabContent content) {
			var impl = GetTabItemImpl(content);
			if (impl == null)
				return false;
			if (impl != ActiveTabItemImpl)
				return false;
			if (TabGroupService.ActiveTabGroup != this)
				return false;
			if (TabGroupService.TabService.ActiveTabGroupService != TabGroupService)
				return false;

			return true;
		}

		sealed class SetFocusWhenVisible {
			readonly TabGroup tabGroup;
			readonly ITabContent content;
			readonly UIElement uiElem;
			readonly Action action;

			public SetFocusWhenVisible(TabGroup tabGroup, ITabContent content, UIElement uiElem, Action action) {
				this.tabGroup = tabGroup;
				this.content = content;
				this.uiElem = uiElem;
				this.action = action;
				uiElem.IsVisibleChanged += uiElem_IsVisibleChanged;
			}

			void uiElem_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
				uiElem.IsVisibleChanged -= uiElem_IsVisibleChanged;
				if (tabGroup.IsActiveTab(content))
					action();
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

		public ITabGroupService TabGroupService => tabGroupService;
		readonly TabGroupService tabGroupService;

		object IStackedContentChild.UIObject => tabControl;

		readonly TabControl tabControl;
		readonly IWpfFocusService wpfFocusService;
		readonly TabGroupServiceOptions options;

		public IContextMenuProvider ContextMenuProvider => contextMenuProvider;
		readonly IContextMenuProvider contextMenuProvider;

		sealed class GuidObjectsProvider : IGuidObjectsProvider {
			readonly TabGroup tabGroup;

			public GuidObjectsProvider(TabGroup tabGroup) {
				this.tabGroup = tabGroup;
			}

			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsProviderArgs args) {
				yield return new GuidObject(MenuConstants.GUIDOBJ_TABGROUP_GUID, tabGroup);
			}
		}

		public TabGroup(TabGroupService tabGroupService, IMenuService menuService, IWpfFocusService wpfFocusService, TabGroupServiceOptions options) {
			this.options = options;
			tabContentAttached = new WeakEventList<TabContentAttachedEventArgs>();
			this.tabGroupService = tabGroupService;
			this.wpfFocusService = wpfFocusService;
			tabControl = new TabControl();
			tabControl.DataContext = this;
			tabControl.SetStyle(options.TabControlStyle ?? "FileTabGroupTabControlStyle");
			tabControl.SelectionChanged += TabControl_SelectionChanged;
			tabControl.PreviewKeyDown += TabControl_PreviewKeyDown;
			if (options.InitializeContextMenu != null)
				contextMenuProvider = options.InitializeContextMenu(menuService, this, tabControl);
			else if (options.TabGroupGuid != Guid.Empty)
				contextMenuProvider = menuService.InitializeContextMenu(tabControl, options.TabGroupGuid, new GuidObjectsProvider(this));
		}

		void TabControl_PreviewKeyDown(object sender, KeyEventArgs e) {
			// Tool windows hack: if there's only one tool window in the TabControl, the tab is
			// hidden, but this causes a crash in TabControl when we press Ctrl+Tab.
			if (tabControl.Items.Count == 1 && e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) {
				e.Handled = true;
				return;
			}
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

			tabGroupService.SetActive(this);
			tabGroupService.OnSelectionChanged(this, selected, unselected);
		}

		internal bool Contains(TabItemImpl impl) => tabControl.Items.Contains(impl);

		void OnStylePropChange() {
			OnPropertyChanged(nameof(TabGroupState));
			OnPropertyChanged(nameof(HasOpenedDoc));
		}

		void AddEvents(TabItemImpl impl) {
			impl.MouseRightButtonDown += tabItem_MouseRightButtonDown;
			impl.PreviewMouseDown += tabItem_PreviewMouseDown;
			impl.PreviewMouseUp += tabItem_PreviewMouseUp;
			impl.PreviewMouseMove += tabItem_PreviewMouseMove;
			impl.DragOver += tabItem_DragOver;
			impl.Drop += tabItem_Drop;
			impl.AddHandler(UIElement.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(tabItem_GotKeyboardFocus), true);
			impl.AddHandler(UIElement.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(tabItem_LostKeyboardFocus), true);
		}

		void RemoveEvents(TabItemImpl impl) {
			impl.MouseRightButtonDown -= tabItem_MouseRightButtonDown;
			impl.PreviewMouseDown -= tabItem_PreviewMouseDown;
			impl.PreviewMouseUp -= tabItem_PreviewMouseUp;
			impl.PreviewMouseMove -= tabItem_PreviewMouseMove;
			impl.DragOver -= tabItem_DragOver;
			impl.Drop -= tabItem_Drop;
			impl.RemoveHandler(UIElement.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(tabItem_GotKeyboardFocus));
			impl.RemoveHandler(UIElement.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(tabItem_LostKeyboardFocus));
		}

		void tabItem_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
			tabGroupService.SetActive(this);
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

		bool IsDragArea(object sender, MouseEventArgs e, TabItem tabItem) => IsDraggableAP.GetIsDraggable(e.OriginalSource as FrameworkElement);

		bool GetTabItem(object sender, MouseEventArgs e, out TabItemImpl tabItem, out TabControl tabControl) {
			tabItem = null;
			tabControl = null;

			tabItem = GetTabItemImpl(sender);
			if (tabItem == null)
				return false;

			tabControl = tabItem.Parent as TabControl;
			if (tabControl == null)
				return false;

			if (!IsDragArea(sender, e, tabItem))
				return false;

			return true;
		}

		void tabItem_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
			if (!GetTabItem(sender, e, out var tabItem, out var tabControl))
				return;

			if (tabControl.SelectedItem == tabItem)
				SetFocus2(tabItem.TabContent);

			if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) {
				tabGroupService.SetActive(this);
				tabControl.SelectedItem = tabItem;
			}

			possibleDrag = true;
		}
		bool possibleDrag;

		void tabItem_PreviewMouseUp(object sender, MouseButtonEventArgs e) => possibleDrag = false;

		void tabItem_PreviewMouseMove(object sender, MouseEventArgs e) {
			bool oldPossibleDrag = possibleDrag;
			possibleDrag = false;
			if (!oldPossibleDrag)
				return;

			if (!(Keyboard.Modifiers == ModifierKeys.None && e.LeftButton == MouseButtonState.Pressed))
				return;

			if (!GetTabItem(sender, e, out var tabItem, out var tabControl))
				return;

			if (tabControl.SelectedItem == tabItem) {
				try {
					DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.Move);
				}
				catch (COMException) { // Rarely happens
				}
			}
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
			if (tabGroupTarget.tabGroupService.TabService != tabGroupSource.tabGroupService.TabService)
				return false;
			if (tabGroupTarget.tabGroupService != tabGroupService)
				return false;

			return true;
		}

		void tabItem_DragOver(object sender, DragEventArgs e) {
			var tabItem = GetTabItemImpl(sender);
			if (tabItem == null)
				return;
			bool canDrag = false;
			if (GetInfo(sender, e, out var tabItemSource, out var tabItemTarget, out var tabGroupSource, out var tabGroupTarget, true))
				canDrag = true;

			e.Effects = canDrag ? DragDropEffects.Move : DragDropEffects.None;
			e.Handled = true;
		}

		void tabItem_Drop(object sender, DragEventArgs e) {
			if (!GetInfo(sender, e, out var tabItemSource, out var tabItemTarget, out var tabGroupSource, out var tabGroupTarget, false))
				return;

			if (tabGroupSource.MoveToAndSelect(tabGroupTarget, tabItemSource, tabItemTarget))
				tabGroupTarget.tabGroupService.SetActive(tabGroupTarget);
		}

		public void Add(ITabContent content) {
			if (content == null)
				throw new ArgumentNullException(nameof(content));
			var impl = new TabItemImpl(this, content, options.TabItemStyle);
			AddEvents(impl);
			content.OnVisibilityChanged(TabContentVisibilityEvent.Added);
			UpdateState(impl);
			AddToTabControl(impl, tabControl.Items.Count);
		}

		int IndexOf(ITabContent content) {
			if (content == null)
				throw new ArgumentNullException(nameof(content));
			for (int i = 0; i < tabControl.Items.Count; i++) {
				var ti = (TabItemImpl)tabControl.Items[i];
				if (ti.TabContent == content)
					return i;
			}
			Debug.Fail(string.Format("Couldn't find {0}", content));
			return -1;
		}

		void Remove(ITabContent content) {
			int index = IndexOf(content);
			if (index >= 0)
				RemoveTabItem((TabItemImpl)tabControl.Items[index]);
		}

		void NotifyIfEmtpy() {
			if (tabControl.Items.Count == 0) {
				OnStylePropChange();
				tabGroupService.Remove(this);
			}
		}

		internal void SetSelectedTab(TabItemImpl tabItem) => tabControl.SelectedItem = tabItem;

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
				tabGroupService.SetActiveTab(srcTabItem);
				IsActive = false;
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
			tabContentAttached.Raise(this, new TabContentAttachedEventArgs(true, tabItem.TabContent));
			return tabItem;
		}

		void AddToTabControl(TabItemImpl tabItem, int insertIndex) {
			tabControl.Items.Insert(insertIndex, tabItem);
			if (tabControl.Items.Count == 1) {
				// Don't select the item because it will always make the first tab active at startup.
				// The tab will then get an IsVisible event which could initialize stuff that takes
				// a long time to initialize (eg. the C# Interactive tool window)
				// DON'T: tabControl.SelectedItem = tabItem;
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
			tabContentAttached.Raise(this, new TabContentAttachedEventArgs(false, tabItem.TabContent));
			NotifyIfEmtpy();
		}

		internal bool SetActiveTab(TabItemImpl tabItem) {
			if (tabItem == null || !Contains(tabItem))
				return false;
			tabControl.SelectedItem = tabItem;
			return true;
		}

		void ITabGroup.Close(ITabContent content) {
			if (content == null)
				throw new ArgumentNullException(nameof(content));
			var impl = GetTabItemImpl(content);
			if (impl == null)
				throw new InvalidOperationException();
			Close(impl);
		}

		internal void Close(TabItemImpl impl) => Remove(impl.TabContent);

		// This method is only executed when the text editor does NOT have keyboard focus
		void SelectTab(int index) {
			if (tabControl.Items.Count == 0)
				return;
			if (index < 0)
				index += tabControl.Items.Count;
			index = index % tabControl.Items.Count;
			tabControl.SelectedIndex = index;
		}

		public void SelectNextTab() => SelectTab(tabControl.SelectedIndex + 1);
		public bool SelectNextTabCanExecute => tabControl.Items.Count > 1;
		public void SelectPreviousTab() => SelectTab(tabControl.SelectedIndex - 1);
		public bool SelectPreviousTabCanExecute => tabControl.Items.Count > 1;
		public void CloseActiveTab() => RemoveTabItem(ActiveTabItemImpl);
		public bool CloseActiveTabCanExecute => ActiveTabItemImpl != null;

		public void CloseAllButActiveTab() {
			var activeTab = ActiveTabItemImpl;
			if (activeTab == null)
				return;
			foreach (var tabItem in AllTabItemImpls.ToArray()) {
				if (tabItem != activeTab)
					RemoveTabItem(tabItem);
			}
		}

		public bool CloseAllButActiveTabCanExecute => tabControl.Items.Count > 1;
		public bool CloseAllTabsCanExecute => tabControl.Items.Count > 0;

		public void CloseAllTabs() {
			foreach (var tabItem in AllTabItemImpls.ToArray())
				RemoveTabItem(tabItem);
			NotifyIfEmtpy();
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

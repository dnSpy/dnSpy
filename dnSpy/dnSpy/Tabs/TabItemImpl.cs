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

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Tabs;

namespace dnSpy.Tabs {
	sealed class TabItemImpl : TabItem {
		internal ITabContent TabContent => tabContent;
		readonly ITabContent tabContent;

		public bool IsActive {
			get { return isActive; }
			set {
				if (isActive != value) {
					isActive = value;
					if (Header is TheHeader hdr)
						hdr.IsActiveChanged();
				}
			}
		}
		bool isActive;

		sealed class TheHeader : ViewModelBase {
			readonly TabItemImpl impl;

			public bool IsSelected {
				get { return isSelected; }
				set {
					if (isSelected != value) {
						isSelected = value;
						OnPropertyChanged(nameof(IsSelected));
					}
				}
			}
			bool isSelected;

			public bool IsActive => impl.IsActive;
			internal void IsActiveChanged() => OnPropertyChanged(nameof(IsActive));

			public object ToolTip => impl.tabContent.ToolTip;
			public string Header => impl.tabContent.Title;
			public ICommand CloseCommand => new RelayCommand(a => impl.Close(), a => impl.CanClose);

			public TheHeader(TabItemImpl impl) {
				this.impl = impl;
				isSelected = impl.IsSelected;
			}

			internal void TabContentPropertyChanged(string propName) {
				if (propName == nameof(ITabContent.ToolTip))
					OnPropertyChanged(nameof(ToolTip));
				else if (propName == nameof(ITabContent.Title))
					OnPropertyChanged(nameof(Header));
			}
		}

		internal TabGroup Owner {
			get { return tabGroup; }
			set { tabGroup = value; }
		}
		TabGroup tabGroup;

		readonly TheHeader theHeader;

		public TabItemImpl(TabGroup tabGroup, ITabContent tabContent, object objStyle) {
			this.tabGroup = tabGroup;
			this.tabContent = tabContent;
			Content = tabContent.UIObject;
			theHeader = new TheHeader(this);
			DataContext = theHeader;
			Header = theHeader;
			AllowDrop = true;
			AddHandler(GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(GotKeyboardFocus2), true);
			AddHandler(LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(LostKeyboardFocus2), true);
			this.SetStyle(objStyle ?? "FileTabGroupTabItemStyle");
			AddEvents();
		}

		protected override void OnMouseDown(MouseButtonEventArgs e) {
			base.OnMouseDown(e);
			if (!e.Handled) {
				if (e.Source == this && e.ChangedButton == MouseButton.Middle)
					Owner.Close(this);
				else
					tabGroup.SetFocus(TabContent);
				e.Handled = true;
				return;
			}
		}

		void GotKeyboardFocus2(object sender, KeyboardFocusChangedEventArgs e) =>
			tabContent.OnVisibilityChanged(TabContentVisibilityEvent.GotKeyboardFocus);

		void LostKeyboardFocus2(object sender, KeyboardFocusChangedEventArgs e) =>
			tabContent.OnVisibilityChanged(TabContentVisibilityEvent.LostKeyboardFocus);

		protected override void OnSelected(RoutedEventArgs e) {
			base.OnSelected(e);
			theHeader.IsSelected = IsSelected;
			tabContent.OnVisibilityChanged(TabContentVisibilityEvent.Visible);
		}

		protected override void OnUnselected(RoutedEventArgs e) {
			base.OnUnselected(e);
			theHeader.IsSelected = IsSelected;
			tabContent.OnVisibilityChanged(TabContentVisibilityEvent.Hidden);
		}

		bool CanClose => true;
		void Close() => tabGroup.Close(this);

		internal void Dispose() {
			Content = null;
			RemoveEvents();
		}

		void AddEvents() {
			if (tabContent is INotifyPropertyChanged npc)
				npc.PropertyChanged += TabContent_PropertyChanged;
		}

		void RemoveEvents() {
			if (tabContent is INotifyPropertyChanged npc)
				npc.PropertyChanged -= TabContent_PropertyChanged;
		}

		void TabContent_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			theHeader.TabContentPropertyChanged(e.PropertyName);
			if (e.PropertyName == nameof(tabContent.UIObject))
				Content = tabContent.UIObject;
		}
	}
}

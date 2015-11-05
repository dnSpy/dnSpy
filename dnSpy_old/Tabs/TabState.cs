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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Shared.UI.MVVM;
using ICSharpCode.ILSpy;

namespace dnSpy.Tabs {
	public enum TabStateType {
		DecompiledCode,
		HexEditor,
	}

	public abstract class TabState : IDisposable, INotifyPropertyChanged {
		public TabItem TabItem;

		public abstract string Header { get; }
		public abstract TabStateType Type { get; }
		public abstract FrameworkElement ScaleElement { get; }
		public abstract string FileName { get; }
		public abstract string Name { get; }
		public abstract UIElement FocusedElement { get; }

		public bool IsActive {
			get { return isActive; }
			set {
				if (isActive != value) {
					isActive = value;
					OnPropertyChanged("IsActive");
				}
			}
		}
		bool isActive;

		public bool IsSelected {
			get { return isSelected; }
			set {
				if (isSelected != value) {
					isSelected = value;
					OnPropertyChanged("IsSelected");
				}
			}
		}
		bool isSelected;

		public ICommand CloseCommand {
			get { return new RelayCommand(a => Close()); }
		}

		internal TabManagerBase Owner;

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propName) {
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}

		const int MAX_HEADER_LENGTH = 40;
		public string ShortHeader {
			get {
				var header = Header;
				if (header.Length <= MAX_HEADER_LENGTH)
					return header;
				return header.Substring(0, MAX_HEADER_LENGTH) + "...";
			}
		}

		public virtual string ToolTip {
			get {
				var shortHeader = ShortHeader;
				var header = Header;
				return shortHeader == header ? null : header;
			}
		}

		protected TabState() {
			var tabItem = new TabItem();
			TabItem = tabItem;
			TabItem.Header = this;
			tabItem.DataContext = this;
			TabItem.Style = (Style)App.Current.FindResource("TabStateTabItemStyle");
		}

		protected void InstallMouseWheelZoomHandler(UIElement elem) {
			elem.MouseWheel += OnMouseWheel;
		}

		void OnMouseWheel(object sender, MouseWheelEventArgs e) {
			if (Keyboard.Modifiers != ModifierKeys.Control)
				return;

			MainWindow.Instance.ZoomMouseWheel(this, e.Delta);
			e.Handled = true;
		}

		public static TabState GetTabState(FrameworkElement elem) {
			if (elem == null)
				return null;
			return (TabState)elem.Tag;
		}

		protected void UpdateHeader() {
			OnPropertyChanged("Header");
			OnPropertyChanged("ShortHeader");
			OnPropertyChanged("ToolTip");
		}

		void Close() {
			Owner.Close(this);
		}

		public virtual void FocusContent() {
			var uiel = TabItem.Content as UIElement;
			var sv = uiel as ScrollViewer;
			if (sv != null)
				uiel = sv.Content as UIElement ?? uiel;
			if (uiel != null)
				uiel.Focus();
		}

		public object Content {
			get { return TabItem.Content; }
			set {
				var elem = value;
				// Add a scrollviewer if necessary, eg. it's just a data object. NOTE: Don't do this
				// for any control though, because it could be a virtualized listview.
				if (!(elem is UIElement)) {
					elem = new ScrollViewer {
						CanContentScroll = true,
						HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
						VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
						Content = elem,
						Focusable = true,
					};
				}

				TabItem.Content = elem;
			}
		}

		public abstract SavedTabState CreateSavedTabState();

		public void Dispose() {
			// Clear the content so that any cached UI elements can be reused (code could check
			// TabItem.Content.Parent to see if it's still being used)
			TabItem.Content = null;
			Dispose(true);
		}

		protected virtual void Dispose(bool isDisposing) {
		}
	}
}

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

using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Shared.MVVM;
using dnSpy.Tabs;

namespace dnSpy.ToolWindows {
	sealed class TabContentImpl : ITabContent, IFocusable, INotifyPropertyChanged {
		public ICommand CloseCommand {
			get { return new RelayCommand(a => Close(), a => CanClose); }
		}

		public ICommand ShowWindowPositionCommand {
			get { return new RelayCommand(a => ShowWindowPositionMenu(a), a => CanShowWindowPositionMenu); }
		}

		bool IFocusable.CanFocus {
			get {
				var focusable = content as IFocusable;
				return focusable != null && focusable.CanFocus;
			}
		}

		void IFocusable.Focus() {
			var focusable = content as IFocusable;
			Debug.Assert(focusable != null);
			if (focusable != null)
				focusable.Focus();
		}

		public IInputElement FocusedElement {
			get { return content.FocusedElement ?? content.UIObject as IInputElement; }
		}

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

		public string Title {
			get { return content.Title; }
		}

		public object ToolTip {
			get { return content.ToolTip; }
		}

		public object UIObject {
			get {
				if (contentPresenter == null) {
					contentPresenter = new ContentPresenter { Content = this };
					contentPresenter.MouseDown += (s, e) => {
						if (!e.Handled && Owner != null) {
							Owner.SetFocus(this);
							e.Handled = true;
						}
					};
					elementScaler.InstallScale(content.ScaleElement);
					contentPresenter.InputBindings.Add(new KeyBinding(CloseCommand, Key.Escape, ModifierKeys.Shift));
					// Needed if the content already has keyboard focus, eg. happens when moving
					// the tool window from one side to the other.
					contentPresenter.IsVisibleChanged += ContentPresenter_IsVisibleChanged;
				}
				return contentPresenter;
			}
		}
		ContentPresenter contentPresenter;

		void ContentPresenter_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			var cp = (ContentPresenter)sender;
			cp.IsVisibleChanged -= ContentPresenter_IsVisibleChanged;

			if ((bool)e.NewValue)
				IsActive = IsKeyboardFocusWithin;
		}

		bool IsKeyboardFocusWithin {
			get {
				if (contentPresenter.IsKeyboardFocusWithin)
					return true;
				var f = ContentUIObject as IInputElement;
				return f != null && f.IsKeyboardFocusWithin;
			}
		}

		public object ContentUIObject {
			get {
				if (!contentUIObject_initd) {
					contentUIObject_initd = true;
					contentUIObject = content.UIObject;
				}
				return contentUIObject;
			}
			set {
				contentUIObject_initd = true;
				if (contentUIObject != value) {
					contentUIObject = value;
					OnPropertyChanged("ContentUIObject");
				}
			}
		}
		object contentUIObject;
		bool contentUIObject_initd;

		public event PropertyChangedEventHandler PropertyChanged;

		public IToolWindowContent Content {
			get { return content; }
		}
		readonly IToolWindowContent content;

		public ToolWindowGroup Owner {
			get {
				Debug.Assert(owner != null);
				return owner;
			}
			set { owner = value; }
		}
		ToolWindowGroup owner;

		readonly TabElementScaler elementScaler;

		public TabContentImpl(ToolWindowGroup owner, IToolWindowContent content) {
			this.elementScaler = new TabElementScaler();
			this.owner = owner;
			this.content = content;
			AddEvents();
		}

		public void PrepareMove() {
			moving = true;
		}
		bool moving = false;

		public void OnVisibilityChanged(TabContentVisibilityEvent visEvent) {
			var ev = Convert(visEvent);
			if (ev != null) {
#if DEBUG
				switch (ev) {
				case ToolWindowContentVisibilityEvent.Added:
					Debug.Assert(!_added);
					Debug.Assert(!_visible);
					_added = true;
					break;
				case ToolWindowContentVisibilityEvent.Removed:
					Debug.Assert(_added);
					Debug.Assert(!_visible);
					_added = false;
					break;
				case ToolWindowContentVisibilityEvent.Visible:
					Debug.Assert(_added);
					Debug.Assert(!_visible);
					_visible = true;
					break;
				case ToolWindowContentVisibilityEvent.Hidden:
					Debug.Assert(_added);
					Debug.Assert(_visible);
					_visible = false;
					break;
				}
#endif
				if (moving && (visEvent == TabContentVisibilityEvent.Added || visEvent == TabContentVisibilityEvent.Removed)) {
					// Don't send the Added/Removed events
					moving = false;
				}
				else
					content.OnVisibilityChanged(ev.Value);
			}

			switch (visEvent) {
			case TabContentVisibilityEvent.Removed:
				elementScaler.Dispose();
				RemoveEvents();
				if (contentPresenter != null)
					contentPresenter.Content = null;
				contentPresenter = null;
				OnPropertyChanged("UIObject");
				ContentUIObject = null;
				break;

			case TabContentVisibilityEvent.GotKeyboardFocus:
				IsActive = true;
				break;

			case TabContentVisibilityEvent.LostKeyboardFocus:
				IsActive = false;
				break;
			}
		}
#if DEBUG
		bool _added, _visible;
#endif

		static ToolWindowContentVisibilityEvent? Convert(TabContentVisibilityEvent ev) {
			switch (ev) {
			case TabContentVisibilityEvent.Added:				return ToolWindowContentVisibilityEvent.Added;
			case TabContentVisibilityEvent.Removed:				return ToolWindowContentVisibilityEvent.Removed;
			case TabContentVisibilityEvent.Visible:				return ToolWindowContentVisibilityEvent.Visible;
			case TabContentVisibilityEvent.Hidden:				return ToolWindowContentVisibilityEvent.Hidden;
			case TabContentVisibilityEvent.GotKeyboardFocus:	return ToolWindowContentVisibilityEvent.GotKeyboardFocus;
			case TabContentVisibilityEvent.LostKeyboardFocus:	return ToolWindowContentVisibilityEvent.LostKeyboardFocus;
			default:
				Debug.Fail(string.Format("Unknown event: {0}", ev));
				return null;
			}
		}

		void OnPropertyChanged(string name) {
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		void AddEvents() {
			var npc = content as INotifyPropertyChanged;
			if (npc != null)
				npc.PropertyChanged += ToolWindowContent_PropertyChanged;
		}

		void RemoveEvents() {
			var npc = content as INotifyPropertyChanged;
			if (npc != null)
				npc.PropertyChanged -= ToolWindowContent_PropertyChanged;
		}

		void ToolWindowContent_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "Title")
				OnPropertyChanged("Title");
			else if (e.PropertyName == "ToolTip")
				OnPropertyChanged("ToolTip");
			else if (e.PropertyName == "UIObject" && contentUIObject_initd)
				ContentUIObject = content.UIObject;
		}

		bool CanClose {
			get { return true; }
		}

		void Close() {
			if (!CanClose)
				return;
			if (Owner != null)
				Owner.Close(this);
		}

		bool CanShowWindowPositionMenu {
			get { return true; }
		}

		void ShowWindowPositionMenu(object uiObj) {
			var fe = uiObj as FrameworkElement;
			Debug.Assert(fe != null);
			if (fe == null)
				return;

			Owner.SetFocus(this);
			Owner.TabGroup.ContextMenuCreator.Show(fe);
		}
	}
}

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
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Tabs;

namespace dnSpy.ToolWindows {
	sealed class TabContentImpl : ITabContent, IFocusable, INotifyPropertyChanged {
		public ICommand CloseCommand => new RelayCommand(a => Close(), a => CanClose);
		public ICommand ShowWindowPositionCommand => new RelayCommand(a => ShowWindowPositionMenu(a), a => CanShowWindowPositionMenu);

		bool IFocusable.CanFocus {
			get {
				var focusable = Content as IFocusable;
				return focusable != null && focusable.CanFocus;
			}
		}

		void IFocusable.Focus() {
			var focusable = Content as IFocusable;
			Debug.Assert(focusable != null);
			if (focusable != null)
				focusable.Focus();
		}

		public IInputElement FocusedElement => Content.FocusedElement ?? Content.UIObject as IInputElement;

		public bool IsActive {
			get { return isActive; }
			set {
				if (isActive != value) {
					isActive = value;
					OnPropertyChanged(nameof(IsActive));
				}
			}
		}
		bool isActive;

		public string Title => Content.Title;

		public object ToolTip => Content.ToolTip;

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
					UpdateScaleElement();
					contentPresenter.InputBindings.Add(new KeyBinding(CloseCommand, Key.Escape, ModifierKeys.Shift));
					// Needed if the content already has keyboard focus, eg. happens when moving
					// the tool window from one side to the other.
					contentPresenter.IsVisibleChanged += ContentPresenter_IsVisibleChanged;
				}
				return contentPresenter;
			}
		}
		ContentPresenter contentPresenter;

		void UpdateScaleElement() => elementScaler.InstallScale(Content, Content.ScaleElement);

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
					contentUIObject = Content.UIObject;
				}
				return contentUIObject;
			}
			set {
				contentUIObject_initd = true;
				if (contentUIObject != value) {
					contentUIObject = value;
					OnPropertyChanged(nameof(ContentUIObject));
				}
			}
		}
		object contentUIObject;
		bool contentUIObject_initd;

		public event PropertyChangedEventHandler PropertyChanged;
		public IToolWindowContent Content { get; }

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
			this.Content = content;
			AddEvents();
		}

		public void PrepareMove() => moving = true;
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
					Content.OnVisibilityChanged(ev.Value);
			}

			switch (visEvent) {
			case TabContentVisibilityEvent.Removed:
				elementScaler.Dispose();
				RemoveEvents();
				if (contentPresenter != null)
					contentPresenter.Content = null;
				contentPresenter = null;
				OnPropertyChanged(nameof(UIObject));
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

		void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		void AddEvents() {
			var npc = Content as INotifyPropertyChanged;
			if (npc != null)
				npc.PropertyChanged += ToolWindowContent_PropertyChanged;
		}

		void RemoveEvents() {
			var npc = Content as INotifyPropertyChanged;
			if (npc != null)
				npc.PropertyChanged -= ToolWindowContent_PropertyChanged;
		}

		void ToolWindowContent_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(IToolWindowContent.Title))
				OnPropertyChanged(nameof(Title));
			else if (e.PropertyName == nameof(IToolWindowContent.ToolTip))
				OnPropertyChanged(nameof(ToolTip));
			else if (e.PropertyName == nameof(IToolWindowContent.UIObject) && contentUIObject_initd)
				ContentUIObject = Content.UIObject;
			else if (e.PropertyName == nameof(IToolWindowContent.ScaleElement) && contentUIObject_initd)
				UpdateScaleElement();
		}

		bool CanClose => true;

		void Close() {
			if (!CanClose)
				return;
			if (Owner != null)
				Owner.Close(this);
		}

		bool CanShowWindowPositionMenu => true;

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

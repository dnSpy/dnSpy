/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
				return !(focusable is null) && focusable.CanFocus;
			}
		}

		void IFocusable.Focus() {
			var focusable = Content as IFocusable;
			Debug2.Assert(!(focusable is null));
			if (!(focusable is null))
				focusable.Focus();
		}

		public IInputElement? FocusedElement => Content.FocusedElement ?? Content.UIObject as IInputElement;

		public bool IsActive {
			get => isActive;
			set {
				if (isActive != value) {
					isActive = value;
					OnPropertyChanged(nameof(IsActive));
				}
			}
		}
		bool isActive;

		public string? Title => Content.Title;

		public object? ToolTip => Content.ToolTip;

		public object? UIObject {
			get {
				if (contentPresenter is null) {
					contentPresenter = new ContentPresenter { Content = this };
					contentPresenter.MouseDown += (s, e) => {
						if (!e.Handled && !(Owner is null)) {
							Owner.SetFocus(this);
							e.Handled = true;
						}
					};
					contentPresenter.InputBindings.Add(new KeyBinding(CloseCommand, Key.Escape, ModifierKeys.Shift));
					// Needed if the content already has keyboard focus, eg. happens when moving
					// the tool window from one side to the other.
					contentPresenter.IsVisibleChanged += ContentPresenter_IsVisibleChanged;
				}
				return contentPresenter;
			}
		}
		ContentPresenter? contentPresenter;

		void UpdateZoomElement() => elementZoomer.InstallZoom(Content, Content.ZoomElement);

		void ContentPresenter_IsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e) {
			var cp = (ContentPresenter)sender!;
			cp.IsVisibleChanged -= ContentPresenter_IsVisibleChanged;

			if ((bool)e.NewValue)
				IsActive = IsKeyboardFocusWithin;
		}

		bool IsKeyboardFocusWithin {
			get {
				if (contentPresenter?.IsKeyboardFocusWithin == true)
					return true;
				var f = ContentUIObject as IInputElement;
				return !(f is null) && f.IsKeyboardFocusWithin;
			}
		}

		public object? ContentUIObject {
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
		object? contentUIObject;
		bool contentUIObject_initd;

		public event PropertyChangedEventHandler? PropertyChanged;
		public ToolWindowContent Content { get; }

		public ToolWindowGroup? Owner {
			get {
				Debug2.Assert(!(owner is null));
				return owner;
			}
			set => owner = value;
		}
		ToolWindowGroup? owner;

		readonly TabElementZoomer elementZoomer;

		public TabContentImpl(ToolWindowGroup owner, ToolWindowContent content) {
			elementZoomer = new TabElementZoomer();
			this.owner = owner;
			Content = content;
			AddEvents();
		}

		public void PrepareMove() => moving = true;
		bool moving = false;

		public void OnVisibilityChanged(TabContentVisibilityEvent visEvent) {
			var ev = Convert(visEvent);
			if (!(ev is null)) {
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
			case TabContentVisibilityEvent.Added:
				installZoom = true;
				break;

			case TabContentVisibilityEvent.Visible:
				if (installZoom) {
					installZoom = false;
					UpdateZoomElement();
				}
				break;

			case TabContentVisibilityEvent.Removed:
				elementZoomer.Dispose();
				RemoveEvents();
				if (!(contentPresenter is null))
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
		bool installZoom;

		static ToolWindowContentVisibilityEvent? Convert(TabContentVisibilityEvent ev) {
			switch (ev) {
			case TabContentVisibilityEvent.Added:				return ToolWindowContentVisibilityEvent.Added;
			case TabContentVisibilityEvent.Removed:				return ToolWindowContentVisibilityEvent.Removed;
			case TabContentVisibilityEvent.Visible:				return ToolWindowContentVisibilityEvent.Visible;
			case TabContentVisibilityEvent.Hidden:				return ToolWindowContentVisibilityEvent.Hidden;
			case TabContentVisibilityEvent.GotKeyboardFocus:	return ToolWindowContentVisibilityEvent.GotKeyboardFocus;
			case TabContentVisibilityEvent.LostKeyboardFocus:	return ToolWindowContentVisibilityEvent.LostKeyboardFocus;
			default:
				Debug.Fail($"Unknown event: {ev}");
				return null;
			}
		}

		void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		void AddEvents() {
			if (Content is INotifyPropertyChanged npc)
				npc.PropertyChanged += ToolWindowContent_PropertyChanged;
		}

		void RemoveEvents() {
			if (Content is INotifyPropertyChanged npc)
				npc.PropertyChanged -= ToolWindowContent_PropertyChanged;
		}

		void ToolWindowContent_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(ToolWindowContent.Title))
				OnPropertyChanged(nameof(Title));
			else if (e.PropertyName == nameof(ToolWindowContent.ToolTip))
				OnPropertyChanged(nameof(ToolTip));
			else if (e.PropertyName == nameof(ToolWindowContent.UIObject) && contentUIObject_initd)
				ContentUIObject = Content.UIObject;
			else if (e.PropertyName == nameof(ToolWindowContent.ZoomElement) && contentUIObject_initd)
				UpdateZoomElement();
		}

		bool CanClose => true;

		void Close() {
			if (!CanClose)
				return;
			if (!(Owner is null))
				Owner.Close(this);
		}

		bool CanShowWindowPositionMenu => true;

		void ShowWindowPositionMenu(object? uiObj) {
			var fe = uiObj as FrameworkElement;
			Debug2.Assert(!(fe is null));
			if (fe is null)
				return;

			Owner!.SetFocus(this);
			Owner.TabGroup.ContextMenuProvider.Show(fe);
		}
	}
}

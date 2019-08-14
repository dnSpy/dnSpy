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

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace dnSpy.Contracts.Controls.ToolWindows {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public sealed partial class EditValueControl : UserControl {
		public static readonly DependencyProperty ReadOnlyContentProperty =
			DependencyProperty.Register(nameof(ReadOnlyContent), typeof(object), typeof(EditValueControl),
			new FrameworkPropertyMetadata(null));

		public object ReadOnlyContent {
			get => GetValue(ReadOnlyContentProperty);
			set => SetValue(ReadOnlyContentProperty, value);
		}

		public static readonly DependencyProperty EditableValueProperty =
			DependencyProperty.Register(nameof(EditableValue), typeof(IEditableValue), typeof(EditValueControl),
			new FrameworkPropertyMetadata(null, EditableValueProperty_PropertyChangedCallback));

		static void EditableValueProperty_PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
			((EditValueControl)d).OnEditableValuePropertyChanged((IEditableValue)e.OldValue, (IEditableValue)e.NewValue, false);

		public IEditableValue EditableValue {
			get => (IEditableValue)GetValue(EditableValueProperty);
			set => SetValue(EditableValueProperty, value);
		}

		public static readonly DependencyProperty EditValueProviderProperty =
			DependencyProperty.Register(nameof(EditValueProvider), typeof(IEditValueProvider), typeof(EditValueControl),
			new FrameworkPropertyMetadata(null, EditValueProviderProperty_PropertyChangedCallback));

		static void EditValueProviderProperty_PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
			((EditValueControl)d).OnEditValueProviderPropertyChanged();

		public IEditValueProvider EditValueProvider {
			get => (IEditValueProvider)GetValue(EditValueProviderProperty);
			set => SetValue(EditValueProviderProperty, value);
		}

		IEditValue? editValue;
		WeakReference? oldKeyboardFocus;

		public EditValueControl() {
			Loaded += EditValueControl_Loaded;
			Unloaded += EditValueControl_Unloaded;
			InitializeComponent();
		}
		bool isLoaded;

		void EditValueControl_Loaded(object? sender, RoutedEventArgs e) {
			// Loaded can be raised multiple times without an Unloaded event
			if (isLoaded)
				return;
			isLoaded = true;
			OnEditableValuePropertyChanged(EditableValue, EditableValue, true);
		}

		void EditValueControl_Unloaded(object? sender, RoutedEventArgs e) {
			if (!isLoaded)
				return;
			isLoaded = false;
			OnEditableValuePropertyChanged(EditableValue, null, true);
		}

		protected override void OnMouseDoubleClick(MouseButtonEventArgs e) {
			if (!e.Handled && isLoaded) {
				var editableValue = EditableValue;
				if (editableValue?.CanEdit == true) {
					editableValue.IsEditingValue = true;
					e.Handled = true;
					return;
				}
			}
			base.OnMouseDoubleClick(e);
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) {
			if (!e.Handled && isLoaded) {
				var editableValue = EditableValue;
				if (editableValue?.CanEdit == true && (editableValue.Options & EditableValueOptions.SingleClick) != 0) {
					editableValue.IsEditingValue = true;
					e.Handled = true;
					return;
				}
			}
			base.OnMouseLeftButtonUp(e);
		}

		void OnEditableValuePropertyChanged(IEditableValue? oldValue, IEditableValue? newValue, bool force) {
			if (!force && !isLoaded)
				return;
			CancelEdit(oldValue);
			UnhookEvents(oldValue);
			HookEvents(newValue);
			OnIsEditingValueChanged(newValue);
		}

		void OnEditValueProviderPropertyChanged() => CancelEdit(EditableValue);

		void HookEvents(IEditableValue? editableValue) {
			if (editableValue is null)
				return;
			editableValue.PropertyChanged += EditableValue_PropertyChanged;
		}

		void UnhookEvents(IEditableValue? editableValue) {
			if (editableValue is null)
				return;
			editableValue.PropertyChanged -= EditableValue_PropertyChanged;
		}

		void EditableValue_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			var editableValue = (IEditableValue)sender!;
			if (editableValue != EditableValue) {
				UnhookEvents(editableValue);
				return;
			}
			if (e.PropertyName == nameof(editableValue.IsEditingValue))
				OnIsEditingValueChanged(editableValue);
		}

		void OnIsEditingValueChanged(IEditableValue? editableValue) {
			if (editableValue is null)
				return;
			if (!editableValue.CanEdit) {
				CancelEdit(editableValue);
				return;
			}
			if (!editableValue.IsEditingValue) {
				// Make sure it gets removed if it gets canceled by setting IsEditingValue to false
				CancelEdit(editableValue);
				return;
			}

			DisposeEditValue();
			Debug2.Assert(editValue is null);
			var info = editableValue.GetText();
			editValue = EditValueProvider?.Create(info.Text, info.Flags);
			if (editValue is null) {
				CancelEdit(editableValue);
				return;
			}
			var border = new Border {
				BorderThickness = new Thickness(1),
				Child = GetUIElement(editValue.UIObject),
			};
			border.SetResourceReference(Border.BorderBrushProperty, "CommonControlsTextBoxBorderFocused");
			oldKeyboardFocus = new WeakReference(Keyboard.FocusedElement);
			Content = border;
			editValue.EditCompleted += EditValue_EditCompleted;
		}

		static UIElement GetUIElement(object? obj) => obj as UIElement ?? new ContentPresenter { Content = obj };

		void CancelEdit(IEditableValue? editableValue) {
			if (!(editableValue is null))
				editableValue.IsEditingValue = false;
			RemoveEditControl();
		}

		void RemoveEditControl() {
			DisposeEditValue();
			var binding = new Binding(nameof(ReadOnlyContent)) {
				Source = this,
			};
			ClearValue(ContentProperty);
			SetBinding(ContentProperty, binding);
		}

		void DisposeEditValue() {
			RestoreOldKeyboardFocus();
			if (editValue is null)
				return;
			editValue.EditCompleted -= EditValue_EditCompleted;
			editValue.Dispose();
			editValue = null;
		}

		void RestoreOldKeyboardFocus() {
			if (editValue is null)
				return;
			// Don't give back focus if the user canceled it by clicking somewhere with the mouse.
			// Only do it if the user pressed Esc or Enter
			if (editValue.IsKeyboardFocused && oldKeyboardFocus?.Target is IInputElement elem)
				elem.Focus();
			oldKeyboardFocus = null;
		}

		void EditValue_EditCompleted(object? sender, EditCompletedEventArgs e) {
			if (editValue != sender)
				return;

			RestoreOldKeyboardFocus();
			RemoveEditControl();
			var editableValue = EditableValue;
			if (editableValue is null)
				return;
			Debug.Assert(editableValue.IsEditingValue);
			if (!editableValue.IsEditingValue)
				return;
			editableValue.IsEditingValue = false;
			Debug.Assert(editableValue.CanEdit);
			if (!editableValue.CanEdit)
				return;
			if (e.NewText is null)
				return;
			editableValue.SetText(e.NewText);
		}
	}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}

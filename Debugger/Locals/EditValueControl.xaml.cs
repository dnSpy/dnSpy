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

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Locals {
	sealed partial class EditValueControl : UserControl {
		public static readonly DependencyProperty ReadOnlyContentProperty =
			DependencyProperty.Register("ReadOnlyContent", typeof(object), typeof(EditValueControl),
			new FrameworkPropertyMetadata(null));

		public object ReadOnlyContent {
			get { return GetValue(ReadOnlyContentProperty); }
			set { SetValue(ReadOnlyContentProperty, value); }
		}

		public EditValueControl() {
			InitializeComponent();
			var binding = new Binding("ReadOnlyContent") {
				Source = this,
			};
			content.SetBinding(ContentProperty, binding);
			DataContextChanged += EditValueControl_DataContextChanged;
			textBox.LostKeyboardFocus += TextBox_LostKeyboardFocus;
			textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
		}

		void EditValueControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
			UninstallHooks(e.OldValue as IEditableValue);
			InstallHooks(e.NewValue as IEditableValue);
		}

		void InstallHooks(IEditableValue ev) {
			if (ev == null)
				return;
			ev.PropertyChanged -= IEditableValue_PropertyChanged;
			ev.PropertyChanged += IEditableValue_PropertyChanged;
		}

		void UninstallHooks(IEditableValue ev) {
			if (ev == null)
				return;
			ev.PropertyChanged -= IEditableValue_PropertyChanged;
		}

		void IEditableValue_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (sender != DataContext) {
				UninstallHooks(sender as IEditableValue);
				return;
			}
			var ev = DataContext as IEditableValue;
			if (ev == null)
				return;

			if (e.PropertyName == "IsEditingValue") {
				if (ev.IsEditingValue)
					StartEditing(ev);
			}
		}

		void TextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
			if (!editing)
				return;
			if (canceled)
				return;

			Debug.Assert(editing);
			var ev = DataContext as IEditableValue;
			if (ev == null)
				return;
			StopEditing(ev);
		}

		void TextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
			if (!editing)
				return;

			if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Escape) {
				CancelEdit();
				e.Handled = true;
				return;
			}
			if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Enter) {
				StopEditing(DataContext as IEditableValue);
				e.Handled = true;
				return;
			}
			if (Keyboard.Modifiers == ModifierKeys.None && (e.Key == Key.Up || e.Key == Key.Down)) {
				StopEditing(DataContext as IEditableValue);
				//TODO: Should also move to next/previous local in the list
				e.Handled = true;
				return;
			}
		}

		void StartEditing(IEditableValue ev) {
			Debug.Assert(!editing);

			editing = true;
			canceled = false;
			content.Visibility = Visibility.Collapsed;
			textBox.Visibility = Visibility.Visible;
			textBox.Text = originalText = ev.GetValueAsText() ?? string.Empty;
			textBox.SelectAll();
			textBox.Focus();
		}
		bool editing;
		string originalText;
		bool canceled;

		void StopEditing(IEditableValue ev) {
			if (ev == null)
				return;
			Debug.Assert(editing);
			editing = false;

			if (!canceled) {
				var newText = textBox.Text;
				if (newText != originalText) {
					string error;
					try {
						error = ev.SetValueAsText(newText);
					}
					catch (Exception ex) {
						error = string.Format(dnSpy_Debugger_Resources.LocalsEditValue_Error_CouldNotWriteNewValue, ex.Message);
					}
					if (!string.IsNullOrEmpty(error))
						Shared.UI.App.MsgBox.Instance.Show(error);
				}
			}

			ev.IsEditingValue = false;
			RestoreControls();
		}

		void CancelEdit() {
			var ev = DataContext as IEditableValue;
			if (ev == null)
				return;
			ev.IsEditingValue = false;
			canceled = true;
			RestoreControls();
		}

		void RestoreControls() {
			content.Visibility = Visibility.Visible;
			textBox.Visibility = Visibility.Collapsed;
			originalText = null;
			editing = false;
		}
	}
}

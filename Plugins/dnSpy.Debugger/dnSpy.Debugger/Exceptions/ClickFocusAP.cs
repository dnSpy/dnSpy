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

using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace dnSpy.Debugger.Exceptions {
	/// <summary>
	/// Gives focus to a target element when an element with the attached property gets clicked.
	/// Can be used to give eg. a <see cref="System.Windows.Controls.TextBox"/> focus when a
	/// <see cref="System.Windows.Controls.Label"/> is clicked.
	/// </summary>
	sealed class ClickFocusAP : DependencyObject {
		public static readonly DependencyProperty TargetProperty = DependencyProperty.RegisterAttached(
			"Target", typeof(UIElement), typeof(ClickFocusAP), new PropertyMetadata(null, TargetPropertyChangedCallback));

		public static void SetTarget(DependencyObject obj, UIElement value) {
			obj.SetValue(TargetProperty, value);
		}

		public static UIElement GetTarget(DependencyObject obj) {
			return (UIElement)obj.GetValue(TargetProperty);
		}

		static void TargetPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var elem = d as UIElement;
			if (elem == null)
				return;
			elem.MouseLeftButtonDown -= UIElement_MouseLeftButtonDown;
			if (e.NewValue != null)
				elem.MouseLeftButtonDown += UIElement_MouseLeftButtonDown;
		}

		static void UIElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			if (e.Handled)
				return;
			var elem = sender as UIElement;
			Debug.Assert(elem != null);
			if (elem == null)
				return;
			var targetElem = GetTarget(elem);
			Debug.Assert(targetElem != null);
			if (targetElem == null)
				return;
			targetElem.Focus();
			e.Handled = true;
		}
	}
}

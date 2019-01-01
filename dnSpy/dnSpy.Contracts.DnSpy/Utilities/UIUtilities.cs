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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Utilities {
	/// <summary>
	/// UI utilities
	/// </summary>
	static class UIUtilities {
		/// <summary>
		/// Gets the parent
		/// </summary>
		/// <param name="depo">Object</param>
		/// <returns></returns>
		public static DependencyObject GetParent(DependencyObject depo) {
			if (depo is Visual || depo is Visual3D)
				return VisualTreeHelper.GetParent(depo);
			else if (depo is FrameworkContentElement)
				return ((FrameworkContentElement)depo).Parent;
			return null;
		}

		static T GetItem<T>(DependencyObject view, object o) where T : class {
			var depo = o as DependencyObject;
			while (depo != null && !(depo is T) && depo != view)
				depo = GetParent(depo);
			return depo as T;
		}

		/// <summary>
		/// Checks if it's left double click
		/// </summary>
		/// <typeparam name="T">Type of element</typeparam>
		/// <param name="view">View</param>
		/// <param name="e">Event args</param>
		/// <returns></returns>
		public static bool IsLeftDoubleClick<T>(DependencyObject view, MouseButtonEventArgs e) where T : class {
			if (MouseButton.Left != e.ChangedButton)
				return false;
			return GetItem<T>(view, e.OriginalSource) != null;
		}

		/// <summary>
		/// Escapes text for <see cref="MenuItem"/> headers
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static string EscapeMenuItemHeader(string s) => NameUtilities.CleanName(s).Replace("_", "__");

		/// <summary>
		/// Truncates the string after <paramref name="length"/> characters and adds an elipsis at the end.
		/// </summary>
		/// <param name="s"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		public static string TruncateWithElipsis(string s, int length = 50) => (s.Length > length ? s.Substring(0, length) + "..." : s);

		/// <summary>
		/// Gives a <see cref="Selector"/> focus
		/// </summary>
		/// <param name="selector"></param>
		public static void FocusSelector(Selector selector) {
			if (!selector.IsVisible)
				selector.IsVisibleChanged += selector_IsVisibleChanged;
			else
				FocusSelectorInternal(selector);
		}

		static void selector_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			var selector = (Selector)sender;
			selector.IsVisibleChanged -= selector_IsVisibleChanged;
			FocusSelectorInternal(selector);
		}

		static void FocusSelectorInternal(Selector selector) {
			bool focused = false;
			var item = selector.SelectedItem as IInputElement;
			if (item == null && selector.SelectedItem != null)
				item = selector.ItemContainerGenerator.ContainerFromItem(selector.SelectedItem) as IInputElement;
			if (item != null)
				focused = item.Focus();
			if (!focused) {
				selector.Focus();
				// Needed by eg. locals window. We have to wait until the item is visible.
				if (item is UIElement ui && !ui.IsVisible)
					ui.IsVisibleChanged += UIElement_IsVisibleChanged;
			}
		}

		static void UIElement_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			var ui = (UIElement)sender;
			ui.IsVisibleChanged -= UIElement_IsVisibleChanged;
			ui.Focus();
		}

		/// <summary>
		/// Focuses <paramref name="element"/>
		/// </summary>
		/// <param name="element">Element to focus</param>
		/// <param name="calledAfterFocus">Delegate that gets called once the element has gotten focus. Can be null.</param>
		public static void Focus(IInputElement element, Action calledAfterFocus = null) {
			var uiElem = element as UIElement;
			var fwkElem = element as FrameworkElement;
			if (uiElem == null || (fwkElem != null && fwkElem.IsLoaded && fwkElem.IsVisible) || (fwkElem == null && uiElem.IsVisible)) {
				element.Focus();
				calledAfterFocus?.Invoke();
				return;
			}

			new FocusHelper(uiElem, calledAfterFocus);
		}

		sealed class FocusHelper {
			readonly Action calledAfterFocus;
			readonly UIElement element;

			public FocusHelper(UIElement element, Action calledAfterFocus) {
				this.element = element;
				var fwkElem = element as FrameworkElement;
				this.calledAfterFocus = calledAfterFocus;
				if (fwkElem == null || fwkElem.IsLoaded)
					element.IsVisibleChanged += UIElement_IsVisibleChanged;
				else
					fwkElem.Loaded += FrameworkElement_Loaded;
			}

			void UIElement_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
				if (sender != element)
					return;
				element.IsVisibleChanged -= UIElement_IsVisibleChanged;
				Focus();
			}

			void FrameworkElement_Loaded(object sender, RoutedEventArgs e) {
				if (sender != element)
					return;
				var fwkElem = (FrameworkElement)element;
				fwkElem.Loaded -= FrameworkElement_Loaded;
				Focus();
			}

			void Focus() {
				element.Focus();
				calledAfterFocus?.Invoke();
			}
		}
	}
}

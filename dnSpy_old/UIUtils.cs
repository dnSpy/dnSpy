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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using dnSpy.Shared.UI.Highlighting;

namespace dnSpy {
	public static class UIUtils {
		public static IEnumerable<DependencyObject> GetChildren(DependencyObject depo) {
			if (depo == null)
				yield break;
			int count = VisualTreeHelper.GetChildrenCount(depo);
			for (int i = 0; i < count; i++)
				yield return VisualTreeHelper.GetChild(depo, i);
		}

		public static DependencyObject GetParent(DependencyObject depo) {
			if (depo is Visual || depo is Visual3D)
				return VisualTreeHelper.GetParent(depo);
			else if (depo is FrameworkContentElement)
				return ((FrameworkContentElement)depo).Parent;
			return null;
		}

		public static T GetItem<T>(DependencyObject view, object o) where T : class {
			var depo = o as DependencyObject;
			while (depo != null && !(depo is T) && depo != view)
				depo = GetParent(depo);
			return depo as T;
		}

		public static bool IsLeftDoubleClick<T>(DependencyObject view, MouseButtonEventArgs e) where T : class {
			if (MouseButton.Left != e.ChangedButton)
				return false;
			return GetItem<T>(view, e.OriginalSource) != null;
		}

		public static string EscapeMenuItemHeader(string s) {
			return NameUtils.CleanName(s).Replace("_", "__");
		}

		public static bool HasSelectedChildrenFocus(ListBox listBox) {
			if (listBox == null)
				return false;

			foreach (var item in listBox.SelectedItems) {
				var elem = listBox.ItemContainerGenerator.ContainerFromItem(item) as UIElement;
				if (elem == null)
					elem = item as UIElement;
				if (elem == null)
					continue;
				if (elem.IsFocused || elem.IsKeyboardFocusWithin)
					return true;
			}
			return false;
		}

		public static void ScrollSelectAndSetFocus(ListBox listBox, object obj) {
			listBox.SelectedItem = obj;
			listBox.ScrollIntoView(obj);
			SetFocus(listBox, obj, DispatcherPriority.Normal);
		}

		public static void SetFocus(Selector selector, object obj, DispatcherPriority prio) {
			selector.Dispatcher.BeginInvoke(prio, new Action(delegate {
				if (selector.SelectedItem == obj) {
					var item = selector.ItemContainerGenerator.ContainerFromItem(obj) as UIElement;
					if (item != null)
						item.Focus();
				}
			}));
		}

		public static void AddCommandBinding(this UIElement elem, ICommand cmd, ICommand realCmd) {
			elem.CommandBindings.Add(new CommandBinding(cmd, (s, e) => realCmd.Execute(e.Parameter), (s, e) => e.CanExecute = realCmd.CanExecute(e.Parameter)));
		}

		public static void AddCommandBinding(this UIElement elem, ICommand cmd, ModifierKeys modifiers, Key key) {
			elem.CommandBindings.Add(new CommandBinding(cmd));
			elem.InputBindings.Add(new KeyBinding(cmd, key, modifiers));
		}

		public static void Focus(UIElement elem) {
			if (!elem.IsVisible)
				elem.IsVisibleChanged += UIElement_IsVisibleChanged;
			else
				elem.Focus();
		}

		static void UIElement_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			var elem = (UIElement)sender;
			elem.IsVisibleChanged -= UIElement_IsVisibleChanged;
			elem.Focus();
		}

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
			var item = selector.SelectedItem as UIElement;
			if (item == null && selector.SelectedItem != null)
				item = selector.ItemContainerGenerator.ContainerFromItem(selector.SelectedItem) as UIElement;
			if (item != null)
				focused = item.Focus();
			if (!focused)
				selector.Focus();
		}
	}
}

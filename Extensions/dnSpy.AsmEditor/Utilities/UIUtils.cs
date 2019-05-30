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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace dnSpy.AsmEditor.Utilities {
	static class UIUtils {
		public static IEnumerable<DependencyObject> GetChildren(DependencyObject? depo) {
			if (depo is null)
				yield break;
			int count = VisualTreeHelper.GetChildrenCount(depo);
			for (int i = 0; i < count; i++)
				yield return VisualTreeHelper.GetChild(depo, i);
		}

		public static bool HasSelectedChildrenFocus(ListBox? listBox) {
			if (listBox is null)
				return false;

			foreach (var item in listBox.SelectedItems) {
				var elem = listBox.ItemContainerGenerator.ContainerFromItem(item) as UIElement;
				if (elem is null)
					elem = item as UIElement;
				if (elem is null)
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

		static void SetFocus(Selector selector, object obj, DispatcherPriority prio) => selector.Dispatcher.BeginInvoke(prio, new Action(() => {
			if (selector.SelectedItem == obj) {
				if (selector.ItemContainerGenerator.ContainerFromItem(obj) is IInputElement item)
					item.Focus();
			}
		}));
	}
}

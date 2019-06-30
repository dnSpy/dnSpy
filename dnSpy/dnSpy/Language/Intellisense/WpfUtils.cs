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
using System.Windows.Media;

namespace dnSpy.Language.Intellisense {
	static class WpfUtils {
		public static void ScrollSelectedItemIntoView(ListBox lb, bool center) {
			var item = lb.SelectedItem;
			if (item is null)
				return;
			lb.ScrollIntoView(item);
			var lbItem = lb.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
			if (lbItem is null)
				return;
			lbItem.Focus();

			if (!center)
				return;
			var scrollViewer = TryGetScrollViewer(lb);
			if (!(scrollViewer is null)) {
				int index = lb.Items.IndexOf(item);
				int itemsPerPage = (int)Math.Max(1, Math.Floor(scrollViewer.ViewportHeight));
				scrollViewer.ScrollToVerticalOffset(Math.Max(0, index - itemsPerPage / 2));
			}
		}

		static T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject {
			int childrenCount = VisualTreeHelper.GetChildrenCount(obj);
			for (int i = 0; i < childrenCount; i++) {
				var child = VisualTreeHelper.GetChild(obj, i);
				if (child is T res)
					return res;

				var res2 = FindVisualChild<T>(child);
				if (!(res2 is null))
					return res2;
			}

			return null;
		}

		public static ScrollViewer? TryGetScrollViewer(ListBox lb)=> FindVisualChild<ScrollViewer>(lb);

		public static int GetItemsPerPage(ListBox lb, int defaultValue) {
			var scrollViewer = TryGetScrollViewer(lb);
			if (scrollViewer is null)
				return defaultValue;
			return (int)Math.Max(1, Math.Floor(scrollViewer.ViewportHeight));
		}

		public static void Scroll(ListBox lb, int lines) {
			var scrollViewer = TryGetScrollViewer(lb);
			if (scrollViewer is null)
				return;
			if (lines > 0) {
				while (lines-- > 0)
					scrollViewer.LineUp();
			}
			else {
				while (lines++ < 0)
					scrollViewer.LineDown();
			}
		}

		public static void ScrollToTop(ListBox lb) {
			var scrollViewer = TryGetScrollViewer(lb);
			if (scrollViewer is null)
				return;
			scrollViewer.ScrollToTop();
			if (lb.Items.Count != 0)
				lb.SelectedItem = lb.Items[0];
		}

		public static void ScrollToBottom(ListBox lb) {
			var scrollViewer = TryGetScrollViewer(lb);
			if (scrollViewer is null)
				return;
			scrollViewer.ScrollToBottom();
			if (lb.Items.Count != 0)
				lb.SelectedItem = lb.Items[lb.Items.Count - 1];
		}
	}
}

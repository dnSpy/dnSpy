/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace dnSpy.Contracts.Utilities {
	/// <summary>
	/// Helper for adding sorting to ListViews
	/// </summary>
	public static class ListViewGridSortableBehavior {
		/// <summary>
		/// Enables sorting for a given ListView
		/// </summary>
		public static void EnableSort(ListView listView, Action<SortDescription> sortVM) {
			// ================================================================
			// global variables
			Action onLoaded = null;
			RoutedEventHandler onHeaderClick = null;
			Action doSort = null;

			ListSortDirection sortDirection = ListSortDirection.Ascending;
			GridViewColumnHeader sortColumn = null;

			// ================================================================
			// onLoad
			onLoaded = () => {
				var gridView = listView.View as GridView;

				var headers = Query<GridViewColumnHeader>(listView);
				foreach (var header in headers) 
					header.Click += onHeaderClick;
			};

			// ================================================================
			// onHeaderClick
			onHeaderClick = (s, e) => {
				var headerClicked = e.OriginalSource as GridViewColumnHeader;

				if (headerClicked == null) { return; }
				if (headerClicked.Role == GridViewColumnHeaderRole.Padding) { return; }

				// Remove arrow from previously sorted header
				if (sortColumn != null) {
					var adornerLayer = AdornerLayer.GetAdornerLayer(sortColumn);
					try { adornerLayer.Remove((adornerLayer.GetAdorners(sortColumn))[0]); }
					catch { }
				}

				if (sortColumn == headerClicked) {
					// Toggle sorting direction
					sortDirection = sortDirection == ListSortDirection.Ascending ?
													 ListSortDirection.Descending :
													 ListSortDirection.Ascending;
				}
				else {
					sortColumn = headerClicked;
					sortDirection = ListSortDirection.Ascending;
				}

				doSort();
			};

			// ================================================================
			// doSort
			doSort = () => {

				var sortingAdorner = new SortingAdorner(sortColumn, sortDirection);
				AdornerLayer.GetAdornerLayer(sortColumn).Add(sortingAdorner);				

				// collect possible property names
				string sortPropertyName = null;

				// get property names
				Binding b = sortColumn.Column.DisplayMemberBinding as Binding;
				if (b != null) {
					// DisplayMemberBinding is used
					sortPropertyName = b.Path.Path;
				}
				else {
					// try extract binding expression from contentPresenter
					var template = sortColumn.Column.CellTemplate.LoadContent();
					var contentPresenter = template as FrameworkElement;

					if (contentPresenter != null) {
						var bindingExpr = contentPresenter.GetBindingExpression(ContentPresenter.ContentProperty);

						if (bindingExpr != null) 
							sortPropertyName = bindingExpr.ParentBinding.Path.Path;
					}
				}

				var sortDesc = new SortDescription(sortPropertyName, sortDirection);
				sortVM(sortDesc);
			};


			// ================================================================
			// run onLoad (only once)
			if (listView.IsLoaded) 
				onLoaded();			
			else {
				RoutedEventHandler evt = null;
				evt = (s, e) => {
					listView.Loaded -= evt;
					onLoaded();
				};
				listView.Loaded += evt;
			}			
		}		

		private static IEnumerable<T> Query<T>(DependencyObject @this, Func<T, bool> predicate = null) where T : DependencyObject {
			if (predicate == null) predicate = x => true;

			var children = GetChildren(@this);
			foreach (var child in children) {
				var typedChild = child as T;
				if (typedChild != null && predicate(typedChild))
					yield return (T)child;
			}

			foreach (var child in children) {
				foreach (var descendant in Query<T>(child, predicate)) {
					yield return descendant;
				}
			}
		}

		private static IEnumerable<DependencyObject> GetChildren(DependencyObject @this) {
			var childrenCount = VisualTreeHelper.GetChildrenCount(@this);
			for (var i = 0; i < childrenCount; ++i) {
				yield return VisualTreeHelper.GetChild(@this, i);
			}
		}
	}
}

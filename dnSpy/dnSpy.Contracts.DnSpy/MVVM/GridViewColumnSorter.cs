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
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace dnSpy.Contracts.MVVM {
	/// <summary>
	/// Adds column sorting code to a <see cref="GridView"/>. The VM gets notified when a column is clicked
	/// and it sorts its list.
	/// </summary>
	public sealed class GridViewColumnSorter {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static readonly DependencyProperty ColumnProviderProperty = DependencyProperty.RegisterAttached(
			"ColumnProvider", typeof(IGridViewColumnDescsProvider), typeof(GridViewColumnSorter), new UIPropertyMetadata(null, ColumnProviderPropertyChangedCallback));
		public static void SetColumnProvider(FrameworkElement element, IGridViewColumnDescsProvider value) => element.SetValue(ColumnProviderProperty, value);
		public static IGridViewColumnDescsProvider GetColumnProvider(FrameworkElement element) => (IGridViewColumnDescsProvider)element.GetValue(ColumnProviderProperty);

		public static readonly DependencyProperty GridViewSortDirectionProperty = DependencyProperty.RegisterAttached(
			"GridViewSortDirection", typeof(GridViewSortDirection), typeof(GridViewColumnSorter), new UIPropertyMetadata(GridViewSortDirection.Default));
		public static void SetGridViewSortDirection(GridViewColumn element, GridViewSortDirection value) => element.SetValue(GridViewSortDirectionProperty, value);
		public static GridViewSortDirection GetGridViewSortDirection(GridViewColumn element) => (GridViewSortDirection)element.GetValue(GridViewSortDirectionProperty);

		public static readonly DependencyProperty IdProperty = DependencyProperty.RegisterAttached(
			"Id", typeof(int), typeof(GridViewColumnSorter), new UIPropertyMetadata(int.MinValue));
		public static void SetId(GridViewColumn element, int value) => element.SetValue(IdProperty, value);
		public static int GetId(GridViewColumn element) => (int)element.GetValue(IdProperty);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		static readonly DependencyProperty GridViewColumnSorterInstanceProperty = DependencyProperty.RegisterAttached(
			"GridViewColumnSorterInstance", typeof(GridViewColumnSorter), typeof(GridViewColumnSorter), new UIPropertyMetadata(null));

		readonly ListView listView;
		readonly Dictionary<GridViewColumn, GridViewColumnDesc> toDesc;
		IGridViewColumnDescsProvider? descsProvider;

		GridViewColumnSorter(ListView listView) {
			this.listView = listView;
			toDesc = new Dictionary<GridViewColumn, GridViewColumnDesc>();
		}

		static GridViewColumnSorter GetInstance(ListView listView) {
			var inst = (GridViewColumnSorter)listView.GetValue(GridViewColumnSorterInstanceProperty);
			if (inst is null) {
				inst = new GridViewColumnSorter(listView);
				listView.SetValue(GridViewColumnSorterInstanceProperty, inst);
			}
			return inst;
		}

		static void ColumnProviderPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var sorter = GetInstance((ListView)d);
			sorter.Initialize((IGridViewColumnDescsProvider)e.NewValue);
		}

		void Initialize(IGridViewColumnDescsProvider descsProvider) {
			if (descsProvider is null)
				return;
			Debug2.Assert(this.descsProvider is null);
			this.descsProvider = descsProvider;
			descsProvider.Descs.SortedColumnChanged += OnSortedColumnChanged;
			listView.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(ListView_Click));

			var gridView = (GridView)listView.View;
			gridView.Columns.CollectionChanged += GridView_Columns_CollectionChanged;

			var cols = gridView.Columns;
			var idToDesc = new Dictionary<int, GridViewColumnDesc>();
			foreach (var desc in descsProvider.Descs.Columns)
				idToDesc.Add(desc.Id, desc);
			foreach (var col in cols) {
				int colId = GetId(col);
				if (!idToDesc.TryGetValue(colId, out var desc))
					throw new InvalidOperationException("Missing GridViewColumn Id");
				toDesc.Add(col, desc);
				col.Header = desc.Name;
			}

			UpdateColumns();
		}

		void GridView_Columns_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
			Debug2.Assert(!(descsProvider is null));
			var gridView = (GridView)listView.View;
			var columns = gridView.Columns;
			Debug.Assert(columns.Count == descsProvider.Descs.Columns.Length);
			descsProvider.Descs.Columns = columns.Select(a => toDesc[a]).ToArray();
		}

		void OnSortedColumnChanged(object? sender, EventArgs e) {
			Debug.Assert(descsProvider?.Descs == sender);
			UpdateColumns();
		}

		void ListView_Click(object? sender, RoutedEventArgs e) {
			var column = (e.OriginalSource as GridViewColumnHeader)?.Column;
			if (column is null || !toDesc.TryGetValue(column, out var desc))
				return;
			e.Handled = true;
			UpdateSortedColumn(desc);
		}

		void UpdateSortedColumn(GridViewColumnDesc desc) {
			Debug2.Assert(!(descsProvider is null));
			if (!desc.CanBeSorted)
				return;
			var sortedColumn = descsProvider.Descs.SortedColumn;
			var sortDir = GetGridViewSortDirection(sortedColumn.Column == desc ? sortedColumn.Direction : GridViewSortDirection.Default);
			descsProvider.Descs.SortedColumn = new GridViewSortedColumn(sortDir == GridViewSortDirection.Default ? null : desc, sortDir);
		}

		static GridViewSortDirection GetGridViewSortDirection(GridViewSortDirection direction) {
			switch (direction) {
			case GridViewSortDirection.Default:
				return GridViewSortDirection.Ascending;

			case GridViewSortDirection.Ascending:
				return GridViewSortDirection.Descending;

			case GridViewSortDirection.Descending:
				return GridViewSortDirection.Default;

			default:
				throw new InvalidOperationException();
			}
		}

		void UpdateColumns() {
			Debug2.Assert(!(descsProvider is null));

			var gridView = (GridView)listView.View;
			var cols = gridView.Columns;
			var sortedColumn = descsProvider.Descs.SortedColumn;
			foreach (var col in cols) {
				var desc = toDesc[col];
				var dir = sortedColumn.Column == desc ? sortedColumn.Direction : GridViewSortDirection.Default;
				SetGridViewSortDirection(col, dir);
			}
		}
	}
}

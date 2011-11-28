// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ICSharpCode.ILSpy.Controls
{
	/// <summary>
	/// Allows to automatically sort a grid view.
	/// </summary>
	public class SortableGridViewColumn : GridViewColumn
	{
		// This class was copied from ICSharpCode.Core.Presentation.
		
		static readonly ComponentResourceKey headerTemplateKey = new ComponentResourceKey(typeof(SortableGridViewColumn), "ColumnHeaderTemplate");
		
		public SortableGridViewColumn()
		{
			this.SetValueToExtension(HeaderTemplateProperty, new DynamicResourceExtension(headerTemplateKey));
		}
		
		string sortBy;
		
		public string SortBy {
			get { return sortBy; }
			set {
				if (sortBy != value) {
					sortBy = value;
					OnPropertyChanged(new PropertyChangedEventArgs("SortBy"));
				}
			}
		}
		
		#region SortDirection property
		public static readonly DependencyProperty SortDirectionProperty =
			DependencyProperty.RegisterAttached("SortDirection", typeof(ColumnSortDirection), typeof(SortableGridViewColumn),
			                                    new FrameworkPropertyMetadata(ColumnSortDirection.None, OnSortDirectionChanged));
		
		public ColumnSortDirection SortDirection {
			get { return (ColumnSortDirection)GetValue(SortDirectionProperty); }
			set { SetValue(SortDirectionProperty, value); }
		}
		
		public static ColumnSortDirection GetSortDirection(ListView listView)
		{
			return (ColumnSortDirection)listView.GetValue(SortDirectionProperty);
		}
		
		public static void SetSortDirection(ListView listView, ColumnSortDirection value)
		{
			listView.SetValue(SortDirectionProperty, value);
		}
		
		static void OnSortDirectionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			ListView grid = sender as ListView;
			if (grid != null) {
				SortableGridViewColumn col = GetCurrentSortColumn(grid);
				if (col != null)
					col.SortDirection = (ColumnSortDirection)args.NewValue;
				Sort(grid);
			}
		}
		#endregion
		
		#region CurrentSortColumn property
		public static readonly DependencyProperty CurrentSortColumnProperty =
			DependencyProperty.RegisterAttached("CurrentSortColumn", typeof(SortableGridViewColumn), typeof(SortableGridViewColumn),
			                                    new FrameworkPropertyMetadata(OnCurrentSortColumnChanged));
		
		public static SortableGridViewColumn GetCurrentSortColumn(ListView listView)
		{
			return (SortableGridViewColumn)listView.GetValue(CurrentSortColumnProperty);
		}
		
		public static void SetCurrentSortColumn(ListView listView, SortableGridViewColumn value)
		{
			listView.SetValue(CurrentSortColumnProperty, value);
		}
		
		static void OnCurrentSortColumnChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			ListView grid = sender as ListView;
			if (grid != null) {
				SortableGridViewColumn oldColumn = (SortableGridViewColumn)args.OldValue;
				if (oldColumn != null)
					oldColumn.SortDirection = ColumnSortDirection.None;
				SortableGridViewColumn newColumn = (SortableGridViewColumn)args.NewValue;
				if (newColumn != null) {
					newColumn.SortDirection = GetSortDirection(grid);
				}
				Sort(grid);
			}
		}
		#endregion
		
		#region SortMode property
		public static readonly DependencyProperty SortModeProperty =
			DependencyProperty.RegisterAttached("SortMode", typeof(ListViewSortMode), typeof(SortableGridViewColumn),
			                                    new FrameworkPropertyMetadata(ListViewSortMode.None, OnSortModeChanged));
		
		public static ListViewSortMode GetSortMode(ListView listView)
		{
			return (ListViewSortMode)listView.GetValue(SortModeProperty);
		}
		
		public static void SetSortMode(ListView listView, ListViewSortMode value)
		{
			listView.SetValue(SortModeProperty, value);
		}
		
		static void OnSortModeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			ListView grid = sender as ListView;
			if (grid != null) {
				if ((ListViewSortMode)args.NewValue != ListViewSortMode.None)
					grid.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(GridViewColumnHeaderClickHandler));
				else
					grid.RemoveHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(GridViewColumnHeaderClickHandler));
			}
		}
		
		static void GridViewColumnHeaderClickHandler(object sender, RoutedEventArgs e)
		{
			ListView grid = sender as ListView;
			GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
			if (grid != null && headerClicked != null && headerClicked.Role != GridViewColumnHeaderRole.Padding) {
				if (headerClicked.Column == GetCurrentSortColumn(grid)) {
					if (GetSortDirection(grid) == ColumnSortDirection.Ascending)
						SetSortDirection(grid, ColumnSortDirection.Descending);
					else
						SetSortDirection(grid, ColumnSortDirection.Ascending);
				} else {
					SetSortDirection(grid, ColumnSortDirection.Ascending);
					SetCurrentSortColumn(grid, headerClicked.Column as SortableGridViewColumn);
				}
			}
		}
		#endregion
		
		static void Sort(ListView grid)
		{
			ColumnSortDirection currentDirection = GetSortDirection(grid);
			SortableGridViewColumn column = GetCurrentSortColumn(grid);
			if (column != null && GetSortMode(grid) == ListViewSortMode.Automatic && currentDirection != ColumnSortDirection.None) {
				ICollectionView dataView = CollectionViewSource.GetDefaultView(grid.ItemsSource);
				
				string sortBy = column.SortBy;
				if (sortBy == null) {
					Binding binding = column.DisplayMemberBinding as Binding;
					if (binding != null && binding.Path != null) {
						sortBy = binding.Path.Path;
					}
				}
				
				dataView.SortDescriptions.Clear();
				if (sortBy != null) {
					ListSortDirection direction;
					if (currentDirection == ColumnSortDirection.Descending)
						direction = ListSortDirection.Descending;
					else
						direction = ListSortDirection.Ascending;
					dataView.SortDescriptions.Add(new SortDescription(sortBy, direction));
				}
				dataView.Refresh();
			}
		}
	}
	
	public enum ColumnSortDirection
	{
		None,
		Ascending,
		Descending
	}
	
	public enum ListViewSortMode
	{
		/// <summary>
		/// Disable automatic sorting when sortable columns are clicked.
		/// </summary>
		None,
		/// <summary>
		/// Fully automatic sorting.
		/// </summary>
		Automatic,
		/// <summary>
		/// Automatically update SortDirection and CurrentSortColumn properties,
		/// but do not actually sort the data.
		/// </summary>
		HalfAutomatic
	}
	
	sealed class ColumnSortDirectionToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Equals(value, parameter) ? Visibility.Visible : Visibility.Collapsed;
		}
		
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}

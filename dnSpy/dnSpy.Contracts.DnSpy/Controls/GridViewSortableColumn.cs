using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using dnSpy.Contracts.MVVM;

namespace dnSpy.Contracts.Controls {
	/// <summary>
	/// GridViewColumn with sort behaviour
	/// </summary>
	public class GridViewSortableColumn : GridViewColumn {
		#region Dependency properties

		/// <summary>
		/// Sort command to be specified for parent ListView
		/// </summary>
		public static readonly DependencyProperty SortCommandProperty;
		/// <summary>
		/// Item model property displayed in this column
		/// </summary>
		public static readonly DependencyProperty SortByProperty;
		/// <summary>
		/// Sort direction for this column
		/// </summary>
		public static readonly DependencyProperty SortDirectionProperty;

		static GridViewSortableColumn() {
			// Attached to ListView
			SortCommandProperty = DependencyProperty.RegisterAttached(
				"SortCommand",
				typeof(ICommand),
				typeof(GridViewSortableColumn),
				new PropertyMetadata(SortCommandPropertyChanged));
			SortByProperty = DependencyProperty.Register(
				nameof(SortBy),
				typeof(string),
				typeof(GridViewSortableColumn),
				null);
			SortDirectionProperty = DependencyProperty.Register(
				nameof(SortDirection),
				typeof(SortDirection?),
				typeof(GridViewSortableColumn),
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure, null));
		}

		/// <summary>
		/// Getter for SortCommand attachable property
		/// </summary>
		public static ICommand GetSortCommand(DependencyObject element) {
			return (ICommand)element.GetValue(SortCommandProperty);
		}

		/// <summary>
		/// Setter for SortCommand attachable property
		/// </summary>
		public static void SetSortCommand(DependencyObject element, ICommand command) {
			element.SetValue(SortCommandProperty, command);
		}

		/// <summary>
		/// Name of the item model property displayed in this column
		/// </summary>
		public string SortBy {
			get { return (string)GetValue(SortByProperty); }
			set { SetValue(SortByProperty, value); }
		}

		/// <summary>
		/// Sort direction for this column
		/// </summary>
		public SortDirection? SortDirection {
			get { return (SortDirection?)GetValue(SortDirectionProperty); }
			set { SetValue(SortDirectionProperty, value); }

		}

		static void SortCommandPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
			var listView = o as ListView;
			if (listView == null)
				throw new InvalidOperationException("SortCommand attached property can be set on ListView only!");

			listView.RemoveHandler(GridViewColumnHeader.ClickEvent, (RoutedEventHandler)ColumnHeaderClickHandler);
			if (e.NewValue != null)
				listView.AddHandler(GridViewColumnHeader.ClickEvent, (RoutedEventHandler)ColumnHeaderClickHandler);
		}

		#endregion Dependency properties

		static void ColumnHeaderClickHandler(object sender, RoutedEventArgs e) {
			var sortableColumn = (e.OriginalSource as GridViewColumnHeader)?.Column as GridViewSortableColumn;
			if (sortableColumn != null) {
				var listView = (ListView) sender;
				sortableColumn.DoSort(listView);
			}
		}

		const string HeaderTemplateResourceKey = "SortableColumnHeaderTemplate";
		const string HeaderStyleResourceKey = "SortableColumnHeaderStyle";

		/// <summary>
		/// 
		/// </summary>
		public GridViewSortableColumn() {
			HeaderTemplate = (DataTemplate) Application.Current.FindResource(HeaderTemplateResourceKey);
			HeaderContainerStyle = (Style) Application.Current.FindResource(HeaderStyleResourceKey);
		}

		/// <summary>
		/// Executes SortCommand attached to the specified ListView
		/// </summary>
		/// <param name="listView">Parent ListView object</param>
		void DoSort(ListView listView) {
			var gridView = (GridView)listView.View;
			foreach (var column in gridView.Columns.OfType<GridViewSortableColumn>()) {
				if (column != this)
					column.SortDirection = null;
			}

			var sortCommand = GetSortCommand(listView);
			if (sortCommand != null) {
				SortDirection = (SortDirection ?? MVVM.SortDirection.Descending) == MVVM.SortDirection.Descending
					? MVVM.SortDirection.Ascending
					: MVVM.SortDirection.Descending;

				sortCommand.Execute(new SortInfo(SortBy, SortDirection.Value));
			}
		}
	}
}

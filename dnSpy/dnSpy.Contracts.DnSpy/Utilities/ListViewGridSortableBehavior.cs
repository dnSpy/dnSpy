using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using dnSpy.Contracts.Controls;

namespace dnSpy.Contracts.Utilities {
	/// <summary>
	/// Helper for adding sorting to ListViews
	/// </summary>
	public class ListViewGridSortableBehavior {

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

				var headers = listView.Query<GridViewColumnHeader>();
				foreach (var header in headers) {
					header.Click += onHeaderClick;
				}
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

						if (bindingExpr != null) {
							sortPropertyName = bindingExpr.ParentBinding.Path.Path;
						}
					}
				}

				var sortDesc = new SortDescription(sortPropertyName, sortDirection);
				sortVM(sortDesc);
			};


			// ================================================================
			// run onLoad (only once)
			if (listView.IsLoaded) {
				onLoaded();
			}
			else {
				RoutedEventHandler evt = null;
				evt = (s, e) => {
					listView.Loaded -= evt;
					onLoaded();
				};
				listView.Loaded += evt;
			}			
		}

		private class ComparerByBinding : IComparer {

			private readonly BindingExpression bindingExpression;

			public ComparerByBinding(BindingExpression bindingExpression) {
				this.bindingExpression = bindingExpression;
			}

			protected virtual object GetValue(object source) {
				var dependencyObject = source as DependencyObject;
				if (dependencyObject == null) {
					return source?.GetType().GetProperty(bindingExpression.ParentBinding.Path.Path).GetGetMethod().Invoke(source, null);
				} else {
					return dependencyObject.GetValue(bindingExpression.TargetProperty);
				}
			}

			public int Compare(object x, object y) {
				object cx = GetValue(x);
				object cy = GetValue(y);

				return Comparer.Default.Compare(cx, cy);
			}
		}

		private class ComparerByConverter : ComparerByBinding {

			private readonly IValueConverter converter;
			private readonly Type converterTargeType;
			private readonly object converterParameter;
			private readonly CultureInfo converterCulture;

			public ComparerByConverter(BindingExpression bindingExpression) 
				: this(bindingExpression, bindingExpression.ParentBinding.Converter, bindingExpression.TargetProperty.PropertyType, bindingExpression.ParentBinding.ConverterParameter, bindingExpression.ParentBinding.ConverterCulture)
			{
			}

			public ComparerByConverter(BindingExpression bindingExpression, IValueConverter converter, Type converterTargeType, object converterParameter, CultureInfo converterCulture) 
			: base(bindingExpression) {

				this.converter = converter;
				this.converterTargeType = converterTargeType;
				this.converterParameter = converterParameter;
				this.converterCulture = converterCulture;
			}

			protected override object GetValue(object source) {
				source = base.GetValue(source);
				var converted = converter.Convert(source, converterTargeType, converterParameter, converterCulture);

				var textBlock = converted as TextBlock;
				if (textBlock != null) {
					return textBlock.Text;
				}

				return converted;
			}
		}
	}
}

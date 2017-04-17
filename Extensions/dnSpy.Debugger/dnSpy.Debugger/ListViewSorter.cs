using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace dnSpy.Debugger {
	class ListViewSorter<T> : IComparer where T : class {
		ListView listView;
		BaseVMComparer<T> comparer;

		ListSortDirection currentSortDirection;
		Dictionary<string, ListSortDirection> lastSortDirections;

		public static ListViewSorter<T> Create(ListView listView, BaseVMComparer<T> vmComparer) {
			return new ListViewSorter<T> {
				listView = listView,
				comparer = vmComparer,
				lastSortDirections = new Dictionary<string, ListSortDirection>()
			};
		}

		protected ListViewSorter() { }

		public void SortBy(string propertyName) {
			if (string.IsNullOrEmpty(propertyName))
				return;

			comparer.CurrentProperty = propertyName;
			currentSortDirection = GetSortDirection(propertyName);

			var view = (ListCollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);
			view.CustomSort = this;
			view.Refresh();
		}

		ListSortDirection GetSortDirection(string propertyName) {
			ListSortDirection sortDirection;
			if (lastSortDirections.TryGetValue(propertyName, out sortDirection)) {
				sortDirection = (sortDirection == ListSortDirection.Ascending)
					? ListSortDirection.Descending
					: ListSortDirection.Ascending;
			}
			else {
				sortDirection = ListSortDirection.Ascending;
			}

			lastSortDirections[propertyName] = sortDirection;
			return sortDirection;
		}

		int IComparer.Compare(object x, object y) {
			var result = comparer.Compare(x, y);
			return currentSortDirection == ListSortDirection.Ascending ? result : -result;
		}
	}
}

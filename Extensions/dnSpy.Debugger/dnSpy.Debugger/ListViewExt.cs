using System.Windows;
using System.Windows.Controls;

namespace dnSpy.Debugger {
	public delegate void ColumnHeaderClickEventHandler(ListView listView, string boundPropertyName);

	public static class ListViewExt {
		#region Attached properties

		public static readonly DependencyProperty ModelPropertyProperty;

		static ListViewExt() {
			ModelPropertyProperty = DependencyProperty.RegisterAttached(
				"ModelProperty",
				typeof(string),
				typeof(ListViewExt));
		}

		public static void SetModelProperty(DependencyObject element, string value) {
			element?.SetValue(ModelPropertyProperty, value);
		}

		public static string GetModelProperty(DependencyObject element) {
			return element?.GetValue(ModelPropertyProperty) as string;
		}

		#endregion Attached properties

		public static void AddColumnHeaderClickHandler(this ListView listView, ColumnHeaderClickEventHandler handler) {
			if (listView == null || handler == null)
				return;

			listView.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler((s, e) => {
				var column = (e.OriginalSource as GridViewColumnHeader)?.Column;
				if (column != null) {
					var propertyName = ListViewExt.GetModelProperty(column);

					if (!string.IsNullOrEmpty(propertyName))
						handler(s as ListView, propertyName);
				}
			}));
		}
	}
}

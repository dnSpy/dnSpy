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

using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.MVVM;

namespace dnSpy.Debugger.Dialogs {
	/// <summary>
	/// Interaction logic for AttachProcessDlg.xaml
	/// </summary>
	public partial class AttachProcessDlg : WindowBase {
		public AttachProcessDlg() {
			InitializeComponent();
			DataContextChanged += (s, e) => {
				var data = DataContext as AttachProcessVM;
				if (data != null) {
					data.PropertyChanged += AttachProcessVM_PropertyChanged;
					data.Collection.CollectionChanged += AttachProcessVM_Collection_CollectionChanged;
				}
			};
			Loaded += OnLoaded;
		}

		void AttachProcessVM_Collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (e.NewItems == null)
				return;
			var vm = DataContext as AttachProcessVM;
			if (vm == null || vm.Collection.Count != 1)
				return;
			FocusListViewElement();
		}

		void FocusListViewElement() {
			var vm = DataContext as AttachProcessVM;
			if (vm == null || vm.Collection.Count == 0)
				return;
			var obj = vm.Collection[0];
			listView.SelectedItem = obj;
			UIUtils.SetFocus(listView, obj, DispatcherPriority.Background);
		}

		void OnLoaded(object sender, RoutedEventArgs e) {
			listView.Focus();
			var vm = DataContext as AttachProcessVM;
			if (listView.SelectedItem == null && vm != null && vm.Collection.Count > 0)
				FocusListViewElement();
		}

		void AttachProcessVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "IsRefreshing") {
				// Make sure Refresh button gets updated
				CommandManager.InvalidateRequerySuggested();
			}
		}

		void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			if (!UIUtils.IsLeftDoubleClick<ListViewItem>(listView, e))
				return;

			var vm = DataContext as AttachProcessVM;
			if (vm == null || vm.HasError)
				return;
			this.okButton_Click(this, e);
		}
	}
}

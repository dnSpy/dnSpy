/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Documents.Tabs.Dialogs {
	partial class TabsDlg : WindowBase {
		public TabsDlg() {
			InitializeComponent();
			listView.SelectionChanged += ListView_SelectionChanged;
			UIUtilities.FocusSelector(listView);
			InputBindings.Add(new KeyBinding(new RelayCommand(a => ClickCancel()), Key.Escape, ModifierKeys.None));
		}

		void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			var vm = DataContext as TabsVM;
			if (vm != null)
				vm.SelectedItems = listView.SelectedItems.OfType<TabVM>().ToArray();
		}

		void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			if (!UIUtilities.IsLeftDoubleClick<ListViewItem>(listView, e))
				return;
			ExitDialog();
		}

		void activateButton_Click(object sender, System.Windows.RoutedEventArgs e) => ExitDialog();

		void ExitDialog() {
			var vm = DataContext as TabsVM;
			if (vm == null)
				return;
			vm.Activate(listView.SelectedItem as TabVM);
			ClickOK();
		}
	}
}

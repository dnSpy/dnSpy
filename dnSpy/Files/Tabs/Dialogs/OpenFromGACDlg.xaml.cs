/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Shared.UI.Controls;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Files.Tabs.Dialogs {
	sealed partial class OpenFromGACDlg : WindowBase {
		public OpenFromGACDlg() {
			InitializeComponent();
			InputBindings.Add(new KeyBinding(new RelayCommand(a => searchBox.Focus()), Key.E, ModifierKeys.Control));
			InputBindings.Add(new KeyBinding(new RelayCommand(a => searchBox.Focus()), Key.F, ModifierKeys.Control));
		}

		public IEnumerable<GACFileVM> SelectedItems {
			get {
				foreach (GACFileVM vm in listView.SelectedItems)
					yield return vm;
			}
		}

		protected override void OnClosed(EventArgs e) {
			var id = DataContext as IDisposable;
			if (id != null)
				id.Dispose();
		}

		void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			if (!UIUtils.IsLeftDoubleClick<ListViewItem>(listView, e))
				return;
			this.ClickOK();
		}
	}
}

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
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Documents.Tabs.Dialogs {
	sealed partial class OpenFromGACDlg : WindowBase {
		public OpenFromGACDlg() {
			InitializeComponent();
			var cmd = new RelayCommand(a => {
				searchBox.Focus();
				searchBox.SelectAll();
			});
			InputBindings.Add(new KeyBinding(cmd, Key.E, ModifierKeys.Control));
			InputBindings.Add(new KeyBinding(cmd, Key.F, ModifierKeys.Control));
		}

		public IEnumerable<GACFileVM> SelectedItems {
			get {
				foreach (GACFileVM vm in listView.SelectedItems)
					yield return vm;
			}
		}

		protected override void OnClosed(EventArgs e) {
			progressBar.IsIndeterminate = false;
			base.OnClosed(e);
			if (DataContext is IDisposable id)
				id.Dispose();
		}

		void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			if (!UIUtilities.IsLeftDoubleClick<ListViewItem>(listView, e))
				return;
			ClickOK();
		}
	}
}

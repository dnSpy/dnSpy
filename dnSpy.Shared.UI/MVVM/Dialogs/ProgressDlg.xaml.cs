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

using System;
using System.ComponentModel;
using System.Windows;
using dnSpy.Shared.UI.Controls;

namespace dnSpy.Shared.UI.MVVM.Dialogs {
	/// <summary>
	/// Interaction logic for ProgressDlg.xaml
	/// </summary>
	public partial class ProgressDlg : WindowBase {
		public ProgressDlg() {
			InitializeComponent();
			DataContextChanged += (s, e) => {
				var data = DataContext as ProgressVM;
				if (data != null)
					data.OnCompleted += ProgressVM_OnCompleted;
				if (data.HasCompleted)
					OnCompleted();
			};
		}

		protected override void OnClosing(CancelEventArgs e) {
			base.OnClosing(e);

			var data = DataContext as ProgressVM;
			if (data == null)
				return;
			data.Cancel();
			if (!data.HasCompleted)
				e.Cancel = true;
		}

		void ProgressVM_OnCompleted(object sender, EventArgs e) {
			OnCompleted();
		}

		void OnCompleted() {
			var data = DataContext as ProgressVM;
			this.DialogResult = data != null && !data.WasCanceled;
			Close();
		}

		public static void Show(IProgressTask task, Window ownerWindow) {
			var win = new ProgressDlg();
			var vm = new ProgressVM(System.Windows.Threading.Dispatcher.CurrentDispatcher, task);
			win.Owner = ownerWindow;
			win.DataContext = vm;
			win.ShowDialog();
		}
	}
}

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
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using dnSpy.Shared.UI.Controls;

namespace dnSpy.Debugger.IMModules {
	sealed partial class LoadEverythingDlg : WindowBase {
		public LoadEverythingDlg() {
			InitializeComponent();
			DataContextChanged += (s, e) => {
				var data = DataContext as LoadEverythingVM;
				if (data != null)
					data.OnCompleted += LoadEverythingVM_OnCompleted;
				if (data.HasCompleted)
					OnCompleted();
			};
			Loaded += LoadEverythingDlg_Loaded;
		}

		void LoadEverythingDlg_Loaded(object sender, RoutedEventArgs e) {
			var data = DataContext as LoadEverythingVM;
			Debug.Assert(data != null);
			if (data != null) {
				this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => {
					data.LoadFiles();
				}));
			}
		}

		protected override void OnClosing(CancelEventArgs e) {
			base.OnClosing(e);

			var data = DataContext as LoadEverythingVM;
			if (data == null)
				return;
			data.Cancel();
			if (!data.HasCompleted)
				e.Cancel = true;
		}

		void LoadEverythingVM_OnCompleted(object sender, EventArgs e) {
			OnCompleted();
		}

		void OnCompleted() {
			var data = DataContext as LoadEverythingVM;
			this.DialogResult = data != null && !data.WasCanceled;
			Close();
		}
	}
}

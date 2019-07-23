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
using System.ComponentModel;
using dnSpy.Contracts.Controls;

namespace dnSpy.Debugger.DotNet.Metadata {
	sealed partial class ModuleLoaderDlg : WindowBase {
		public ModuleLoaderDlg() {
			InitializeComponent();
			DataContextChanged += (s, e) => {
				var data = DataContext as ModuleLoaderVM;
				if (!(data is null)) {
					data.OnCompleted += ModuleLoaderVM_OnCompleted;
					if (data.HasCompleted)
						OnCompleted();
				}
			};
		}

		protected override void OnClosed(EventArgs e) {
			progressBar.IsIndeterminate = false;
			base.OnClosed(e);
		}

		protected override void OnClosing(CancelEventArgs e) {
			base.OnClosing(e);

			var data = DataContext as ModuleLoaderVM;
			if (data is null)
				return;
			if (!data.HasCompleted)
				data.AskCancel();
			if (!data.HasCompleted)
				e.Cancel = true;
		}

		void ModuleLoaderVM_OnCompleted(object? sender, EventArgs e) => OnCompleted();

		void OnCompleted() {
			var data = DataContext as ModuleLoaderVM;
			DialogResult = !(data is null) && !data.WasCanceled;
			Close();
		}
	}
}

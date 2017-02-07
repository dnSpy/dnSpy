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

using System.Diagnostics;
using System.Windows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.Dialogs_OLD {
	sealed partial class DebugCoreCLRDlg : WindowBase {
		public DebugCoreCLRDlg() {
			InitializeComponent();
			DataContextChanged += (s, e) => {
				var data = DataContext as DebugCoreCLRVM;
				if (data != null) {
					data.PickDirectory = new PickDirectory();
					data.PickFilename = new PickFilename();
				}
			};
			Loaded += DebugCoreCLRDlg_Loaded;
		}

		void DebugCoreCLRDlg_Loaded(object sender, RoutedEventArgs e) {
			Loaded -= DebugCoreCLRDlg_Loaded;
			var vm = DataContext as DebugCoreCLRVM;
			Debug.Assert(vm != null);
			bool focusArgs = vm == null || !string.IsNullOrEmpty(vm.HostFilename);
			if (focusArgs)
				argsTextBox.Focus();
			else
				hostTextBox.Focus();
		}
	}
}

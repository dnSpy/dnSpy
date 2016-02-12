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
using dnSpy.Shared.Controls;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.Dialogs {
	sealed partial class DebugProcessDlg : WindowBase {
		public DebugProcessDlg() {
			InitializeComponent();
			DataContextChanged += (s, e) => {
				var data = DataContext as DebugProcessVM;
				if (data != null) {
					data.PickDirectory = new PickDirectory();
					data.PickFilename = new PickFilename();
				}
			};
			Loaded += DebugProcessDlg_Loaded;
		}

		void DebugProcessDlg_Loaded(object sender, System.Windows.RoutedEventArgs e) {
			Loaded -= DebugProcessDlg_Loaded;
			var vm = DataContext as DebugProcessVM;
			Debug.Assert(vm != null);
			bool focusArgs = vm == null || !string.IsNullOrEmpty(vm.Filename);
			if (focusArgs)
				argsTextBox.Focus();
			else
				exeTextBox.Focus();
		}
	}
}

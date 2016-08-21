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

using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.SaveModule {
	sealed partial class SaveMultiModuleDlg : SaveModuleWindow {
		public SaveMultiModuleDlg() {
			InitializeComponent();
		}

		void Options_CanExecute(object sender, CanExecuteRoutedEventArgs e) =>
			e.CanExecute = e.Parameter is SaveOptionsVM;
		void Options_Executed(object sender, ExecutedRoutedEventArgs e) =>
			ShowOptions((SaveOptionsVM)e.Parameter);

		void listBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			if (!UIUtilities.IsLeftDoubleClick<ListBoxItem>(listBox, e))
				return;
			ShowOptions((SaveOptionsVM)listBox.SelectedItem);
		}
	}
}

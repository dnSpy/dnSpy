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

using System.Windows;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;

namespace dnSpy.MainApp {
	sealed partial class MsgBoxDlg : WindowBase {
		public MsgBoxButton ClickedButton {
			get => clickedButton;
			set => clickedButton = value;
		}
		MsgBoxButton clickedButton;

		public MsgBoxDlg() {
			clickedButton = MsgBoxButton.None;
			InitializeComponent();
			IsVisibleChanged += MsgBoxDlg_IsVisibleChanged;
		}

		void MsgBoxDlg_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			var vm = DataContext as MsgBoxVM;
			if (vm == null)
				return;
			if (vm.HasOKButton)
				okButton.Focus();
			else if (vm.HasYesButton)
				yesButton.Focus();
		}

		public void Close(MsgBoxButton button) {
			ClickedButton = button;
			DialogResult = true;
			Close();
		}
	}
}

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
using System.Windows;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;

namespace dnSpy.AsmEditor.SaveModule {
	class SaveModuleWindow : WindowBase {
		public SaveModuleWindow() {
			Loaded += SaveMultiModule_Loaded;
		}

		void SaveMultiModule_Loaded(object sender, RoutedEventArgs e) {
			var data = (SaveMultiModuleVM)DataContext;
			data.OnSavedEvent += SaveMultiModuleVM_OnSavedEvent;
		}

		void SaveMultiModuleVM_OnSavedEvent(object sender, EventArgs e) {
			var data = (SaveMultiModuleVM)DataContext;
			if (!data.HasError)
				okButton_Click(null, null);
		}

		protected override void OnClosing(CancelEventArgs e) {
			base.OnClosing(e);

			var data = (SaveMultiModuleVM)DataContext;
			if (data.IsSaving) {
				var res = MsgBox.Instance.Show(dnSpy_AsmEditor_Resources.AskCancelSave, MsgBoxButton.Yes | MsgBoxButton.No);
				if (res == MsgBoxButton.Yes)
					data.CancelSave();
				e.Cancel = true;
				return;
			}

			if (data.IsCanceling) {
				var res = MsgBox.Instance.Show(dnSpy_AsmEditor_Resources.AskCancelSaveCloseWindow, MsgBoxButton.Yes | MsgBoxButton.No);
				if (res != MsgBoxButton.Yes)
					e.Cancel = true;
				return;
			}
		}

		internal void ShowOptions(SaveOptionsVM data) {
			if (data == null)
				return;

			var mvm = data as SaveModuleOptionsVM;
			if (mvm != null) {
				var win = new SaveModuleOptionsDlg();
				win.Owner = this;
				var clone = mvm.Clone();
				win.DataContext = clone;
				var res = win.ShowDialog();
				if (res == true) {
					clone.CopyTo(mvm);
					((SaveMultiModuleVM)DataContext).OnModuleSettingsSaved();
				}
				return;
			}

			var hvm = data as SaveHexOptionsVM;
			if (hvm != null) {
				var win = new SaveHexOptionsDlg();
				win.Owner = this;
				var clone = hvm.Clone();
				win.DataContext = clone;
				var res = win.ShowDialog();
				if (res == true) {
					clone.CopyTo(hvm);
					((SaveMultiModuleVM)DataContext).OnModuleSettingsSaved();
				}
				return;
			}

			throw new InvalidOperationException();
		}
	}
}

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

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Shared.UI.Controls;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed partial class MemberPickerDlg : WindowBase {
		public MemberPickerDlg(IFileTreeView globalFileTreeView, IFileTreeView newFileTreeView, IImageManager imageManager) {
			InitializeComponent();
			DataContextChanged += (s, e) => {
				var data = DataContext as MemberPickerVM;
				if (data != null) {
					data.OpenAssembly = new OpenAssembly(globalFileTreeView.FileManager);
					data.PropertyChanged += MemberPickerVM_PropertyChanged;
				}
			};
			openImage.Source = imageManager.GetImage(GetType().Assembly, "Open", BackgroundType.DialogWindow);

			var treeView = (Control)newFileTreeView.TreeView.UIObject;
			cpTreeView.Content = treeView;
			Validation.SetErrorTemplate(treeView, (ControlTemplate)FindResource("noRedBorderOnValidationError"));
			treeView.AllowDrop = false;
			treeView.BorderThickness = new Thickness(1);

			var binding = new Binding {
				ValidatesOnDataErrors = true,
				ValidatesOnExceptions = true,
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
				Path = new PropertyPath("SelectedItem"),
				Mode = BindingMode.TwoWay,
			};
			treeView.SetBinding(Selector.SelectedItemProperty, binding);
		}

		void MemberPickerVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var vm = (MemberPickerVM)sender;
			if (e.PropertyName == "TooManyResults") {
				if (vm.TooManyResults)
					listBox.SetResourceReference(Control.BorderBrushProperty, "CommonControlsTextBoxBorderError");
				else
					listBox.ClearValue(Control.BorderBrushProperty);
			}
		}
	}
}

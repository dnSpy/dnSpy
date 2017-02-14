/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Windows.Controls;
using dnSpy.AsmEditor.ViewHelpers;

namespace dnSpy.AsmEditor.Resources {
	sealed partial class ResourceElementControl : UserControl {
		public ResourceElementControl() {
			InitializeComponent();
			DataContextChanged += (s, e) => {
				if (DataContext is ResourceElementVM data) {
					var ownerWindow = Window.GetWindow(this);
					data.OpenFile = new OpenFile(ownerWindow);
					data.DnlibTypePicker = new DnlibTypePicker(ownerWindow);
				}
			};
			Loaded += ResourceElementControl_Loaded;
		}

		void ResourceElementControl_Loaded(object sender, RoutedEventArgs e) {
			var vm = DataContext as ResourceElementVM;
			Debug.Assert(vm != null);
			if (vm != null && !string.IsNullOrEmpty(vm.Name) && vm.IsSingleLineValue)
				valueTextBox.Focus();
			else if (vm != null && !string.IsNullOrEmpty(vm.Name) && vm.IsMultiLineValue)
				multiLineTextBox.Focus();
			else
				nameTextBox.Focus();
		}
	}
}

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

using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Controls;

namespace dnSpy.Hex.Commands {
	sealed partial class GoToMetadataDlg : WindowBase {
		public GoToMetadataDlg() {
			InitializeComponent();
			DataContextChanged += (s, e) => {
				var vm = DataContext as GoToMetadataVM;
				if (vm != null) {
					InputBindings.Add(new KeyBinding(vm.SelectTableCommand, new KeyGesture(Key.D1, ModifierKeys.Control)));
					InputBindings.Add(new KeyBinding(vm.SelectMemberRvaCommand, new KeyGesture(Key.D2, ModifierKeys.Control)));
					InputBindings.Add(new KeyBinding(vm.SelectBlobCommand, new KeyGesture(Key.D3, ModifierKeys.Control)));
					InputBindings.Add(new KeyBinding(vm.SelectStringsCommand, new KeyGesture(Key.D4, ModifierKeys.Control)));
					InputBindings.Add(new KeyBinding(vm.SelectUSCommand, new KeyGesture(Key.D5, ModifierKeys.Control)));
					InputBindings.Add(new KeyBinding(vm.SelectGUIDCommand, new KeyGesture(Key.D6, ModifierKeys.Control)));
				}
			};
			Loaded += OnLoaded;
		}

		void OnLoaded(object sender, RoutedEventArgs e) {
			Loaded -= OnLoaded;
			numberTextBox.SelectAll();
		}
	}
}

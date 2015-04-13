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

using System;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ICSharpCode.ILSpy.Commands
{
	abstract class ToolbarCommand : ICommand
	{
		public bool CanExecute(object parameter)
		{
			return true;
		}

		public event EventHandler CanExecuteChanged {
			add { }
			remove { }
		}

		public void Execute(object parameter)
		{
		}
	}

	[ExportToolbarCommand(ToolbarCategory = "MainMenu", ToolbarOrder = 0)]
	sealed class MainMenuToolbarCommand : ToolbarCommand, IToolbarItemCreator
	{
		public object CreateToolbarItem()
		{
			return MainWindow.Instance.mainMenu;
		}
	}

	[ExportToolbarCommand(ToolbarCategory = "Options", ToolbarOrder = 3000)]
	sealed class ShowInternalTypesAndMembersToolbarCommand : ToolbarCommand, IToolbarItemCreator
	{
		CheckBox checkBox;

		public object CreateToolbarItem()
		{
			if (checkBox == null) {
				checkBox = new CheckBox() {
					Content = new Image {
						Width = 16,
						Height = 16,
						Source = Images.PrivateInternal,
					},
					ToolTip = "Show Internal Types and Members",
				};
				var binding = new Binding("FilterSettings.ShowInternalApi") {
					Source = MainWindow.Instance.SessionSettings,
				};
				checkBox.SetBinding(CheckBox.IsCheckedProperty, binding);
			}
			return checkBox;
		}
	}

	[ExportToolbarCommand(ToolbarCategory = "Language", ToolbarOrder = 4000)]
	sealed class LanguageComboBoxToolbarCommand : ToolbarCommand, IToolbarItemCreator
	{
		public object CreateToolbarItem()
		{
			return MainWindow.Instance.languageComboBox;
		}
	}
}

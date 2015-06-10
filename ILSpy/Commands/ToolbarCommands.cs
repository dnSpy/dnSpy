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
using System.Windows;
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
		public object CreateToolbarItem()
		{
			var checkBox = new CheckBox() {
				Content = new Image {
					Width = 16,
					Height = 16,
					Source = ImageCache.Instance.GetImage("PrivateInternal", BackgroundType.Toolbar),
				},
				ToolTip = "Show Internal Types and Members",
			};
			var binding = new Binding("FilterSettings.ShowInternalApi") {
				Source = MainWindow.Instance.SessionSettings,
			};
			checkBox.SetBinding(CheckBox.IsCheckedProperty, binding);
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

	[ExportToolbarCommand(ToolbarCategory = "FullScreen", ToolbarOrder = 10000)]
	sealed class FullScreenToolbarCommand : ToolbarCommand, IToolbarItemCreator, IToolbarCommand
	{
		public FullScreenToolbarCommand()
		{
			MainWindow.Instance.IsFullScreenChanged += (s, e) => MainWindow.Instance.UpdateToolbar();
		}

		public object CreateToolbarItem()
		{
			var sp = new StackPanel {
				Orientation = Orientation.Horizontal,
				ToolTip = "Full Screen",
			};
			sp.Children.Add(new Image {
				Width = 16,
				Height = 16,
				Source = ImageCache.Instance.GetImage("FullScreen", BackgroundType.ToolBarButtonChecked),
			});
			sp.Children.Add(new TextBlock {
				Text = "Full Screen",
				Margin = new Thickness(5, 0, 0, 0),
			});
			var checkBox = new CheckBox { Content = sp };
			var binding = new Binding("IsFullScreen") {
				Source = MainWindow.Instance,
			};
			checkBox.SetBinding(CheckBox.IsCheckedProperty, binding);
			return checkBox;
		}

		public bool IsVisible {
			get { return MainWindow.Instance.IsFullScreen; }
		}
	}
}

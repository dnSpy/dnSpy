
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

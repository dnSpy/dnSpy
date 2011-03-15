// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Input;

using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy
{
	[ExportMainMenuCommand(Menu = "_File", Header = "E_xit", MenuOrder = 99999, MenuCategory = "Exit")]
	sealed class ExitCommand : SimpleCommand
	{
		public override void Execute(object parameter)
		{
			MainWindow.Instance.Close();
		}
	}
	
	[ExportToolbarCommand(ToolTip = "Back", ToolbarIcon = "Images/Back.png", ToolbarCategory = "Navigation", ToolbarOrder = 0)]
	sealed class BrowseBackCommand : CommandWrapper {
		public BrowseBackCommand() : base(NavigationCommands.BrowseBack) {}
	}
	
	[ExportToolbarCommand(ToolTip = "Forward", ToolbarIcon = "Images/Forward.png", ToolbarCategory = "Navigation", ToolbarOrder = 1)]
	sealed class BrowseForwardCommand : CommandWrapper {
		public BrowseForwardCommand() : base(NavigationCommands.BrowseForward) {}
	}
	
	[ExportToolbarCommand(ToolTip = "Open", ToolbarIcon = "Images/Open.png", ToolbarCategory = "Open", ToolbarOrder = 0)]
	[ExportMainMenuCommand(Menu = "_File", MenuIcon = "Images/Open.png", MenuCategory = "Open", MenuOrder = 0)]
	sealed class OpenCommand : CommandWrapper {
		public OpenCommand() : base(ApplicationCommands.Open) {}
	}
	
	[ExportMainMenuCommand(Menu = "_File", Header = "Open from _GAC", MenuCategory = "Open", MenuOrder = 1)]
	sealed class OpenFromGacCommand : SimpleCommand
	{
		public override void Execute(object parameter)
		{
			OpenFromGacDialog dlg = new OpenFromGacDialog();
			dlg.Owner = MainWindow.Instance;
			if (dlg.ShowDialog() == true) {
				MainWindow.Instance.OpenFiles(dlg.SelectedFileNames);
			}
		}
	}
	
	[ExportToolbarCommand(ToolTip = "Reload all assemblies", ToolbarIcon = "Images/Refresh.png", ToolbarCategory = "Open", ToolbarOrder = 2)]
	[ExportMainMenuCommand(Menu = "_File", Header = "Reload", MenuIcon = "Images/Refresh.png", MenuCategory = "Open", MenuOrder = 2)]
	sealed class RefreshCommand : CommandWrapper {
		public RefreshCommand() : base(NavigationCommands.Refresh) {}
	}
	
	[ExportMainMenuCommand(Menu = "_File", Header = "_Save Code...", MenuIcon = "Images/SaveFile.png", MenuCategory = "Save", MenuOrder = 0)]
	sealed class SaveCommand : CommandWrapper
	{
		public SaveCommand() : base(ApplicationCommands.Save) {}
	}
	
	class CommandWrapper : ICommand
	{
		ICommand wrappedCommand;
		
		public CommandWrapper(ICommand wrappedCommand)
		{
			this.wrappedCommand = wrappedCommand;
		}
		
		public static ICommand Unwrap(ICommand command)
		{
			CommandWrapper w = command as CommandWrapper;
			if (w != null)
				return w.wrappedCommand;
			else
				return command;
		}
		
		public event EventHandler CanExecuteChanged {
			add { wrappedCommand.CanExecuteChanged += value; }
			remove { wrappedCommand.CanExecuteChanged -= value; }
		}
		
		public void Execute(object parameter)
		{
			wrappedCommand.Execute(parameter);
		}
		
		public bool CanExecute(object parameter)
		{
			return wrappedCommand.CanExecute(parameter);
		}
	}
	
	public abstract class SimpleCommand : ICommand
	{
		public event EventHandler CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}
		
		public abstract void Execute(object parameter);
		
		public virtual bool CanExecute(object parameter)
		{
			return true;
		}
	}
}

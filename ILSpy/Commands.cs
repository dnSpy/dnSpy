// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Windows.Input;

namespace ICSharpCode.ILSpy
{
	[ExportMainMenuCommand(Menu = "_File", Header = "E_xit", Order = 99999, Category = "Exit")]
	sealed class ExitCommand : SimpleCommand
	{
		public override void Execute(object parameter)
		{
			MainWindow.Instance.Close();
		}
	}
	
	[ExportToolbarCommand(ToolTip = "Back", Icon = "Images/Back.png", Category = "Navigation")]
	sealed class BrowseBackCommand : CommandWrapper {
		public BrowseBackCommand() : base(NavigationCommands.BrowseBack) {}
	}
	
	[ExportToolbarCommand(ToolTip = "Forward", Icon = "Images/Forward.png", Category = "Navigation", Order = 1)]
	sealed class BrowseForwardCommand : CommandWrapper {
		public BrowseForwardCommand() : base(NavigationCommands.BrowseForward) {}
	}
	
	[ExportToolbarCommand(ToolTip = "Open", Icon = "Images/Open.png", Category = "Open")]
	sealed class OpenCommand : CommandWrapper {
		public OpenCommand() : base(ApplicationCommands.Open) {}
	}
	
	[ExportToolbarCommand(ToolTip = "Reload all assemblies", Icon = "Images/Refresh.png", Category = "Open", Order = 1)]
	sealed class RefreshCommand : CommandWrapper {
		public RefreshCommand() : base(NavigationCommands.Refresh) {}
	}
	
	class CommandWrapper : ICommand
	{
		ICommand wrappedCommand;
		
		public CommandWrapper(ICommand wrappedCommand)
		{
			this.wrappedCommand = wrappedCommand;
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

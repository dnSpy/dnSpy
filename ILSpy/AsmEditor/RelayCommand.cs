
using System;
using System.Windows.Input;

namespace ICSharpCode.ILSpy.AsmEditor
{
	sealed class RelayCommand : ICommand
	{
		readonly Action<object> exec;
		readonly Predicate<object> canExec;

		public RelayCommand(Action<object> exec, Predicate<object> canExec = null)
		{
			this.exec = exec;
			this.canExec = canExec;
		}

		public bool CanExecute(object parameter)
		{
			return canExec == null ? true : canExec(parameter);
		}

		public event EventHandler CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public void Execute(object parameter)
		{
			exec(parameter);
		}
	}
}

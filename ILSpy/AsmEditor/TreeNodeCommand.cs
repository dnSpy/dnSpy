
using System;
using System.Windows.Input;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.AsmEditor
{
	public abstract class TreeNodeCommand : ICommand
	{
		protected static ILSpyTreeNode[] GetSelectedNodes()
		{
			return MainWindow.Instance.SelectedNodes;
		}

		public event EventHandler CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public bool CanExecute(object parameter)
		{
			return CanExecute(GetSelectedNodes());
		}

		bool CanExecute(ILSpyTreeNode[] nodes)
		{
			return nodes != null &&
				nodes.Length > 0 &&
				CanExecuteInternal(nodes);
		}

		public void Execute(object parameter)
		{
			Execute(GetSelectedNodes());
		}

		void Execute(ILSpyTreeNode[] nodes)
		{
			if (CanExecute(nodes))
				ExecuteInternal(nodes);
		}

		protected abstract bool CanExecuteInternal(ILSpyTreeNode[] nodes);
		protected abstract void ExecuteInternal(ILSpyTreeNode[] nodes);
	}
}

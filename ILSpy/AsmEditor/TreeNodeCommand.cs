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
using System.Windows.Input;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.AsmEditor
{
	public abstract class TreeNodeCommand : ICommand
	{
		readonly bool canAcceptEmptySelectedNodes;

		protected TreeNodeCommand()
			: this(false)
		{
		}

		protected TreeNodeCommand(bool canAcceptEmptySelectedNodes)
		{
			this.canAcceptEmptySelectedNodes = canAcceptEmptySelectedNodes;
		}

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

		protected bool CanExecute(ILSpyTreeNode[] nodes)
		{
			return nodes != null &&
				(canAcceptEmptySelectedNodes || nodes.Length > 0) &&
				CanExecuteInternal(nodes);
		}

		public void Execute(object parameter)
		{
			Execute(GetSelectedNodes());
		}

		protected void Execute(ILSpyTreeNode[] nodes)
		{
			if (CanExecute(nodes))
				ExecuteInternal(nodes);
		}

		protected abstract bool CanExecuteInternal(ILSpyTreeNode[] nodes);
		protected abstract void ExecuteInternal(ILSpyTreeNode[] nodes);
	}
}

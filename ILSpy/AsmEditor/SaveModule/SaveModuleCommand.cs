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
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.AsmEditor.SaveModule
{
	[ExportMainMenuCommand(Menu = "_File", MenuCategory = "Save", MenuOrder = 1010)]
	sealed class SaveModuleCommand : TreeNodeCommand, IMainMenuCommandInitialize
	{
		public SaveModuleCommand()
		{
			MainWindow.Instance.SetMenuAlwaysRegenerate("_File");
		}

		HashSet<LoadedAssembly> GetAssemblyNodes(ILSpyTreeNode[] nodes)
		{
			var hash = new HashSet<LoadedAssembly>();
			foreach (var node in nodes) {
				var asmNode = ILSpyTreeNode.GetNode<AssemblyTreeNode>(node);
				if (asmNode != null && asmNode.LoadedAssembly.ModuleDefinition != null)
					hash.Add(asmNode.LoadedAssembly);
			}
			return hash;
		}

		protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes)
		{
			return GetAssemblyNodes(nodes).Count > 0;
		}

		protected override void ExecuteInternal(ILSpyTreeNode[] nodes)
		{
			var asmNodes = GetAssemblyNodes(nodes);
			Saver.SaveAssemblies(asmNodes);
		}

		public void Initialize(MenuItem menuItem)
		{
			menuItem.Header = GetAssemblyNodes(GetSelectedNodes()).Count <= 1 ? "Save _Module…" : "Save _Modules…";
		}
	}

	[ExportToolbarCommand(ToolTip = "Save All (Ctrl+Shift+S)",
						  ToolbarIcon = "SaveAll",
						  ToolbarCategory = "Open",
						  ToolbarOrder = 2010)]
	sealed class SaveAllToolbarCommand : ICommand
	{
		public SaveAllToolbarCommand()
		{
			MainWindow.Instance.SetMenuAlwaysRegenerate("_File");
			MainWindow.Instance.InputBindings.Add(new KeyBinding(this, Key.S, ModifierKeys.Control | ModifierKeys.Shift));
		}

		static LoadedAssembly[] GetDirtyAssemblies()
		{
			var list = new List<LoadedAssembly>();
			foreach (var asmNode in UndoCommandManager.Instance.GetModifiedAssemblyTreeNodes())
				list.Add(asmNode.LoadedAssembly);
			return list.ToArray();
		}

		public event EventHandler CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public bool CanExecute(object parameter)
		{
			return CanExecute();
		}

		public void Execute(object parameter)
		{
			Execute();
		}

		public static bool CanExecute()
		{
			return GetDirtyAssemblies().Length > 0;
		}

		public static void Execute()
		{
			Saver.SaveAssemblies(GetDirtyAssemblies());
		}
	}

	[ExportMainMenuCommand(Menu = "_File", MenuHeader = "Save A_ll…", MenuInputGestureText = "Ctrl+Shift+S", MenuCategory = "Save", MenuOrder = 1020, MenuIcon = "SaveAll")]
	sealed class SaveAllCommand : ICommand
	{
		public event EventHandler CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public bool CanExecute(object parameter)
		{
			return SaveAllToolbarCommand.CanExecute();
		}

		public void Execute(object parameter)
		{
			SaveAllToolbarCommand.Execute();
		}
	}
}

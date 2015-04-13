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
using System.Linq;
using System.Windows;
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
				var asmNode = ILSpyTreeNode.GetAssemblyTreeNode(node);
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
			SaveAssemblies(asmNodes);
		}

		public void Initialize(MenuItem menuItem)
		{
			menuItem.Header = GetAssemblyNodes(GetSelectedNodes()).Count <= 1 ? "Save _Module..." : "Save _Modules...";
		}

		public static void SaveAssemblies(IEnumerable<LoadedAssembly> asms)
		{
			var asmsAry = asms.ToArray();
			if (asmsAry.Length == 0)
				return;

			if (asmsAry.Length == 1) {
				var optsData = new SaveModuleOptionsVM(asmsAry[0].ModuleDefinition);
				var optsWin = new SaveModuleOptions();
				optsWin.Owner = MainWindow.Instance;
				optsWin.DataContext = optsData;
				var res = optsWin.ShowDialog();
				if (res != true)
					return;

				var data = new SaveMultiModuleVM(optsData);
				var win = new SaveSingleModule();
				win.Owner = MainWindow.Instance;
				win.DataContext = data;
				data.Save();
				win.ShowDialog();
				MarkAsSaved(data, asmsAry);
			}
			else {
				var data = new SaveMultiModuleVM(asmsAry.Select(a => a.ModuleDefinition));
				var win = new SaveMultiModule();
				win.Owner = MainWindow.Instance;
				win.DataContext = data;
				win.ShowDialog();
				MarkAsSaved(data, asmsAry);
			}
		}

		static void MarkAsSaved(SaveMultiModuleVM vm, LoadedAssembly[] nodes)
		{
			foreach (var asm in nodes) {
				if (vm.WasSaved(asm.ModuleDefinition))
					UndoCommandManager.Instance.MarkAsSaved(asm);
			}
		}
	}

	[ExportToolbarCommand(ToolTip = "Save All (Ctrl+Shift+S)",
						  ToolbarIcon = "Images/SaveAll.png",
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
			SaveModuleCommand.SaveAssemblies(GetDirtyAssemblies());
		}
	}

	[ExportMainMenuCommand(Menu = "_File", Header = "Save A_ll...", InputGestureText = "Ctrl+Shift+S", MenuCategory = "Save", MenuOrder = 1020, MenuIcon = "Images/SaveAll.png")]
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

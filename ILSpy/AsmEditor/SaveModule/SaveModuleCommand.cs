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
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor.SaveModule {
	[ExportMainMenuCommand(Menu = "_File", MenuCategory = "Save", MenuOrder = 1010)]
	sealed class SaveModuleCommand : TreeNodeCommand, IMainMenuCommandInitialize {
		public SaveModuleCommand() {
			MainWindow.Instance.SetMenuAlwaysRegenerate("_File");
		}

		HashSet<IUndoObject> GetAssemblyNodes(ILSpyTreeNode[] nodes) {
			var hash = new HashSet<IUndoObject>();
			foreach (var node in nodes) {
				var asmNode = ILSpyTreeNode.GetNode<AssemblyTreeNode>(node);
				if (asmNode == null)
					continue;

				bool added = false;

				if (asmNode.LoadedAssembly.ModuleDefinition != null && UndoCommandManager.Instance.IsModified(asmNode.LoadedAssembly)) {
					hash.Add(asmNode.LoadedAssembly);
					added = true;
				}

				var doc = HexDocumentManager.Instance.TryGet(asmNode.LoadedAssembly.FileName);
				if (doc != null && UndoCommandManager.Instance.IsModified(doc)) {
					hash.Add(doc);
					added = true;
				}

				// If nothing was modified, just include the selected module
				if (!added && asmNode.LoadedAssembly.ModuleDefinition != null)
					hash.Add(asmNode.LoadedAssembly);
			}
			return hash;
		}

		protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
			return GetAssemblyNodes(nodes).Count > 0;
		}

		protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
			var asmNodes = GetAssemblyNodes(nodes);
			Saver.SaveAssemblies(asmNodes);
		}

		public void Initialize(MenuItem menuItem) {
			menuItem.Header = GetAssemblyNodes(GetSelectedNodes()).Count <= 1 ? "Save _Module…" : "Save _Modules…";
		}
	}

	[ExportToolbarCommand(ToolTip = "Save All (Ctrl+Shift+S)",
						  ToolbarIcon = "SaveAll",
						  ToolbarCategory = "Open",
						  ToolbarOrder = 2010)]
	sealed class SaveAllToolbarCommand : ICommand {
		public SaveAllToolbarCommand() {
			MainWindow.Instance.SetMenuAlwaysRegenerate("_File");
			MainWindow.Instance.InputBindings.Add(new KeyBinding(this, Key.S, ModifierKeys.Control | ModifierKeys.Shift));
		}

		static IUndoObject[] GetDirtyObjects() {
			return UndoCommandManager.Instance.GetModifiedObjects().ToArray();
		}

		public event EventHandler CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public bool CanExecute(object parameter) {
			return CanExecute();
		}

		public void Execute(object parameter) {
			Execute();
		}

		public static bool CanExecute() {
			return GetDirtyObjects().Length > 0;
		}

		public static void Execute() {
			Saver.SaveAssemblies(GetDirtyObjects());
		}
	}

	[ExportMainMenuCommand(Menu = "_File", MenuHeader = "Save A_ll…", MenuInputGestureText = "Ctrl+Shift+S", MenuCategory = "Save", MenuOrder = 1020, MenuIcon = "SaveAll")]
	sealed class SaveAllCommand : ICommand {
		public event EventHandler CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public bool CanExecute(object parameter) {
			return SaveAllToolbarCommand.CanExecute();
		}

		public void Execute(object parameter) {
			SaveAllToolbarCommand.Execute();
		}
	}
}

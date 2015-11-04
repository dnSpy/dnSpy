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
using System.Windows.Input;
using dnSpy.Contracts.Menus;
using dnSpy.Tabs;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor.SaveModule {
	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Group = MenuConstants.GROUP_APP_MENU_FILE_SAVE, Order = 10)]
	sealed class SaveModuleCommand : FileMenuHandler {
		HashSet<IUndoObject> GetAssemblyNodes(ILSpyTreeNode[] nodes) {
			var hash = new HashSet<IUndoObject>();

			if (nodes.Length == 0) {
				var hex = MainWindow.Instance.ActiveTabState as HexTabState;
				if (hex != null) {
					var doc = hex.HexBox.Document as AsmEdHexDocument;
					if (doc != null)
						hash.Add(UndoCommandManager.Instance.GetUndoObject(doc));
				}
			}

			foreach (var node in nodes) {
				var asmNode = ILSpyTreeNode.GetNode<AssemblyTreeNode>(node);
				if (asmNode == null)
					continue;

				bool added = false;

				if (asmNode.DnSpyFile.ModuleDef != null) {
					var uo = UndoCommandManager.Instance.GetUndoObject(asmNode.DnSpyFile);
					if (UndoCommandManager.Instance.IsModified(uo)) {
						hash.Add(uo);
						added = true;
					}
				}

				var doc = HexDocumentManager.Instance.TryGet(asmNode.DnSpyFile.Filename);
				if (doc != null) {
					var uo = UndoCommandManager.Instance.GetUndoObject(doc);
					if (UndoCommandManager.Instance.IsModified(uo)) {
						hash.Add(uo);
						added = true;
					}
				}

				// If nothing was modified, just include the selected module
				if (!added && asmNode.DnSpyFile.ModuleDef != null)
					hash.Add(UndoCommandManager.Instance.GetUndoObject(asmNode.DnSpyFile));
			}
			return hash;
		}

		public override bool IsVisible(AsmEditorContext context) {
			return true;
		}

		public override bool IsEnabled(AsmEditorContext context) {
			return GetAssemblyNodes(context.Nodes).Count > 0;
		}

		public override void Execute(AsmEditorContext context) {
			var asmNodes = GetAssemblyNodes(context.Nodes);
			Saver.SaveAssemblies(asmNodes);
		}

		public override string GetHeader(AsmEditorContext context) {
			return GetAssemblyNodes(context.Nodes).Count <= 1 ? "Save _Module..." : "Save _Modules...";
		}
	}

	[ExportToolbarCommand(ToolTip = "Save All (Ctrl+Shift+S)",
						  ToolbarIcon = "SaveAll",
						  ToolbarCategory = "Open",
						  ToolbarOrder = 2010)]
	sealed class SaveAllToolbarCommand : ICommand {
		public SaveAllToolbarCommand() {
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

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Header = "Save A_ll...", Icon = "SaveAll", InputGestureText = "Ctrl+Shift+S", Group = MenuConstants.GROUP_APP_MENU_FILE_SAVE, Order = 20)]
	sealed class SaveAllCommand : FileMenuHandler {
		public override bool IsEnabled(AsmEditorContext context) {
			return SaveAllToolbarCommand.CanExecute();
		}

		public override void Execute(AsmEditorContext context) {
			SaveAllToolbarCommand.Execute();
		}
	}
}

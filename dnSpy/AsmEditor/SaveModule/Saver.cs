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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnSpy.Files;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor.SaveModule {
	static class Saver {
		public static bool AskUserToSaveIfModified(AssemblyTreeNode asmNode) {
			return AskUserToSaveIfModified(new[] { asmNode.DnSpyFile });
		}

		public static bool AskUserToSaveIfModified(IEnumerable<AssemblyTreeNode> asmNodes) {
			return AskUserToSaveIfModified(asmNodes.Select(n => n.DnSpyFile));
		}

		public static bool AskUserToSaveIfModified(IEnumerable<IUndoObject> objs) {
			var modifiedObjs = objs.Where(n => UndoCommandManager.Instance.IsModified(n)).Distinct().ToArray();
			if (modifiedObjs.Length == 0)
				return true;

			var msg = modifiedObjs.Length == 1 ?
				"The file hasn't been saved yet. Do you want to save it before continuing?" :
				"The files haven't been saved yet. Do you want to save them before continuing?";
			var res = MainWindow.Instance.ShowMessageBox(msg, System.Windows.MessageBoxButton.YesNo);
			if (res == MsgBoxButton.No)
				return true;
			return SaveAssemblies(modifiedObjs);
		}

		/// <summary>
		/// Saves all asms/hex docs and returns true if all of them were saved to disk
		/// </summary>
		/// <param name="objs">All objects to save</param>
		/// <returns></returns>
		public static bool SaveAssemblies(IEnumerable<IUndoObject> objs) {
			var objsAry = objs.ToArray();
			if (objsAry.Length == 0)
				return true;

			if (objsAry.Length == 1) {
				SaveOptionsVM options;

				var asm = objsAry[0] as DnSpyFile;
				if (asm != null) {
					var optsData = new SaveModuleOptionsVM(asm);
					var optsWin = new SaveModuleOptionsDlg();
					optsWin.Owner = MainWindow.Instance;
					optsWin.DataContext = optsData;
					var res = optsWin.ShowDialog();
					if (res != true)
						return false;
					options = optsData;
				}
				else {
					var doc = (AsmEdHexDocument)objsAry[0];
					var optsData = new SaveHexOptionsVM(doc);
					var optsWin = new SaveHexOptionsDlg();
					optsWin.Owner = MainWindow.Instance;
					optsWin.DataContext = optsData;
					var res = optsWin.ShowDialog();
					if (res != true)
						return false;
					options = optsData;
				}

				var data = new SaveMultiModuleVM(MainWindow.Instance.Dispatcher, options);
				var win = new SaveSingleModuleDlg();
				win.Owner = MainWindow.Instance;
				win.DataContext = data;
				data.Save();
				win.ShowDialog();
				return MarkAsSaved(data, objsAry);
			}
			else {
				var data = new SaveMultiModuleVM(MainWindow.Instance.Dispatcher, objsAry);
				var win = new SaveMultiModuleDlg();
				win.Owner = MainWindow.Instance;
				win.DataContext = data;
				win.ShowDialog();
				return MarkAsSaved(data, objsAry);
			}
		}

		static bool MarkAsSaved(SaveMultiModuleVM vm, IUndoObject[] objs) {
			bool setNewFileName = false;
			bool allSaved = true;
			foreach (var obj in objs) {
				if (!vm.WasSaved(obj))
					allSaved = false;
				else {
					UndoCommandManager.Instance.MarkAsSaved(obj);
					var asm = obj as DnSpyFile;
					if (asm != null && string.IsNullOrEmpty(asm.Filename)) {
						var filename = vm.GetSavedFileName(asm);
						if (!string.IsNullOrWhiteSpace(filename) && asm.ModuleDef != null) {
							asm.ModuleDef.Location = filename;
							asm.Filename = filename;
							setNewFileName = true;
							var asmNode = MainWindow.Instance.FindTreeNode(asm.ModuleDef) as AssemblyTreeNode;
							Debug.Assert(asmNode != null);
							if (asmNode != null) {
								asmNode.OnFileNameChanged();
								Utils.NotifyModifiedAssembly(asm);
							}
						}
					}
				}
			}
			if (setNewFileName)
				MainWindow.Instance.DnSpyFileListManager.RefreshSave(MainWindow.Instance.DnSpyFileList);
			return allSaved;
		}
	}
}

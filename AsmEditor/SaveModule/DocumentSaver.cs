/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using dnSpy.AsmEditor.Hex;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.AsmEditor.SaveModule {
	interface IDocumentSaver {
		bool AskUserToSaveIfModified(IEnumerable<object> docs);

		/// <summary>
		/// Saves all asms/hex docs and returns true if all of them were saved to disk
		/// </summary>
		/// <param name="docs">All docs to save</param>
		/// <returns></returns>
		bool Save(IEnumerable<object> docs);
	}

	[Export, Export(typeof(IDocumentSaver)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class DocumentSaver : IDocumentSaver {
		readonly Lazy<IUndoCommandManager> undoCommandManager;
		readonly Lazy<IMmapDisabler> mmapDisabler;
		readonly IAppWindow appWindow;
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		DocumentSaver(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IMmapDisabler> mmapDisabler, IFileTabManager fileTabManager, IAppWindow appWindow) {
			this.undoCommandManager = undoCommandManager;
			this.mmapDisabler = mmapDisabler;
			this.fileTabManager = fileTabManager;
			this.appWindow = appWindow;
		}

		IEnumerable<object> Distinct(IEnumerable<object> objs) {
			return undoCommandManager.Value.GetUniqueDocuments(objs);
		}

		public bool AskUserToSaveIfModified(IEnumerable<object> docs) {
			var modifiedDocs = Distinct(docs).Where(a => undoCommandManager.Value.IsModified(undoCommandManager.Value.GetUndoObject(a))).ToArray();
			if (modifiedDocs.Length == 0)
				return true;

			var msg = modifiedDocs.Length == 1 ? dnSpy_AsmEditor_Resources.AskSaveFile : dnSpy_AsmEditor_Resources.AskSaveFiles;
			var res = Shared.UI.App.MsgBox.Instance.Show(msg, MsgBoxButton.Yes | MsgBoxButton.No);
			if (res == MsgBoxButton.No)
				return true;
			return Save(modifiedDocs);
		}

		public bool Save(IEnumerable<object> docs) {
			var objsAry = Distinct(docs).ToArray();
			if (objsAry.Length == 0)
				return true;

			if (objsAry.Length == 1) {
				SaveOptionsVM options;

				var file = objsAry[0] as IDnSpyFile;
				if (file != null) {
					var optsData = new SaveModuleOptionsVM(file);
					var optsWin = new SaveModuleOptionsDlg();
					optsWin.Owner = appWindow.MainWindow;
					optsWin.DataContext = optsData;
					var res = optsWin.ShowDialog();
					if (res != true)
						return false;
					options = optsData;
				}
				else {
					var doc = objsAry[0] as AsmEdHexDocument;
					Debug.Assert(doc != null);
					var optsData = new SaveHexOptionsVM(doc);
					var optsWin = new SaveHexOptionsDlg();
					optsWin.Owner = appWindow.MainWindow;
					optsWin.DataContext = optsData;
					var res = optsWin.ShowDialog();
					if (res != true)
						return false;
					options = optsData;
				}

				var data = new SaveMultiModuleVM(mmapDisabler.Value, Dispatcher.CurrentDispatcher, options);
				var win = new SaveSingleModuleDlg();
				win.Owner = appWindow.MainWindow;
				win.DataContext = data;
				data.Save();
				win.ShowDialog();
				return MarkAsSaved(data, objsAry);
			}
			else {
				var data = new SaveMultiModuleVM(mmapDisabler.Value, Dispatcher.CurrentDispatcher, objsAry);
				var win = new SaveMultiModuleDlg();
				win.Owner = appWindow.MainWindow;
				win.DataContext = data;
				win.ShowDialog();
				return MarkAsSaved(data, objsAry);
			}
		}

		bool MarkAsSaved(SaveMultiModuleVM vm, object[] docs) {
			bool allSaved = true;
			foreach (var doc in docs) {
				if (!vm.WasSaved(doc))
					allSaved = false;
				else {
					undoCommandManager.Value.MarkAsSaved(undoCommandManager.Value.GetUndoObject(doc));
					var file = doc as IDnSpyFile;
					if (file != null && string.IsNullOrEmpty(file.Filename)) {
						var filename = vm.GetSavedFileName(doc);
						if (!string.IsNullOrWhiteSpace(filename) && file.ModuleDef != null) {
							file.ModuleDef.Location = filename;
							file.Filename = filename;
							var modNode = fileTabManager.FileTreeView.FindNode(file.ModuleDef) as IModuleFileNode;
							Debug.Assert(modNode != null);
							if (modNode != null) {
								modNode.TreeNode.RefreshUI();
								fileTabManager.RefreshModifiedFile(modNode.DnSpyFile);
							}
						}
					}
				}
			}
			return allSaved;
		}
	}
}

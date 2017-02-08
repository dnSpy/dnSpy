/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Hex;

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

	[Export(typeof(IDocumentSaver))]
	sealed class DocumentSaver : IDocumentSaver {
		readonly Lazy<IUndoCommandService> undoCommandService;
		readonly Lazy<IMmapDisabler> mmapDisabler;
		readonly IAppWindow appWindow;
		readonly IDocumentTabService documentTabService;

		[ImportingConstructor]
		DocumentSaver(Lazy<IUndoCommandService> undoCommandService, Lazy<IMmapDisabler> mmapDisabler, IDocumentTabService documentTabService, IAppWindow appWindow) {
			this.undoCommandService = undoCommandService;
			this.mmapDisabler = mmapDisabler;
			this.documentTabService = documentTabService;
			this.appWindow = appWindow;
		}

		IEnumerable<object> Distinct(IEnumerable<object> objs) => undoCommandService.Value.GetUniqueDocuments(objs);

		public bool AskUserToSaveIfModified(IEnumerable<object> docs) {
			var modifiedDocs = Distinct(docs).Where(a => undoCommandService.Value.IsModified(undoCommandService.Value.GetUndoObject(a))).ToArray();
			if (modifiedDocs.Length == 0)
				return true;

			var msg = modifiedDocs.Length == 1 ? dnSpy_AsmEditor_Resources.AskSaveFile : dnSpy_AsmEditor_Resources.AskSaveFiles;
			var res = MsgBox.Instance.Show(msg, MsgBoxButton.Yes | MsgBoxButton.No);
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

				var document = objsAry[0] as IDsDocument;
				if (document != null) {
					var optsData = new SaveModuleOptionsVM(document);
					var optsWin = new SaveModuleOptionsDlg();
					optsWin.Owner = appWindow.MainWindow;
					optsWin.DataContext = optsData;
					var res = optsWin.ShowDialog();
					if (res != true)
						return false;
					options = optsData;
				}
				else {
					var buffer = objsAry[0] as HexBuffer;
					Debug.Assert(buffer != null);
					var optsData = new SaveHexOptionsVM(buffer);
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
					undoCommandService.Value.MarkAsSaved(undoCommandService.Value.GetUndoObject(doc));
					var document = doc as IDsDocument;
					if (document != null && string.IsNullOrEmpty(document.Filename)) {
						var filename = vm.GetSavedFileName(doc);
						if (!string.IsNullOrWhiteSpace(filename) && document.ModuleDef != null) {
							document.ModuleDef.Location = filename;
							document.Filename = filename;
							var modNode = documentTabService.DocumentTreeView.FindNode(document.ModuleDef) as ModuleDocumentNode;
							Debug.Assert(modNode != null);
							if (modNode != null) {
								modNode.TreeNode.RefreshUI();
								documentTabService.RefreshModifiedDocument(modNode.Document);
							}
						}
					}
				}
			}
			return allSaved;
		}
	}
}

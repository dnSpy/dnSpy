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
using System.ComponentModel.Composition;
using dnSpy.AsmEditor.Hex;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Files.Tabs;

namespace dnSpy.AsmEditor.UndoRedo {
	[ExportFileListListener]
	sealed class UndoRedoIFileListListener : IFileListListener {
		readonly Lazy<IUndoCommandManager> undoCommandManager;
		readonly Lazy<IHexDocumentManager> hexDocumentManager;
		readonly IMessageBoxManager messageBoxManager;

		public bool CanLoad {
			get { return true; }
		}

		public bool CanReload {
			get { return true; }
		}

		[ImportingConstructor]
		UndoRedoIFileListListener(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IHexDocumentManager> hexDocumentManager, IMessageBoxManager messageBoxManager) {
			this.undoCommandManager = undoCommandManager;
			this.hexDocumentManager = hexDocumentManager;
			this.messageBoxManager = messageBoxManager;
		}

		public void BeforeLoad(bool isReload) {
			undoCommandManager.Value.Clear();
			hexDocumentManager.Value.Clear();
		}

		public void AfterLoad(bool isReload) {
		}

		public bool CheckCanLoad(bool isReload) {
			int count = undoCommandManager.Value.NumberOfModifiedDocuments;
			if (count == 0)
				return true;

			var question = isReload ? dnSpy_AsmEditor_Resources.AskReloadAssembliesLoseChanges :
						dnSpy_AsmEditor_Resources.AskLoadAssembliesLoseChanges;

			var msg = count == 1 ? dnSpy_AsmEditor_Resources.UnsavedFile : string.Format(dnSpy_AsmEditor_Resources.UnsavedFiles, count);
			var res = messageBoxManager.Show(string.Format("{0} {1}", msg, question), MsgBoxButton.Yes | MsgBoxButton.No);
			return res == MsgBoxButton.Yes;
		}
	}
}

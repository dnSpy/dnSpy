/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Windows.Threading;
using dnSpy.AsmEditor.Hex;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Documents.Tabs;

namespace dnSpy.AsmEditor.UndoRedo {
	[ExportDocumentListListener]
	sealed class UndoRedoDocumentListListener : IDocumentListListener {
		readonly Lazy<IUndoCommandService> undoCommandService;
		readonly Lazy<IHexBufferService> hexBufferService;
		readonly IMessageBoxService messageBoxService;

		public bool CanLoad => true;
		public bool CanReload => true;

		[ImportingConstructor]
		UndoRedoDocumentListListener(Lazy<IUndoCommandService> undoCommandService, Lazy<IHexBufferService> hexBufferService, IMessageBoxService messageBoxService) {
			this.undoCommandService = undoCommandService;
			this.hexBufferService = hexBufferService;
			this.messageBoxService = messageBoxService;
		}

		public void BeforeLoad(bool isReload) {
			undoCommandService.Value.Clear();
			var buffersToDispose = hexBufferService.Value.Clear();
			if (buffersToDispose.Length != 0) {
				// Delay it since the hex views are still alive
				Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => {
					foreach (var buffer in buffersToDispose)
						buffer.Dispose();
				}));
			}
		}

		public void AfterLoad(bool isReload) { }

		public bool CheckCanLoad(bool isReload) {
			int count = undoCommandService.Value.NumberOfModifiedDocuments;
			if (count == 0)
				return true;

			var question = isReload ? dnSpy_AsmEditor_Resources.AskReloadAssembliesLoseChanges :
						dnSpy_AsmEditor_Resources.AskLoadAssembliesLoseChanges;

			var msg = count == 1 ? dnSpy_AsmEditor_Resources.UnsavedFile : string.Format(dnSpy_AsmEditor_Resources.UnsavedFiles, count);
			var res = messageBoxService.Show($"{msg} {question}", MsgBoxButton.Yes | MsgBoxButton.No);
			return res == MsgBoxButton.Yes;
		}
	}
}

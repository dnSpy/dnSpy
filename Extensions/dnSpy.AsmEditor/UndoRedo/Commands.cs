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
using System.ComponentModel;
using System.ComponentModel.Composition;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.AsmEditor.SaveModule;

namespace dnSpy.AsmEditor.UndoRedo {
	[ExportAutoLoaded]
	sealed class UndoRedoCommmandLoader : IAutoLoaded {
		readonly Lazy<IUndoCommandService> undoCommandService;
		readonly IMessageBoxService messageBoxService;
		readonly Lazy<IDocumentSaver> documentSaver;

		[ImportingConstructor]
		UndoRedoCommmandLoader(IWpfCommandService wpfCommandService, Lazy<IUndoCommandService> undoCommandService, IAppWindow appWindow, IMessageBoxService messageBoxService, Lazy<IDocumentSaver> documentSaver) {
			this.undoCommandService = undoCommandService;
			this.messageBoxService = messageBoxService;
			this.documentSaver = documentSaver;

			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);
			cmds.Add(UndoRoutedCommands.Undo, (s, e) => undoCommandService.Value.Undo(), (s, e) => e.CanExecute = undoCommandService.Value.CanUndo);
			cmds.Add(UndoRoutedCommands.Redo, (s, e) => undoCommandService.Value.Redo(), (s, e) => e.CanExecute = undoCommandService.Value.CanRedo);

			appWindow.MainWindowClosing += AppWindow_MainWindowClosing;
		}

		void AppWindow_MainWindowClosing(object sender, CancelEventArgs e) {
			var count = undoCommandService.Value.NumberOfModifiedDocuments;
			if (count == 0)
				return;
			var msg = count == 1 ? dnSpy_AsmEditor_Resources.AskExitUnsavedFile :
					string.Format(dnSpy_AsmEditor_Resources.AskExitUnsavedFiles, count);
			var res = messageBoxService.Show(msg, MsgBoxButton.Yes | MsgBoxButton.No);
			if (res == MsgBoxButton.Yes)
				documentSaver.Value.Save(undoCommandService.Value.GetModifiedDocuments());

			if (res == MsgBoxButton.None)
				e.Cancel = true;
		}
	}
}

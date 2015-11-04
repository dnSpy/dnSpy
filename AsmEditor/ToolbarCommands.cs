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
using System.Windows;
using System.Windows.Input;
using ICSharpCode.ILSpy;

namespace dnSpy.AsmEditor {
	[ExportToolbarCommand(ToolTip = "Undo (Ctrl+Z)",
						  ToolbarIcon = "Undo",
						  ToolbarCategory = "AsmEdit",
						  ToolbarOrder = 5000)]
	sealed class UndoAsmEdCommand : CommandWrapper {
		public UndoAsmEdCommand()
			: base(UndoCommandManagerLoader.Undo) {
		}
	}

	[ExportToolbarCommand(ToolTip = "Redo (Ctrl+Y)",
						  ToolbarIcon = "Redo",
						  ToolbarCategory = "AsmEdit",
						  ToolbarOrder = 5010)]
	sealed class RedoAsmEdCommand : CommandWrapper {
		public RedoAsmEdCommand()
			: base(UndoCommandManagerLoader.Redo) {
		}
	}

	[ExportToolbarCommand(ToolTip = "Clear Undo/Redo History",
						  ToolbarIcon = "DeleteHistory",
						  ToolbarCategory = "AsmEdit",
						  ToolbarOrder = 5020)]
	sealed class DeleteHistoryAsmEdCommand : ICommand {
		event EventHandler ICommand.CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		bool ICommand.CanExecute(object parameter) {
			return UndoCommandManager.Instance.CanUndo ||
				UndoCommandManager.Instance.CanRedo;
		}

		void ICommand.Execute(object parameter) {
			var res = MainWindow.Instance.ShowIgnorableMessageBox("undo: clear history", "Do you want to clear the undo/redo history?", MessageBoxButton.YesNo);
			if (res == null || res == MsgBoxButton.OK)
				UndoCommandManager.Instance.Clear();
		}
	}
}

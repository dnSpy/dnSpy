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

using System.Windows;
using dnSpy.Contracts.ToolBars;
using dnSpy.Shared.UI.ToolBars;
using ICSharpCode.ILSpy;

namespace dnSpy.AsmEditor {
	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, Icon = "Undo", ToolTip = "Undo (Ctrl+Z)", Group = ToolBarConstants.GROUP_APP_TB_MAIN_ASMED_UNDO, Order = 0)]
	sealed class UndoAsmEdCommand : ToolBarButtonCommand {
		public UndoAsmEdCommand()
			: base(UndoCommandManagerLoader.Undo) {
		}
	}

	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, Icon = "Redo", ToolTip = "Redo (Ctrl+Y)", Group = ToolBarConstants.GROUP_APP_TB_MAIN_ASMED_UNDO, Order = 10)]
	sealed class RedoAsmEdCommand : ToolBarButtonCommand {
		public RedoAsmEdCommand()
			: base(UndoCommandManagerLoader.Redo) {
		}
	}

	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, Icon = "DeleteHistory", ToolTip = "Clear Undo/Redo History", Group = ToolBarConstants.GROUP_APP_TB_MAIN_ASMED_UNDO, Order = 20)]
	sealed class DeleteHistoryAsmEdCommand : ToolBarButtonBase {
		public override bool IsEnabled(IToolBarItemContext context) {
			return UndoCommandManager.Instance.CanUndo ||
				UndoCommandManager.Instance.CanRedo;
		}

		public override void Execute(IToolBarItemContext context) {
			var res = MainWindow.Instance.ShowIgnorableMessageBox("undo: clear history", "Do you want to clear the undo/redo history?", MessageBoxButton.YesNo);
			if (res == null || res == MsgBoxButton.OK)
				UndoCommandManager.Instance.Clear();
		}
	}
}

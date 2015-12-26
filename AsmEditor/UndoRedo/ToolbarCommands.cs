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
using System.ComponentModel.Composition;
using dnSpy.Contracts.App;
using dnSpy.Contracts.ToolBars;
using dnSpy.Shared.UI.ToolBars;

namespace dnSpy.AsmEditor.UndoRedo {
	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, Icon = "Undo", ToolTip = "Undo (Ctrl+Z)", Group = ToolBarConstants.GROUP_APP_TB_MAIN_ASMED_UNDO, Order = 0)]
	sealed class UndoAsmEdCommand : ToolBarButtonCommand {
		public UndoAsmEdCommand()
			: base(UndoRoutedCommands.Undo) {
		}
	}

	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, Icon = "Redo", ToolTip = "Redo (Ctrl+Y)", Group = ToolBarConstants.GROUP_APP_TB_MAIN_ASMED_UNDO, Order = 10)]
	sealed class RedoAsmEdCommand : ToolBarButtonCommand {
		public RedoAsmEdCommand()
			: base(UndoRoutedCommands.Redo) {
		}
	}

	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, Icon = "DeleteHistory", ToolTip = "Clear Undo/Redo History", Group = ToolBarConstants.GROUP_APP_TB_MAIN_ASMED_UNDO, Order = 20)]
	sealed class DeleteHistoryAsmEdCommand : ToolBarButtonBase {
		readonly Lazy<IUndoCommandManager> undoCommandManager;
		readonly IMessageBoxManager messageBoxManager;

		[ImportingConstructor]
		DeleteHistoryAsmEdCommand(Lazy<IUndoCommandManager> undoCommandManager, IMessageBoxManager messageBoxManager) {
			this.undoCommandManager = undoCommandManager;
			this.messageBoxManager = messageBoxManager;
		}

		public override bool IsEnabled(IToolBarItemContext context) {
			return undoCommandManager.Value.CanUndo ||
				undoCommandManager.Value.CanRedo;
		}

		public override void Execute(IToolBarItemContext context) {
			var res = messageBoxManager.ShowIgnorableMessage("undo: clear history", "Do you want to clear the undo/redo history?", MsgBoxButton.Yes | MsgBoxButton.No);
			if (res == null || res == MsgBoxButton.Yes)
				undoCommandManager.Value.Clear();
		}
	}
}

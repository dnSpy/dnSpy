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
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.App;
using dnSpy.Contracts.ToolBars;

namespace dnSpy.AsmEditor.UndoRedo {
	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, Icon = "Undo", ToolTip = "res:UndoToolBarToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_ASMED_UNDO, Order = 0)]
	sealed class UndoAsmEdCommand : ToolBarButtonCommand {
		public UndoAsmEdCommand()
			: base(UndoRoutedCommands.Undo) {
		}
	}

	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, Icon = "Redo", ToolTip = "res:RedoToolBarToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_ASMED_UNDO, Order = 10)]
	sealed class RedoAsmEdCommand : ToolBarButtonCommand {
		public RedoAsmEdCommand()
			: base(UndoRoutedCommands.Redo) {
		}
	}

	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, Icon = "DeleteHistory", ToolTip = "res:ClearHistoryToolBarToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_ASMED_UNDO, Order = 20)]
	sealed class DeleteHistoryAsmEdCommand : ToolBarButtonBase {
		readonly Lazy<IUndoCommandService> undoCommandService;
		readonly IMessageBoxService messageBoxService;

		[ImportingConstructor]
		DeleteHistoryAsmEdCommand(Lazy<IUndoCommandService> undoCommandService, IMessageBoxService messageBoxService) {
			this.undoCommandService = undoCommandService;
			this.messageBoxService = messageBoxService;
		}

		public override bool IsEnabled(IToolBarItemContext context) =>
			undoCommandService.Value.CanUndo || undoCommandService.Value.CanRedo;

		public override void Execute(IToolBarItemContext context) {
			var res = messageBoxService.ShowIgnorableMessage(new Guid("FC8FC68F-4285-4CDF-BEC0-FF6498EEC4AA"), dnSpy_AsmEditor_Resources.AskClearUndoHistory, MsgBoxButton.Yes | MsgBoxButton.No);
			if (res == null || res == MsgBoxButton.Yes)
				undoCommandService.Value.Clear();
		}
	}
}

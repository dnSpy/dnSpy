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

using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.ToolBars;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.UndoRedo {
	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, Icon = DsImagesAttribute.Undo, Group = ToolBarConstants.GROUP_APP_TB_MAIN_ASMED_UNDO, Order = 0)]
	sealed class UndoAsmEdCommand : ToolBarButtonCommand {
		public UndoAsmEdCommand()
			: base(UndoRoutedCommands.Undo) {
		}
		public override string GetToolTip(IToolBarItemContext context) => ToolTipHelper.AddKeyboardShortcut(dnSpy_AsmEditor_Resources.UndoToolBarToolTip, dnSpy_AsmEditor_Resources.ShortCutKeyCtrlZ);
	}

	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, Icon = DsImagesAttribute.Redo, Group = ToolBarConstants.GROUP_APP_TB_MAIN_ASMED_UNDO, Order = 10)]
	sealed class RedoAsmEdCommand : ToolBarButtonCommand {
		public RedoAsmEdCommand()
			: base(UndoRoutedCommands.Redo) {
		}
		public override string GetToolTip(IToolBarItemContext context) => ToolTipHelper.AddKeyboardShortcut(dnSpy_AsmEditor_Resources.RedoToolBarToolTip, dnSpy_AsmEditor_Resources.ShortCutKeyCtrlY);
	}
}

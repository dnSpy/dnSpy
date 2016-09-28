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

using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;

namespace dnSpy.AsmEditor.UndoRedo {
	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:UndoCommand", InputGestureText = "res:ShortCutKeyCtrlZ", Icon = DsImagesAttribute.Undo, Group = MenuConstants.GROUP_APP_MENU_EDIT_UNDO, Order = 0)]
	sealed class UndoMainMenuEntryCommand : MenuItemCommand {
		public UndoMainMenuEntryCommand()
			: base(UndoRoutedCommands.Undo) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:RedoCommand", InputGestureText = "res:ShortCutKeyCtrlY", Icon = DsImagesAttribute.Redo, Group = MenuConstants.GROUP_APP_MENU_EDIT_UNDO, Order = 10)]
	sealed class RedoMainMenuEntryCommand : MenuItemCommand {
		public RedoMainMenuEntryCommand()
			: base(UndoRoutedCommands.Redo) {
		}
	}
}

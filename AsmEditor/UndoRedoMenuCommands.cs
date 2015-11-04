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

using dnSpy.Contracts.Menus;
using dnSpy.Menus;

namespace dnSpy.AsmEditor {
	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Undo", InputGestureText = "Ctrl+Z", Icon = "Undo", Group = MenuConstants.GROUP_APP_MENU_EDIT_UNDO, Order = 0)]
	sealed class UndoMainMenuEntryCommand : MenuItemCommand {
		public UndoMainMenuEntryCommand()
			: base(UndoCommandManagerLoader.Undo) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "Redo", InputGestureText = "Ctrl+Y", Icon = "Redo", Group = MenuConstants.GROUP_APP_MENU_EDIT_UNDO, Order = 10)]
	sealed class RedoMainMenuEntryCommand : MenuItemCommand {
		public RedoMainMenuEntryCommand()
			: base(UndoCommandManagerLoader.Redo) {
		}
	}
}

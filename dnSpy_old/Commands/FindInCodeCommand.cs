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
using System.Windows.Input;
using dnSpy.Contracts.Menus;
using dnSpy.Menus;
using dnSpy.Shared.UI.Menus;
using ICSharpCode.ILSpy;

namespace dnSpy.Commands {
	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "_Find", Icon = "Find", InputGestureText = "Ctrl+F", Group = MenuConstants.GROUP_APP_MENU_EDIT_FIND, Order = 0)]
	sealed class FindInCodeCommand : MenuItemCommand {
		public FindInCodeCommand()
			: base(ApplicationCommands.Find) {
		}
	}

	[ExportMenuItem(Header = "Find", Icon = "Find", InputGestureText = "Ctrl+F", Group = MenuConstants.GROUP_CTX_CODE_EDITOR, Order = 10)]
	sealed class FindInCodeContexMenuEntry : MenuItemBase {
		public override void Execute(IMenuItemContext context) {
			if (ApplicationCommands.Find.CanExecute(null, MainWindow.Instance))
				ApplicationCommands.Find.Execute(null, MainWindow.Instance);
		}

		public override bool IsVisible(IMenuItemContext context) {
			return context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DECOMPILED_CODE_GUID);
		}
	}
}

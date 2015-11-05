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
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts;
using dnSpy.Contracts.Menus;
using dnSpy.Shared.UI.Menus;

namespace dnSpy.Themes {
	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_THEMES_GUID, Group = MenuConstants.GROUP_APP_MENU_THEMES_THEMES, Order = 0)]
	sealed class ThemesMenu : MenuItemBase, IMenuItemCreator {
		public override void Execute(IMenuItemContext context) {
		}

		sealed class MyMenuItem : MenuItemBase {
			readonly Action<IMenuItemContext> action;
			readonly bool isChecked;

			public MyMenuItem(Action<IMenuItemContext> action, bool isChecked) {
				this.action = action;
				this.isChecked = isChecked;
			}

			public override void Execute(IMenuItemContext context) {
				action(context);
			}

			public override bool IsChecked(IMenuItemContext context) {
				return isChecked;
			}
		}

		public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
			var mgr = Globals.App.ThemesManager as ThemesManager;
			Debug.Assert(mgr != null);
			if (mgr == null)
				yield break;
			foreach (var theme in mgr.AllThemesSorted) {
				var attr = new ExportMenuItemAttribute { Header = theme.MenuName };
				var tmp = theme;
				var item = new MyMenuItem(ctx => mgr.Theme = tmp, theme == mgr.Theme);
				yield return new CreatedMenuItem(attr, item);
			}
		}
	}
}

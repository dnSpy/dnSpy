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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Menus;

namespace dnSpy.Themes {
	static class ThemesConstants {
		public const string THEMES_GUID = "D34C16A1-1940-4EAD-A4CD-3E00148E5FB3";
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Guid = ThemesConstants.THEMES_GUID, Header = "res:Menu_Themes", Group = MenuConstants.GROUP_APP_MENU_VIEW_OPTS, Order = 9000)]
	sealed class ThemesMenuItem : MenuItemBase {
		public override void Execute(IMenuItemContext context) => Debug.Fail("Shouldn't execute");
	}

	[ExportMenuItem(OwnerGuid = ThemesConstants.THEMES_GUID, Group = MenuConstants.GROUP_APP_MENU_THEMES_THEMES, Order = 0)]
	sealed class ThemesMenu : MenuItemBase, IMenuItemProvider {
		readonly IThemeServiceImpl themeService;

		[ImportingConstructor]
		ThemesMenu(IThemeServiceImpl themeService) => this.themeService = themeService;

		public override void Execute(IMenuItemContext context) { }

		sealed class MyMenuItem : MenuItemBase {
			readonly Action<IMenuItemContext> action;
			readonly bool isChecked;

			public MyMenuItem(Action<IMenuItemContext> action, bool isChecked) {
				this.action = action;
				this.isChecked = isChecked;
			}

			public override void Execute(IMenuItemContext context) => action(context);

			public override bool IsChecked(IMenuItemContext context) => isChecked;
		}

		public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
			foreach (var theme in themeService.VisibleThemes) {
				var attr = new ExportMenuItemAttribute { Header = theme.GetMenuName() };
				var tmp = theme;
				var item = new MyMenuItem(ctx => themeService.Theme = tmp, theme == themeService.Theme);
				yield return new CreatedMenuItem(attr, item);
			}
		}
	}
}

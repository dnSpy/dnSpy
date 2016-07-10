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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Themes;
using dnSpy.Properties;

namespace dnSpy.Themes {
	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_THEMES_GUID, Group = MenuConstants.GROUP_APP_MENU_THEMES_THEMES, Order = 0)]
	sealed class ThemesMenu : MenuItemBase, IMenuItemCreator {
		readonly ThemeManager themeManager;

		[ImportingConstructor]
		ThemesMenu(ThemeManager themeManager) {
			this.themeManager = themeManager;
		}

		public override void Execute(IMenuItemContext context) { }

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

			public override bool IsChecked(IMenuItemContext context) => isChecked;
		}

		public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
			foreach (var theme in themeManager.AllThemesSorted) {
				if (!themeManager.Settings.ShowAllThemes && !themeManager.IsHighContrast && theme.IsHighContrast)
					continue;
				var attr = new ExportMenuItemAttribute { Header = GetThemeHeaderName(theme) };
				var tmp = theme;
				var item = new MyMenuItem(ctx => themeManager.Theme = tmp, theme == themeManager.Theme);
				yield return new CreatedMenuItem(attr, item);
			}
		}

		static string GetThemeHeaderName(ITheme theme) {
			if (theme.Guid == ThemeConstants.THEME_HIGHCONTRAST_GUID)
				return dnSpy_Resources.Theme_HighContrast;
			if (theme.Guid == ThemeConstants.THEME_BLUE_GUID)
				return dnSpy_Resources.Theme_Blue;
			if (theme.Guid == ThemeConstants.THEME_DARK_GUID)
				return dnSpy_Resources.Theme_Dark;
			if (theme.Guid == ThemeConstants.THEME_LIGHT_GUID)
				return dnSpy_Resources.Theme_Light;
			return theme.MenuName;
		}
	}
}

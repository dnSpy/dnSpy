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

using dnSpy.Contracts.Themes;
using dnSpy.Properties;

namespace dnSpy.Themes {
	static class ThemeHelpers {
		public static string GetMenuName(this ITheme theme) {
			if (theme.Guid == ThemeConstants.THEME_HIGHCONTRAST_GUID)
				return dnSpy_Resources.Theme_HighContrast;
			if (theme.Guid == ThemeConstants.THEME_BLUE_GUID)
				return dnSpy_Resources.Theme_Blue;
			if (theme.Guid == ThemeConstants.THEME_DARK_GUID)
				return dnSpy_Resources.Theme_Dark;
			if (theme.Guid == ThemeConstants.THEME_LIGHT_GUID)
				return dnSpy_Resources.Theme_Light;
			var name = theme.MenuName;
			if (!string.IsNullOrWhiteSpace(name))
				return name;
			name = theme.Name;
			if (!string.IsNullOrWhiteSpace(name))
				return name;
			return theme.Guid.ToString();
		}

		public static string GetName(this ITheme theme) {
			var name = theme.GetMenuName();
			const string UNDERSCORE_TMP = "<<<<<<>>>>>>";
			return name.Replace("__", UNDERSCORE_TMP).Replace("_", "").Replace(UNDERSCORE_TMP, "_");
		}
	}
}

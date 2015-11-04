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
using System.IO;
using System.Linq;
using System.Security;
using System.Windows;
using System.Xml.Linq;
using dnSpy.Contracts.Menus;
using dnSpy.Menus;
using Microsoft.Win32;

namespace dnSpy.DnTheme {
	public sealed class HighContrastEventArgs : EventArgs {
		public bool IsHighContrast { get; private set; }

		public HighContrastEventArgs(bool isHighContrast) {
			this.IsHighContrast = isHighContrast;
		}
	}

	public static class Themes {
		static Dictionary<string, Theme> themes = new Dictionary<string, Theme>();

		static Theme theme;
		public static Theme Theme {
			get { return theme; }
			set {
				if (theme != value) {
					theme = value;
					if (ThemeChanged != null)
						ThemeChanged(null, EventArgs.Empty);
				}
			}
		}

		public static event EventHandler<EventArgs> ThemeChanged;
		public static event EventHandler<HighContrastEventArgs> IsHighContrastChanged;

		public static IEnumerable<Theme> AllThemesSorted {
			get { return themes.Values.OrderBy(x => x.Sort); }
		}

		public static string DefaultThemeName {
			get { return "dark"; }
		}

		public static string DefaultHighContrastThemeName {
			get { return "hc"; }
		}

		public static string CurrentDefaultThemeName {
			get { return IsHighContrast ? DefaultHighContrastThemeName : DefaultThemeName; }
		}

		public static bool IsHighContrast {
			get { return isHighContrast; }
			private set {
				if (isHighContrast != value) {
					isHighContrast = value;
					if (IsHighContrastChanged != null)
						IsHighContrastChanged(null, new HighContrastEventArgs(IsHighContrast));
				}
			}
		}
		static bool isHighContrast;

		static Themes() {
			Load();
			SystemEvents.UserPreferenceChanged += (s, e) => IsHighContrast = SystemParameters.HighContrast;
			IsHighContrast = SystemParameters.HighContrast;
		}

		static void Load() {
			foreach (var basePath in GetDnthemePaths()) {
				string[] files;
				try {
					if (!Directory.Exists(basePath))
						continue;
					files = Directory.GetFiles(basePath, "*.dntheme", SearchOption.TopDirectoryOnly);
				}
				catch (IOException) {
					continue;
				}
				catch (UnauthorizedAccessException) {
					continue;
				}
				catch (SecurityException) {
					continue;
				}

				foreach (var filename in files)
					Load(filename);
			}
		}

		static IEnumerable<string> GetDnthemePaths() {
			yield return Path.Combine(Path.GetDirectoryName(typeof(Themes).Assembly.Location), "DnTheme");
			yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "dnSpy", "DnTheme");
		}

		static Theme Load(string filename) {
			try {
				var root = XDocument.Load(filename).Root;
				if (root.Name != "theme")
					return null;

				var theme = new Theme(root);
				if (string.IsNullOrEmpty(theme.Name) || string.IsNullOrEmpty(theme.MenuName))
					return null;

				themes[theme.Name] = theme;
				return theme;
			}
			catch (Exception) {
				Debug.Fail(string.Format("Failed to load file '{0}'", filename));
			}
			return null;
		}

		public static Theme GetThemeOrDefault(string name) {
			var theme = themes[name] ?? themes[DefaultThemeName] ?? AllThemesSorted.FirstOrDefault();
			Debug.Assert(theme != null);
			return theme;
		}

		public static void SwitchThemeIfNecessary() {
			if (Theme.IsHighContrast != IsHighContrast)
				Theme = GetThemeOrDefault(CurrentDefaultThemeName);
		}
	}

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
			foreach (var theme in Themes.AllThemesSorted) {
				var attr = new ExportMenuItemAttribute { Header = theme.MenuName };
				var tmp = theme;
				var item = new MyMenuItem(ctx => Themes.Theme = tmp, theme == Themes.Theme);
				yield return new CreatedMenuItem(attr, item);
			}
		}
	}
}

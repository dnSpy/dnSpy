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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ICSharpCode.ILSpy.dntheme
{
	public static class Themes
	{
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

		public static IEnumerable<Theme> AllThemesSorted {
			get { return themes.Values.OrderBy(x => x.Sort); }
		}

		static Themes()
		{
			Load();
		}

		static void Load()
		{
			var path = Path.Combine(Path.GetDirectoryName(typeof(Themes).Assembly.Location), "dntheme");
			foreach (var filename in Directory.GetFiles(path, "*.dntheme", SearchOption.TopDirectoryOnly))
				Load(filename);
		}

		public static Theme GetThemeOrDefault(string name)
		{
			return themes[name] ?? AllThemesSorted.FirstOrDefault();
		}

		static Theme Load(string filename)
		{
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
			}
			return null;
		}
	}

	[ExportMainMenuCommand(Menu = "_Themes", MenuCategory = "Themes", MenuOrder = 4000)]
	class ThemesMenu : ICommand, IMenuItemProvider
	{
		public ThemesMenu()
		{
			Themes.ThemeChanged += (s, e) => UpdateThemesMenu();
		}

		void UpdateThemesMenu()
		{
			MainWindow.Instance.UpdateMainSubMenu("_Themes");
		}

		public bool CanExecute(object parameter)
		{
			return false;
		}

		public event EventHandler CanExecuteChanged {
			add { }
			remove { }
		}

		public void Execute(object parameter)
		{
		}

		public IEnumerable<MenuItem> CreateMenuItems(MenuItem cachedMenuItem)
		{
			int index = 0;
			foreach (var theme in Themes.AllThemesSorted) {
				var item = index++ == 0 ? cachedMenuItem : new MenuItem();
				item.Header = theme.MenuName;
				item.IsChecked = theme == Themes.Theme;
				item.Command = new SetThemeCommand(theme);
				yield return item;
			}
		}

		sealed class SetThemeCommand : ICommand
		{
			readonly Theme theme;

			public SetThemeCommand(Theme theme)
			{
				this.theme = theme;
			}

			public bool CanExecute(object parameter)
			{
				return true;
			}

			public event EventHandler CanExecuteChanged {
				add { }
				remove { }
			}

			public void Execute(object parameter)
			{
				Themes.Theme = theme;
			}
		}
	}
}

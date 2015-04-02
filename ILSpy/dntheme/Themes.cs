
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
	static class Themes
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

	[ExportMainMenuCommand(Menu = "_Themes", Header = "_Options", MenuCategory = "Options", MenuOrder = 3000)]
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

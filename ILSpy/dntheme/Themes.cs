
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ICSharpCode.ILSpy.dntheme
{
	static class Themes
	{
		static Dictionary<string, Theme> themes = new Dictionary<string, Theme>();

		public static IEnumerable<Theme> AllThemes {
			get { return themes.Values; }
		}

		static Themes() {
			Load();
		}

		static void Load()
		{
			var path = Path.Combine(Path.GetDirectoryName(typeof(Themes).Assembly.Location), "dntheme");
			foreach (var filename in Directory.GetFiles(path, "*.dntheme", SearchOption.TopDirectoryOnly))
				Load(filename);
		}

		public static Theme GetTheme(string name)
		{
			return themes[name];
		}

		public static Theme GetThemeOrDefault(string name)
		{
			return GetTheme(name) ?? GetDefaultTheme();
		}

		public static Theme GetDefaultTheme()
		{
			return AllThemes.FirstOrDefault();
		}

		public static Theme Load(string filename)
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
}

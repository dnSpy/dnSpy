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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Windows;
using System.Xml.Linq;
using dnSpy.Contracts.Themes;
using dnSpy.Events;
using Microsoft.Win32;

namespace dnSpy.Themes {
	[Export, Export(typeof(IThemeManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class ThemeManager : IThemeManager {
		readonly Dictionary<string, Theme> themes;

		public ITheme Theme {
			get { return theme; }
			internal set {
				if (theme != value) {
					theme = value;
					themeChanged.Raise(this, new ThemeChangedEventArgs());
				}
			}
		}
		ITheme theme;

		public IEnumerable<ITheme> AllThemesSorted {
			get { return themes.Values.OrderBy(x => x.Order); }
		}

		string IThemeManager.DefaultThemeName {
			get { return DefaultThemeName; }
		}

		bool IsHighContrast {
			get { return isHighContrast; }
			set {
				if (isHighContrast != value) {
					isHighContrast = value;
					SwitchThemeIfNecessary();
				}
			}
		}
		bool isHighContrast;

		public event EventHandler<ThemeChangedEventArgs> ThemeChanged {
			add { themeChanged.Add(value); }
			remove { themeChanged.Remove(value); }
		}
		readonly WeakEventList<ThemeChangedEventArgs> themeChanged;

		ThemeManager() {
			this.themeChanged = new WeakEventList<ThemeChangedEventArgs>();
			this.themes = new Dictionary<string, Theme>();
			Load();
			Debug.Assert(themes.Count != 0);
			SystemEvents.UserPreferenceChanged += (s, e) => IsHighContrast = SystemParameters.HighContrast;
			IsHighContrast = SystemParameters.HighContrast;
		}

		string CurrentDefaultThemeName {
			get { return IsHighContrast ? DefaultHighContrastThemeName : DefaultThemeName; }
		}
		const string DefaultThemeName = "dark";
		const string DefaultHighContrastThemeName = "hc";

		ITheme GetThemeOrDefault(string name) {
			return themes[name] ?? themes[DefaultThemeName] ?? AllThemesSorted.FirstOrDefault();
		}

		void SwitchThemeIfNecessary() {
			if (Theme == null || Theme.IsHighContrast != IsHighContrast)
				Theme = GetThemeOrDefault(CurrentDefaultThemeName);
		}

		void Load() {
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

		const string DNTHEME_DIR = "Themes";
		IEnumerable<string> GetDnthemePaths() {
			yield return Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), DNTHEME_DIR);
			yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "dnSpy", DNTHEME_DIR);
		}

		Theme Load(string filename) {
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

		internal void Initialize(string themeName) {
			var theme = GetThemeOrDefault(themeName);
			if (theme.IsHighContrast != IsHighContrast)
				theme = GetThemeOrDefault(CurrentDefaultThemeName) ?? theme;
			Theme = theme;
		}
	}
}

/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.App;
using dnSpy.Contracts.Themes;
using dnSpy.Events;
using Microsoft.Win32;

namespace dnSpy.Themes {
	interface IThemeServiceImpl : IThemeService {
		IEnumerable<ITheme> AllThemes { get; }
		IEnumerable<ITheme> VisibleThemes { get; }
		new ITheme Theme { get; set; }
	}

	[Export(typeof(IThemeService)), Export(typeof(IThemeServiceImpl))]
	sealed class ThemeService : IThemeServiceImpl {
		readonly Dictionary<Guid, Theme> themes;

		public ITheme Theme {
			get => theme;
			set {
				if (theme != value) {
					theme = value;
					themeSettings.ThemeGuid = value.Guid;
					InitializeResources();
					themeChangedHighPriority.Raise(this, new ThemeChangedEventArgs());
					themeChanged.Raise(this, new ThemeChangedEventArgs());
					themeChangedLowPriority.Raise(this, new ThemeChangedEventArgs());
				}
			}
		}
		ITheme theme;

		public IEnumerable<ITheme> AllThemes => themes.Values.OrderBy(x => x.Order);

		public IEnumerable<ITheme> VisibleThemes {
			get {
				foreach (var theme in AllThemes) {
					if (!Settings.ShowAllThemes && !IsHighContrast && theme.IsHighContrast)
						continue;
					yield return theme;
				}
			}
		}

		public bool IsHighContrast {
			get => isHighContrast;
			set {
				if (isHighContrast != value) {
					isHighContrast = value;
					SwitchThemeIfNecessary();
				}
			}
		}
		bool isHighContrast;

		public event EventHandler<ThemeChangedEventArgs> ThemeChangedHighPriority {
			add => themeChangedHighPriority.Add(value);
			remove => themeChangedHighPriority.Remove(value);
		}
		readonly WeakEventList<ThemeChangedEventArgs> themeChangedHighPriority;

		public event EventHandler<ThemeChangedEventArgs> ThemeChanged {
			add => themeChanged.Add(value);
			remove => themeChanged.Remove(value);
		}
		readonly WeakEventList<ThemeChangedEventArgs> themeChanged;

		public event EventHandler<ThemeChangedEventArgs> ThemeChangedLowPriority {
			add => themeChangedLowPriority.Add(value);
			remove => themeChangedLowPriority.Remove(value);
		}
		readonly WeakEventList<ThemeChangedEventArgs> themeChangedLowPriority;

		public ThemeSettings Settings => themeSettings;
		readonly ThemeSettings themeSettings;

		[ImportingConstructor]
		ThemeService(ThemeSettings themeSettings) {
			this.themeSettings = themeSettings;
			themeChangedHighPriority = new WeakEventList<ThemeChangedEventArgs>();
			themeChanged = new WeakEventList<ThemeChangedEventArgs>();
			themeChangedLowPriority = new WeakEventList<ThemeChangedEventArgs>();
			themes = new Dictionary<Guid, Theme>();
			Load();
			Debug.Assert(themes.Count != 0);
			SystemEvents.UserPreferenceChanged += (s, e) => IsHighContrast = SystemParameters.HighContrast;
			IsHighContrast = SystemParameters.HighContrast;
			Initialize(themeSettings.ThemeGuid ?? DefaultThemeGuid);
		}

		void InitializeResources() {
			var app = Application.Current;
			Debug.Assert(app != null);
			if (app != null)
				((Theme)Theme).UpdateResources(app.Resources);
		}

		Guid CurrentDefaultThemeGuid => IsHighContrast ? DefaultHighContrastThemeGuid : DefaultThemeGuid;
		static readonly Guid DefaultThemeGuid = ThemeConstants.THEME_DARK_GUID;
		static readonly Guid DefaultHighContrastThemeGuid = ThemeConstants.THEME_HIGHCONTRAST_GUID;

		ITheme GetThemeOrDefault(Guid guid) {
			if (themes.TryGetValue(guid, out var theme))
				return theme;
			if (themes.TryGetValue(DefaultThemeGuid, out theme))
				return theme;
			return AllThemes.FirstOrDefault();
		}

		void SwitchThemeIfNecessary() {
			if (Theme == null || Theme.IsHighContrast != IsHighContrast)
				Theme = GetThemeOrDefault(CurrentDefaultThemeGuid);
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

		IEnumerable<string> GetDnthemePaths() => AppDirectories.GetDirectories("Themes");

		Theme Load(string filename) {
			try {
				var root = XDocument.Load(filename).Root;
				if (root.Name != "theme")
					return null;

				var theme = new Theme(root);
				if (string.IsNullOrEmpty(theme.MenuName))
					return null;

				themes[theme.Guid] = theme;
				return theme;
			}
			catch (Exception) {
				Debug.Fail($"Failed to load file '{filename}'");
			}
			return null;
		}

		void Initialize(Guid themeGuid) {
			var theme = GetThemeOrDefault(themeGuid);
			if (theme.IsHighContrast != IsHighContrast)
				theme = GetThemeOrDefault(CurrentDefaultThemeGuid) ?? theme;
			Theme = theme;
		}
	}
}

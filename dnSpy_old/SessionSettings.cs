// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using dnSpy.Contracts;
using dnSpy.Contracts.Settings;
using dnSpy.Tabs;

namespace ICSharpCode.ILSpy {
	/// <summary>
	/// Per-session setting:
	/// Loaded at startup; saved at exit.
	/// </summary>
	public sealed class SessionSettings : INotifyPropertyChanged {
		const string SETTINGS_NAME = "66D5A354-FF55-4FD6-8F30-2D0C3C8250EC";

		public SessionSettings() {
			var section = DnSpy.App.SettingsManager.GetOrCreateSection(SETTINGS_NAME);

			this.FilterSettings = new FilterSettings();

			this.ActiveAssemblyList = section.Attribute<string>("ActiveAssemblyList");

			this.WindowState = section.Attribute<WindowState?>("WindowState") ?? WindowState.Normal;
			this.IsFullScreen = section.Attribute<bool?>("IsFullScreen") ?? false;
			var winBoundsString = section.Attribute<Rect?>("WindowBounds");
			if (winBoundsString != null)
				this.WindowBounds = winBoundsString.Value;
			this.LeftColumnWidth = section.Attribute<double?>("LeftColumnWidth") ?? 0.0;
			this.WordWrap = section.Attribute<bool?>("WordWrap") ?? false;
			this.HighlightCurrentLine = section.Attribute<bool?>("HighlightCurrentLine") ?? true;
			this.TopPaneSettings.Name = section.Attribute<string>("TopPaneName") ?? string.Empty;
			this.TopPaneSettings.Height = section.Attribute<double?>("TopPaneHeight") ?? 200.0;
			this.BottomPaneSettings.Name = section.Attribute<string>("BottomPaneName") ?? string.Empty;
			this.BottomPaneSettings.Height = section.Attribute<double?>("BottomPaneHeight") ?? 200.0;
			this.ThemeName = section.Attribute<string>("ThemeName") ?? DnSpy.App.ThemeManager.DefaultThemeName;

			var ignoreSection = section.TryGetSection("IgnoredWarnings");
			if (ignoreSection != null) {
				foreach (var warning in ignoreSection.SectionsWithName("Warning")) {
					var id = warning.Attribute<string>("id");
					if (id != null)
						IgnoredWarnings.Add(id);
				}
			}

			var groups = section.TryGetSection("TabGroups");
			if (groups == null) {
				this.TabsFound = false;
				this.SavedTabGroupsState = new SavedTabGroupsState();
			}
			else {
				this.TabsFound = true;
				this.SavedTabGroupsState = SavedTabGroupsState.Read(groups);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string propertyName) {
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public FilterSettings FilterSettings { get; private set; }

		public string ActiveAssemblyList;

		public WindowState WindowState = WindowState.Normal;
		public bool IsFullScreen;
		public Rect? WindowBounds;
		internal static Rect DefaultWindowBounds = new Rect(10, 10, 1300, 730);
		public double LeftColumnWidth;
		public PaneSettings TopPaneSettings;
		public PaneSettings BottomPaneSettings;
		public string ThemeName;
		public HashSet<string> IgnoredWarnings = new HashSet<string>();

		public SavedTabGroupsState SavedTabGroupsState;
		public bool TabsFound;

		public bool WordWrap {
			get { return wordWrap; }
			set {
				if (wordWrap != value) {
					wordWrap = value;
					OnPropertyChanged("WordWrap");
				}
			}
		}
		bool wordWrap;

		public bool HighlightCurrentLine {
			get { return highlightCurrentLine; }
			set {
				if (highlightCurrentLine != value) {
					highlightCurrentLine = value;
					OnPropertyChanged("HighlightCurrentLine");
				}
			}
		}
		bool highlightCurrentLine;

		public struct PaneSettings {
			public double Height;
			public string Name;
		}

		public void Save() {
			var section = DnSpy.App.SettingsManager.CreateSection(SETTINGS_NAME);
			if (this.ActiveAssemblyList != null)
				section.Attribute("ActiveAssemblyList", this.ActiveAssemblyList);
			section.Attribute("WindowState", this.WindowState);
			section.Attribute("IsFullScreen", this.IsFullScreen);
			if (this.WindowBounds != null)
				section.Attribute("WindowBounds", this.WindowBounds);
			section.Attribute("WordWrap", this.WordWrap);
			section.Attribute("HighlightCurrentLine", this.HighlightCurrentLine);
			section.Attribute("LeftColumnWidth", this.LeftColumnWidth);
			section.Attribute("TopPaneHeight", this.TopPaneSettings.Height);
			section.Attribute("TopPaneName", this.TopPaneSettings.Name);
			section.Attribute("BottomPaneHeight", this.BottomPaneSettings.Height);
			section.Attribute("BottomPaneName", this.BottomPaneSettings.Name);
			section.Attribute("ThemeName", this.ThemeName);
			if (IgnoredWarnings.Count != 0) {
				var ignoredSection = section.CreateSection("IgnoredWarnings");
				foreach (var id in IgnoredWarnings)
					ignoredSection.CreateSection("Warning").Attribute("id", id);
			}

			if (ICSharpCode.ILSpy.Options.DisplaySettingsPanel.CurrentDisplaySettings.RestoreTabsAtStartup)
				SavedTabGroupsState.Write(section.CreateSection("TabGroups"));
		}
	}
}

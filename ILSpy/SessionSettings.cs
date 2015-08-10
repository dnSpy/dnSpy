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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml.Linq;
using dnSpy.Tabs;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Per-session setting:
	/// Loaded at startup; saved at exit.
	/// </summary>
	public sealed class SessionSettings : INotifyPropertyChanged
	{
		public SessionSettings(ILSpySettings spySettings)
		{
			XElement doc = spySettings["SessionSettings"];
			
			XElement filterSettings = doc.Element("FilterSettings");
			if (filterSettings == null) filterSettings = new XElement("FilterSettings");
			
			this.FilterSettings = new FilterSettings(filterSettings);
			
			this.ActiveAssemblyList = Unescape((string)doc.Element("ActiveAssemblyList"));
			
			this.WindowState = FromString((string)doc.Element("WindowState"), WindowState.Normal);
			this.IsFullScreen = FromString((string)doc.Element("IsFullScreen"), false);
			var winBoundsString = (string)doc.Element("WindowBounds");
			if (winBoundsString != null)
				this.WindowBounds = FromString(winBoundsString, DefaultWindowBounds);
			this.LeftColumnWidth = FromString((string)doc.Element("LeftColumnWidth"), 0.0);
			this.WordWrap = FromString((string)doc.Element("WordWrap"), false);
			this.HighlightCurrentLine = FromString((string)doc.Element("HighlightCurrentLine"), true);
			this.TopPaneSettings.Name = FromString((string)doc.Element("TopPaneName"), string.Empty);
			this.TopPaneSettings.Height = FromString((string)doc.Element("TopPaneHeight"), 200.0);
			this.BottomPaneSettings.Name = FromString((string)doc.Element("BottomPaneName"), string.Empty);
			this.BottomPaneSettings.Height = FromString((string)doc.Element("BottomPaneHeight"), 200.0);
			this.ThemeName = (string)doc.Element("ThemeName") ?? dnSpy.dntheme.Themes.DefaultThemeName;

			var ignoreXml = doc.Element("IgnoredWarnings");
			if (ignoreXml != null) {
				foreach (var child in ignoreXml.Elements("Warning")) {
					var id = Unescape((string)child);
					if (id != null)
						IgnoredWarnings.Add(id);
				}
			}

			var groups = doc.Element("TabGroups");
			if (groups == null) {
				this.TabsFound = false;
				this.SavedTabGroupsState = new SavedTabGroupsState();
			}
			else {
				this.TabsFound = true;
				this.SavedTabGroupsState = SavedTabGroupsState.FromXml(groups);
			}
		}
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		
		public FilterSettings FilterSettings { get; private set; }
		
		public string ActiveAssemblyList;
		
		public WindowState WindowState = WindowState.Normal;
		public bool IsFullScreen;
		public Rect? WindowBounds;
		internal static Rect DefaultWindowBounds =  new Rect(10, 10, 1300, 730);
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

		public struct PaneSettings
		{
			public double Height;
			public string Name;
		}
		
		public void Save()
		{
			XElement doc = new XElement("SessionSettings");
			doc.Add(this.FilterSettings.SaveAsXml());
			if (this.ActiveAssemblyList != null) {
				doc.Add(new XElement("ActiveAssemblyList", Escape(this.ActiveAssemblyList)));
			}
			doc.Add(new XElement("WindowState", ToString(this.WindowState)));
			doc.Add(new XElement("IsFullScreen", ToString(this.IsFullScreen)));
			if (this.WindowBounds != null)
				doc.Add(new XElement("WindowBounds", ToString(this.WindowBounds)));
			doc.Add(new XElement("WordWrap", ToString(this.WordWrap)));
			doc.Add(new XElement("HighlightCurrentLine", ToString(this.HighlightCurrentLine)));
			doc.Add(new XElement("LeftColumnWidth", ToString(this.LeftColumnWidth)));
			doc.Add(new XElement("TopPaneHeight", ToString(this.TopPaneSettings.Height)));
			doc.Add(new XElement("TopPaneName", ToString(this.TopPaneSettings.Name)));
			doc.Add(new XElement("BottomPaneHeight", ToString(this.BottomPaneSettings.Height)));
			doc.Add(new XElement("BottomPaneName", ToString(this.BottomPaneSettings.Name)));
			doc.Add(new XElement("ThemeName", ToString(this.ThemeName)));
			doc.Add(new XElement("IgnoredWarnings", IgnoredWarnings.Select(id => new XElement("Warning", Escape(id)))));

			if (ICSharpCode.ILSpy.Options.DisplaySettingsPanel.CurrentDisplaySettings.RestoreTabsAtStartup)
				doc.Add(SavedTabGroupsState.ToXml(new XElement("TabGroups")));
			
			ILSpySettings.SaveSettings(doc);
		}

		static bool IsValidChar(char c)
		{
			return c >= 0x20 && c != '©';
		}

		static Regex regex = new Regex("©(?<num>[0-9A-f]{4})");
		
		public static string Escape(string p)
		{
			if (p == null)
				return p;
			StringBuilder sb = new StringBuilder();
			foreach (char ch in p) {
				if (IsValidChar(ch))
					sb.Append(ch);
				else
					sb.AppendFormat("©{0:X4}", (int)ch);
			}
			return sb.ToString();
		}
		
		public static string Unescape(string p)
		{
			if (p == null)
				return p;
			return regex.Replace(p, m => ((char)int.Parse(m.Groups["num"].Value, NumberStyles.HexNumber)).ToString());
		}
		
		internal static T FromString<T>(string s, T defaultValue)
		{
			if (s == null)
				return defaultValue;
			try {
				TypeConverter c = TypeDescriptor.GetConverter(typeof(T));
				return (T)c.ConvertFromInvariantString(s);
			} catch (FormatException) {
				return defaultValue;
			}
		}

		internal static string ToString<T>(T obj)
		{
			TypeConverter c = TypeDescriptor.GetConverter(typeof(T));
			return c.ConvertToInvariantString(obj);
		}
	}
}

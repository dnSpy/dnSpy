// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Per-session setting:
	/// Loaded at startup; saved at exit.
	/// </summary>
	public class SessionSettings
	{
		public SessionSettings()
		{
			XElement doc = ILSpySettings.LoadSettings("SessionSettings");
			
			XElement filterSettings = doc.Element("FilterSettings");
			if (filterSettings == null) filterSettings = new XElement("FilterSettings");
			
			this.FilterSettings = new FilterSettings(filterSettings);
			
			XElement activeTreeViewPath = doc.Element("ActiveTreeViewPath");
			if (activeTreeViewPath != null) {
				this.ActiveTreeViewPath = activeTreeViewPath.Elements().Select(e => (string)e).ToArray();
			}
			
			this.WindowState = FromString((string)doc.Element("WindowState"), WindowState.Normal);
			this.WindowBounds = FromString((string)doc.Element("WindowBounds"), new Rect(10, 10, 750, 550));
		}
		
		public FilterSettings FilterSettings;
		
		public string[] ActiveTreeViewPath;
		
		public WindowState WindowState = WindowState.Normal;
		public Rect WindowBounds;
		
		public void Save()
		{
			XElement doc = new XElement("SessionSettings");
			doc.Add(this.FilterSettings.SaveAsXml());
			if (this.ActiveTreeViewPath != null) {
				doc.Add(new XElement("ActiveTreeViewPath", ActiveTreeViewPath.Select(p => new XElement("Node", p))));
			}
			doc.Add(new XElement("WindowState", ToString(this.WindowState)));
			doc.Add(new XElement("WindowBounds", ToString(this.WindowBounds)));
			ILSpySettings.SaveSettings(doc);
		}
		
		static T FromString<T>(string s, T defaultValue)
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
		
		static string ToString<T>(T obj)
		{
			TypeConverter c = TypeDescriptor.GetConverter(typeof(T));
			return c.ConvertToInvariantString(obj);
		}
	}
}

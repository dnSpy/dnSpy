// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;

using ICSharpCode.ILSpy.Debugger;

namespace ICSharpCode.ILSpy.Options
{
	[ExportOptionPage(Title = "Debugger", Order = 1)]
	partial class DebuggerSettingsPanel : UserControl, IOptionPage
	{
		public DebuggerSettingsPanel()
		{
			InitializeComponent();
		}
		
		public void Load(ILSpySettings settings)
		{
			this.DataContext = LoadDebuggerSettings(settings);
		}
		
		static DebuggerSettings currentDebuggerSettings;
		
		public static DebuggerSettings CurrentDebuggerSettings {
			get {
				return currentDebuggerSettings ?? (currentDebuggerSettings = LoadDebuggerSettings(ILSpySettings.Load()));
			}
		}
		
		public static DebuggerSettings LoadDebuggerSettings(ILSpySettings settings)
		{
			XElement e = settings["DebuggerSettings"];
			DebuggerSettings s = new DebuggerSettings();
			s.ShowWarnings = (bool?)e.Attribute("showWarnings") ?? s.ShowWarnings;
			return s;
		}
		
		public void Save(XElement root)
		{
			var s = (DebuggerSettings)this.DataContext;
			XElement section = new XElement("DebuggerSettings");
			section.SetAttributeValue("showWarnings", s.ShowWarnings);
			
			XElement existingElement = root.Element("DebuggerSettings");
			if (existingElement != null)
				existingElement.ReplaceWith(section);
			else
				root.Add(section);
			
			currentDebuggerSettings = null; // invalidate cached settings
		}
	}
}
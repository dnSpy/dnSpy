// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

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
	[ExportOptionPage(Title = "Debugger", Order = 2)]
	partial class DebuggerSettingsPanel : UserControl, IOptionPage
	{
		private const string DEBUGGER_SETTINGS = "DebuggerSettings";
		private const string SHOW_WARNINGS = "showWarnings";
		private const string ASK_ARGUMENTS = "askForArguments";
	 	
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
			XElement e = settings[DEBUGGER_SETTINGS];
			DebuggerSettings s = new DebuggerSettings();
			s.ShowWarnings = (bool?)e.Attribute(SHOW_WARNINGS) ?? s.ShowWarnings;
			s.AskForArguments = (bool?)e.Attribute(ASK_ARGUMENTS) ?? s.AskForArguments;
			
			return s;
		}
		
		public void Save(XElement root)
		{
			var s = (DebuggerSettings)this.DataContext;
			XElement section = new XElement(DEBUGGER_SETTINGS);
			section.SetAttributeValue(SHOW_WARNINGS, s.ShowWarnings);
			section.SetAttributeValue(ASK_ARGUMENTS, s.AskForArguments);

			XElement existingElement = root.Element(DEBUGGER_SETTINGS);
			if (existingElement != null)
				existingElement.ReplaceWith(section);
			else
				root.Add(section);
			
			currentDebuggerSettings = null; // invalidate cached settings
		}
	}
}
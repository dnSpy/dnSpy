// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
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
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Interaction logic for DecompilerSettingsPanel.xaml
	/// </summary>
	[ExportOptionPage("Decompiler")]
	partial class DecompilerSettingsPanel : UserControl, IOptionPage
	{
		public DecompilerSettingsPanel()
		{
			InitializeComponent();
		}
		
		public void Load(ILSpySettings settings)
		{
			this.DataContext = LoadDecompilerSettings(settings);
		}
		
		static DecompilerSettings currentDecompilerSettings;
		
		public static DecompilerSettings CurrentDecompilerSettings {
			get {
				return currentDecompilerSettings ?? (currentDecompilerSettings = LoadDecompilerSettings(ILSpySettings.Load()));
			}
		}
		
		public static DecompilerSettings LoadDecompilerSettings(ILSpySettings settings)
		{
			XElement e = settings["DecompilerSettings"];
			DecompilerSettings s = new DecompilerSettings();
			s.AnonymousMethods = (bool?)e.Attribute("anonymousMethods") ?? s.AnonymousMethods;
			s.YieldReturn = (bool?)e.Attribute("yieldReturn") ?? s.YieldReturn;
			s.QueryExpressions = (bool?)e.Attribute("queryExpressions") ?? s.QueryExpressions;
			s.UseDebugSymbols = (bool?)e.Attribute("useDebugSymbols") ?? s.UseDebugSymbols;
			return s;
		}
		
		public void Save(XElement root)
		{
			DecompilerSettings s = (DecompilerSettings)this.DataContext;
			XElement section = new XElement("DecompilerSettings");
			section.SetAttributeValue("anonymousMethods", s.AnonymousMethods);
			section.SetAttributeValue("yieldReturn", s.YieldReturn);
			section.SetAttributeValue("queryExpressions", s.QueryExpressions);
			section.SetAttributeValue("useDebugSymbols", s.UseDebugSymbols);
			
			XElement existingElement = root.Element("DecompilerSettings");
			if (existingElement != null)
				existingElement.ReplaceWith(section);
			else
				root.Add(section);
			
			currentDecompilerSettings = null; // invalidate cached settings
		}
	}
}
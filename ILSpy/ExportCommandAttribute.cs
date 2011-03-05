// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.ComponentModel.Composition;
using System.Windows.Input;

namespace ICSharpCode.ILSpy
{
	#region Toolbar
	public interface IToolbarCommandMetadata
	{
		string ToolbarIcon { get; }
		string ToolTip { get; }
		string ToolbarCategory { get; }
		object Tag { get; }
		double ToolbarOrder { get; }
	}
	
	[MetadataAttribute]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
	public class ExportToolbarCommandAttribute : ExportAttribute
	{
		public ExportToolbarCommandAttribute()
			: base("ToolbarCommand", typeof(ICommand))
		{
		}
		
		public string ToolTip { get; set; }
		public string ToolbarIcon { get; set; }
		public string ToolbarCategory { get; set; }
		public double ToolbarOrder { get; set; }
		public object Tag { get; set; }
	}
	#endregion
	
	#region Main Menu
	public interface IMainMenuCommandMetadata
	{
		string MenuIcon { get; }
		string Header { get; }
		string Menu { get; }
		string MenuCategory { get; }
		
		double MenuOrder { get; }
	}
	
	[MetadataAttribute]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
	public class ExportMainMenuCommandAttribute : ExportAttribute
	{
		public ExportMainMenuCommandAttribute()
			: base("MainMenuCommand", typeof(ICommand))
		{
		}
		
		public string MenuIcon { get; set; }
		public string Header { get; set; }
		public string Menu { get; set; }
		public string MenuCategory { get; set; }
		public double MenuOrder { get; set; }
	}
	#endregion
}

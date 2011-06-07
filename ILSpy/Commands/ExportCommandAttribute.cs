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
	public class ExportToolbarCommandAttribute : ExportAttribute, IToolbarCommandMetadata
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
		string InputGestureText { get; }
		bool IsEnabled { get; }
		
		double MenuOrder { get; }
	}
	
	[MetadataAttribute]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
	public class ExportMainMenuCommandAttribute : ExportAttribute, IMainMenuCommandMetadata
	{
		bool isEnabled = true;
		
		public ExportMainMenuCommandAttribute()
			: base("MainMenuCommand", typeof(ICommand))
		{
		}
		
		public string MenuIcon { get; set; }
		public string Header { get; set; }
		public string Menu { get; set; }
		public string MenuCategory { get; set; }
		public string InputGestureText { get; set; }
		public bool IsEnabled {
			get { return isEnabled; }
			set { isEnabled = value; }
		}
		public double MenuOrder { get; set; }
	}
	#endregion
}

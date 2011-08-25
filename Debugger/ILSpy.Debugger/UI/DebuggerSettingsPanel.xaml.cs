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
		public DebuggerSettingsPanel()
		{
			InitializeComponent();
		}
		
		public void Load(ILSpySettings settings)
		{
			var s = DebuggerSettings.Instance;
			s.Load(settings);
			this.DataContext = s;
		}
		
		public void Save(XElement root)
		{
			var s = (DebuggerSettings)this.DataContext;
			s.Save(root);
		}
	}
}
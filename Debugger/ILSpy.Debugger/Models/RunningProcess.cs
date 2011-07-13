// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace ICSharpCode.ILSpy.Debugger.Models
{
	sealed class RunningProcess
	{
		public int ProcessId { get; set; }
		
		public string WindowTitle { get; set; }
	
		public string ProcessName { get; set; }
		
		public string FileName { get; set; }
		
		public string Managed { get; set; }
		
		public Process Process { get; set; }
	}
}

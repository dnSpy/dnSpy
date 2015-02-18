// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace Debugger
{
	[Serializable]
	public class ProcessEventArgs: DebuggerEventArgs
	{
		Process process;
		
		public Process Process {
			get { return process; }
		}
		
		public ProcessEventArgs(Process process): base(process == null ? null : process.Debugger)
		{
			this.process = process;
		}
	}
}

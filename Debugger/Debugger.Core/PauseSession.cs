// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

namespace Debugger
{
	/// <summary>
	/// Holds information about the state of paused debugger.
	/// Expires when when Continue is called on debugger.
	/// </summary>
	public class PauseSession: DebuggerObject
	{
		Process process;
		PausedReason pausedReason;
		
		public Process Process {
			get { return process; }
		}
		
		public PausedReason PausedReason {
			get { return pausedReason; }
		}
		
		public PauseSession(Process process, PausedReason pausedReason)
		{
			this.process = process;
			this.pausedReason = pausedReason;
		}
	}
}

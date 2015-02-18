// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

namespace Debugger
{
	/// <summary>
	/// Represents span of time in which the debugger state is assumed to
	/// be unchanged.
	/// </summary>
	/// <remarks>
	/// For example, although property evaluation can in theory change
	/// any memory, it is assumed that they behave 'correctly' and thus
	/// property evaluation does not change debugger state.
	/// </remarks>
	public class DebuggeeState: DebuggerObject
	{
		Process process;
		
		public Process Process {
			get { return process; }
		}
		
		public DebuggeeState(Process process)
		{
			this.process = process;
		}
	}
}

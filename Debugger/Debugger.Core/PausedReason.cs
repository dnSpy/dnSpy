// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

namespace Debugger
{
	public enum PausedReason : int
	{
		EvalComplete,
		StepComplete,
		Breakpoint,
		Break,
		ControlCTrap,
		Exception,
		ForcedBreak, // Process.Break called
		DebuggerError,
		CurrentThreadChanged,
		CurrentFunctionChanged,
		ExceptionIntercepted,
		SetIP,
		Other
	}
}

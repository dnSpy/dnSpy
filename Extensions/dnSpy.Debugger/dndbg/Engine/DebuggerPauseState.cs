/*
    Copyright (C) 2014-2017 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	enum DebuggerPauseReason {
		/// <summary>
		/// Some unknown reason
		/// </summary>
		Other,

		/// <summary>
		/// An unhandled exception
		/// </summary>
		UnhandledException,

		/// <summary>
		/// An exception
		/// </summary>
		Exception,

		/// <summary>
		/// A <see cref="DnDebugEventBreakpoint"/> breakpoint got triggered
		/// </summary>
		DebugEventBreakpoint,

		/// <summary>
		/// A <see cref="DnAnyDebugEventBreakpoint"/> breakpoint got triggered
		/// </summary>
		AnyDebugEventBreakpoint,

		/// <summary>
		/// A 'break' IL instruction was executed
		/// </summary>
		Break,

		/// <summary>
		/// An IL code breakpoint got triggered
		/// </summary>
		ILCodeBreakpoint,

		/// <summary>
		/// A native code breakpoint got triggered
		/// </summary>
		NativeCodeBreakpoint,

		/// <summary>
		/// A step in, step out or step over command has completed
		/// </summary>
		Step,

		/// <summary>
		/// TryBreakProcesses() was called
		/// </summary>
		UserBreak,

		/// <summary>
		/// Evaluation completed
		/// </summary>
		Eval,

		/// <summary>
		/// Start of user pause reasons
		/// </summary>
		UserReason = 0x10000000,
	}

	class DebuggerPauseState {
		public DebuggerPauseReason Reason { get; }

		public DebuggerPauseState(DebuggerPauseReason reason) => Reason = reason;
	}

	sealed class DebugEventBreakpointPauseState : DebuggerPauseState {
		public DnDebugEventBreakpoint Breakpoint { get; }

		public DebugEventBreakpointPauseState(DnDebugEventBreakpoint bp)
			: base(DebuggerPauseReason.DebugEventBreakpoint) => Breakpoint = bp;
	}

	sealed class AnyDebugEventBreakpointPauseState : DebuggerPauseState {
		public DnAnyDebugEventBreakpoint Breakpoint { get; }

		public AnyDebugEventBreakpointPauseState(DnAnyDebugEventBreakpoint bp)
			: base(DebuggerPauseReason.AnyDebugEventBreakpoint) => Breakpoint = bp;
	}

	sealed class ILCodeBreakpointPauseState : DebuggerPauseState {
		public DnILCodeBreakpoint Breakpoint { get; }

		public ILCodeBreakpointPauseState(DnILCodeBreakpoint bp)
			: base(DebuggerPauseReason.ILCodeBreakpoint) => Breakpoint = bp;
	}

	sealed class NativeCodeBreakpointPauseState : DebuggerPauseState {
		public DnNativeCodeBreakpoint Breakpoint { get; }

		public NativeCodeBreakpointPauseState(DnNativeCodeBreakpoint bp)
			: base(DebuggerPauseReason.NativeCodeBreakpoint) => Breakpoint = bp;
	}

	sealed class StepPauseState : DebuggerPauseState {
		public CorDebugStepReason StepReason { get; }

		public StepPauseState(CorDebugStepReason stepReason)
			: base(DebuggerPauseReason.Step) => StepReason = stepReason;
	}
}

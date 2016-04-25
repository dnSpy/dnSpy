/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
	public enum DebuggerPauseReason {
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

	public class DebuggerPauseState {
		public DebuggerPauseReason Reason {
			get { return reason; }
		}
		readonly DebuggerPauseReason reason;

		public DebuggerPauseState(DebuggerPauseReason reason) {
			this.reason = reason;
		}
	}

	public sealed class DebugEventBreakpointPauseState : DebuggerPauseState {
		public DnDebugEventBreakpoint Breakpoint {
			get { return bp; }
		}
		readonly DnDebugEventBreakpoint bp;

		public DebugEventBreakpointPauseState(DnDebugEventBreakpoint bp)
			: base(DebuggerPauseReason.DebugEventBreakpoint) {
			this.bp = bp;
		}
	}

	public sealed class AnyDebugEventBreakpointPauseState : DebuggerPauseState {
		public DnAnyDebugEventBreakpoint Breakpoint {
			get { return bp; }
		}
		readonly DnAnyDebugEventBreakpoint bp;

		public AnyDebugEventBreakpointPauseState(DnAnyDebugEventBreakpoint bp)
			: base(DebuggerPauseReason.AnyDebugEventBreakpoint) {
			this.bp = bp;
		}
	}

	public sealed class ILCodeBreakpointPauseState : DebuggerPauseState {
		public DnILCodeBreakpoint Breakpoint {
			get { return bp; }
		}
		readonly DnILCodeBreakpoint bp;

		public ILCodeBreakpointPauseState(DnILCodeBreakpoint bp)
			: base(DebuggerPauseReason.ILCodeBreakpoint) {
			this.bp = bp;
		}
	}

	public sealed class NativeCodeBreakpointPauseState : DebuggerPauseState {
		public DnNativeCodeBreakpoint Breakpoint {
			get { return bp; }
		}
		readonly DnNativeCodeBreakpoint bp;

		public NativeCodeBreakpointPauseState(DnNativeCodeBreakpoint bp)
			: base(DebuggerPauseReason.NativeCodeBreakpoint) {
			this.bp = bp;
		}
	}

	public sealed class StepPauseState : DebuggerPauseState {
		public CorDebugStepReason StepReason {
			get { return stepReason; }
		}
		readonly CorDebugStepReason stepReason;

		public StepPauseState(CorDebugStepReason stepReason)
			: base(DebuggerPauseReason.Step) {
			this.stepReason = stepReason;
		}
	}
}

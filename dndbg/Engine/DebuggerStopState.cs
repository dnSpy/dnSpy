/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using dndbg.Engine.COM.CorDebug;

namespace dndbg.Engine {
	public enum DebuggerStopReason {
		/// <summary>
		/// Some unknown reason
		/// </summary>
		Other,

		/// <summary>
		/// A debug event breakpoint got triggered
		/// </summary>
		DebugEventBreakpoint,

		/// <summary>
		/// A 'break' IL instruction was executed
		/// </summary>
		Break,

		/// <summary>
		/// An IL code breakpoint got triggered
		/// </summary>
		ILCodeBreakpoint,

		/// <summary>
		/// A step in, step out or step over command has completed
		/// </summary>
		Step,

		/// <summary>
		/// Start of user stop reasons
		/// </summary>
		UserReason = 0x10000000,
	}

	public class DebuggerStopState {
		public DebuggerStopReason Reason {
			get { return reason; }
		}
		readonly DebuggerStopReason reason;

		public DebuggerStopState(DebuggerStopReason reason) {
			this.reason = reason;
		}
	}

	public sealed class DebugEventBreakpointStopState : DebuggerStopState {
		public DebugEventBreakpoint Breakpoint {
			get { return bp; }
		}
		readonly DebugEventBreakpoint bp;

		public DebugEventBreakpointStopState(DebugEventBreakpoint bp)
			: base(DebuggerStopReason.DebugEventBreakpoint) {
			this.bp = bp;
		}
	}

	public sealed class ILCodeBreakpointStopState : DebuggerStopState {
		public ILCodeBreakpoint Breakpoint {
			get { return bp; }
		}
		readonly ILCodeBreakpoint bp;

		public ILCodeBreakpointStopState(ILCodeBreakpoint bp)
			: base(DebuggerStopReason.ILCodeBreakpoint) {
			this.bp = bp;
		}
	}

	public sealed class StepStopState : DebuggerStopState {
		public CorDebugStepReason StepReason {
			get { return stepReason; }
		}
		readonly CorDebugStepReason stepReason;

		public StepStopState(CorDebugStepReason stepReason)
			: base(DebuggerStopReason.Step) {
			this.stepReason = stepReason;
		}
	}
}

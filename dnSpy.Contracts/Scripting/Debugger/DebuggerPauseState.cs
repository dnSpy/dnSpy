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

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// Pause reason
	/// </summary>
	public enum PauseReason {
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
		/// A <see cref="IEventBreakpoint"/> breakpoint got triggered
		/// </summary>
		DebugEventBreakpoint,

		/// <summary>
		/// A <see cref="IAnyEventBreakpoint"/> breakpoint got triggered
		/// </summary>
		AnyDebugEventBreakpoint,

		/// <summary>
		/// A 'break' IL instruction was executed
		/// </summary>
		Break,

		/// <summary>
		/// An <see cref="IILBreakpoint"/> breakpoint got triggered
		/// </summary>
		ILCodeBreakpoint,

		/// <summary>
		/// A <see cref="INativeBreakpoint"/> breakpoint got triggered
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
		/// The process has been terminated
		/// </summary>
		Terminated,
	}

	/// <summary>
	/// Pause state
	/// </summary>
	public class DebuggerPauseState {
		/// <summary>
		/// Reason
		/// </summary>
		public PauseReason Reason {
			get { return reason; }
		}
		readonly PauseReason reason;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reason">Reason</param>
		public DebuggerPauseState(PauseReason reason) {
			this.reason = reason;
		}
	}

	/// <summary>
	/// Debug event breakpoint
	/// </summary>
	public sealed class EventBreakpointPauseState : DebuggerPauseState {
		/// <summary>
		/// Breakpoint
		/// </summary>
		public IEventBreakpoint Breakpoint {
			get { return bp; }
		}
		readonly IEventBreakpoint bp;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bp">Breakpoint</param>
		public EventBreakpointPauseState(IEventBreakpoint bp)
			: base(PauseReason.DebugEventBreakpoint) {
			this.bp = bp;
		}
	}

	/// <summary>
	/// Any debug event breakpoint
	/// </summary>
	public sealed class AnyEventBreakpointPauseState : DebuggerPauseState {
		/// <summary>
		/// Breakpoint
		/// </summary>
		public IAnyEventBreakpoint Breakpoint {
			get { return bp; }
		}
		readonly IAnyEventBreakpoint bp;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bp">Breakpoint</param>
		public AnyEventBreakpointPauseState(IAnyEventBreakpoint bp)
			: base(PauseReason.AnyDebugEventBreakpoint) {
			this.bp = bp;
		}
	}

	/// <summary>
	/// IL code breakpoint
	/// </summary>
	public sealed class ILBreakpointPauseState : DebuggerPauseState {
		/// <summary>
		/// Breakpoint
		/// </summary>
		public IILBreakpoint Breakpoint {
			get { return bp; }
		}
		readonly IILBreakpoint bp;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bp">Breakpoint</param>
		public ILBreakpointPauseState(IILBreakpoint bp)
			: base(PauseReason.ILCodeBreakpoint) {
			this.bp = bp;
		}
	}

	/// <summary>
	/// Native code breakpoint
	/// </summary>
	public sealed class NativeBreakpointPauseState : DebuggerPauseState {
		/// <summary>
		/// Breakpoint
		/// </summary>
		public INativeBreakpoint Breakpoint {
			get { return bp; }
		}
		readonly INativeBreakpoint bp;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bp">Breakpoint</param>
		public NativeBreakpointPauseState(INativeBreakpoint bp)
			: base(PauseReason.NativeCodeBreakpoint) {
			this.bp = bp;
		}
	}

	/// <summary>
	/// Step in / out / over
	/// </summary>
	public sealed class StepPauseState : DebuggerPauseState {
		/// <summary>
		/// Step reason
		/// </summary>
		public DebugStepReason StepReason {
			get { return stepReason; }
		}
		readonly DebugStepReason stepReason;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stepReason">Step reason</param>
		public StepPauseState(DebugStepReason stepReason)
			: base(PauseReason.Step) {
			this.stepReason = stepReason;
		}
	}
}

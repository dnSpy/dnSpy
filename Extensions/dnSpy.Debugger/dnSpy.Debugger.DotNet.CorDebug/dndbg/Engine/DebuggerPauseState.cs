/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

namespace dndbg.Engine {
	enum DebuggerPauseReason {
		Other,
		AsyncStepperBreakpoint,
		UnhandledException,
		Exception,
		DebugEventBreakpoint,
		AnyDebugEventBreakpoint,
		Break,
		ILCodeBreakpoint,
		NativeCodeBreakpoint,
		UserBreak,
		Eval,
		EntryPointBreakpoint,
	}

	class DebuggerPauseState {
		public DebuggerPauseReason Reason { get; }
		public bool Handled { get; set; }

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
		public CorAppDomain CorAppDomain { get; }
		public CorThread CorThread { get; }

		public ILCodeBreakpointPauseState(DnILCodeBreakpoint bp, CorAppDomain corAppDomain, CorThread corThread)
			: base(DebuggerPauseReason.ILCodeBreakpoint) {
			Breakpoint = bp;
			CorAppDomain = corAppDomain;
			CorThread = corThread;
		}
	}

	sealed class NativeCodeBreakpointPauseState : DebuggerPauseState {
		public DnNativeCodeBreakpoint Breakpoint { get; }
		public CorAppDomain CorAppDomain { get; }
		public CorThread CorThread { get; }

		public NativeCodeBreakpointPauseState(DnNativeCodeBreakpoint bp, CorAppDomain corAppDomain, CorThread corThread)
			: base(DebuggerPauseReason.NativeCodeBreakpoint) {
			Breakpoint = bp;
			CorAppDomain = corAppDomain;
			CorThread = corThread;
		}
	}

	sealed class BreakPauseState : DebuggerPauseState {
		public DnNativeCodeBreakpoint Breakpoint { get; }
		public CorAppDomain CorAppDomain { get; }
		public CorThread CorThread { get; }

		public BreakPauseState(CorAppDomain corAppDomain, CorThread corThread)
			: base(DebuggerPauseReason.Break) {
			CorAppDomain = corAppDomain;
			CorThread = corThread;
		}
	}

	sealed class EntryPointBreakpointPauseState : DebuggerPauseState {
		public CorAppDomain CorAppDomain { get; }
		public CorThread CorThread { get; }

		public EntryPointBreakpointPauseState(CorAppDomain corAppDomain, CorThread corThread)
			: base(DebuggerPauseReason.EntryPointBreakpoint) {
			CorAppDomain = corAppDomain;
			CorThread = corThread;
		}
	}
}

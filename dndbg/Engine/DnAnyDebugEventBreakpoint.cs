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

namespace dndbg.Engine {
	public sealed class AnyDebugEventBreakpointConditionContext : BreakpointConditionContext {
		public override DnBreakpoint Breakpoint {
			get { return bp; }
		}

		public DnAnyDebugEventBreakpoint DebugEventBreakpoint {
			get { return bp; }
		}
		readonly DnAnyDebugEventBreakpoint bp;

		public DebugCallbackEventArgs EventArgs {
			get { return e; }
		}
		readonly DebugCallbackEventArgs e;

		public AnyDebugEventBreakpointConditionContext(DnDebugger debugger, DnAnyDebugEventBreakpoint bp, DebugCallbackEventArgs e)
			: base(debugger) {
			this.bp = bp;
			this.e = e;
		}
	}

	public sealed class DnAnyDebugEventBreakpoint : DnBreakpoint {
		internal DnAnyDebugEventBreakpoint(IBreakpointCondition bpCond)
			: base(bpCond) {
		}
	}
}

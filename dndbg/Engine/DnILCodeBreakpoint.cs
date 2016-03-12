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

using System;

namespace dndbg.Engine {
	public sealed class ILCodeBreakpointConditionContext : BreakpointConditionContext {
		public override DnBreakpoint Breakpoint {
			get { return bp; }
		}

		public DnILCodeBreakpoint ILCodeBreakpoint {
			get { return bp; }
		}
		readonly DnILCodeBreakpoint bp;

		public ILCodeBreakpointConditionContext(DnDebugger debugger, DnILCodeBreakpoint bp)
			: base(debugger) {
			this.bp = bp;
		}
	}

	public sealed class DnILCodeBreakpoint : DnCodeBreakpoint {
		internal Func<ILCodeBreakpointConditionContext, bool> Condition {
			get { return cond; }
		}
		readonly Func<ILCodeBreakpointConditionContext, bool> cond;

		internal DnILCodeBreakpoint(SerializedDnModule module, uint token, uint offset, Func<ILCodeBreakpointConditionContext, bool> cond)
			: base(module, token, offset) {
			this.cond = cond ?? defaultCond;
		}
		static readonly Func<ILCodeBreakpointConditionContext, bool> defaultCond = a => true;

		internal override CorCode GetCode(CorFunction func) {
			return func.ILCode;
		}
	}
}

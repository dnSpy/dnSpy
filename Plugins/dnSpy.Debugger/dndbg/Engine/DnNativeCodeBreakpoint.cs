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
	public sealed class NativeCodeBreakpointConditionContext : BreakpointConditionContext {
		public override DnBreakpoint Breakpoint => NativeCodeBreakpoint;
		public DnNativeCodeBreakpoint NativeCodeBreakpoint { get; }

		public NativeCodeBreakpointConditionContext(DnDebugger debugger, DnNativeCodeBreakpoint bp)
			: base(debugger) {
			this.NativeCodeBreakpoint = bp;
		}
	}

	public sealed class DnNativeCodeBreakpoint : DnCodeBreakpoint {
		internal Func<NativeCodeBreakpointConditionContext, bool> Condition { get; }

		internal DnNativeCodeBreakpoint(DnModuleId module, uint token, uint offset, Func<NativeCodeBreakpointConditionContext, bool> cond)
			: base(module, token, offset) {
			this.Condition = cond ?? defaultCond;
		}
		static readonly Func<NativeCodeBreakpointConditionContext, bool> defaultCond = a => true;

		internal DnNativeCodeBreakpoint(CorCode code, uint offset, Func<NativeCodeBreakpointConditionContext, bool> cond)
			: base(code, offset) {
			this.Condition = cond ?? defaultCond;
		}

		internal override CorCode GetCode(CorFunction func) => func.NativeCode;
	}
}

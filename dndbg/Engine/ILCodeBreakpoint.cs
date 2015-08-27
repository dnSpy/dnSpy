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

using System.Collections.Generic;
using dndbg.Engine.COM.CorDebug;

namespace dndbg.Engine {
	public sealed class ILCodeBreakpointConditionContext : BreakpointConditionContext {
		public override Breakpoint Breakpoint {
			get { return bp; }
		}

		public ILCodeBreakpoint ILCodeBreakpoint {
			get { return bp; }
		}
		readonly ILCodeBreakpoint bp;

		public ILCodeBreakpointConditionContext(DnDebugger debugger, ILCodeBreakpoint bp)
			: base(debugger) {
			this.bp = bp;
		}
	}

	sealed class ModuleILCodeBreakpoint {
		public DnModule Module {
			get { return module; }
		}
		readonly DnModule module;

		public ICorDebugFunctionBreakpoint FunctionBreakpoint {
			get { return funcBp; }
		}
		readonly ICorDebugFunctionBreakpoint funcBp;

		public ModuleILCodeBreakpoint(DnModule module, ICorDebugFunctionBreakpoint funcBp) {
			this.module = module;
			this.funcBp = funcBp;
		}
	}

	public sealed class ILCodeBreakpoint : Breakpoint {
		public SerializedDnModule Module {
			get { return module; }
		}
		readonly SerializedDnModule module;

		public uint Token {
			get { return token; }
		}
		readonly uint token;

		public uint ILOffset {
			get { return ilOffset; }
		}
		readonly uint ilOffset;

		readonly List<ModuleILCodeBreakpoint> rawBps = new List<ModuleILCodeBreakpoint>();

		internal ILCodeBreakpoint(SerializedDnModule module, uint token, uint ilOffset, IBreakpointCondition bpCond)
			: base(bpCond) {
			this.module = module;
			this.token = token;
			this.ilOffset = ilOffset;
		}

		protected override void OnIsEnabledChanged() {
			foreach (var bp in rawBps)
				bp.FunctionBreakpoint.Activate(IsEnabled ? 1 : 0);
		}

		internal bool AddBreakpoint(DnModule module) {
			ICorDebugFunction func;
			int hr = module.RawObject.GetFunctionFromToken(Token, out func);
			if (hr < 0 || func == null)
				return false;

			ICorDebugCode ilCode;
			hr = func.GetILCode(out ilCode);
			if (hr < 0 || ilCode == null)
				return false;

			ICorDebugFunctionBreakpoint funcBp;
			hr = ilCode.CreateBreakpoint(ILOffset, out funcBp);
			if (hr < 0 || funcBp == null)
				return false;

			var modIlBp = new ModuleILCodeBreakpoint(module, funcBp);
			rawBps.Add(modIlBp);
			hr = funcBp.Activate(IsEnabled ? 1 : 0);

			return true;
		}

		internal override void OnRemoved() {
			foreach (var bp in rawBps)
				bp.FunctionBreakpoint.Activate(0);
			rawBps.Clear();
		}

		public bool IsBreakpoint(ICorDebugBreakpoint comBp) {
			foreach (var bp in rawBps) {
				if (bp.FunctionBreakpoint == comBp)
					return true;
			}
			return false;
		}

		internal void RemoveModule(DnModule module) {
			foreach (var bp in rawBps.ToArray()) {
				if (bp.Module == module)
					rawBps.Remove(bp);
			}
		}
	}
}

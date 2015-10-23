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
using dndbg.COM.CorDebug;

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

	sealed class ModuleILCodeBreakpoint {
		public DnModule Module {
			get { return module; }
		}
		readonly DnModule module;

		public CorFunctionBreakpoint FunctionBreakpoint {
			get { return funcBp; }
		}
		readonly CorFunctionBreakpoint funcBp;

		public ModuleILCodeBreakpoint(DnModule module, CorFunctionBreakpoint funcBp) {
			this.module = module;
			this.funcBp = funcBp;
		}
	}

	public sealed class DnILCodeBreakpoint : DnBreakpoint {
		public SerializedDnModuleWithAssembly Module {
			get { return module; }
		}
		readonly SerializedDnModuleWithAssembly module;

		public uint Token {
			get { return token; }
		}
		readonly uint token;

		public uint ILOffset {
			get { return ilOffset; }
		}
		readonly uint ilOffset;

		readonly List<ModuleILCodeBreakpoint> rawBps = new List<ModuleILCodeBreakpoint>();

		internal DnILCodeBreakpoint(SerializedDnModuleWithAssembly module, uint token, uint ilOffset, IBreakpointCondition bpCond)
			: base(bpCond) {
			this.module = module;
			this.token = token;
			this.ilOffset = ilOffset;
		}

		protected override void OnIsEnabledChanged() {
			foreach (var bp in rawBps)
				bp.FunctionBreakpoint.IsActive = IsEnabled;
		}

		internal bool AddBreakpoint(DnModule module) {
			foreach (var bp in rawBps) {
				if (bp.Module == module)
					return true;
			}

			var func = module.CorModule.GetFunctionFromToken(Token);
			if (func == null)
				return false;

			var ilCode = func.ILCode;
			if (ilCode == null)
				return false;

			var funcBp = ilCode.CreateBreakpoint(ILOffset);
			if (funcBp == null)
				return false;

			var modIlBp = new ModuleILCodeBreakpoint(module, funcBp);
			rawBps.Add(modIlBp);
			funcBp.IsActive = IsEnabled;

			return true;
		}

		internal override void OnRemoved() {
			foreach (var bp in rawBps)
				bp.FunctionBreakpoint.IsActive = false;
			rawBps.Clear();
		}

		public bool IsBreakpoint(ICorDebugBreakpoint comBp) {
			foreach (var bp in rawBps) {
				if (bp.FunctionBreakpoint.RawObject == comBp)
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

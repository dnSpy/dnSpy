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

using System.Collections.Generic;
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	sealed class ModuleCodeBreakpoint {
		public DnModule Module { get; }
		public CorFunctionBreakpoint FunctionBreakpoint { get; }

		public ModuleCodeBreakpoint(DnModule module, CorFunctionBreakpoint funcBp) {
			this.Module = module;
			this.FunctionBreakpoint = funcBp;
		}
	}

	public abstract class DnCodeBreakpoint : DnBreakpoint {
		public SerializedDnModule Module { get; }
		public uint Token { get; }
		public uint Offset { get; }

		readonly List<ModuleCodeBreakpoint> rawBps = new List<ModuleCodeBreakpoint>();
		readonly CorCode code;

		internal DnCodeBreakpoint(SerializedDnModule module, uint token, uint offset) {
			this.Module = module;
			this.Token = token;
			this.Offset = offset;
			this.code = null;
		}

		internal DnCodeBreakpoint(CorCode code, uint offset) {
			this.Module = GetModule(code);
			var func = code.Function;
			this.Token = func?.Token ?? 0;
			this.Offset = offset;
			this.code = code;
		}

		static SerializedDnModule GetModule(CorCode code) =>
			code.Function?.Module?.SerializedDnModule ?? new SerializedDnModule();

		sealed protected override void OnIsEnabledChanged() {
			foreach (var bp in rawBps)
				bp.FunctionBreakpoint.IsActive = IsEnabled;
		}

		internal bool AddBreakpoint(DnModule module) {
			foreach (var bp in rawBps) {
				if (bp.Module == module)
					return true;
			}

			var c = code;
			if (c == null) {
				var func = module.CorModule.GetFunctionFromToken(Token);
				if (func == null)
					return false;

				c = GetCode(func);
			}
			else {
				if (GetModule(code) != module.SerializedDnModule)
					return false;
			}
			if (c == null)
				return false;

			var funcBp = c.CreateBreakpoint(Offset);
			if (funcBp == null)
				return false;

			var modIlBp = new ModuleCodeBreakpoint(module, funcBp);
			rawBps.Add(modIlBp);
			funcBp.IsActive = IsEnabled;

			return true;
		}

		internal abstract CorCode GetCode(CorFunction func);

		sealed internal override void OnRemoved() {
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
			for (int i = rawBps.Count - 1; i >= 0; i--) {
				if (rawBps[i].Module == module)
					rawBps.RemoveAt(i);
			}
		}
	}
}

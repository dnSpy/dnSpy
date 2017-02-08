/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Diagnostics;
using dndbg.Engine;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.Scripting.Debugger;

namespace dnSpy.Debugger.Scripting {
	sealed class NullNativeBreakpoint : INativeBreakpoint {
		public static readonly NullNativeBreakpoint Instance = new NullNativeBreakpoint();

		public bool IsEnabled {
			get { return false; }
			set { }
		}

		public object Tag {
			get { return null; }
			set { }
		}

		public bool IsIL => false;
		public bool IsNative => true;
		public BreakpointKind Kind => BreakpointKind.Native;
		public ModuleId Module => ModuleId.Create(string.Empty, string.Empty, true, true, false);
		public uint Offset => 0;
		public uint Token => 0x06000000;
		public void Remove() { }
	}

	sealed class NativeBreakpoint : INativeBreakpoint {
		public BreakpointKind Kind => BreakpointKind.Native;

		public bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled == value)
					return;
				debugger.Dispatcher.UI(() => {
					if (isEnabled == value)
						return;
					isEnabled = value;
					if (dbgBreakpoint != null)
						dbgBreakpoint.IsEnabled = value;
				});
			}
		}
		bool isEnabled;

		public object Tag { get; set; }
		public bool IsIL => false;
		public bool IsNative => true;
		public ModuleId Module { get; }
		public uint Token { get; }
		public uint Offset { get; }
		public DnBreakpoint DnBreakpoint => dbgBreakpoint;

		DnNativeCodeBreakpoint dbgBreakpoint;
		readonly Debugger debugger;
		readonly Func<INativeBreakpoint, bool> cond;
		readonly DebuggerCode code;

		public NativeBreakpoint(Debugger debugger, ModuleId module, uint token, uint offset, Func<INativeBreakpoint, bool> cond) {
			this.debugger = debugger;
			Module = module;
			Token = token;
			Offset = offset;
			this.cond = cond ?? condAlwaysTrue;
			isEnabled = true;
			code = null;
		}
		static readonly Func<INativeBreakpoint, bool> condAlwaysTrue = bp => true;

		public NativeBreakpoint(Debugger debugger, DebuggerCode code, uint offset, Func<INativeBreakpoint, bool> cond) {
			Debug.Assert(!code.IsIL);
			this.debugger = debugger;
			Module = code.Method.Module.ModuleId;
			Token = code.Method.Token;
			Offset = offset;
			this.cond = cond ?? condAlwaysTrue;
			isEnabled = true;
			this.code = code;
		}

		public void Remove() => debugger.Remove(this);

		public void Initialize(DnDebugger dbg) {
			Debug.Assert(debugger.Dispatcher.CheckAccess());
			Debug.Assert(dbgBreakpoint == null);
			if (dbgBreakpoint != null)
				throw new InvalidOperationException();
			if (code == null)
				dbgBreakpoint = dbg.CreateNativeBreakpoint(Module.ToDnModuleId(), Token, Offset, a => cond(this));
			else
				dbgBreakpoint = dbg.CreateNativeBreakpoint(code.CorCode, Offset, a => cond(this));
			dbgBreakpoint.IsEnabled = isEnabled;
			dbgBreakpoint.Tag = this;
		}
	}
}

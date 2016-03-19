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
using System.Diagnostics;
using dndbg.Engine;
using dnSpy.Contracts.Scripting.Debugger;
using dnSpy.Shared.Scripting;

namespace dnSpy.Debugger.Scripting {
	sealed class NullILBreakpoint : IILBreakpoint {
		public static readonly NullILBreakpoint Instance = new NullILBreakpoint();

		public bool IsEnabled {
			get { return false; }
			set { }
		}

		public object Tag {
			get { return null; }
			set { }
		}

		public bool IsIL {
			get { return true; }
		}

		public bool IsNative {
			get { return false; }
		}

		public BreakpointKind Kind {
			get { return BreakpointKind.IL; }
		}

		public ModuleName Module {
			get { return ModuleName.Create(string.Empty, string.Empty, true, true, false); }
		}

		public uint Offset {
			get { return 0; }
		}

		public uint Token {
			get { return 0x06000000; }
		}

		public void Remove() {
		}
	}

	sealed class ILBreakpoint : IILBreakpoint, IDnBreakpointHolder {
		public BreakpointKind Kind {
			get { return BreakpointKind.IL; }
		}

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

		public bool IsIL {
			get { return true; }
		}

		public bool IsNative {
			get { return false; }
		}

		public ModuleName Module {
			get { return module; }
		}
		/*readonly*/ ModuleName module;

		public uint Token {
			get { return token; }
		}
		readonly uint token;

		public uint Offset {
			get { return offset; }
		}
		readonly uint offset;

		public DnBreakpoint DnBreakpoint {
			get { return dbgBreakpoint; }
		}
		DnILCodeBreakpoint dbgBreakpoint;

		readonly Debugger debugger;
		readonly Func<IILBreakpoint, bool> cond;

		public ILBreakpoint(Debugger debugger, ModuleName module, uint token, uint offset, Func<IILBreakpoint, bool> cond) {
			this.debugger = debugger;
			this.module = module;
			this.token = token;
			this.offset = offset;
			this.cond = cond ?? condAlwaysTrue;
			this.isEnabled = true;
		}
		static readonly Func<IILBreakpoint, bool> condAlwaysTrue = bp => true;

		public void Remove() {
			debugger.Remove(this);
		}

		public void Initialize(DnDebugger dbg) {
			Debug.Assert(debugger.Dispatcher.CheckAccess());
			Debug.Assert(dbgBreakpoint == null);
			if (dbgBreakpoint != null)
				throw new InvalidOperationException();
			dbgBreakpoint = dbg.CreateBreakpoint(module.ToSerializedDnModule(), token, offset, a => cond(this));
			dbgBreakpoint.IsEnabled = isEnabled;
			dbgBreakpoint.Tag = this;
		}
	}
}

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
	sealed class NullEventBreakpoint : IEventBreakpoint {
		public static readonly NullEventBreakpoint Instance = new NullEventBreakpoint();

		public DebugEventKind EventKind {
			get { return (DebugEventKind)(-1); }
		}

		public bool IsEnabled {
			get { return false; }
			set { }
		}

		public object Tag {
			get { return null; }
			set { }
		}

		public BreakpointKind Kind {
			get { return BreakpointKind.Event; }
		}

		public void Remove() {
		}
	}

	sealed class EventBreakpoint : IEventBreakpoint, IDnBreakpointHolder {
		public BreakpointKind Kind {
			get { return BreakpointKind.Event; }
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

		public DebugEventKind EventKind {
			get { return eventKind; }
		}
		readonly DebugEventKind eventKind;

		public DnBreakpoint DnBreakpoint {
			get { return dbgBreakpoint; }
		}
		DnDebugEventBreakpoint dbgBreakpoint;

		readonly Debugger debugger;
		readonly Func<IEventBreakpoint, IDebugEventContext, bool> cond;

		public EventBreakpoint(Debugger debugger, DebugEventKind eventKind, Func<IEventBreakpoint, IDebugEventContext, bool> cond) {
			this.debugger = debugger;
			this.eventKind = eventKind;
			this.cond = cond;
			this.isEnabled = true;
		}

		public void Remove() {
			debugger.Remove(this);
		}

		public void Initialize(DnDebugger dbg) {
			Debug.Assert(debugger.Dispatcher.CheckAccess());
			Debug.Assert(dbgBreakpoint == null);
			if (dbgBreakpoint != null)
				throw new InvalidOperationException();
			dbgBreakpoint = dbg.CreateBreakpoint(eventKind.ToDebugEventBreakpointKind(), HitHandler);
			dbgBreakpoint.IsEnabled = isEnabled;
			dbgBreakpoint.Tag = this;
		}

		bool HitHandler(DebugEventBreakpointConditionContext ctx) {
			if (cond == null)
				return true;

			Debug.Assert(ctx.DebugEventBreakpoint == dbgBreakpoint);

			var dectx = ctx.EventArgs.TryCreateDebugEventContext(debugger);
			if (dectx == null)
				return false;
			return cond(this, dectx);
		}
	}
}

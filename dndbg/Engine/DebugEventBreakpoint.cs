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
	public enum DebugEventBreakpointType {
		CreateProcess,
		ExitProcess,
		CreateThread,
		ExitThread,
		LoadModule,
		UnloadModule,
		LoadClass,
		UnloadClass,
		LogMessage,
		LogSwitch,
		CreateAppDomain,
		ExitAppDomain,
		LoadAssembly,
		UnloadAssembly,
		ControlCTrap,
		NameChange,
		UpdateModuleSymbols,
		MDANotification,
		CustomNotification,
	}

	public sealed class DebugEventBreakpointConditionContext : BreakpointConditionContext {
		public override Breakpoint Breakpoint {
			get { return bp; }
		}

		public DebugEventBreakpoint DebugEventBreakpoint {
			get { return bp; }
		}
		readonly DebugEventBreakpoint bp;

		public DebugEventBreakpointConditionContext(DnDebugger debugger, DebugEventBreakpoint bp)
			: base(debugger) {
			this.bp = bp;
		}
	}

	public sealed class DebugEventBreakpoint : Breakpoint {
		public DebugEventBreakpointType EventType {
			get { return eventType; }
		}
		readonly DebugEventBreakpointType eventType;

		internal DebugEventBreakpoint(DebugEventBreakpointType eventType, IBreakpointCondition bpCond)
			: base(bpCond) {
			this.eventType = eventType;
		}

		public static DebugEventBreakpointType? GetDebugEventBreakpointType(DebugCallbackEventArgs e) {
			switch (e.Type) {
			case DebugCallbackType.CreateProcess:		return DebugEventBreakpointType.CreateProcess;
			case DebugCallbackType.ExitProcess:			return DebugEventBreakpointType.ExitProcess;
			case DebugCallbackType.CreateThread:		return DebugEventBreakpointType.CreateThread;
			case DebugCallbackType.ExitThread:			return DebugEventBreakpointType.ExitThread;
			case DebugCallbackType.LoadModule:			return DebugEventBreakpointType.LoadModule;
			case DebugCallbackType.UnloadModule:		return DebugEventBreakpointType.UnloadModule;
			case DebugCallbackType.LoadClass:			return DebugEventBreakpointType.LoadClass;
			case DebugCallbackType.UnloadClass:			return DebugEventBreakpointType.UnloadClass;
			case DebugCallbackType.LogMessage:			return DebugEventBreakpointType.LogMessage;
			case DebugCallbackType.LogSwitch:			return DebugEventBreakpointType.LogSwitch;
			case DebugCallbackType.CreateAppDomain:		return DebugEventBreakpointType.CreateAppDomain;
			case DebugCallbackType.ExitAppDomain:		return DebugEventBreakpointType.ExitAppDomain;
			case DebugCallbackType.LoadAssembly:		return DebugEventBreakpointType.LoadAssembly;
			case DebugCallbackType.UnloadAssembly:		return DebugEventBreakpointType.UnloadAssembly;
			case DebugCallbackType.ControlCTrap:		return DebugEventBreakpointType.ControlCTrap;
			case DebugCallbackType.NameChange:			return DebugEventBreakpointType.NameChange;
			case DebugCallbackType.UpdateModuleSymbols:	return DebugEventBreakpointType.UpdateModuleSymbols;
			case DebugCallbackType.MDANotification:		return DebugEventBreakpointType.MDANotification;
			case DebugCallbackType.CustomNotification:	return DebugEventBreakpointType.CustomNotification;
			default: return null;
			}
		}
	}
}

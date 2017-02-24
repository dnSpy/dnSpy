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

namespace dndbg.Engine {
	enum DebugEventBreakpointKind {
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

	sealed class DebugEventBreakpointConditionContext : BreakpointConditionContext {
		public override DnBreakpoint Breakpoint => DebugEventBreakpoint;
		public DnDebugEventBreakpoint DebugEventBreakpoint { get; }
		public DebugCallbackEventArgs EventArgs { get; }

		public DebugEventBreakpointConditionContext(DnDebugger debugger, DnDebugEventBreakpoint bp, DebugCallbackEventArgs e)
			: base(debugger) {
			DebugEventBreakpoint = bp;
			EventArgs = e;
		}
	}

	sealed class DnDebugEventBreakpoint : DnBreakpoint {
		internal Func<DebugEventBreakpointConditionContext, bool> Condition { get; }
		public DebugEventBreakpointKind EventKind { get; }

		internal DnDebugEventBreakpoint(DebugEventBreakpointKind eventKind, Func<DebugEventBreakpointConditionContext, bool> cond) {
			EventKind = eventKind;
			Condition = cond ?? defaultCond;
		}
		static readonly Func<DebugEventBreakpointConditionContext, bool> defaultCond = a => true;

		public static DebugEventBreakpointKind? GetDebugEventBreakpointKind(DebugCallbackEventArgs e) {
			switch (e.Kind) {
			case DebugCallbackKind.CreateProcess:		return DebugEventBreakpointKind.CreateProcess;
			case DebugCallbackKind.ExitProcess:			return DebugEventBreakpointKind.ExitProcess;
			case DebugCallbackKind.CreateThread:		return DebugEventBreakpointKind.CreateThread;
			case DebugCallbackKind.ExitThread:			return DebugEventBreakpointKind.ExitThread;
			case DebugCallbackKind.LoadModule:			return DebugEventBreakpointKind.LoadModule;
			case DebugCallbackKind.UnloadModule:		return DebugEventBreakpointKind.UnloadModule;
			case DebugCallbackKind.LoadClass:			return DebugEventBreakpointKind.LoadClass;
			case DebugCallbackKind.UnloadClass:			return DebugEventBreakpointKind.UnloadClass;
			case DebugCallbackKind.LogMessage:			return DebugEventBreakpointKind.LogMessage;
			case DebugCallbackKind.LogSwitch:			return DebugEventBreakpointKind.LogSwitch;
			case DebugCallbackKind.CreateAppDomain:		return DebugEventBreakpointKind.CreateAppDomain;
			case DebugCallbackKind.ExitAppDomain:		return DebugEventBreakpointKind.ExitAppDomain;
			case DebugCallbackKind.LoadAssembly:		return DebugEventBreakpointKind.LoadAssembly;
			case DebugCallbackKind.UnloadAssembly:		return DebugEventBreakpointKind.UnloadAssembly;
			case DebugCallbackKind.ControlCTrap:		return DebugEventBreakpointKind.ControlCTrap;
			case DebugCallbackKind.NameChange:			return DebugEventBreakpointKind.NameChange;
			case DebugCallbackKind.UpdateModuleSymbols:	return DebugEventBreakpointKind.UpdateModuleSymbols;
			case DebugCallbackKind.MDANotification:		return DebugEventBreakpointKind.MDANotification;
			case DebugCallbackKind.CustomNotification:	return DebugEventBreakpointKind.CustomNotification;
			default: return null;
			}
		}
	}
}

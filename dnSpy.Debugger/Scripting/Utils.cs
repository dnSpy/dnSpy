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
using System.IO;
using dnSpy.Contracts.Scripting.Debugger;

using DBG = dndbg.Engine;

namespace dnSpy.Debugger.Scripting {
	static class Utils {
		public static DebuggerProcessState Convert(DBG.DebuggerProcessState state) {
			switch (state) {
			case DBG.DebuggerProcessState.Starting:		return DebuggerProcessState.Starting;
			case DBG.DebuggerProcessState.Continuing:	return DebuggerProcessState.Continuing;
			case DBG.DebuggerProcessState.Running:		return DebuggerProcessState.Running;
			case DBG.DebuggerProcessState.Paused:		return DebuggerProcessState.Paused;
			case DBG.DebuggerProcessState.Terminated:	return DebuggerProcessState.Terminated;
			default: Debug.Fail("Invalid state");		return (DebuggerProcessState)(-1);
			}
		}

		static DBG.BreakProcessKind Convert(BreakProcessKind kind) {
			switch (kind) {
			case BreakProcessKind.None:						return DBG.BreakProcessKind.None;
			case BreakProcessKind.CreateProcess:			return DBG.BreakProcessKind.CreateProcess;
			case BreakProcessKind.CreateAppDomain:			return DBG.BreakProcessKind.CreateAppDomain;
			case BreakProcessKind.LoadModule:				return DBG.BreakProcessKind.LoadModule;
			case BreakProcessKind.LoadClass:				return DBG.BreakProcessKind.LoadClass;
			case BreakProcessKind.CreateThread:				return DBG.BreakProcessKind.CreateThread;
			case BreakProcessKind.ExeLoadModule:			return DBG.BreakProcessKind.ExeLoadModule;
			case BreakProcessKind.ExeLoadClass:				return DBG.BreakProcessKind.ExeLoadClass;
			case BreakProcessKind.ModuleCctorOrEntryPoint:	return DBG.BreakProcessKind.ModuleCctorOrEntryPoint;
			case BreakProcessKind.EntryPoint:				return DBG.BreakProcessKind.EntryPoint;
			default: Debug.Fail("Invalid break kind");		return (DBG.BreakProcessKind)(-1);
			}
		}

		public static DBG.DebugProcessOptions Convert(DebugOptions options, IDebuggerSettings settings, DBG.CLRTypeDebugInfo info) {
			if (options == null)
				throw new ArgumentNullException();
			var o = new DBG.DebugProcessOptions(info);
			o.Filename = options.Filename;
			o.CommandLine = options.CommandLine;
			o.CurrentDirectory = options.CurrentDirectory;
			o.DebugMessageDispatcher = WpfDebugMessageDispatcher.Instance;
			o.BreakProcessKind = Convert(options.BreakProcessKind);
			o.DebugOptions.IgnoreBreakInstructions = settings.IgnoreBreakInstructions;
			return o;
		}

		public static DBG.AttachProcessOptions Convert(AttachOptions options, IDebuggerSettings settings, string debuggeeVersion = null) {
			if (options == null)
				throw new ArgumentNullException();
			var o = new DBG.AttachProcessOptions(new DBG.DesktopCLRTypeAttachInfo(debuggeeVersion));
			o.ProcessId = options.ProcessId;
			o.DebugMessageDispatcher = WpfDebugMessageDispatcher.Instance;
			o.DebugOptions.IgnoreBreakInstructions = settings.IgnoreBreakInstructions;
			return o;
		}

		public static DBG.CorType[] ToCorType(this IDebuggerType[] types) {
			if (types == null)
				return null;
			var ctypes = new DBG.CorType[types.Length];
			for (int i = 0; i < types.Length; i++) {
				var t = (DebuggerType)types[i];
				ctypes[i] = t.CorType;
			}
			return ctypes;
		}

		public static DBG.SerializedDnModule ToSerializedDnModule(this ModuleName moduleName) {
			return new DBG.SerializedDnModule(moduleName.AssemblyFullName, moduleName.Name, moduleName.IsDynamic, moduleName.IsInMemory, moduleName.ModuleNameOnly);
		}

		public static ModuleName ToModuleName(this DBG.SerializedDnModule serMod) {
			return new ModuleName(serMod.AssemblyFullName, serMod.ModuleName, serMod.IsDynamic, serMod.IsInMemory, serMod.ModuleNameOnly);
		}

		public static DBG.DebugEventBreakpointKind ToDebugEventBreakpointKind(this DebugEventKind eventKind) {
			switch (eventKind) {
			case DebugEventKind.CreateProcess:		return DBG.DebugEventBreakpointKind.CreateProcess;
			case DebugEventKind.ExitProcess:		return DBG.DebugEventBreakpointKind.ExitProcess;
			case DebugEventKind.CreateThread:		return DBG.DebugEventBreakpointKind.CreateThread;
			case DebugEventKind.ExitThread:			return DBG.DebugEventBreakpointKind.ExitThread;
			case DebugEventKind.LoadModule:			return DBG.DebugEventBreakpointKind.LoadModule;
			case DebugEventKind.UnloadModule:		return DBG.DebugEventBreakpointKind.UnloadModule;
			case DebugEventKind.LoadClass:			return DBG.DebugEventBreakpointKind.LoadClass;
			case DebugEventKind.UnloadClass:		return DBG.DebugEventBreakpointKind.UnloadClass;
			case DebugEventKind.LogMessage:			return DBG.DebugEventBreakpointKind.LogMessage;
			case DebugEventKind.LogSwitch:			return DBG.DebugEventBreakpointKind.LogSwitch;
			case DebugEventKind.CreateAppDomain:	return DBG.DebugEventBreakpointKind.CreateAppDomain;
			case DebugEventKind.ExitAppDomain:		return DBG.DebugEventBreakpointKind.ExitAppDomain;
			case DebugEventKind.LoadAssembly:		return DBG.DebugEventBreakpointKind.LoadAssembly;
			case DebugEventKind.UnloadAssembly:		return DBG.DebugEventBreakpointKind.UnloadAssembly;
			case DebugEventKind.ControlCTrap:		return DBG.DebugEventBreakpointKind.ControlCTrap;
			case DebugEventKind.NameChange:			return DBG.DebugEventBreakpointKind.NameChange;
			case DebugEventKind.UpdateModuleSymbols:return DBG.DebugEventBreakpointKind.UpdateModuleSymbols;
			case DebugEventKind.MDANotification:	return DBG.DebugEventBreakpointKind.MDANotification;
			case DebugEventKind.CustomNotification:	return DBG.DebugEventBreakpointKind.CustomNotification;
			default:
				Debug.Fail("Invalid event kind: " + eventKind);
				return (DBG.DebugEventBreakpointKind)(-1);
			}
		}

		public static DebugEventKind ToDebugEventKind(this DBG.DebugCallbackKind eventKind) {
			switch (eventKind) {
			case DBG.DebugCallbackKind.CreateProcess:		return DebugEventKind.CreateProcess;
			case DBG.DebugCallbackKind.ExitProcess:			return DebugEventKind.ExitProcess;
			case DBG.DebugCallbackKind.CreateThread:		return DebugEventKind.CreateThread;
			case DBG.DebugCallbackKind.ExitThread:			return DebugEventKind.ExitThread;
			case DBG.DebugCallbackKind.LoadModule:			return DebugEventKind.LoadModule;
			case DBG.DebugCallbackKind.UnloadModule:		return DebugEventKind.UnloadModule;
			case DBG.DebugCallbackKind.LoadClass:			return DebugEventKind.LoadClass;
			case DBG.DebugCallbackKind.UnloadClass:			return DebugEventKind.UnloadClass;
			case DBG.DebugCallbackKind.LogMessage:			return DebugEventKind.LogMessage;
			case DBG.DebugCallbackKind.LogSwitch:			return DebugEventKind.LogSwitch;
			case DBG.DebugCallbackKind.CreateAppDomain:		return DebugEventKind.CreateAppDomain;
			case DBG.DebugCallbackKind.ExitAppDomain:		return DebugEventKind.ExitAppDomain;
			case DBG.DebugCallbackKind.LoadAssembly:		return DebugEventKind.LoadAssembly;
			case DBG.DebugCallbackKind.UnloadAssembly:		return DebugEventKind.UnloadAssembly;
			case DBG.DebugCallbackKind.ControlCTrap:		return DebugEventKind.ControlCTrap;
			case DBG.DebugCallbackKind.NameChange:			return DebugEventKind.NameChange;
			case DBG.DebugCallbackKind.UpdateModuleSymbols:	return DebugEventKind.UpdateModuleSymbols;
			case DBG.DebugCallbackKind.MDANotification:		return DebugEventKind.MDANotification;
			case DBG.DebugCallbackKind.CustomNotification:	return DebugEventKind.CustomNotification;
			default:
				Debug.Fail("Invalid event kind: " + eventKind);
				return (DebugEventKind)(-1);
			}
		}

		public static DebugEventContext TryCreateDebugEventContext(this DBG.DebugCallbackEventArgs e, Debugger debugger) {
			Debug.Assert(debugger.Dispatcher.CheckAccess());
			switch (e.Kind) {
			case DBG.DebugCallbackKind.CreateProcess:
			case DBG.DebugCallbackKind.ExitProcess:
				return new ProcessEventContext(debugger, (DBG.ProcessDebugCallbackEventArgs)e);

			case DBG.DebugCallbackKind.CreateThread:
			case DBG.DebugCallbackKind.ExitThread:
				return new ThreadEventContext(debugger, (DBG.ThreadDebugCallbackEventArgs)e);

			case DBG.DebugCallbackKind.LoadModule:
			case DBG.DebugCallbackKind.UnloadModule:
				return new ModuleEventContext(debugger, (DBG.ModuleDebugCallbackEventArgs)e);

			case DBG.DebugCallbackKind.LoadClass:
			case DBG.DebugCallbackKind.UnloadClass:
				return new ClassEventContext(debugger, (DBG.ClassDebugCallbackEventArgs)e);

			case DBG.DebugCallbackKind.LogMessage:
				return new LogMessageEventContext(debugger, (DBG.LogMessageDebugCallbackEventArgs)e);

			case DBG.DebugCallbackKind.LogSwitch:
				return new LogSwitchEventContext(debugger, (DBG.LogSwitchDebugCallbackEventArgs)e);

			case DBG.DebugCallbackKind.CreateAppDomain:
			case DBG.DebugCallbackKind.ExitAppDomain:
				return new AppDomainEventContext(debugger, (DBG.AppDomainDebugCallbackEventArgs)e);

			case DBG.DebugCallbackKind.LoadAssembly:
			case DBG.DebugCallbackKind.UnloadAssembly:
				return new AssemblyEventContext(debugger, (DBG.AssemblyDebugCallbackEventArgs)e);

			case DBG.DebugCallbackKind.ControlCTrap:
				return new ControlCTrapEventContext(debugger, (DBG.ControlCTrapDebugCallbackEventArgs)e);

			case DBG.DebugCallbackKind.NameChange:
				return new NameChangeEventContext(debugger, (DBG.NameChangeDebugCallbackEventArgs)e);

			case DBG.DebugCallbackKind.UpdateModuleSymbols:
				return new UpdateModuleSymbolsEventContext(debugger, (DBG.UpdateModuleSymbolsDebugCallbackEventArgs)e);

			case DBG.DebugCallbackKind.MDANotification:
				return new MDANotificationEventContext(debugger, (DBG.MDANotificationDebugCallbackEventArgs)e);

			case DBG.DebugCallbackKind.CustomNotification:
				return new CustomNotificationEventContext(debugger, (DBG.CustomNotificationDebugCallbackEventArgs)e);

			default:
				Debug.Fail("Invalid event kind: " + e.Kind);
				return null;
			}
		}

		public static DBG.CorValueResult ToCorValueResult(this ValueResult value) {
			return value.IsValid ? new DBG.CorValueResult(value.Value) : new DBG.CorValueResult();
		}

		public static bool IsSameFile(string filename, string nameToMatch) {
			if (StringComparer.OrdinalIgnoreCase.Equals(filename, nameToMatch))
				return true;
			if (StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileName(filename), nameToMatch))
				return true;
			if (StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileNameWithoutExtension(filename), nameToMatch))
				return true;

			return false;
		}
	}
}

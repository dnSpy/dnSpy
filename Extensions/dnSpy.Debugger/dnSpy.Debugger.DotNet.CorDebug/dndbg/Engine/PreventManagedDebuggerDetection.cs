/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using System.Runtime.InteropServices;

namespace dndbg.Engine {
	sealed class PreventManagedDebuggerDetection {
		public static void Initialize(DnDebugger debugger) => new PreventManagedDebuggerDetection(debugger);

		// Make sure both of these work in x86 and x64 mode
		static readonly byte[] returnFalse_x86 = new byte[] { 0x33, 0xC0, 0xC3 };		// xor eax,eax / retn
		static readonly byte[] returnTrue_x86 = new byte[] { 0x6A, 0x01, 0x58, 0xC3 };	// push 1 / pop eax / retn
		readonly byte[] returnFalse;
		readonly byte[] returnTrue;

		PreventManagedDebuggerDetection(DnDebugger debugger) {
			// We only allow debugging on the same computer
			switch (RuntimeInformation.ProcessArchitecture) {
			case Architecture.X86:
			case Architecture.X64:
				returnFalse = returnFalse_x86;
				returnTrue = returnTrue_x86;
				break;

			case Architecture.Arm:
			case Architecture.Arm64:
				Debug.Fail($"Unsupported CPU arch {RuntimeInformation.ProcessArchitecture}");
				return;

			default:
				throw new InvalidOperationException($"Unknown CPU arch: {RuntimeInformation.ProcessArchitecture}");
			}
			debugger.DebugCallbackEvent += DnDebugger_DebugCallbackEvent;
		}

		void DnDebugger_DebugCallbackEvent(DnDebugger dbg, DebugCallbackEventArgs e) {
			if (e.Kind == DebugCallbackKind.CreateProcess) {
				dbg.DebugCallbackEvent -= DnDebugger_DebugCallbackEvent;
				var cpArgs = (CreateProcessDebugCallbackEventArgs)e;
				bool b = Initialize(cpArgs.CorProcess, dbg.DebuggeeVersion, dbg.CLRPath);
				Debug.Assert(b);
			}
		}

		bool Initialize(CorProcess process, string debuggeeVersion, string clrPath) {
			try {
				var mgr = new ECallManager(process.ProcessId, clrPath);
				if (!mgr.FoundClrModule)
					return false;

				const string debuggerClassName = "System.Diagnostics.Debugger";
				bool error = false, b;

				bool isClrV2OrOlder =
					debuggeeVersion != null &&
					(debuggeeVersion.StartsWith("v1.", StringComparison.OrdinalIgnoreCase) ||
					debuggeeVersion.StartsWith("v2.", StringComparison.OrdinalIgnoreCase));

				// Launch() returns true in CLR 4.x and false in earlier CLR versions when there's
				// no debugger. At least on my system...
				b = mgr.FindFunc(debuggerClassName, "LaunchInternal", out ulong addr);
				error |= !b;
				if (b)
					error |= !(isClrV2OrOlder ? WriteReturnFalse(process, addr) : WriteReturnTrue(process, addr));

				b = mgr.FindFunc(debuggerClassName, "get_IsAttached", out addr);
				if (!b)
					b = mgr.FindFunc(debuggerClassName, "IsDebuggerAttached", out addr);
				error |= !b;
				if (b)
					error |= !WriteReturnFalse(process, addr);

				b = mgr.FindFunc(debuggerClassName, "IsLogging", out addr);
				error |= !b;
				if (b)
					error |= !WriteReturnFalse(process, addr);

				return !error;
			}
			catch {
				return false;
			}
		}

		bool WriteReturnFalse(CorProcess process, ulong addr) => WriteBytes(process, addr, returnFalse);
		bool WriteReturnTrue(CorProcess process, ulong addr) => WriteBytes(process, addr, returnTrue);

		unsafe static bool WriteBytes(CorProcess process, ulong addr, byte[] data) {
			if (process == null || addr == 0 || data == null)
				return false;

			var hProcess = process.Handle;
			if (!NativeMethods.VirtualProtectEx(hProcess, new IntPtr((void*)addr), new IntPtr(data.Length), NativeMethods.PAGE_EXECUTE_READWRITE, out uint oldProtect))
				return false;
			int sizeWritten;
			try {
				process.WriteMemory(addr, data, 0, data.Length, out sizeWritten);
			}
			finally {
				NativeMethods.VirtualProtectEx(hProcess, new IntPtr((void*)addr), new IntPtr(data.Length), oldProtect, out oldProtect);
			}

			return sizeWritten == data.Length;
		}
	}
}

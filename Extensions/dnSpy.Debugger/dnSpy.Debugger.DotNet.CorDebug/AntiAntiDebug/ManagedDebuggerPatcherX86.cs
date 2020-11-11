/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Diagnostics.CodeAnalysis;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.AntiAntiDebug;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;

namespace dnSpy.Debugger.DotNet.CorDebug.AntiAntiDebug {
	struct ManagedDebuggerPatcherX86 {
		// xor eax,eax / retn
		static readonly byte[] returnFalse_x86 = new byte[] { 0x33, 0xC0, 0xC3 };
		// push 1 / pop eax / retn
		static readonly byte[] returnTrue_x86 = new byte[] { 0x6A, 0x01, 0x58, 0xC3 };

		readonly DbgProcess process;
		readonly DbgCorDebugInternalRuntime runtime;

		public ManagedDebuggerPatcherX86(DbgNativeFunctionHookContext context, DbgCorDebugInternalRuntime runtime) {
			if (context is null)
				throw new ArgumentNullException(nameof(context));
			process = context.Process;
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
		}

		public bool TryPatch([NotNullWhen(false)] out string? errorMessage) {
			var mgr = new ECallManager(process.Id, runtime.ClrFilename);
			if (!mgr.FoundClrModule) {
				errorMessage = $"Couldn't find the CLR dll file: {runtime.ClrFilename}";
				return false;
			}

			const string debuggerClassName = "System.Diagnostics.Debugger";

			var debuggeeVersion = runtime.Version.Version;
			bool isClrV2OrOlder =
				debuggeeVersion is not null &&
				(debuggeeVersion.StartsWith("v1.", StringComparison.OrdinalIgnoreCase) ||
				debuggeeVersion.StartsWith("v2.", StringComparison.OrdinalIgnoreCase));

			// Launch() returns true in CLR 4.x and false in earlier CLR versions when there's
			// no debugger. At least on my system...
			bool b = mgr.FindFunc(debuggerClassName, "LaunchInternal", out ulong addr);
			if (b) {
				if (isClrV2OrOlder)
					WriteReturnFalse(addr);
				else
					WriteReturnTrue(addr);
			}

			b = mgr.FindFunc(debuggerClassName, "get_IsAttached", out addr);
			if (!b)
				b = mgr.FindFunc(debuggerClassName, "IsDebuggerAttached", out addr);
			if (b)
				WriteReturnFalse(addr);

			b = mgr.FindFunc(debuggerClassName, "IsLogging", out addr);
			if (b)
				WriteReturnFalse(addr);

			errorMessage = null;
			return true;
		}

		void WriteReturnFalse(ulong addr) => process.WriteMemory(addr, returnFalse_x86);
		void WriteReturnTrue(ulong addr) => process.WriteMemory(addr, returnTrue_x86);
	}
}

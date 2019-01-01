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

using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.AntiAntiDebug;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;

namespace dnSpy.Debugger.DotNet.CorDebug.AntiAntiDebug {
	[ExportDbgNativeFunctionHook("clr.dll", "System.Diagnostics.Debugger", new DbgArchitecture[0], new[] { DbgOperatingSystem.Windows })]
	sealed class ManagedDebuggerHook : IDbgNativeFunctionHook {
		readonly DebuggerSettings debuggerSettings;

		[ImportingConstructor]
		ManagedDebuggerHook(DebuggerSettings debuggerSettings) => this.debuggerSettings = debuggerSettings;

		public bool IsEnabled(DbgNativeFunctionHookContext context) {
			if (!debuggerSettings.PreventManagedDebuggerDetection)
				return false;

			return CorDebugUtils.TryGetInternalRuntime(context.Process, out _);
		}

		public void Hook(DbgNativeFunctionHookContext context, out string errorMessage) {
			if (!CorDebugUtils.TryGetInternalRuntime(context.Process, out var runtime)) {
				errorMessage = "Couldn't find CorDebug runtime";
				return;
			}

			switch (context.Process.Architecture) {
			case DbgArchitecture.X86:
			case DbgArchitecture.X64:
				HookX86(context, runtime, out errorMessage);
				break;

			default:
				Debug.Fail($"Unsupported architecture: {context.Process.Architecture}");
				errorMessage = $"Unsupported architecture: {context.Process.Architecture}";
				break;
			}
		}

		void HookX86(DbgNativeFunctionHookContext context, DbgCorDebugInternalRuntime runtime, out string errorMessage) =>
			new ManagedDebuggerPatcherX86(context, runtime).TryPatch(out errorMessage);
	}
}

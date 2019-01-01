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

namespace dnSpy.Debugger.AntiAntiDebug {
	static class IsDebuggerPresentConstants {
		public const string DllName = "kernel32.dll";
		public const string FuncName = "IsDebuggerPresent";
	}

	[ExportDbgNativeFunctionHook(IsDebuggerPresentConstants.DllName, IsDebuggerPresentConstants.FuncName, new DbgArchitecture[0], new[] { DbgOperatingSystem.Windows })]
	sealed class IsDebuggerPresentHook : IDbgNativeFunctionHook {
		readonly DebuggerSettings debuggerSettings;

		[ImportingConstructor]
		IsDebuggerPresentHook(DebuggerSettings debuggerSettings) => this.debuggerSettings = debuggerSettings;

		public bool IsEnabled(DbgNativeFunctionHookContext context) => debuggerSettings.AntiIsDebuggerPresent;

		public void Hook(DbgNativeFunctionHookContext context, out string errorMessage) {
			switch (context.Process.Architecture) {
			case DbgArchitecture.X86:
				HookX86(context, out errorMessage);
				break;

			case DbgArchitecture.X64:
				HookX64(context, out errorMessage);
				break;

			default:
				Debug.Fail($"Unsupported architecture: {context.Process.Architecture}");
				errorMessage = $"Unsupported architecture: {context.Process.Architecture}";
				break;
			}
		}

		void HookX86(DbgNativeFunctionHookContext context, out string errorMessage) =>
			new IsDebuggerPresentPatcherX86(context).TryPatchX86(out errorMessage);

		void HookX64(DbgNativeFunctionHookContext context, out string errorMessage) =>
			new IsDebuggerPresentPatcherX86(context).TryPatchX64(out errorMessage);
	}
}

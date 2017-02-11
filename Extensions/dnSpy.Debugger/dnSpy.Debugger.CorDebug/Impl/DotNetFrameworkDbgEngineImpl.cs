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
using dndbg.Engine;
using dnSpy.Contracts.Debugger.CorDebug;
using dnSpy.Contracts.Debugger.Engine;

namespace dnSpy.Debugger.CorDebug.Impl {
	sealed class DotNetFrameworkDbgEngineImpl : DbgEngineImpl {
		public DotNetFrameworkDbgEngineImpl(DbgStartKind startKind)
			: base(startKind) {
		}

		internal void Start(DotNetFrameworkStartDebuggingOptions options) =>
			ExecDebugThreadAsync(StartCore, options);

		void StartCore(object arg) {
			DotNetFrameworkStartDebuggingOptions options = null;
			try {
				options = (DotNetFrameworkStartDebuggingOptions)arg;
				var dbgOptions = new DebugProcessOptions(new DesktopCLRTypeDebugInfo()) {
					DebugMessageDispatcher = new WpfDebugMessageDispatcher(debuggerDispatcher),
					CurrentDirectory = options.WorkingDirectory,
					Filename = options.Filename,
					CommandLine = options.CommandLine,
					BreakProcessKind = options.BreakProcessKind.ToDndbg(),
				};
				dbgOptions.DebugOptions.IgnoreBreakInstructions = options.IgnoreBreakInstructions;

				dnDebugger = DnDebugger.DebugProcess(dbgOptions);
				if (options.DisableManagedDebuggerDetection)
					DisableSystemDebuggerDetection.Initialize(dnDebugger);

				dnDebugger.DebugCallbackEvent += DnDebugger_DebugCallbackEvent;
				return;
			}
			catch (Exception ex) {
				HandleExceptionInStart(ex, options?.Filename);
				return;
			}
		}
	}
}

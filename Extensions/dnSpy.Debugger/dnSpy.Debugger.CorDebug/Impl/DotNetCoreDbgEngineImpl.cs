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
using System.IO;
using dndbg.Engine;
using dnSpy.Contracts.Debugger.CorDebug;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Debugger.CorDebug.Properties;

namespace dnSpy.Debugger.CorDebug.Impl {
	sealed class DotNetCoreDbgEngineImpl : DbgEngineImpl {
		public DotNetCoreDbgEngineImpl(DbgStartKind startKind)
			: base(startKind) {
		}

		internal void Start(DotNetCoreStartDebuggingOptions options) =>
			ExecDebugThreadAsync(StartCore, options);

		void StartCore(object arg) {
			DotNetCoreStartDebuggingOptions options = null;
			try {
				options = (DotNetCoreStartDebuggingOptions)arg;
				var dbgShimFilename = DotNetCoreHelpers.GetDebugShimFilename(IntPtr.Size * 8);
				if (!File.Exists(dbgShimFilename))
					throw new Exception("Couldn't find dbgshim.dll");
				string hostFilename, hostCommandLine;
				if (string.IsNullOrEmpty(options.Host)) {
					hostFilename = DotNetCoreHelpers.GetPathToDotNetExeHost(IntPtr.Size * 8);
					if (!File.Exists(hostFilename))
						throw new Exception(string.Format(dnSpy_Debugger_CorDebug_Resources.Error_CouldNotFindDotNetCoreHost, DotNetCoreHelpers.DotNetExeName));
					hostCommandLine = "exec";
				}
				else {
					hostFilename = options.Host;
					hostCommandLine = options.CommandLine ?? string.Empty;
				}
				var dbgOptions = new DebugProcessOptions(new CoreCLRTypeDebugInfo(dbgShimFilename, hostFilename, hostCommandLine)) {
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

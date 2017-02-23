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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CorDebug;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Debugger.CorDebug.Properties;

namespace dnSpy.Debugger.CorDebug.Impl {
	sealed class DotNetCoreDbgEngineImpl : DbgEngineImpl {
		protected override CorDebugRuntimeKind CorDebugRuntimeKind => CorDebugRuntimeKind.DotNetCore;

		public DotNetCoreDbgEngineImpl(DbgManager dbgManager, DbgStartKind startKind)
			: base(dbgManager, startKind) {
		}

		protected override CLRTypeDebugInfo CreateDebugInfo(CorDebugStartDebuggingOptions options) {
			var dncOptions = (DotNetCoreStartDebuggingOptions)options;

			var dbgShimFilename = DotNetCoreHelpers.GetDebugShimFilename(IntPtr.Size * 8);
			if (!File.Exists(dbgShimFilename))
				throw new Exception("Couldn't find dbgshim.dll");
			string hostFilename, hostCommandLine;
			if (string.IsNullOrWhiteSpace(dncOptions.Host)) {
				hostFilename = DotNetCoreHelpers.GetPathToDotNetExeHost(IntPtr.Size * 8);
				if (!File.Exists(hostFilename))
					throw new Exception(string.Format(dnSpy_Debugger_CorDebug_Resources.Error_CouldNotFindDotNetCoreHost, DotNetCoreHelpers.DotNetExeName));
				if (string.IsNullOrWhiteSpace(dncOptions.HostArguments))
					hostCommandLine = "exec";
				else
					hostCommandLine = dncOptions.HostArguments;
			}
			else {
				hostFilename = dncOptions.Host;
				hostCommandLine = dncOptions.HostArguments ?? string.Empty;
			}
			return new CoreCLRTypeDebugInfo(dbgShimFilename, hostFilename, hostCommandLine);
		}
	}
}

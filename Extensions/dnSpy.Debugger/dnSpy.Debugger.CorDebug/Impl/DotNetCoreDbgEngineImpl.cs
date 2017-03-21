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
using System.Diagnostics;
using System.IO;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Debugger.CorDebug.DAC;
using dnSpy.Debugger.CorDebug.Properties;
using dnSpy.Debugger.CorDebug.Utilities;

namespace dnSpy.Debugger.CorDebug.Impl {
	sealed class DotNetCoreDbgEngineImpl : DbgEngineImpl {
		protected override CorDebugRuntimeKind CorDebugRuntimeKind => CorDebugRuntimeKind.DotNetCore;
		public override string Debugging => "CoreCLR";

		public override DbgEngineRuntimeInfo RuntimeInfo {
			get {
				Debug.Assert(runtimeInfo != null);
				return runtimeInfo;
			}
		}
		DbgEngineRuntimeInfo runtimeInfo;

		int Bitness => IntPtr.Size * 8;

		public DotNetCoreDbgEngineImpl(ClrDacProvider clrDacProvider, DbgManager dbgManager, DbgStartKind startKind)
			: base(clrDacProvider, dbgManager, startKind) {
		}

		string GetDbgShimAndVerify() {
			var dbgShimFilename = DotNetCoreHelpers.GetDebugShimFilename(Bitness);
			if (!File.Exists(dbgShimFilename))
				throw new Exception("Couldn't find dbgshim.dll");
			return dbgShimFilename;
		}

		protected override CLRTypeDebugInfo CreateDebugInfo(CorDebugStartDebuggingOptions options) {
			var dncOptions = (DotNetCoreStartDebuggingOptions)options;
			string hostFilename, hostCommandLine;
			if (string.IsNullOrWhiteSpace(dncOptions.Host)) {
				hostFilename = DotNetCoreHelpers.GetPathToDotNetExeHost(Bitness);
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
			return new CoreCLRTypeDebugInfo(GetDbgShimAndVerify(), hostFilename, hostCommandLine);
		}

		protected override CLRTypeAttachInfo CreateAttachInfo(CorDebugAttachDebuggingOptions options) {
			var dncOptions = (DotNetCoreAttachDebuggingOptions)options;
			return new CoreCLRTypeAttachInfo(dncOptions.ClrModuleVersion, GetDbgShimAndVerify(), dncOptions.CoreCLRFilename);
		}

		protected override void OnDebugProcess(DnDebugger dnDebugger) =>
			runtimeInfo = new DbgEngineRuntimeInfo("CoreCLR", new DotNetCoreRuntimeId(dnDebugger.OtherVersion));
	}

	sealed class DotNetCoreRuntimeId : RuntimeId {
		readonly string version;
		public DotNetCoreRuntimeId(string version) => this.version = version;
		public override bool Equals(object obj) => obj is DotNetCoreRuntimeId other && StringComparer.Ordinal.Equals(version, other.version);
		public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(version);
	}
}

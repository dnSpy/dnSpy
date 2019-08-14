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
using System.Collections.ObjectModel;
using System.Diagnostics;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Debugger.DotNet.CorDebug.Impl.Attach;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	sealed class DotNetFrameworkDbgEngineImpl : DbgEngineImpl {
		protected override CorDebugRuntimeKind CorDebugRuntimeKind => CorDebugRuntimeKind.DotNetFramework;
		public override string[] Debugging => debugging;
		static readonly string[] debugging = new[] { "CLR" };

		public override DbgEngineRuntimeInfo RuntimeInfo {
			get {
				Debug2.Assert(!(runtimeInfo is null));
				return runtimeInfo;
			}
		}
		DbgEngineRuntimeInfo? runtimeInfo;

		public DotNetFrameworkDbgEngineImpl(DbgEngineImplDependencies deps, DbgManager dbgManager, DbgStartKind startKind)
			: base(deps, dbgManager, startKind) {
		}

		protected override CLRTypeDebugInfo CreateDebugInfo(CorDebugStartDebuggingOptions options) =>
			new DesktopCLRTypeDebugInfo();

		protected override CLRTypeAttachInfo CreateAttachInfo(CorDebugAttachToProgramOptions options) =>
			new DesktopCLRTypeAttachInfo(((DotNetFrameworkAttachToProgramOptions)options).DebuggeeVersion);

		protected override void OnDebugProcess(DnDebugger dnDebugger) =>
			runtimeInfo = new DbgEngineRuntimeInfo(PredefinedDbgRuntimeGuids.DotNetFramework_Guid, PredefinedDbgRuntimeKindGuids.DotNet_Guid, "CLR " + dnDebugger.DebuggeeVersion, new DotNetFrameworkRuntimeId(dnDebugger.DebuggeeVersion), runtimeTags);
		static readonly ReadOnlyCollection<string> runtimeTags = new ReadOnlyCollection<string>(new[] {
			PredefinedDotNetDbgRuntimeTags.DotNet,
			PredefinedDotNetDbgRuntimeTags.DotNetFramework,
		});
	}

	sealed class DotNetFrameworkRuntimeId : RuntimeId {
		readonly string version;
		public DotNetFrameworkRuntimeId(string version) => this.version = version;
		public override bool Equals(object? obj) => obj is DotNetFrameworkRuntimeId other && StringComparer.Ordinal.Equals(version, other.version);
		public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(version);
	}
}

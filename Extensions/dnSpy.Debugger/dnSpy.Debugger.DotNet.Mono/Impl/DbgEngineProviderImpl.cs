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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Mono;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Debugger.DotNet.Mono.Impl.Attach;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	[ExportDbgEngineProvider]
	sealed class DbgEngineProviderImpl : DbgEngineProvider {
		readonly Lazy<DbgEngineImplDependencies> dbgEngineImplDependencies;

		[ImportingConstructor]
		DbgEngineProviderImpl(Lazy<DbgEngineImplDependencies> dbgEngineImplDependencies) =>
			this.dbgEngineImplDependencies = dbgEngineImplDependencies;

		public override DbgEngine? Create(DbgManager dbgManager, DebugProgramOptions options) {
			switch (options) {
			case MonoStartDebuggingOptions _:
			case MonoConnectStartDebuggingOptions _:
				return new DbgEngineImpl(dbgEngineImplDependencies.Value, dbgManager, MonoDebugRuntimeKind.Mono);

			case UnityConnectStartDebuggingOptions _:
			case UnityAttachToProgramOptions _:
			case UnityStartDebuggingOptions _:
				return new DbgEngineImpl(dbgEngineImplDependencies.Value, dbgManager, MonoDebugRuntimeKind.Unity);
			}

			return null;
		}
	}
}

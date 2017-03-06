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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Debugger.CorDebug.DAC;

namespace dnSpy.Debugger.CorDebug.Impl {
	[ExportDbgEngineProvider]
	sealed class DbgEngineProviderImpl : DbgEngineProvider {
		readonly Lazy<ClrDacProvider> clrDacProvider;

		[ImportingConstructor]
		DbgEngineProviderImpl(Lazy<ClrDacProvider> clrDacProvider) {
			this.clrDacProvider = clrDacProvider;
		}

		public override DbgEngine Create(DbgManager dbgManager, StartDebuggingOptions options) {
			if (options is DotNetFrameworkStartDebuggingOptions dnfOptions)
				return StartDotNetFramework(dbgManager, dnfOptions);

			if (options is DotNetCoreStartDebuggingOptions dncOptions)
				return StartDotNetCore(dbgManager, dncOptions);

			return null;
		}

		DbgEngine StartDotNetFramework(DbgManager dbgManager, DotNetFrameworkStartDebuggingOptions dnfOptions) =>
			new DotNetFrameworkDbgEngineImpl(clrDacProvider.Value, dbgManager, DbgStartKind.Start);

		DbgEngine StartDotNetCore(DbgManager dbgManager, DotNetCoreStartDebuggingOptions dncOptions) =>
			new DotNetCoreDbgEngineImpl(clrDacProvider.Value, dbgManager, DbgStartKind.Start);
	}
}

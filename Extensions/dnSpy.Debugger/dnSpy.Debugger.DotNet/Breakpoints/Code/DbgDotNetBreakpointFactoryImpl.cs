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
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.DotNet.Breakpoints.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Debugger.DotNet.Breakpoints.Code {
	[Export(typeof(DbgDotNetBreakpointFactory))]
	sealed class DbgDotNetBreakpointFactoryImpl : DbgDotNetBreakpointFactory {
		readonly DbgDispatcher dbgDispatcher;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		readonly DbgDotNetCodeLocationFactory dbgDotNetCodeLocationFactory;

		[ImportingConstructor]
		DbgDotNetBreakpointFactoryImpl(DbgDispatcher dbgDispatcher, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService, DbgDotNetCodeLocationFactory dbgDotNetCodeLocationFactory) {
			this.dbgDispatcher = dbgDispatcher;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			this.dbgDotNetCodeLocationFactory = dbgDotNetCodeLocationFactory;
		}

		public override DbgCodeBreakpoint[] Create(DbgDotNetBreakpointInfo[] breakpoints) =>
			dbgCodeBreakpointsService.Value.Add(breakpoints.Select(a => new DbgCodeBreakpointInfo(dbgDotNetCodeLocationFactory.Create(a.Module, a.Token, a.Offset), a.Settings)).ToArray());

		public override DbgCodeBreakpoint? TryGetBreakpoint(ModuleId module, uint token, uint offset) {
			var loc = dbgDotNetCodeLocationFactory.Create(module, token, offset);
			try {
				return dbgCodeBreakpointsService.Value.TryGetBreakpoint(loc);
			}
			finally {
				dbgDispatcher.BeginInvoke(() => loc.Close(dbgDispatcher));
			}
		}
	}
}

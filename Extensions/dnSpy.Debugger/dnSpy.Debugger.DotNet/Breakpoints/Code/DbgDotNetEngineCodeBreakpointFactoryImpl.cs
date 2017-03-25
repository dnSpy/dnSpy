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
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.DotNet.Breakpoints.Code;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Debugger.DotNet.Breakpoints.Code {
	[Export(typeof(DbgDotNetEngineCodeBreakpointFactory))]
	sealed class DbgDotNetEngineCodeBreakpointFactoryImpl : DbgDotNetEngineCodeBreakpointFactory {
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;

		[ImportingConstructor]
		DbgDotNetEngineCodeBreakpointFactoryImpl(Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService) =>
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;

		public override DbgCodeBreakpoint Create(ModuleId module, uint token, uint offset, DbgCodeBreakpointSettings bpSettings) {
			var dnbp = new DbgDotNetEngineCodeBreakpointImpl(module, token, offset);
			return dbgCodeBreakpointsService.Value.Add(new DbgCodeBreakpointInfo(dnbp, bpSettings));
		}
	}
}

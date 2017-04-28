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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.CallStack;

namespace dnSpy.Debugger.CallStack {
	[Export(typeof(DbgStackFrameBreakpointLocationService))]
	sealed class DbgStackFrameBreakpointLocationServiceImpl : DbgStackFrameBreakpointLocationService {
		readonly Lazy<DbgStackFrameBreakpointLocationProvider, IDbgStackFrameBreakpointLocationProviderMetadata>[] dbgStackFrameBreakpointLocationProviders;

		[ImportingConstructor]
		DbgStackFrameBreakpointLocationServiceImpl([ImportMany] IEnumerable<Lazy<DbgStackFrameBreakpointLocationProvider, IDbgStackFrameBreakpointLocationProviderMetadata>> dbgStackFrameBreakpointLocationProviders) =>
			this.dbgStackFrameBreakpointLocationProviders = dbgStackFrameBreakpointLocationProviders.OrderBy(a => a.Metadata.Order).ToArray();

		public override DbgBreakpointLocation Create(DbgStackFrameLocation location) {
			if (location == null)
				return null;
			foreach (var lz in dbgStackFrameBreakpointLocationProviders) {
				var bpLoc = lz.Value.Create(location);
				if (bpLoc != null)
					return bpLoc;
			}
			return null;
		}
	}
}

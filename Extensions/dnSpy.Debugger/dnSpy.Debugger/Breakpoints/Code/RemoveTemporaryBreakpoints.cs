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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;

namespace dnSpy.Debugger.Breakpoints.Code {
	[Export(typeof(IDbgManagerStartListener))]
	sealed class RemoveTemporaryBreakpoints : IDbgManagerStartListener {
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;

		[ImportingConstructor]
		RemoveTemporaryBreakpoints(Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService) =>
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) => dbgManager.IsDebuggingChanged += DbgManager_IsDebuggingChanged;

		void DbgManager_IsDebuggingChanged(object? sender, EventArgs e) {
			var dbgManager = (DbgManager)sender!;
			if (!dbgManager.IsDebugging) {
				List<DbgCodeBreakpoint>? bpsToRemove = null;
				foreach (var bp in dbgCodeBreakpointsService.Value.Breakpoints) {
					if (bp.IsTemporary) {
						if (bpsToRemove is null)
							bpsToRemove = new List<DbgCodeBreakpoint>();
						bpsToRemove.Add(bp);
					}
				}
				if (!(bpsToRemove is null))
					dbgCodeBreakpointsService.Value.Remove(bpsToRemove.ToArray());
			}
		}
	}
}

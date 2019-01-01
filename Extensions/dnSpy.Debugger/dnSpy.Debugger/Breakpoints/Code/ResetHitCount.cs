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
using dnSpy.Contracts.Debugger.Breakpoints.Code;

namespace dnSpy.Debugger.Breakpoints.Code {
	/// <summary>
	/// Resets the hit count whenever the user adds a new hit count option. This matches VS behavior
	/// </summary>
	[Export(typeof(IDbgCodeBreakpointsServiceListener))]
	sealed class ResetHitCount : IDbgCodeBreakpointsServiceListener {
		readonly Lazy<DbgCodeBreakpointHitCountService> dbgCodeBreakpointHitCountService;

		[ImportingConstructor]
		ResetHitCount(Lazy<DbgCodeBreakpointHitCountService> dbgCodeBreakpointHitCountService) =>
			this.dbgCodeBreakpointHitCountService = dbgCodeBreakpointHitCountService;

		void IDbgCodeBreakpointsServiceListener.Initialize(DbgCodeBreakpointsService dbgCodeBreakpointsService) =>
			dbgCodeBreakpointsService.BreakpointsModified += DbgCodeBreakpointsService_BreakpointsModified;

		void DbgCodeBreakpointsService_BreakpointsModified(object sender, DbgBreakpointsModifiedEventArgs e) {
			List<DbgCodeBreakpoint> resetThese = null;
			foreach (var info in e.Breakpoints) {
				if (info.OldSettings.HitCount != null)
					continue;
				var breakpoint = info.Breakpoint;
				if (breakpoint.Settings.HitCount == null)
					continue;
				if (resetThese == null)
					resetThese = new List<DbgCodeBreakpoint>();
				resetThese.Add(breakpoint);
			}
			if (resetThese != null)
				dbgCodeBreakpointHitCountService.Value.Reset(resetThese.ToArray());
		}
	}
}

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
using dnSpy.Contracts.Debugger.Breakpoints.Code;

namespace dnSpy.Debugger.Breakpoints.Code.CondChecker {
	[ExportDbgManagerStartListener]
	sealed class BreakpointBreakChecker : IDbgManagerStartListener {
		readonly Lazy<DbgCodeBreakpointHitCountService2> dbgCodeBreakpointHitCountService;
		readonly Lazy<DbgCodeBreakpointFilterChecker> dbgCodeBreakpointFilterChecker;
		readonly Lazy<DbgCodeBreakpointHitCountChecker> dbgCodeBreakpointHitCountChecker;
		readonly Lazy<DbgCodeBreakpointConditionChecker> dbgCodeBreakpointConditionChecker;
		readonly Lazy<DbgCodeBreakpointTraceMessagePrinter> dbgCodeBreakpointTraceMessagePrinter;

		[ImportingConstructor]
		BreakpointBreakChecker(Lazy<DbgCodeBreakpointHitCountService2> dbgCodeBreakpointHitCountService, Lazy<DbgCodeBreakpointFilterChecker> dbgCodeBreakpointFilterChecker, Lazy<DbgCodeBreakpointHitCountChecker> dbgCodeBreakpointHitCountChecker, Lazy<DbgCodeBreakpointConditionChecker> dbgCodeBreakpointConditionChecker, Lazy<DbgCodeBreakpointTraceMessagePrinter> dbgCodeBreakpointTraceMessagePrinter) {
			this.dbgCodeBreakpointHitCountService = dbgCodeBreakpointHitCountService;
			this.dbgCodeBreakpointFilterChecker = dbgCodeBreakpointFilterChecker;
			this.dbgCodeBreakpointHitCountChecker = dbgCodeBreakpointHitCountChecker;
			this.dbgCodeBreakpointConditionChecker = dbgCodeBreakpointConditionChecker;
			this.dbgCodeBreakpointTraceMessagePrinter = dbgCodeBreakpointTraceMessagePrinter;
		}

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) => dbgManager.Message += DbgManager_Message;

		void DbgManager_Message(object sender, DbgMessageEventArgs e) {
			if (e.Kind == DbgMessageKind.BoundBreakpoint) {
				var be = (DbgMessageBoundBreakpointEventArgs)e;
				e.Pause = ShouldBreak(be.BoundBreakpoint, be.Thread);
			}
		}

		bool ShouldBreak(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread thread) {
			var bp = boundBreakpoint.Breakpoint;
			if (bp.IsClosed || boundBreakpoint.IsClosed)
				return false;

			var settings = bp.Settings;
			if (!settings.IsEnabled)
				return false;

			// Don't reorder these checks. This seems to be the order VS' debugger checks everything:
			//		condition && hitCount && filter

			if (settings.Condition is DbgCodeBreakpointCondition condition) {
				if (!dbgCodeBreakpointConditionChecker.Value.ShouldBreak(boundBreakpoint, thread, condition))
					return false;
			}

			// This counts as a hit, even if there's no 'hit count' option
			int currentHitCount = dbgCodeBreakpointHitCountService.Value.Hit_DbgThread(boundBreakpoint.Breakpoint);
			if (settings.HitCount is DbgCodeBreakpointHitCount hitCount) {
				if (!dbgCodeBreakpointHitCountChecker.Value.ShouldBreak(boundBreakpoint, thread, hitCount, currentHitCount))
					return false;
			}

			if (settings.Filter is DbgCodeBreakpointFilter filter) {
				if (!dbgCodeBreakpointFilterChecker.Value.ShouldBreak(boundBreakpoint, thread, filter))
					return false;
			}

			if (settings.Trace is DbgCodeBreakpointTrace trace) {
				dbgCodeBreakpointTraceMessagePrinter.Value.Print(boundBreakpoint, thread, trace);
				return !trace.Continue;
			}
			return true;
		}
	}
}

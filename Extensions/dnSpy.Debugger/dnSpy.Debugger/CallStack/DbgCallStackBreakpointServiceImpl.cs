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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Debugger.Impl;

namespace dnSpy.Debugger.CallStack {
	[Export(typeof(DbgCallStackBreakpointService))]
	sealed class DbgCallStackBreakpointServiceImpl : DbgCallStackBreakpointService {
		readonly object lockObj;
		readonly List<DbgObject> objsToClose;
		readonly DbgDispatcher dbgDispatcher;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		readonly Lazy<DbgStackFrameBreakpointLocationService> dbgStackFrameBreakpointLocationService;

		[ImportingConstructor]
		DbgCallStackBreakpointServiceImpl(DbgDispatcher dbgDispatcher, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService, Lazy<DbgStackFrameBreakpointLocationService> dbgStackFrameBreakpointLocationService) {
			lockObj = new object();
			objsToClose = new List<DbgObject>();
			this.dbgDispatcher = dbgDispatcher;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			this.dbgStackFrameBreakpointLocationService = dbgStackFrameBreakpointLocationService;
		}

		public override DbgCodeBreakpoint Create(DbgStackFrameLocation location) {
			var bpLoc = dbgStackFrameBreakpointLocationService.Value.Create(location);
			if (bpLoc == null)
				return null;
			return dbgCodeBreakpointsService.Value.Add(new DbgCodeBreakpointInfo(bpLoc, new DbgCodeBreakpointSettings { IsEnabled = true }));
		}

		public override DbgCodeBreakpoint TryGetBreakpoint(DbgStackFrameLocation location) {
			var bpLoc = dbgStackFrameBreakpointLocationService.Value.Create(location);
			if (bpLoc == null)
				return null;
			try {
				return dbgCodeBreakpointsService.Value.TryGetBreakpoint(bpLoc);
			}
			finally {
				Close(bpLoc);
			}
		}

		void Close(DbgObject obj) {
			bool start;
			lock (lockObj) {
				objsToClose.Add(obj);
				start = objsToClose.Count == 1;
			}
			if (start)
				dbgDispatcher.DispatcherThread.BeginInvoke(() => CloseAllObjs_DbgThread());
		}

		void CloseAllObjs_DbgThread() {
			dbgDispatcher.DispatcherThread.VerifyAccess();
			DbgObject[] objs;
			lock (lockObj) {
				objs = objsToClose.ToArray();
				objsToClose.Clear();
			}
			foreach (var obj in objs)
				obj.Close(dbgDispatcher.DispatcherThread);
		}
	}
}

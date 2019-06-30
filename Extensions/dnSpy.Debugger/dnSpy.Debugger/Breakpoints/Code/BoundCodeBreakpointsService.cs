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
	abstract class BoundCodeBreakpointsService {
		public abstract event EventHandler<DbgCollectionChangedEventArgs<DbgCodeBreakpoint>> BreakpointsChanged;
		public abstract event EventHandler<DbgBreakpointsModifiedEventArgs> BreakpointsModified;
		public abstract DbgCodeBreakpoint[] Breakpoints { get; }
		public abstract DbgBoundCodeBreakpoint[] AddBoundBreakpoints_DbgThread(IList<DbgBoundCodeBreakpoint> boundBreakpoints);
		public abstract void RemoveBoundBreakpoints_DbgThread(IList<DbgBoundCodeBreakpoint> boundBreakpoints);
		public abstract DbgBoundCodeBreakpoint[] RemoveBoundBreakpoints_DbgThread(DbgRuntime runtime);
	}

	[Export(typeof(BoundCodeBreakpointsService))]
	sealed class BoundCodeBreakpointsServiceImpl : BoundCodeBreakpointsService {
		public override event EventHandler<DbgCollectionChangedEventArgs<DbgCodeBreakpoint>> BreakpointsChanged;
		public override event EventHandler<DbgBreakpointsModifiedEventArgs> BreakpointsModified;
		public override DbgCodeBreakpoint[] Breakpoints => dbgCodeBreakpointsService.Breakpoints;

		readonly DbgManager dbgManager;
		readonly DbgCodeBreakpointsService2 dbgCodeBreakpointsService;

		[ImportingConstructor]
		BoundCodeBreakpointsServiceImpl(DbgManager dbgManager, DbgCodeBreakpointsService2 dbgCodeBreakpointsService) {
			this.dbgManager = dbgManager;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			dbgCodeBreakpointsService.BreakpointsChanged += DbgCodeBreakpointsService_BreakpointsChanged;
			dbgCodeBreakpointsService.BreakpointsModified += DbgCodeBreakpointsService_BreakpointsModified;
			dbgManager.Dispatcher.BeginInvoke(() => dbgCodeBreakpointsService.UpdateIsDebugging_DbgThread(dbgManager.IsDebugging));
			dbgManager.IsDebuggingChanged += DbgManager_IsDebuggingChanged;
		}

		void DbgCodeBreakpointsService_BreakpointsChanged(object sender, DbgCollectionChangedEventArgs<DbgCodeBreakpoint> e) =>
			BreakpointsChanged?.Invoke(this, e);

		void DbgCodeBreakpointsService_BreakpointsModified(object sender, DbgBreakpointsModifiedEventArgs e) =>
			BreakpointsModified?.Invoke(this, e);

		void DbgManager_IsDebuggingChanged(object sender, EventArgs e) =>
			dbgCodeBreakpointsService.UpdateIsDebugging_DbgThread(dbgManager.IsDebugging);

		public override DbgBoundCodeBreakpoint[] AddBoundBreakpoints_DbgThread(IList<DbgBoundCodeBreakpoint> boundBreakpoints) =>
			dbgCodeBreakpointsService.AddBoundBreakpoints_DbgThread(boundBreakpoints);

		public override void RemoveBoundBreakpoints_DbgThread(IList<DbgBoundCodeBreakpoint> boundBreakpoints) =>
			dbgCodeBreakpointsService.RemoveBoundBreakpoints_DbgThread(boundBreakpoints);

		public override DbgBoundCodeBreakpoint[] RemoveBoundBreakpoints_DbgThread(DbgRuntime runtime) =>
			dbgCodeBreakpointsService.RemoveBoundBreakpoints_DbgThread(runtime);
	}
}

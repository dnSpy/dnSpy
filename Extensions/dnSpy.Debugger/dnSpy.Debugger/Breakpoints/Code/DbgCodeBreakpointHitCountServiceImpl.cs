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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Debugger.Impl;

namespace dnSpy.Debugger.Breakpoints.Code {
	abstract class DbgCodeBreakpointHitCountService2 : DbgCodeBreakpointHitCountService {
		public abstract int Hit_DbgThread(DbgCodeBreakpoint breakpoint);
	}

	[Export(typeof(DbgCodeBreakpointHitCountService))]
	[Export(typeof(DbgCodeBreakpointHitCountService2))]
	[ExportDbgManagerStartListener]
	sealed class DbgCodeBreakpointHitCountServiceImpl : DbgCodeBreakpointHitCountService2, IDbgManagerStartListener {
		public override event EventHandler<DbgHitCountChangedEventArgs> HitCountChanged;

		readonly object lockObj;
		readonly DbgDispatcher dbgDispatcher;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		Dictionary<DbgCodeBreakpoint, int> bpToHitCount;
		DbgManager dbgManager;

		[ImportingConstructor]
		DbgCodeBreakpointHitCountServiceImpl(DbgDispatcher dbgDispatcher, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService) {
			lockObj = new object();
			this.dbgDispatcher = dbgDispatcher;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			bpToHitCount = new Dictionary<DbgCodeBreakpoint, int>();
		}

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) {
			lock (lockObj)
				this.dbgManager = dbgManager;
			dbgManager.IsDebuggingChanged += DbgManager_IsDebuggingChanged;
		}

		void Dbg(Action action) => dbgDispatcher.DispatcherThread.BeginInvoke(action);

		void DbgManager_IsDebuggingChanged(object sender, EventArgs e) {
			var dbgManager = (DbgManager)sender;
			DbgCodeBreakpointAndHitCount[] infos;
			if (dbgManager.IsDebugging) {
				dbgCodeBreakpointsService.Value.BreakpointsChanged += DbgCodeBreakpointsService_BreakpointsChanged;
				infos = dbgCodeBreakpointsService.Value.Breakpoints.Select(a => new DbgCodeBreakpointAndHitCount(a, 0)).ToArray();
			}
			else {
				dbgCodeBreakpointsService.Value.BreakpointsChanged -= DbgCodeBreakpointsService_BreakpointsChanged;
				infos = dbgCodeBreakpointsService.Value.Breakpoints.Select(a => new DbgCodeBreakpointAndHitCount(a, null)).ToArray();
			}
			lock (lockObj)
				bpToHitCount = new Dictionary<DbgCodeBreakpoint, int>();
			if (infos.Length > 0)
				HitCountChanged?.Invoke(this, new DbgHitCountChangedEventArgs(new ReadOnlyCollection<DbgCodeBreakpointAndHitCount>(infos)));
		}

		void DbgCodeBreakpointsService_BreakpointsChanged(object sender, DbgCollectionChangedEventArgs<DbgCodeBreakpoint> e) {
			if (!e.Added) {
				lock (lockObj) {
					foreach (var bp in e.Objects)
						bpToHitCount.Remove(bp);
				}
			}
		}

		public override int? GetHitCount(DbgCodeBreakpoint breakpoint) {
			if (breakpoint == null)
				throw new ArgumentNullException(nameof(breakpoint));
			lock (lockObj) {
				if (dbgManager?.IsDebugging != true)
					return null;
				if (bpToHitCount.TryGetValue(breakpoint, out var hitCount))
					return hitCount;
			}
			return 0;
		}

		public override int Hit_DbgThread(DbgCodeBreakpoint breakpoint) {
			dbgDispatcher.VerifyAccess();
			if (breakpoint == null)
				throw new ArgumentNullException(nameof(breakpoint));
			int hitCount;
			lock (lockObj) {
				if (!bpToHitCount.TryGetValue(breakpoint, out hitCount))
					hitCount = 0;
				hitCount++;
				bpToHitCount[breakpoint] = hitCount;
			}
			HitCountChanged?.Invoke(this, new DbgHitCountChangedEventArgs(new ReadOnlyCollection<DbgCodeBreakpointAndHitCount>(new[] { new DbgCodeBreakpointAndHitCount(breakpoint, hitCount) })));
			return hitCount;
		}

		public override void Reset(DbgCodeBreakpoint[] breakpoints) {
			if (breakpoints == null)
				throw new ArgumentNullException(nameof(breakpoints));
			Dbg(() => Reset_DbgThread(breakpoints));
		}

		void Reset_DbgThread(DbgCodeBreakpoint[] breakpoints) {
			dbgDispatcher.VerifyAccess();
			List<DbgCodeBreakpointAndHitCount> updated = null;
			lock (lockObj) {
				var defaultHitCount = dbgManager?.IsDebugging == true ? 0 : (int?)null;
				foreach (var bp in breakpoints) {
					if (bpToHitCount.Remove(bp)) {
						if (updated == null)
							updated = new List<DbgCodeBreakpointAndHitCount>();
						updated.Add(new DbgCodeBreakpointAndHitCount(bp, defaultHitCount));
					}
				}
			}
			if (updated != null)
				HitCountChanged?.Invoke(this, new DbgHitCountChangedEventArgs(new ReadOnlyCollection<DbgCodeBreakpointAndHitCount>(updated)));
		}
	}
}

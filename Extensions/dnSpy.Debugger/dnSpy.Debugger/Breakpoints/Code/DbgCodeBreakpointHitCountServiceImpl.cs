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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Debugger.Impl;

namespace dnSpy.Debugger.Breakpoints.Code {
	abstract class DbgCodeBreakpointHitCountService2 : DbgCodeBreakpointHitCountService {
		public abstract int Hit_DbgThread(DbgCodeBreakpoint breakpoint);
	}

	[Export(typeof(DbgCodeBreakpointHitCountService))]
	[Export(typeof(DbgCodeBreakpointHitCountService2))]
	[Export(typeof(IDbgManagerStartListener))]
	sealed class DbgCodeBreakpointHitCountServiceImpl : DbgCodeBreakpointHitCountService2, IDbgManagerStartListener {
		public override event EventHandler<DbgHitCountChangedEventArgs>? HitCountChanged;

		readonly object lockObj;
		readonly DbgDispatcherProvider dbgDispatcherProvider;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		Dictionary<DbgCodeBreakpoint, int> bpToHitCount;
		DbgManager? dbgManager;

		[ImportingConstructor]
		DbgCodeBreakpointHitCountServiceImpl(DbgDispatcherProvider dbgDispatcherProvider, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService) {
			lockObj = new object();
			this.dbgDispatcherProvider = dbgDispatcherProvider;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			bpToHitCount = new Dictionary<DbgCodeBreakpoint, int>();
		}

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) {
			lock (lockObj)
				this.dbgManager = dbgManager;
			dbgManager.IsRunningChanged += DbgManager_IsRunningChanged;
			dbgManager.IsDebuggingChanged += DbgManager_IsDebuggingChanged;
		}

		void Dbg(Action callback) => dbgDispatcherProvider.Dispatcher.BeginInvoke(callback);

		void DbgManager_IsRunningChanged(object? sender, EventArgs e) {
			var dbgManager = (DbgManager)sender!;
			// Make sure it gets updated immediately without a delay
			if (dbgManager.IsRunning == false)
				FlushPendingHitCountChanged_DbgThread();
		}

		void DbgManager_IsDebuggingChanged(object? sender, EventArgs e) {
			StopAndClearPendingHitCountChangedTimer();
			var dbgManager = (DbgManager)sender!;
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

		void DbgCodeBreakpointsService_BreakpointsChanged(object? sender, DbgCollectionChangedEventArgs<DbgCodeBreakpoint> e) {
			if (!e.Added) {
				lock (lockObj) {
					foreach (var bp in e.Objects)
						bpToHitCount.Remove(bp);
				}
			}
		}

		public override int? GetHitCount(DbgCodeBreakpoint breakpoint) {
			if (breakpoint is null)
				throw new ArgumentNullException(nameof(breakpoint));
			lock (lockObj)
				return GetHitCount_NoLock_DbgThread(breakpoint);
		}

		int? GetHitCount_NoLock_DbgThread(DbgCodeBreakpoint breakpoint) {
			if (dbgManager?.IsDebugging != true)
				return null;
			if (bpToHitCount.TryGetValue(breakpoint, out var hitCount))
				return hitCount;
			return 0;
		}

		public override int Hit_DbgThread(DbgCodeBreakpoint breakpoint) {
			dbgDispatcherProvider.VerifyAccess();
			Debug2.Assert(!(dbgManager is null));
			if (breakpoint is null)
				throw new ArgumentNullException(nameof(breakpoint));
			int hitCount;
			bool start;
			lock (lockObj) {
				if (!bpToHitCount.TryGetValue(breakpoint, out hitCount))
					hitCount = 0;
				hitCount++;
				bpToHitCount[breakpoint] = hitCount;

				start = dbgManager.IsRunning == false || pendingHitCountChanged.Count == 0;
				pendingHitCountChanged.Add(breakpoint);
				if (start) {
					StopPendingHitCountChangedTimer_NoLock();
					if (dbgManager.IsRunning == false)
						Dbg(() => FlushPendingHitCountChanged_DbgThread());
					else {
						pendingHitCountChangedTimer = new Timer(HITCOUNT_CHANGED_DELAY_MS);
						pendingHitCountChangedTimer.Elapsed += PendingHitCountChangedTimer_Elapsed;
						pendingHitCountChangedTimer.Start();
					}
				}
			}

			return hitCount;
		}
		const double HITCOUNT_CHANGED_DELAY_MS = 250;
		readonly HashSet<DbgCodeBreakpoint> pendingHitCountChanged = new HashSet<DbgCodeBreakpoint>();
		Timer? pendingHitCountChangedTimer;

		void StopPendingHitCountChangedTimer_NoLock() {
			pendingHitCountChangedTimer?.Stop();
			pendingHitCountChangedTimer?.Dispose();
			pendingHitCountChangedTimer = null;
		}

		void StopAndClearPendingHitCountChangedTimer() {
			lock (lockObj) {
				pendingHitCountChanged.Clear();
				StopPendingHitCountChangedTimer_NoLock();
			}
		}

		void PendingHitCountChangedTimer_Elapsed(object? sender, ElapsedEventArgs e) {
			lock (lockObj)
				pendingHitCountChangedTimer?.Stop();
			Dbg(() => FlushPendingHitCountChanged_DbgThread());
		}

		void FlushPendingHitCountChanged_DbgThread() {
			dbgDispatcherProvider.VerifyAccess();
			DbgCodeBreakpointAndHitCount[] breakpoints;
			lock (lockObj) {
				StopPendingHitCountChangedTimer_NoLock();
				breakpoints = pendingHitCountChanged.Where(a => !a.IsClosed).Select(a => new DbgCodeBreakpointAndHitCount(a, GetHitCount_NoLock_DbgThread(a))).ToArray();
				pendingHitCountChanged.Clear();
			}
			if (breakpoints.Length > 0)
				HitCountChanged?.Invoke(this, new DbgHitCountChangedEventArgs(new ReadOnlyCollection<DbgCodeBreakpointAndHitCount>(breakpoints)));
		}

		public override void Reset(DbgCodeBreakpoint[] breakpoints) {
			if (breakpoints is null)
				throw new ArgumentNullException(nameof(breakpoints));
			Dbg(() => Reset_DbgThread(breakpoints));
		}

		void Reset_DbgThread(DbgCodeBreakpoint[] breakpoints) {
			dbgDispatcherProvider.VerifyAccess();
			List<DbgCodeBreakpointAndHitCount>? updated = null;
			bool raisePendingEvent;
			lock (lockObj) {
				raisePendingEvent = pendingHitCountChanged.Count != 0;
				var defaultHitCount = dbgManager?.IsDebugging == true ? 0 : (int?)null;
				foreach (var bp in breakpoints) {
					if (bpToHitCount.Remove(bp)) {
						if (updated is null)
							updated = new List<DbgCodeBreakpointAndHitCount>();
						updated.Add(new DbgCodeBreakpointAndHitCount(bp, defaultHitCount));
					}
				}
			}
			if (raisePendingEvent)
				FlushPendingHitCountChanged_DbgThread();
			if (!(updated is null))
				HitCountChanged?.Invoke(this, new DbgHitCountChangedEventArgs(new ReadOnlyCollection<DbgCodeBreakpointAndHitCount>(updated)));
		}
	}
}

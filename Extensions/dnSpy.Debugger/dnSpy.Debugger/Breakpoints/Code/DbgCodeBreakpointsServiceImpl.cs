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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Debugger.Impl;

namespace dnSpy.Debugger.Breakpoints.Code {
	interface IDbgCodeBreakpointsServiceListener {
		void Initialize(DbgCodeBreakpointsService dbgCodeBreakpointsService);
	}

	sealed class DbgCodeBreakpointsServiceImpl : DbgCodeBreakpointsService {
		readonly object lockObj;
		readonly HashSet<DbgCodeBreakpointImpl> breakpoints;
		readonly DbgDispatcher dbgDispatcher;
		int breakpointId;

		internal DbgDispatcher DbgDispatcher => dbgDispatcher;

		[ImportingConstructor]
		DbgCodeBreakpointsServiceImpl(DbgDispatcher dbgDispatcher, [ImportMany] IEnumerable<Lazy<IDbgCodeBreakpointsServiceListener>> dbgCodeBreakpointsServiceListener) {
			lockObj = new object();
			breakpoints = new HashSet<DbgCodeBreakpointImpl>();
			this.dbgDispatcher = dbgDispatcher;
			breakpointId = -1;

			foreach (var lz in dbgCodeBreakpointsServiceListener)
				lz.Value.Initialize(this);
		}

		void Dbg(Action action) => dbgDispatcher.Dbg(action);

		public override void Modify(DbgCodeBreakpointAndSettings[] settings) {
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));
			Dbg(() => ModifyCore(settings));
		}

		void ModifyCore(DbgCodeBreakpointAndSettings[] settings) {
			dbgDispatcher.VerifyAccess();
			var bps = new List<DbgCodeBreakpoint>(settings.Length);
			lock (lockObj) {
				foreach (var info in settings) {
					var bpImpl = info.Breakpoint as DbgCodeBreakpointImpl;
					Debug.Assert(bpImpl != null);
					if (bpImpl == null)
						continue;
					Debug.Assert(breakpoints.Contains(bpImpl));
					if (!breakpoints.Contains(bpImpl))
						continue;
					bps.Add(bpImpl);
					bpImpl.WriteSettings(info.Settings);
				}
			}
			if (bps.Count > 0)
				BreakpointsModified?.Invoke(this, new DbgBreakpointsModifiedEventArgs(new ReadOnlyCollection<DbgCodeBreakpoint>(bps)));
		}

		public override event EventHandler<DbgBreakpointsModifiedEventArgs> BreakpointsModified;

		public override event EventHandler<DbgCollectionChangedEventArgs<DbgCodeBreakpoint>> BreakpointsChanged;
		public override DbgCodeBreakpoint[] Breakpoints {
			get {
				lock (lockObj)
					return breakpoints.ToArray();
			}
		}

		public override DbgCodeBreakpoint[] Add(DbgCodeBreakpointInfo[] breakpoints) {
			if (breakpoints == null)
				throw new ArgumentNullException(nameof(breakpoints));
			// Return a copy since the caller could modify the array
			var bps = new DbgCodeBreakpoint[breakpoints.Length];
			var bpImpls = new DbgCodeBreakpointImpl[breakpoints.Length];
			for (int i = 0; i < bps.Length; i++) {
				var bp = new DbgCodeBreakpointImpl(this, Interlocked.Increment(ref breakpointId), breakpoints[i].EngineBreakpoint, breakpoints[i].Settings);
				bps[i] = bp;
				bpImpls[i] = bp;
			}
			Dbg(() => AddCore(bpImpls));
			return bps;
		}

		void AddCore(DbgCodeBreakpointImpl[] breakpoints) {
			dbgDispatcher.VerifyAccess();
			var added = new List<DbgCodeBreakpoint>(breakpoints.Length);
			lock (lockObj) {
				foreach (var bp in breakpoints) {
					Debug.Assert(!this.breakpoints.Contains(bp));
					if (this.breakpoints.Contains(bp))
						continue;
					added.Add(bp);
					this.breakpoints.Add(bp);
				}
			}
			if (added.Count > 0)
				BreakpointsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgCodeBreakpoint>(added, added: true));
		}

		public override void Remove(DbgCodeBreakpoint[] breakpoints) {
			if (breakpoints == null)
				throw new ArgumentNullException(nameof(breakpoints));
			Dbg(() => RemoveCore(breakpoints));
		}

		void RemoveCore(DbgCodeBreakpoint[] breakpoints) {
			dbgDispatcher.VerifyAccess();
			var removed = new List<DbgCodeBreakpoint>(breakpoints.Length);
			lock (lockObj) {
				foreach (var bp in breakpoints) {
					var bpImpl = bp as DbgCodeBreakpointImpl;
					Debug.Assert(bpImpl != null);
					if (bpImpl == null)
						continue;
					Debug.Assert(this.breakpoints.Contains(bpImpl));
					if (!this.breakpoints.Contains(bpImpl))
						continue;
					removed.Add(bpImpl);
					this.breakpoints.Remove(bpImpl);
				}
			}
			if (removed.Count > 0) {
				BreakpointsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgCodeBreakpoint>(removed, added: false));
				foreach (var bp in removed)
					bp.Close(dbgDispatcher.DispatcherThread);
			}
		}

		public override void Clear() => Dbg(() => ClearCore());
		void ClearCore() {
			dbgDispatcher.VerifyAccess();
			DbgCodeBreakpoint[] removed;
			lock (lockObj) {
				removed = breakpoints.ToArray();
				breakpoints.Clear();
			}
			if (removed.Length > 0) {
				BreakpointsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgCodeBreakpoint>(removed, added: false));
				foreach (var bp in removed)
					bp.Close(dbgDispatcher.DispatcherThread);
			}
		}
	}
}

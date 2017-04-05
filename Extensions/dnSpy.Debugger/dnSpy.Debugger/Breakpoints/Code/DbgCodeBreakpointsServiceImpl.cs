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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Debugger.Impl;

namespace dnSpy.Debugger.Breakpoints.Code {
	[Export(typeof(DbgCodeBreakpointsService))]
	sealed class DbgCodeBreakpointsServiceImpl : DbgCodeBreakpointsService {
		readonly object lockObj;
		readonly HashSet<DbgCodeBreakpointImpl> breakpoints;
		readonly Dictionary<DbgBreakpointLocation, DbgCodeBreakpointImpl> locationToBreakpoint;
		readonly DbgDispatcher dbgDispatcher;
		int breakpointId;

		internal DbgDispatcher DbgDispatcher => dbgDispatcher;

		[ImportingConstructor]
		DbgCodeBreakpointsServiceImpl(DbgDispatcher dbgDispatcher, [ImportMany] IEnumerable<Lazy<IDbgCodeBreakpointsServiceListener>> dbgCodeBreakpointsServiceListener) {
			lockObj = new object();
			breakpoints = new HashSet<DbgCodeBreakpointImpl>();
			locationToBreakpoint = new Dictionary<DbgBreakpointLocation, DbgCodeBreakpointImpl>();
			this.dbgDispatcher = dbgDispatcher;
			breakpointId = 0;

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
			var bps = new List<DbgCodeBreakpointAndOldSettings>(settings.Length);
			lock (lockObj) {
				foreach (var info in settings) {
					var bpImpl = info.Breakpoint as DbgCodeBreakpointImpl;
					Debug.Assert(bpImpl != null);
					if (bpImpl == null)
						continue;
					Debug.Assert(breakpoints.Contains(bpImpl));
					if (!breakpoints.Contains(bpImpl))
						continue;
					var currentSettings = bpImpl.Settings;
					if (currentSettings == info.Settings)
						continue;
					bps.Add(new DbgCodeBreakpointAndOldSettings(bpImpl, currentSettings));
					bpImpl.WriteSettings(info.Settings);
				}
			}
			if (bps.Count > 0)
				BreakpointsModified?.Invoke(this, new DbgBreakpointsModifiedEventArgs(new ReadOnlyCollection<DbgCodeBreakpointAndOldSettings>(bps)));
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
			var bpImpls = new List<DbgCodeBreakpointImpl>(breakpoints.Length);
			List<DbgObject> objsToClose = null;
			lock (lockObj) {
				for (int i = 0; i < breakpoints.Length; i++) {
					var location = breakpoints[i].Location;
					if (locationToBreakpoint.ContainsKey(location)) {
						if (objsToClose == null)
							objsToClose = new List<DbgObject>();
						objsToClose.Add(location);
					}
					else {
						var bp = new DbgCodeBreakpointImpl(this, breakpointId++, location, breakpoints[i].Settings);
						bpImpls.Add(bp);
					}
				}
				Dbg(() => AddCore(bpImpls, objsToClose));
			}
			return bpImpls.ToArray();
		}

		void AddCore(List<DbgCodeBreakpointImpl> breakpoints, List<DbgObject> objsToClose) {
			dbgDispatcher.VerifyAccess();
			var added = new List<DbgCodeBreakpoint>(breakpoints.Count);
			lock (lockObj) {
				foreach (var bp in breakpoints) {
					Debug.Assert(!this.breakpoints.Contains(bp));
					if (this.breakpoints.Contains(bp))
						continue;
					if (locationToBreakpoint.ContainsKey(bp.Location)) {
						if (objsToClose == null)
							objsToClose = new List<DbgObject>();
						objsToClose.Add(bp);
					}
					else {
						added.Add(bp);
						this.breakpoints.Add(bp);
						locationToBreakpoint.Add(bp.Location, bp);
					}
				}
			}
			if (objsToClose != null) {
				foreach (var obj in objsToClose)
					obj.Close(dbgDispatcher.DispatcherThread);
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
					bool b = locationToBreakpoint.Remove(bpImpl.Location);
					Debug.Assert(b);
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
				locationToBreakpoint.Clear();
			}
			if (removed.Length > 0) {
				BreakpointsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgCodeBreakpoint>(removed, added: false));
				foreach (var bp in removed)
					bp.Close(dbgDispatcher.DispatcherThread);
			}
		}
	}
}

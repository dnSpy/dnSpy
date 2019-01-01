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
using System.ComponentModel;
using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Breakpoints.Code {
	sealed class DbgCodeBreakpointImpl : DbgCodeBreakpoint {
		public override int Id { get; }
		public override DbgCodeBreakpointOptions Options { get; }
		public override DbgCodeLocation Location { get; }
		public override event EventHandler<DbgBreakpointHitCheckEventArgs> HitCheck;
		public override event EventHandler<DbgBreakpointHitEventArgs> Hit;

		public override DbgCodeBreakpointSettings Settings {
			get {
				lock (lockObj)
					return settings;
			}
			set => owner.Modify(this, value);
		}
		DbgCodeBreakpointSettings settings;

		public override bool IsEnabled {
			get => Settings.IsEnabled;
			set {
				var settings = Settings;
				if (settings.IsEnabled == value)
					return;
				settings.IsEnabled = value;
				Settings = settings;
			}
		}

		public override DbgCodeBreakpointCondition? Condition {
			get => Settings.Condition;
			set {
				var settings = Settings;
				settings.Condition = value;
				Settings = settings;
			}
		}

		public override DbgCodeBreakpointHitCount? HitCount {
			get => Settings.HitCount;
			set {
				var settings = Settings;
				settings.HitCount = value;
				Settings = settings;
			}
		}

		public override DbgCodeBreakpointFilter? Filter {
			get => Settings.Filter;
			set {
				var settings = Settings;
				settings.Filter = value;
				Settings = settings;
			}
		}

		public override DbgCodeBreakpointTrace? Trace {
			get => Settings.Trace;
			set {
				var settings = Settings;
				settings.Trace = value;
				Settings = settings;
			}
		}

		public override ReadOnlyCollection<string> Labels {
			get => Settings.Labels;
			set {
				var settings = Settings;
				settings.Labels = value;
				Settings = settings;
			}
		}

		public override event EventHandler<DbgCollectionChangedEventArgs<DbgBoundCodeBreakpoint>> BoundBreakpointsChanged;
		public override DbgBoundCodeBreakpoint[] BoundBreakpoints {
			get {
				lock (lockObj)
					return boundCodeBreakpoints.ToArray();
			}
		}
		readonly List<DbgBoundCodeBreakpoint> boundCodeBreakpoints;

		public override event EventHandler BoundBreakpointsMessageChanged;
		public override DbgBoundCodeBreakpointMessage BoundBreakpointsMessage {
			get {
				lock (lockObj)
					return boundBreakpointsMessage;
			}
		}
		DbgBoundCodeBreakpointMessage boundBreakpointsMessage;
		bool isDebugging;

		readonly object lockObj;
		readonly DbgCodeBreakpointsServiceImpl owner;

		public DbgCodeBreakpointImpl(DbgCodeBreakpointsServiceImpl owner, int id, DbgCodeBreakpointOptions options, DbgCodeLocation location, DbgCodeBreakpointSettings settings, bool isDebugging) {
			lockObj = new object();
			boundCodeBreakpoints = new List<DbgBoundCodeBreakpoint>();
			this.isDebugging = isDebugging;
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			Id = id;
			Options = options;
			Location = location ?? throw new ArgumentNullException(nameof(location));
			this.settings = settings;
			boundBreakpointsMessage = CalculateBoundBreakpointsMessage_NoLock();
		}

		internal bool WriteSettings_DbgThread(DbgCodeBreakpointSettings newSettings) {
			owner.DbgDispatcher.VerifyAccess();
			bool raiseBoundBreakpointsMessageChanged;
			lock (lockObj) {
				settings = newSettings;
				if (settings.Labels == null)
					settings.Labels = emptyLabels;
				var oldMessage = boundBreakpointsMessage;
				boundBreakpointsMessage = CalculateBoundBreakpointsMessage_NoLock();
				raiseBoundBreakpointsMessageChanged = oldMessage != boundBreakpointsMessage;
			}
			return raiseBoundBreakpointsMessageChanged;
		}
		static readonly ReadOnlyCollection<string> emptyLabels = new ReadOnlyCollection<string>(Array.Empty<string>());

		internal bool WriteIsDebugging_DbgThread(bool isDebugging) {
			owner.DbgDispatcher.VerifyAccess();
			bool raiseBoundBreakpointsMessageChanged;
			lock (lockObj) {
				this.isDebugging = isDebugging;
				var oldMessage = boundBreakpointsMessage;
				boundBreakpointsMessage = CalculateBoundBreakpointsMessage_NoLock();
				raiseBoundBreakpointsMessageChanged = oldMessage != boundBreakpointsMessage;
			}
			return raiseBoundBreakpointsMessageChanged;
		}

		internal void RaiseBoundBreakpointsMessageChanged_DbgThread() {
			owner.DbgDispatcher.VerifyAccess();
			BoundBreakpointsMessageChanged?.Invoke(this, EventArgs.Empty);
		}

		internal void RaiseEvents_DbgThread(bool raiseBoundBreakpointsMessageChanged, List<DbgBoundCodeBreakpoint> boundBreakpoints, bool added) {
			owner.DbgDispatcher.VerifyAccess();
			if (raiseBoundBreakpointsMessageChanged)
				BoundBreakpointsMessageChanged?.Invoke(this, EventArgs.Empty);
			if (boundBreakpoints.Count > 0)
				BoundBreakpointsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgBoundCodeBreakpoint>(boundBreakpoints, added: added));
		}

		internal bool AddBoundBreakpoints_DbgThread(List<DbgBoundCodeBreakpoint> bps) {
			owner.DbgDispatcher.VerifyAccess();
			bool raiseBoundBreakpointsMessageChanged;
			lock (lockObj) {
				foreach (var bp in bps) {
					Debug.Assert(!boundCodeBreakpoints.Contains(bp));
					boundCodeBreakpoints.Add(bp);
					bp.PropertyChanged += DbgBoundCodeBreakpoint_PropertyChanged;
				}
				var oldMessage = boundBreakpointsMessage;
				boundBreakpointsMessage = CalculateBoundBreakpointsMessage_NoLock();
				raiseBoundBreakpointsMessageChanged = oldMessage != boundBreakpointsMessage;
			}
			return raiseBoundBreakpointsMessageChanged;
		}

		internal bool RemoveBoundBreakpoints_DbgThread(List<DbgBoundCodeBreakpoint> bps) {
			owner.DbgDispatcher.VerifyAccess();
			bool raiseBoundBreakpointsMessageChanged;
			lock (lockObj) {
				foreach (var bp in bps) {
					bool b = boundCodeBreakpoints.Remove(bp);
					Debug.Assert(b);
					bp.PropertyChanged -= DbgBoundCodeBreakpoint_PropertyChanged;
				}
				var oldMessage = boundBreakpointsMessage;
				boundBreakpointsMessage = CalculateBoundBreakpointsMessage_NoLock();
				raiseBoundBreakpointsMessageChanged = oldMessage != boundBreakpointsMessage;
			}
			return raiseBoundBreakpointsMessageChanged;
		}

		void DbgBoundCodeBreakpoint_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			owner.DbgDispatcher.VerifyAccess();
			if (e.PropertyName == nameof(DbgBoundCodeBreakpoint.Message)) {
				bool raiseBoundBreakpointsMessageChanged;
				lock (lockObj) {
					var oldMessage = boundBreakpointsMessage;
					boundBreakpointsMessage = CalculateBoundBreakpointsMessage_NoLock();
					raiseBoundBreakpointsMessageChanged = oldMessage != boundBreakpointsMessage;
				}
				if (raiseBoundBreakpointsMessageChanged)
					owner.OnBoundBreakpointsMessageChanged_DbgThread(this);
			}
		}

		DbgBoundCodeBreakpointMessage CalculateBoundBreakpointsMessage_NoLock() {
			DbgBoundCodeBreakpointMessage? errorMsg = null;
			DbgBoundCodeBreakpointMessage? warningMsg = null;
			foreach (var bp in boundCodeBreakpoints) {
				var msg = bp.Message;
				Debug.Assert(msg.Message != null);
				if (msg.Message == null)
					continue;
				switch (msg.Severity) {
				case DbgBoundCodeBreakpointSeverity.None:
					break;

				case DbgBoundCodeBreakpointSeverity.Warning:
					if (warningMsg == null)
						warningMsg = msg;
					break;

				case DbgBoundCodeBreakpointSeverity.Error:
					if (errorMsg == null)
						errorMsg = msg;
					break;

				default:
					Debug.Fail($"Unknown message severity: {msg.Severity}");
					if (errorMsg == null)
						errorMsg = new DbgBoundCodeBreakpointMessage(DbgBoundCodeBreakpointSeverity.Error, "???");
					break;
				}
			}
			var m = errorMsg ?? warningMsg ?? GetDefaultMessage_NoLock();
			if (m.Severity == DbgBoundCodeBreakpointSeverity.None)
				return m;
			return new DbgBoundCodeBreakpointMessage(m.Severity, string.Format(dnSpy_Debugger_Resources.Breakpoints_BreakpointWillNotBeHit, m.Message));
		}

		DbgBoundCodeBreakpointMessage GetDefaultMessage_NoLock() {
			Debug.Assert(isDebugging || boundCodeBreakpoints.Count == 0);
			if (settings.IsEnabled && isDebugging && boundCodeBreakpoints.Count == 0)
				return new DbgBoundCodeBreakpointMessage(DbgBoundCodeBreakpointSeverity.Warning, dnSpy_Debugger_Resources.Breakpoints_ModuleNotLoaded);
			return DbgBoundCodeBreakpointMessage.None;
		}

		public override void Remove() => owner.Remove(this);

		internal bool RaiseHitCheck(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread thread) {
			var e = new DbgBreakpointHitCheckEventArgs(boundBreakpoint, thread);
			HitCheck?.Invoke(this, e);
			return e.Pause;
		}

		internal void RaiseHit(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread thread) =>
			Hit?.Invoke(this, new DbgBreakpointHitEventArgs(boundBreakpoint, thread));

		protected override void CloseCore(DbgDispatcher dispatcher) {
			Location.Close(dispatcher);
			HitCheck = null;
			Hit = null;
			BoundBreakpointsChanged = null;
			BoundBreakpointsMessageChanged = null;
		}
	}
}

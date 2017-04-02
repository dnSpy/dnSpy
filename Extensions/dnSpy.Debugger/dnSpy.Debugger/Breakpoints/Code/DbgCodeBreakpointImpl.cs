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
using dnSpy.Contracts.Debugger.Breakpoints.Code;

namespace dnSpy.Debugger.Breakpoints.Code {
	sealed class DbgCodeBreakpointImpl : DbgCodeBreakpoint {
		public override int Id { get; }
		public override DbgBreakpointLocation Location { get; }

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

		readonly object lockObj;
		readonly DbgCodeBreakpointsServiceImpl owner;

		public DbgCodeBreakpointImpl(DbgCodeBreakpointsServiceImpl owner, int id, DbgBreakpointLocation breakpointLocation, DbgCodeBreakpointSettings settings) {
			lockObj = new object();
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			Id = id;
			Location = breakpointLocation ?? throw new ArgumentNullException(nameof(breakpointLocation));
			this.settings = settings;
		}

		internal void WriteSettings(DbgCodeBreakpointSettings newSettings) {
			lock (lockObj)
				settings = newSettings;
		}

		public override void Remove() => owner.Remove(this);

		protected override void CloseCore() {
			owner.DbgDispatcher.VerifyAccess();
			Location.Close(owner.DbgDispatcher.DispatcherThread);
		}
	}
}

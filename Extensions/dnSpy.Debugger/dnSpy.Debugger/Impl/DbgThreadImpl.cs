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
using System.Collections.ObjectModel;
using System.ComponentModel;
using dnSpy.Contracts.Debugger;

namespace dnSpy.Debugger.Impl {
	sealed class DbgThreadImpl : DbgThread {
		public override DbgRuntime Runtime => runtime;
		public override DbgAppDomain AppDomain => appDomain;
		public override string Kind => kind;
		public override int Id => id;
		public override string Name => name;
		public override int SuspendedCount => suspendedCount;

		public override ReadOnlyCollection<DbgStateInfo> State {
			get {
				lock (lockObj)
					return state;
			}
		}

		public override int? ManagedId {
			get {
				lock (lockObj)
					return managedId;
			}
		}

		DispatcherThread DispatcherThread => Process.DbgManager.DispatcherThread;

		readonly object lockObj;
		readonly DbgRuntimeImpl runtime;
		DbgAppDomainImpl appDomain;
		string kind;
		int id;
		int? managedId;
		string name;
		int suspendedCount;
		ReadOnlyCollection<DbgStateInfo> state;
		static readonly ReadOnlyCollection<DbgStateInfo> emptyState = new ReadOnlyCollection<DbgStateInfo>(Array.Empty<DbgStateInfo>());

		public DbgThreadImpl(DbgRuntimeImpl runtime, DbgAppDomainImpl appDomain, string kind, int id, int? managedId, string name, int suspendedCount, ReadOnlyCollection<DbgStateInfo> state) {
			lockObj = new object();
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			this.appDomain = appDomain;
			this.kind = kind;
			this.id = id;
			this.managedId = managedId;
			this.name = name;
			this.suspendedCount = suspendedCount;
			this.state = state ?? emptyState;
		}

		public override event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) {
			DispatcherThread.VerifyAccess();
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		}

		internal void UpdateAppDomain_DbgThread(DbgAppDomainImpl appDomain) {
			DispatcherThread.VerifyAccess();
			if (this.appDomain != appDomain) {
				// Caller has verified that it's a valid app domain
				this.appDomain = appDomain;
				OnPropertyChanged(nameof(AppDomain));
			}
		}

		internal void UpdateKind_DbgThread(string kind) {
			DispatcherThread.VerifyAccess();
			if (this.kind != kind) {
				this.kind = kind;
				OnPropertyChanged(nameof(Kind));
			}
		}

		internal void UpdateId_DbgThread(int id) {
			DispatcherThread.VerifyAccess();
			if (this.id != id) {
				this.id = id;
				OnPropertyChanged(nameof(Id));
			}
		}

		internal void UpdateManagedId_DbgThread(int? managedId) {
			DispatcherThread.VerifyAccess();
			bool raiseEvent;
			lock (lockObj) {
				raiseEvent = this.managedId != managedId;
				this.managedId = managedId;
			}
			if (raiseEvent)
				OnPropertyChanged(nameof(ManagedId));
		}

		internal void UpdateName_DbgThread(string name) {
			DispatcherThread.VerifyAccess();
			if (this.name != name) {
				this.name = name;
				OnPropertyChanged(nameof(Name));
			}
		}

		internal void UpdateSuspendedCount_DbgThread(int suspendedCount) {
			DispatcherThread.VerifyAccess();
			if (this.suspendedCount != suspendedCount) {
				this.suspendedCount = suspendedCount;
				OnPropertyChanged(nameof(SuspendedCount));
			}
		}

		internal void UpdateState_DbgThread(ReadOnlyCollection<DbgStateInfo> state) {
			DispatcherThread.VerifyAccess();
			if (state == null)
				state = emptyState;
			bool raiseEvent;
			lock (lockObj) {
				raiseEvent = !EqualsState(this.state, state);
				this.state = state;
			}
			if (raiseEvent)
				OnPropertyChanged(nameof(State));
		}

		static bool EqualsState(ReadOnlyCollection<DbgStateInfo> a, ReadOnlyCollection<DbgStateInfo> b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		internal void Remove() => DispatcherThread.BeginInvoke(() => runtime.Remove_DbgThread(this));

		protected override void CloseCore() => DispatcherThread.VerifyAccess();
	}
}

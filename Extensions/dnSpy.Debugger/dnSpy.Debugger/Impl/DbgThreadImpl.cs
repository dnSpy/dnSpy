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
using System.ComponentModel;
using dnSpy.Contracts.Debugger;

namespace dnSpy.Debugger.Impl {
	sealed class DbgThreadImpl : DbgThread {
		public override DbgRuntime Runtime => runtime;
		public override DbgAppDomain AppDomain => appDomain;
		public override string Kind => kind;
		public override int Id => id;
		public override int? ManagedId => managedId;
		public override string Name => name;

		DispatcherThread DispatcherThread => Process.DbgManager.DispatcherThread;

		readonly DbgRuntimeImpl runtime;
		readonly DbgAppDomainImpl appDomain;
		string kind;
		int id;
		int? managedId;
		string name;

		public DbgThreadImpl(DbgRuntimeImpl runtime, DbgAppDomainImpl appDomain, string kind, int id, int? managedId, string name) {
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			this.appDomain = appDomain;
			this.kind = kind;
			this.id = id;
			this.managedId = managedId;
			this.name = name;
		}

		public override event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) {
			DispatcherThread.VerifyAccess();
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
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
			if (this.managedId != managedId) {
				this.managedId = managedId;
				OnPropertyChanged(nameof(ManagedId));
			}
		}

		internal void UpdateName_DbgThread(string name) {
			DispatcherThread.VerifyAccess();
			if (this.name != name) {
				this.name = name;
				OnPropertyChanged(nameof(Name));
			}
		}

		internal void Remove() => DispatcherThread.BeginInvoke(() => runtime.Remove_DbgThread(this));

		protected override void CloseCore() => DispatcherThread.VerifyAccess();
	}
}

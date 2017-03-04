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
	sealed class DbgAppDomainImpl : DbgAppDomain {
		public override DbgRuntime Runtime => runtime;
		public override string Name => name;
		public override int Id => id;

		DispatcherThread DispatcherThread => Process.DbgManager.DispatcherThread;

		readonly DbgRuntimeImpl runtime;
		string name;
		int id;

		public DbgAppDomainImpl(DbgRuntimeImpl runtime, string name, int id) {
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			this.name = name;
			this.id = id;
		}

		public override event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) {
			DispatcherThread.VerifyAccess();
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		}

		internal void UpdateName_DbgThread(string name) {
			DispatcherThread.VerifyAccess();
			if (this.name != name) {
				this.name = name;
				OnPropertyChanged(nameof(Name));
			}
		}

		internal void UpdateId_DbgThread(int id) {
			DispatcherThread.VerifyAccess();
			if (this.id != id) {
				this.id = id;
				OnPropertyChanged(nameof(Id));
			}
		}

		internal void Remove() => DispatcherThread.BeginInvoke(() => runtime.Remove_DbgThread(this));

		protected override void CloseCore() => DispatcherThread.VerifyAccess();
	}
}

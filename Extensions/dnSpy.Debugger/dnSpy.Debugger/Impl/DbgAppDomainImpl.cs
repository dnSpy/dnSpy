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
using System.ComponentModel;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;

namespace dnSpy.Debugger.Impl {
	sealed class DbgAppDomainImpl : DbgAppDomain {
		public override DbgRuntime Runtime => runtime;
		public override string Name => name;
		public override int Id => id;
		public override DbgInternalAppDomain InternalAppDomain { get; }

		DbgDispatcher Dispatcher => Process.DbgManager.Dispatcher;

		readonly DbgRuntimeImpl runtime;
		string name;
		int id;

		public DbgAppDomainImpl(DbgRuntimeImpl runtime, DbgInternalAppDomain internalAppDomain, string name, int id) {
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			InternalAppDomain = internalAppDomain ?? throw new ArgumentNullException(nameof(internalAppDomain));
			this.name = name;
			this.id = id;
		}

		public override event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) {
			Dispatcher.VerifyAccess();
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		}

		internal void UpdateName_DbgThread(string name) {
			Dispatcher.VerifyAccess();
			if (this.name != name) {
				this.name = name;
				OnPropertyChanged(nameof(Name));
			}
		}

		internal void UpdateId_DbgThread(int id) {
			Dispatcher.VerifyAccess();
			if (this.id != id) {
				this.id = id;
				OnPropertyChanged(nameof(Id));
			}
		}

		internal void Remove(DbgEngineMessageFlags messageFlags) => Dispatcher.BeginInvoke(() => runtime.Remove_DbgThread(this, messageFlags));

		protected override void CloseCore(DbgDispatcher dispatcher) => InternalAppDomain.Close(dispatcher);
	}
}

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
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;

namespace dnSpy.Debugger.Impl {
	sealed class DbgModuleImpl : DbgModule {
		public override DbgRuntime Runtime => runtime;
		public override DbgAppDomain AppDomain => appDomain;
		public override DbgInternalModule InternalModule { get; }
		public override bool IsExe => isExe;
		public override uint Size => size;
		public override DbgImageLayout ImageLayout => imageLayout;
		public override string Name => name;
		public override string Filename => filename;
		public override bool IsDynamic => isDynamic;
		public override bool IsInMemory => isInMemory;
		public override int Order => order;
		public override string Version => version;

		public override int RefreshedVersion => refreshedVersion;
		volatile int refreshedVersion;
		public override event EventHandler Refreshed;
		internal void RaiseRefreshed() {
			Interlocked.Increment(ref refreshedVersion);
			Refreshed?.Invoke(this, EventArgs.Empty);
		}

		public override ulong Address {
			get {
				lock (lockObj)
					return address;
			}
		}

		public override bool? IsOptimized {
			get {
				lock (lockObj)
					return isOptimized;
			}
		}

		public override DateTime? Timestamp {
			get {
				lock (lockObj)
					return timestamp;
			}
		}

		DbgDispatcher Dispatcher => Process.DbgManager.Dispatcher;

		readonly object lockObj;
		readonly DbgRuntimeImpl runtime;
		readonly DbgAppDomainImpl appDomain;
		bool isExe;
		ulong address;
		uint size;
		DbgImageLayout imageLayout;
		string name;
		string filename;
		bool isDynamic;
		bool isInMemory;
		bool? isOptimized;
		int order;
		DateTime? timestamp;
		string version;

		public DbgModuleImpl(DbgRuntimeImpl runtime, DbgAppDomainImpl appDomain, DbgInternalModule internalModule, bool isExe, ulong address, uint size, DbgImageLayout imageLayout, string name, string filename, bool isDynamic, bool isInMemory, bool? isOptimized, int order, DateTime? timestamp, string version) {
			lockObj = new object();
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			this.appDomain = appDomain;
			InternalModule = internalModule ?? throw new ArgumentNullException(nameof(internalModule));
			this.isExe = isExe;
			this.address = address;
			this.size = size;
			this.imageLayout = imageLayout;
			this.name = name;
			this.filename = filename;
			this.isDynamic = isDynamic;
			this.isInMemory = isInMemory;
			this.isOptimized = isOptimized;
			this.order = order;
			this.timestamp = timestamp?.ToUniversalTime();
			this.version = version;
		}

		public override event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) {
			Dispatcher.VerifyAccess();
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		}

		internal void UpdateIsExe_DbgThread(bool isExe) {
			Dispatcher.VerifyAccess();
			if (this.isExe != isExe) {
				this.isExe = isExe;
				OnPropertyChanged(nameof(IsExe));
			}
		}

		internal void UpdateAddress_DbgThread(ulong address) {
			Dispatcher.VerifyAccess();
			bool raiseEvent;
			lock (lockObj) {
				raiseEvent = this.address != address;
				this.address = address;
			}
			if (raiseEvent)
				OnPropertyChanged(nameof(Address));
		}

		internal void UpdateSize_DbgThread(uint size) {
			Dispatcher.VerifyAccess();
			if (this.size != size) {
				this.size = size;
				OnPropertyChanged(nameof(Size));
			}
		}

		internal void UpdateImageLayout_DbgThread(DbgImageLayout imageLayout) {
			Dispatcher.VerifyAccess();
			if (this.imageLayout != imageLayout) {
				this.imageLayout = imageLayout;
				OnPropertyChanged(nameof(ImageLayout));
			}
		}

		internal void UpdateName_DbgThread(string name) {
			Dispatcher.VerifyAccess();
			if (this.name != name) {
				this.name = name;
				OnPropertyChanged(nameof(Name));
			}
		}

		internal void UpdateFilename_DbgThread(string filename) {
			Dispatcher.VerifyAccess();
			if (this.filename != filename) {
				this.filename = filename;
				OnPropertyChanged(nameof(Filename));
			}
		}

		internal void UpdateIsDynamic_DbgThread(bool isDynamic) {
			Dispatcher.VerifyAccess();
			if (this.isDynamic != isDynamic) {
				this.isDynamic = isDynamic;
				OnPropertyChanged(nameof(IsDynamic));
			}
		}

		internal void UpdateIsInMemory_DbgThread(bool isInMemory) {
			Dispatcher.VerifyAccess();
			if (this.isInMemory != isInMemory) {
				this.isInMemory = isInMemory;
				OnPropertyChanged(nameof(IsInMemory));
			}
		}

		internal void UpdateIsOptimized_DbgThread(bool? isOptimized) {
			Dispatcher.VerifyAccess();
			bool raiseEvent;
			lock (lockObj) {
				raiseEvent = this.isOptimized != isOptimized;
				this.isOptimized = isOptimized;
			}
			if (raiseEvent)
				OnPropertyChanged(nameof(IsOptimized));
		}

		internal void UpdateOrder_DbgThread(int order) {
			Dispatcher.VerifyAccess();
			if (this.order != order) {
				this.order = order;
				OnPropertyChanged(nameof(Order));
			}
		}

		internal void UpdateTimestamp_DbgThread(DateTime? timestamp) {
			Dispatcher.VerifyAccess();
			bool raiseEvent;
			timestamp = timestamp?.ToUniversalTime();
			lock (lockObj) {
				raiseEvent = this.timestamp != timestamp;
				this.timestamp = timestamp;
			}
			if (raiseEvent)
				OnPropertyChanged(nameof(Timestamp));
		}

		internal void UpdateVersion_DbgThread(string version) {
			Dispatcher.VerifyAccess();
			if (this.version != version) {
				this.version = version;
				OnPropertyChanged(nameof(Version));
			}
		}

		internal void Remove(DbgEngineMessageFlags messageFlags) => Dispatcher.BeginInvoke(() => runtime.Remove_DbgThread(this, messageFlags));

		protected override void CloseCore(DbgDispatcher dispatcher) => InternalModule.Close(dispatcher);
	}
}

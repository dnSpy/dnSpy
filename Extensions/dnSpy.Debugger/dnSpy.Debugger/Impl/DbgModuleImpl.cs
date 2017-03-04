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
	sealed class DbgModuleImpl : DbgModule {
		public override DbgRuntime Runtime => runtime;
		public override DbgAppDomain AppDomain => appDomain;
		public override bool IsExe => isExe;
		public override ulong Address => address;
		public override uint Size => size;
		public override DbgImageLayout ImageLayout => imageLayout;
		public override string Name => name;
		public override string Filename => filename;
		public override string RealFilename => realFilename;
		public override bool IsDynamic => isDynamic;
		public override bool IsInMemory => isInMemory;
		public override bool? IsOptimized => isOptimized;
		public override int Order => order;
		public override DateTime? Timestamp => timestamp;
		public override string Version => version;

		DispatcherThread DispatcherThread => Process.DbgManager.DispatcherThread;

		readonly DbgRuntimeImpl runtime;
		readonly DbgAppDomainImpl appDomain;
		bool isExe;
		ulong address;
		uint size;
		DbgImageLayout imageLayout;
		string name;
		string filename;
		string realFilename;
		bool isDynamic;
		bool isInMemory;
		bool? isOptimized;
		int order;
		DateTime? timestamp;
		string version;

		public DbgModuleImpl(DbgRuntimeImpl runtime, DbgAppDomainImpl appDomain, bool isExe, ulong address, uint size, DbgImageLayout imageLayout, string name, string filename, string realFilename, bool isDynamic, bool isInMemory, bool? isOptimized, int order, DateTime? timestamp, string version) {
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			this.appDomain = appDomain;
			this.isExe = isExe;
			this.address = address;
			this.size = size;
			this.imageLayout = imageLayout;
			this.name = name;
			this.filename = filename;
			this.realFilename = realFilename;
			this.isDynamic = isDynamic;
			this.isInMemory = isInMemory;
			this.isOptimized = isOptimized;
			this.order = order;
			this.timestamp = timestamp;
			this.version = version;
		}

		public override event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) {
			DispatcherThread.VerifyAccess();
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		}

		internal void UpdateIsExe_DbgThread(bool isExe) {
			DispatcherThread.VerifyAccess();
			if (this.isExe != isExe) {
				this.isExe = isExe;
				OnPropertyChanged(nameof(IsExe));
			}
		}

		internal void UpdateAddress_DbgThread(ulong address) {
			DispatcherThread.VerifyAccess();
			if (this.address != address) {
				this.address = address;
				OnPropertyChanged(nameof(Address));
			}
		}

		internal void UpdateSize_DbgThread(uint size) {
			DispatcherThread.VerifyAccess();
			if (this.size != size) {
				this.size = size;
				OnPropertyChanged(nameof(Size));
			}
		}

		internal void UpdateImageLayout_DbgThread(DbgImageLayout imageLayout) {
			DispatcherThread.VerifyAccess();
			if (this.imageLayout != imageLayout) {
				this.imageLayout = imageLayout;
				OnPropertyChanged(nameof(ImageLayout));
			}
		}

		internal void UpdateName_DbgThread(string name) {
			DispatcherThread.VerifyAccess();
			if (this.name != name) {
				this.name = name;
				OnPropertyChanged(nameof(Name));
			}
		}

		internal void UpdateFilename_DbgThread(string filename) {
			DispatcherThread.VerifyAccess();
			if (this.filename != filename) {
				this.filename = filename;
				OnPropertyChanged(nameof(Filename));
			}
		}

		internal void UpdateRealFilename_DbgThread(string realFilename) {
			DispatcherThread.VerifyAccess();
			if (this.realFilename != realFilename) {
				this.realFilename = realFilename;
				OnPropertyChanged(nameof(RealFilename));
			}
		}

		internal void UpdateIsDynamic_DbgThread(bool isDynamic) {
			DispatcherThread.VerifyAccess();
			if (this.isDynamic != isDynamic) {
				this.isDynamic = isDynamic;
				OnPropertyChanged(nameof(IsDynamic));
			}
		}

		internal void UpdateIsInMemory_DbgThread(bool isInMemory) {
			DispatcherThread.VerifyAccess();
			if (this.isInMemory != isInMemory) {
				this.isInMemory = isInMemory;
				OnPropertyChanged(nameof(IsInMemory));
			}
		}

		internal void UpdateIsOptimized_DbgThread(bool? isOptimized) {
			DispatcherThread.VerifyAccess();
			if (this.isOptimized != isOptimized) {
				this.isOptimized = isOptimized;
				OnPropertyChanged(nameof(IsOptimized));
			}
		}

		internal void UpdateOrder_DbgThread(int order) {
			DispatcherThread.VerifyAccess();
			if (this.order != order) {
				this.order = order;
				OnPropertyChanged(nameof(Order));
			}
		}

		internal void UpdateTimestamp_DbgThread(DateTime? timestamp) {
			DispatcherThread.VerifyAccess();
			if (this.timestamp != timestamp) {
				this.timestamp = timestamp;
				OnPropertyChanged(nameof(Timestamp));
			}
		}

		internal void UpdateVersion_DbgThread(string version) {
			DispatcherThread.VerifyAccess();
			if (this.version != version) {
				this.version = version;
				OnPropertyChanged(nameof(Version));
			}
		}

		internal void Remove() => DispatcherThread.BeginInvoke(() => runtime.Remove_DbgThread(this));

		protected override void CloseCore() => DispatcherThread.VerifyAccess();
	}
}

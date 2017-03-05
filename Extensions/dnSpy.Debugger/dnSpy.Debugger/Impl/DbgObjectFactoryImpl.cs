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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;

namespace dnSpy.Debugger.Impl {
	sealed class DbgObjectFactoryImpl : DbgObjectFactory {
		public override DbgManager DbgManager => owner;
		public override DbgRuntime Runtime => runtime;

		readonly DbgManagerImpl owner;
		readonly DbgRuntimeImpl runtime;
		readonly DbgEngine engine;
		bool disposed;

		public DbgObjectFactoryImpl(DbgManagerImpl owner, DbgRuntimeImpl runtime, DbgEngine engine) {
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
		}

		public override DbgEngineAppDomain CreateAppDomain<T>(string name, int id, T data) {
			if (disposed)
				throw new ObjectDisposedException(nameof(DbgObjectFactoryImpl));
			var appDomain = new DbgAppDomainImpl(runtime, name, id);
			if (data != null)
				appDomain.GetOrCreateData(() => data);
			var engineAppDomain = new DbgEngineAppDomainImpl(appDomain);
			owner.DispatcherThread.BeginInvoke(() => runtime.Add_DbgThread(appDomain));
			return engineAppDomain;
		}

		DbgAppDomainImpl VerifyOptionalAppDomain(DbgAppDomain appDomain) {
			if (appDomain == null)
				return null;
			var appDomainImpl = appDomain as DbgAppDomainImpl;
			if (appDomainImpl == null)
				throw new ArgumentOutOfRangeException(nameof(appDomain));
			if (appDomainImpl.Runtime != runtime)
				throw new ArgumentException();
			return appDomainImpl;
		}

		public override DbgEngineModule CreateModule<T>(DbgAppDomain appDomain, bool isExe, ulong address, uint size, DbgImageLayout imageLayout, string name, string filename, bool isDynamic, bool isInMemory, bool? isOptimized, int order, DateTime? timestamp, string version, T data) {
			if (disposed)
				throw new ObjectDisposedException(nameof(DbgObjectFactoryImpl));
			var module = new DbgModuleImpl(runtime, VerifyOptionalAppDomain(appDomain), isExe, address, size, imageLayout, name, filename, isDynamic, isInMemory, isOptimized, order, timestamp, version);
			if (data != null)
				module.GetOrCreateData(() => data);
			var engineModule = new DbgEngineModuleImpl(module);
			owner.DispatcherThread.BeginInvoke(() => runtime.Add_DbgThread(module));
			return engineModule;
		}

		public override DbgEngineThread CreateThread<T>(DbgAppDomain appDomain, string kind, int id, int? managedId, string name, ReadOnlyCollection<DbgStateInfo> state, T data) {
			if (disposed)
				throw new ObjectDisposedException(nameof(DbgObjectFactoryImpl));
			var thread = new DbgThreadImpl(runtime, VerifyOptionalAppDomain(appDomain), kind, id, managedId, name, state);
			if (data != null)
				thread.GetOrCreateData(() => data);
			var engineThread = new DbgEngineThreadImpl(thread);
			owner.DispatcherThread.BeginInvoke(() => runtime.Add_DbgThread(thread));
			return engineThread;
		}

		internal void Dispose() => disposed = true;
	}
}

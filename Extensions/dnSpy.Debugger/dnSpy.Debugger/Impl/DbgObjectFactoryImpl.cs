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
using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Debugger.Breakpoints.Code;
using dnSpy.Debugger.CallStack;
using dnSpy.Debugger.Exceptions;

namespace dnSpy.Debugger.Impl {
	sealed class DbgObjectFactoryImpl : DbgObjectFactory {
		public override DbgManager DbgManager => owner;
		public override DbgRuntime Runtime => runtime;

		readonly DbgManagerImpl owner;
		readonly DbgRuntimeImpl runtime;
		readonly DbgEngine engine;
		readonly Lazy<BoundCodeBreakpointsService> boundCodeBreakpointsService;
		bool disposed;

		public DbgObjectFactoryImpl(DbgManagerImpl owner, DbgRuntimeImpl runtime, DbgEngine engine, Lazy<BoundCodeBreakpointsService> boundCodeBreakpointsService) {
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			this.boundCodeBreakpointsService = boundCodeBreakpointsService ?? throw new ArgumentNullException(nameof(boundCodeBreakpointsService));
		}

		public override DbgEngineAppDomain CreateAppDomain<T>(DbgInternalAppDomain internalAppDomain, string name, int id, DbgEngineMessageFlags messageFlags, T? data, Action<DbgEngineAppDomain>? onCreated) where T : class {
			if (disposed)
				throw new ObjectDisposedException(nameof(DbgObjectFactoryImpl));
			var appDomain = new DbgAppDomainImpl(runtime, internalAppDomain, name, id);
			if (data is not null)
				appDomain.GetOrCreateData(() => data);
			var engineAppDomain = new DbgEngineAppDomainImpl(appDomain);
			onCreated?.Invoke(engineAppDomain);
			owner.Dispatcher.BeginInvoke(() => owner.AddAppDomain_DbgThread(runtime, appDomain, messageFlags));
			return engineAppDomain;
		}

		DbgAppDomainImpl? VerifyOptionalAppDomain(DbgAppDomain? appDomain) {
			if (appDomain is null)
				return null;
			var appDomainImpl = appDomain as DbgAppDomainImpl;
			if (appDomainImpl is null)
				throw new ArgumentOutOfRangeException(nameof(appDomain));
			if (appDomainImpl.Runtime != runtime)
				throw new ArgumentException();
			return appDomainImpl;
		}

		public override DbgEngineModule CreateModule<T>(DbgAppDomain? appDomain, DbgInternalModule internalModule, bool isExe, ulong address, uint size, DbgImageLayout imageLayout, string name, string filename, bool isDynamic, bool isInMemory, bool? isOptimized, int order, DateTime? timestamp, string version, DbgEngineMessageFlags messageFlags, T? data, Action<DbgEngineModule>? onCreated) where T : class {
			if (disposed)
				throw new ObjectDisposedException(nameof(DbgObjectFactoryImpl));
			var module = new DbgModuleImpl(runtime, VerifyOptionalAppDomain(appDomain), internalModule, isExe, address, size, imageLayout, name, filename, isDynamic, isInMemory, isOptimized, order, timestamp, version);
			if (data is not null)
				module.GetOrCreateData(() => data);
			var engineModule = new DbgEngineModuleImpl(module);
			onCreated?.Invoke(engineModule);
			owner.Dispatcher.BeginInvoke(() => owner.AddModule_DbgThread(runtime, module, messageFlags));
			return engineModule;
		}

		public override DbgEngineThread CreateThread<T>(DbgAppDomain? appDomain, string kind, ulong id, ulong? managedId, string? name, int suspendedCount, ReadOnlyCollection<DbgStateInfo> state, DbgEngineMessageFlags messageFlags, T? data, Action<DbgEngineThread>? onCreated) where T : class {
			if (disposed)
				throw new ObjectDisposedException(nameof(DbgObjectFactoryImpl));
			var thread = new DbgThreadImpl(runtime, VerifyOptionalAppDomain(appDomain), kind, id, managedId, name, suspendedCount, state);
			if (data is not null)
				thread.GetOrCreateData(() => data);
			var engineThread = new DbgEngineThreadImpl(thread);
			onCreated?.Invoke(engineThread);
			owner.Dispatcher.BeginInvoke(() => owner.AddThread_DbgThread(runtime, thread, messageFlags));
			return engineThread;
		}

		public override DbgException CreateException<T>(DbgExceptionId id, DbgExceptionEventFlags flags, string? message, DbgThread? thread, DbgModule? module, DbgEngineMessageFlags messageFlags, T? data, Action<DbgException>? onCreated) where T : class {
			if (id.IsDefaultId)
				throw new ArgumentException();
			var exception = new DbgExceptionImpl(runtime, id, flags, message, thread, module);
			if (data is not null)
				exception.GetOrCreateData(() => data);
			onCreated?.Invoke(exception);
			owner.Dispatcher.BeginInvoke(() => owner.AddException_DbgThread(runtime, exception, messageFlags));
			return exception;
		}

		public override DbgEngineBoundCodeBreakpoint[] Create<T>(DbgBoundCodeBreakpointInfo<T>[] infos) {
			if (infos is null)
				throw new ArgumentNullException(nameof(infos));
			if (infos.Length == 0)
				return Array.Empty<DbgEngineBoundCodeBreakpoint>();
			var bps = new List<DbgEngineBoundCodeBreakpoint>(infos.Length);
			var bpImpls = new List<DbgEngineBoundCodeBreakpointImpl>(infos.Length);
			List<IDisposable>? dataToDispose = null;

			var allBreakpoints = boundCodeBreakpointsService.Value.Breakpoints;
			var dict = new Dictionary<DbgCodeLocation, DbgCodeBreakpoint>(allBreakpoints.Length);
			foreach (var bp in allBreakpoints) {
				Debug.Assert(!dict.ContainsKey(bp.Location));
				dict[bp.Location] = bp;
			}

			for (int i = 0; i < infos.Length; i++) {
				var info = infos[i];
				if (!dict.TryGetValue(info.Location, out var breakpoint)) {
					if (info.Data is IDisposable id) {
						if (dataToDispose is null)
							dataToDispose = new List<IDisposable>();
						dataToDispose.Add(id);
					}
				}
				else {
					var bp = new DbgBoundCodeBreakpointImpl(runtime, breakpoint, info.Module, info.Address, info.Message.ToDbgBoundCodeBreakpointMessage());
					var data = info.Data;
					if (data is not null)
						bp.GetOrCreateData(() => data);
					var ebp = new DbgEngineBoundCodeBreakpointImpl(bp);
					bps.Add(ebp);
					bpImpls.Add(ebp);
				}
			}
			if (bpImpls.Count > 0 || dataToDispose is not null) {
				owner.Dispatcher.BeginInvoke(() => {
					if (dataToDispose is not null) {
						foreach (var id in dataToDispose)
							id.Dispose();
					}
					if (bpImpls.Count > 0)
						owner.AddBoundCodeBreakpoints_DbgThread(runtime, bpImpls.ToArray());
				});
			}
			return bps.ToArray();
		}

		public override DbgEngineStackFrame CreateSpecialStackFrame(string name, DbgCodeLocation? location, DbgModule? module, uint functionOffset, uint functionToken) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			return new SpecialDbgEngineStackFrame(name, location, module, functionOffset, functionToken);
		}

		internal void Dispose() => disposed = true;
	}
}

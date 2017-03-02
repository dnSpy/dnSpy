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
using System.Collections.Generic;
using System.Diagnostics;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;

namespace dnSpy.Debugger.CorDebug.Impl {
	sealed class DbgClrRuntimeImpl : CorDebugRuntime {
		public override DbgProcess Process => process;
		DbgProcess process;
		public override CorDebugRuntimeVersion Version { get; }
		public override string ClrFilename { get; }
		public override string RuntimeDirectory { get; }

		DispatcherThread DispatcherThread => dbgManager.DispatcherThread;

		public override event EventHandler<DbgCollectionChangedEventArgs<DbgModule>> ModulesChanged;
		public override DbgModule[] Modules {
			get {
				lock (lockObj)
					return clrModules.ToArray();
			}
		}
		readonly List<DbgClrModuleImpl> clrModules;

		public override event EventHandler<DbgCollectionChangedEventArgs<DbgClrAssembly>> AssembliesChanged;
		public override DbgClrAssembly[] Assemblies {
			get {
				lock (lockObj)
					return clrAssemblies.ToArray();
			}
		}
		readonly List<DbgClrAssemblyImpl> clrAssemblies;

		public override event EventHandler<DbgCollectionChangedEventArgs<DbgAppDomain>> AppDomainsChanged;
		public override DbgAppDomain[] AppDomains {
			get {
				lock (lockObj)
					return clrAppDomains.ToArray();
			}
		}
		readonly List<DbgClrAppDomainImpl> clrAppDomains;

		public override event EventHandler<DbgCollectionChangedEventArgs<DbgThread>> ThreadsChanged;
		public override DbgThread[] Threads {
			get {
				lock (lockObj)
					return clrThreads.ToArray();
			}
		}
		readonly List<DbgClrThreadImpl> clrThreads;

		readonly object lockObj;
		readonly DbgManager dbgManager;

		public DbgClrRuntimeImpl(DbgManager dbgManager, CorDebugRuntimeKind kind, string version, string clrPath, string runtimeDir) {
			lockObj = new object();
			clrModules = new List<DbgClrModuleImpl>();
			clrAssemblies = new List<DbgClrAssemblyImpl>();
			clrAppDomains = new List<DbgClrAppDomainImpl>();
			clrThreads = new List<DbgClrThreadImpl>();
			this.dbgManager = dbgManager ?? throw new ArgumentNullException(nameof(dbgManager));
			Version = new CorDebugRuntimeVersion(kind, version ?? throw new ArgumentNullException(nameof(version)));
			ClrFilename = clrPath ?? throw new ArgumentNullException(nameof(clrPath));
			RuntimeDirectory = runtimeDir ?? throw new ArgumentNullException(nameof(runtimeDir));
		}

		internal void SetProcess(DbgProcess process) => this.process = process;

		DbgClrAssemblyImpl TryGetAssembly_NoLock(DnAssembly dnAssembly) {
			foreach (var assembly in clrAssemblies) {
				if (assembly.DnAssembly == dnAssembly)
					return assembly;
			}
			return null;
		}

		internal void AddModule_DbgThread(DbgClrModuleImpl module) {
			dbgManager.DispatcherThread.VerifyAccess();
			DbgClrAssemblyImpl assembly;
			lock (lockObj) {
				assembly = TryGetAssembly_NoLock(module.DnModule.Assembly);
				Debug.Assert(assembly != null);
				if (assembly == null)
					return;
				clrModules.Add(module);
				module.Initialize(assembly);
			}
			assembly.AddModule_DbgThread(module);
			ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgModule>(module, added: true));
		}

		internal void RemoveModule_DbgThread(DnModule dnModule) {
			dbgManager.DispatcherThread.VerifyAccess();
			DbgClrModuleImpl removedModule = null;
			lock (lockObj) {
				for (int i = 0; i < clrModules.Count; i++) {
					var module = clrModules[i];
					if (module.DnModule == dnModule) {
						clrModules.RemoveAt(i);
						removedModule = module;
						break;
					}
				}
			}
			Debug.Assert(removedModule != null);
			if (removedModule != null) {
				ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgModule>(removedModule, added: false));
				removedModule.ClrAssemblyImpl.RemoveModule_DbgThread(removedModule);
				removedModule.Close(DispatcherThread);
			}
		}

		internal DbgClrAppDomainImpl TryGetAppDomain(DnAppDomain dnAppDomain) {
			if (dnAppDomain == null)
				return null;
			lock (lockObj)
				return TryGetAppDomain_NoLock(dnAppDomain);
		}

		DbgClrAppDomainImpl TryGetAppDomain_NoLock(DnAppDomain dnAppDomain) {
			foreach (var appDomain in clrAppDomains) {
				if (appDomain.DnAppDomain == dnAppDomain)
					return appDomain;
			}
			return null;
		}

		internal void AddAssembly_DbgThread(DnAssembly dnAssembly) {
			dbgManager.DispatcherThread.VerifyAccess();
			DbgClrAssemblyImpl assembly;
			lock (lockObj) {
				var appDomain = TryGetAppDomain_NoLock(dnAssembly.AppDomain);
				Debug.Assert(appDomain != null);
				if (appDomain == null)
					return;
				assembly = new DbgClrAssemblyImpl(appDomain, dnAssembly);
				clrAssemblies.Add(assembly);
			}
			AssembliesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgClrAssembly>(assembly, added: true));
		}

		internal void RemoveAssembly_DbgThread(DnAssembly dnAssembly) {
			dbgManager.DispatcherThread.VerifyAccess();
			DbgClrAssemblyImpl removedAssembly = null;
			lock (lockObj) {
				for (int i = 0; i < clrAssemblies.Count; i++) {
					var assembly = clrAssemblies[i];
					if (assembly.DnAssembly == dnAssembly) {
						clrAssemblies.RemoveAt(i);
						removedAssembly = assembly;
						break;
					}
				}
			}
			Debug.Assert(removedAssembly != null);
			if (removedAssembly != null) {
				AssembliesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgClrAssembly>(removedAssembly, added: false));
				removedAssembly.Close(DispatcherThread);
			}
		}

		internal void UpdateAppDomainName_DbgThread(DnAppDomain dnAppDomain, string newName) {
			dbgManager.DispatcherThread.VerifyAccess();
			DbgClrAppDomainImpl foundAppDomain = null;
			lock (lockObj)
				foundAppDomain = TryGetAppDomain_NoLock(dnAppDomain);
			Debug.Assert(foundAppDomain != null);
			foundAppDomain?.SetName_DbgThread(newName ?? string.Empty);
		}

		internal void AddThread_DbgThread(DbgClrThreadImpl thread) {
			dbgManager.DispatcherThread.VerifyAccess();
			lock (lockObj)
				clrThreads.Add(thread);
			ThreadsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgThread>(thread, added: true));
		}

		internal void RemoveThread_DbgThread(DnThread dnThread) {
			dbgManager.DispatcherThread.VerifyAccess();
			DbgClrThreadImpl removedThread = null;
			lock (lockObj) {
				for (int i = 0; i < clrThreads.Count; i++) {
					var thread = clrThreads[i];
					if (thread.DnThread == dnThread) {
						clrThreads.RemoveAt(i);
						removedThread = thread;
						break;
					}
				}
			}
			Debug.Assert(removedThread != null);
			if (removedThread != null) {
				ThreadsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgThread>(removedThread, added: false));
				removedThread.Close(DispatcherThread);
			}
		}

		internal void AddAppDomain_DbgThread(DbgClrAppDomainImpl appDomain) {
			dbgManager.DispatcherThread.VerifyAccess();
			lock (lockObj)
				clrAppDomains.Add(appDomain);
			AppDomainsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgAppDomain>(appDomain, added: true));
		}

		internal void RemoveAppDomain_DbgThread(DnAppDomain dnAppDomain) {
			dbgManager.DispatcherThread.VerifyAccess();
			DbgClrAppDomainImpl removedAppDomain = null;
			lock (lockObj) {
				for (int i = 0; i < clrAppDomains.Count; i++) {
					var appDomain = clrAppDomains[i];
					if (appDomain.DnAppDomain == dnAppDomain) {
						clrAppDomains.RemoveAt(i);
						removedAppDomain = appDomain;
						break;
					}
				}
			}
			Debug.Assert(removedAppDomain != null);
			if (removedAppDomain != null) {
				AppDomainsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgAppDomain>(removedAppDomain, added: false));
				removedAppDomain.Close(DispatcherThread);
			}
		}

		protected override void CloseCore() {
			DbgThread[] threads;
			DbgModule[] modules;
			DbgClrAssembly[] assemblies;
			DbgAppDomain[] appDomains;
			lock (lockObj) {
				threads = clrThreads.ToArray();
				modules = clrModules.ToArray();
				assemblies = clrAssemblies.ToArray();
				appDomains = clrAppDomains.ToArray();
				clrThreads.Clear();
				clrModules.Clear();
				clrAssemblies.Clear();
				clrAppDomains.Clear();
			}
			if (threads.Length != 0)
				ThreadsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgThread>(threads, added: false));
			if (modules.Length != 0)
				ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgModule>(modules, added: false));
			if (assemblies.Length != 0)
				AssembliesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgClrAssembly>(assemblies, added: false));
			if (appDomains.Length != 0)
				AppDomainsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgAppDomain>(appDomains, added: false));
			foreach (var thread in threads)
				thread.Close(DispatcherThread);
			foreach (var module in modules)
				module.Close(DispatcherThread);
			foreach (var assembly in assemblies)
				assembly.Close(DispatcherThread);
			foreach (var appDomain in appDomains)
				appDomain.Close(DispatcherThread);
		}
	}
}

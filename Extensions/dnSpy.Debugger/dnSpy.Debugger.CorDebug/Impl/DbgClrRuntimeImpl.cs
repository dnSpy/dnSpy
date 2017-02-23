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

		readonly object lockObj;
		readonly DbgManager dbgManager;

		public DbgClrRuntimeImpl(DbgManager dbgManager, CorDebugRuntimeKind kind, string version, string clrPath, string runtimeDir) {
			lockObj = new object();
			clrModules = new List<DbgClrModuleImpl>();
			clrAssemblies = new List<DbgClrAssemblyImpl>();
			clrAppDomains = new List<DbgClrAppDomainImpl>();
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

		internal void AddModule(DnModule dnModule) {
			lock (lockObj) {
				var assembly = TryGetAssembly_NoLock(dnModule.Assembly);
				Debug.Assert(assembly != null);
				if (assembly == null)
					return;
				var module = new DbgClrModuleImpl(this, assembly, dnModule);
				clrModules.Add(module);
				DispatcherThread.BeginInvoke(() => ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgModule>(module, added: true)));
				assembly.AddModule(module);
			}
		}

		internal void RemoveModule(DnModule dnModule) {
			lock (lockObj) {
				for (int i = 0; i < clrModules.Count; i++) {
					var module = clrModules[i];
					if (module.DnModule == dnModule) {
						clrModules.RemoveAt(i);
						module.ClrAssemblyImpl.RemoveModule(module);
						DispatcherThread.BeginInvoke(() => {
							ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgModule>(module, added: false));
							module.Close(DispatcherThread);
						});
						return;
					}
				}
				Debug.Fail($"Couldn't remove module: {dnModule}");
			}
		}

		DbgClrAppDomainImpl TryGetAppDomain_NoLock(DnAppDomain dnAppDomain) {
			foreach (var appDomain in clrAppDomains) {
				if (appDomain.DnAppDomain == dnAppDomain)
					return appDomain;
			}
			return null;
		}

		internal void AddAssembly(DnAssembly dnAssembly) {
			lock (lockObj) {
				var appDomain = TryGetAppDomain_NoLock(dnAssembly.AppDomain);
				Debug.Assert(appDomain != null);
				if (appDomain == null)
					return;
				var assembly = new DbgClrAssemblyImpl(dbgManager, appDomain, dnAssembly);
				clrAssemblies.Add(assembly);
				DispatcherThread.BeginInvoke(() => AssembliesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgClrAssembly>(assembly, added: true)));
			}
		}

		internal void RemoveAssembly(DnAssembly dnAssembly) {
			lock (lockObj) {
				for (int i = 0; i < clrAssemblies.Count; i++) {
					var assembly = clrAssemblies[i];
					if (assembly.DnAssembly == dnAssembly) {
						clrAssemblies.RemoveAt(i);
						DispatcherThread.BeginInvoke(() => {
							AssembliesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgClrAssembly>(assembly, added: false));
							assembly.Close(DispatcherThread);
						});
						return;
					}
				}
				Debug.Fail($"Couldn't remove assembly: {dnAssembly}");
			}
		}

		internal void UpdateAppDomainName(DnAppDomain dnAppDomain) {
			lock (lockObj) {
				foreach (var appDomain in clrAppDomains) {
					if (appDomain.DnAppDomain == dnAppDomain) {
						appDomain.SetName(dnAppDomain.Name);
						return;
					}
				}
				Debug.Fail($"Couldn't find app domain: {dnAppDomain}");
			}
		}

		internal void AddAppDomain(DnAppDomain dnAppDomain) {
			var appDomain = new DbgClrAppDomainImpl(dbgManager, this, dnAppDomain);
			lock (lockObj) {
				clrAppDomains.Add(appDomain);
				DispatcherThread.BeginInvoke(() => AppDomainsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgAppDomain>(appDomain, added: true)));
			}
		}

		internal void RemoveAppDomain(DnAppDomain dnAppDomain) {
			lock (lockObj) {
				for (int i = 0; i < clrAppDomains.Count; i++) {
					var appDomain = clrAppDomains[i];
					if (appDomain.DnAppDomain == dnAppDomain) {
						clrAppDomains.RemoveAt(i);
						DispatcherThread.BeginInvoke(() => {
							AppDomainsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgAppDomain>(appDomain, added: false));
							appDomain.Close(DispatcherThread);
						});
						return;
					}
				}
				Debug.Fail($"Couldn't remove app domain: {dnAppDomain}");
			}
		}

		protected override void CloseCore() {
			DbgModule[] modules;
			DbgClrAssembly[] assemblies;
			DbgAppDomain[] appDomains;
			lock (lockObj) {
				modules = clrModules.ToArray();
				assemblies = clrAssemblies.ToArray();
				appDomains = clrAppDomains.ToArray();
				clrModules.Clear();
				clrAssemblies.Clear();
				clrAppDomains.Clear();
			}
			foreach (var module in modules)
				module.Close(DispatcherThread);
			foreach (var assembly in assemblies)
				assembly.Close(DispatcherThread);
			foreach (var appDomain in appDomains)
				appDomain.Close(DispatcherThread);
		}
	}
}

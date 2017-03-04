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
using dnSpy.Contracts.Debugger;

namespace dnSpy.Debugger.Impl {
	sealed class DbgRuntimeImpl : DbgRuntime {
		public override DbgProcess Process { get; }

		public override event EventHandler<DbgCollectionChangedEventArgs<DbgAppDomain>> AppDomainsChanged;
		public override DbgAppDomain[] AppDomains {
			get {
				lock (lockObj)
					return appDomains.ToArray();
			}
		}
		readonly List<DbgAppDomain> appDomains;

		public override event EventHandler<DbgCollectionChangedEventArgs<DbgModule>> ModulesChanged;
		public override DbgModule[] Modules {
			get {
				lock (lockObj)
					return modules.ToArray();
			}
		}
		readonly List<DbgModule> modules;

		public override event EventHandler<DbgCollectionChangedEventArgs<DbgThread>> ThreadsChanged;
		public override DbgThread[] Threads {
			get {
				lock (lockObj)
					return threads.ToArray();
			}
		}
		readonly List<DbgThread> threads;

		DispatcherThread DispatcherThread => Process.DbgManager.DispatcherThread;

		readonly object lockObj;

		public DbgRuntimeImpl(DbgProcess process) {
			lockObj = new object();
			Process = process ?? throw new ArgumentNullException(nameof(process));
			appDomains = new List<DbgAppDomain>();
			modules = new List<DbgModule>();
			threads = new List<DbgThread>();
		}

		internal void Add_DbgThread(DbgAppDomain appDomain) {
			DispatcherThread.VerifyAccess();
			lock (lockObj)
				appDomains.Add(appDomain);
			AppDomainsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgAppDomain>(appDomain, added: true));
		}

		internal void Remove_DbgThread(DbgAppDomain appDomain) {
			DispatcherThread.VerifyAccess();
			List<DbgThread> threadsToRemove = null;
			List<DbgModule> modulesToRemove = null;
			lock (lockObj) {
				bool b = appDomains.Remove(appDomain);
				if (!b)
					return;
				for (int i = threads.Count - 1; i >= 0; i--) {
					var thread = threads[i];
					if (thread.AppDomain == appDomain) {
						if (threadsToRemove == null)
							threadsToRemove = new List<DbgThread>();
						threadsToRemove.Add(thread);
						threads.RemoveAt(i);
					}
				}
				for (int i = modules.Count - 1; i >= 0; i--) {
					var module = modules[i];
					if (module.AppDomain == appDomain) {
						if (modulesToRemove == null)
							modulesToRemove = new List<DbgModule>();
						modulesToRemove.Add(module);
						modules.RemoveAt(i);
					}
				}
			}
			if (threadsToRemove != null && threadsToRemove.Count != 0)
				ThreadsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgThread>(threadsToRemove, added: false));
			if (modulesToRemove != null && modulesToRemove.Count != 0)
				ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgModule>(modulesToRemove, added: false));
			AppDomainsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgAppDomain>(appDomain, added: false));
			if (threadsToRemove != null) {
				foreach (var thread in threadsToRemove)
					thread.Close(DispatcherThread);
			}
			if (modulesToRemove != null) {
				foreach (var module in modulesToRemove)
					module.Close(DispatcherThread);
			}
			appDomain.Close(DispatcherThread);
		}

		internal void Add_DbgThread(DbgModule module) {
			DispatcherThread.VerifyAccess();
			lock (lockObj)
				modules.Add(module);
			ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgModule>(module, added: true));
		}

		internal void Remove_DbgThread(DbgModule module) {
			DispatcherThread.VerifyAccess();
			lock (lockObj) {
				bool b = modules.Remove(module);
				if (!b)
					return;
			}
			ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgModule>(module, added: false));
			module.Close(DispatcherThread);
		}

		internal void Add_DbgThread(DbgThread thread) {
			DispatcherThread.VerifyAccess();
			lock (lockObj)
				threads.Add(thread);
			ThreadsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgThread>(thread, added: true));
		}

		internal void Remove_DbgThread(DbgThread thread) {
			DispatcherThread.VerifyAccess();
			lock (lockObj) {
				bool b = threads.Remove(thread);
				if (!b)
					return;
			}
			ThreadsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgThread>(thread, added: false));
			thread.Close(DispatcherThread);
		}

		protected override void CloseCore() {
			DispatcherThread.VerifyAccess();
			DbgThread[] removedThreads;
			DbgModule[] removedModules;
			DbgAppDomain[] removedAppDomains;
			lock (lockObj) {
				removedThreads = threads.ToArray();
				removedModules = modules.ToArray();
				removedAppDomains = appDomains.ToArray();
				threads.Clear();
				modules.Clear();
				appDomains.Clear();
			}
			if (removedThreads.Length != 0)
				ThreadsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgThread>(removedThreads, added: false));
			if (removedModules.Length != 0)
				ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgModule>(removedModules, added: false));
			if (removedAppDomains.Length != 0)
				AppDomainsChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgAppDomain>(removedAppDomains, added: false));
			foreach (var thread in removedThreads)
				thread.Close(DispatcherThread);
			foreach (var module in removedModules)
				module.Close(DispatcherThread);
			foreach (var appDomain in removedAppDomains)
				appDomain.Close(DispatcherThread);
		}
	}
}

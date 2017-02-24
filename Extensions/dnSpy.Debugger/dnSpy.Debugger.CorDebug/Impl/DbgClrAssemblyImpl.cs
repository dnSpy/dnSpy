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
using System.Linq;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet;

namespace dnSpy.Debugger.CorDebug.Impl {
	sealed class DbgClrAssemblyImpl : DbgClrAssembly {
		public override DbgClrAppDomain AppDomain { get; }

		public override event EventHandler<DbgCollectionChangedEventArgs<DbgClrModule>> ModulesChanged;
		public override DbgClrModule[] Modules {
			get {
				lock (lockObj)
					return clrModules.ToArray();
			}
		}
		readonly List<DbgClrModuleImpl> clrModules;

		public override DbgClrModule ManifestModule {
			get {
				if (manifestModule == null) {
					lock (lockObj) {
						if (manifestModule == null)
							manifestModule = clrModules.FirstOrDefault(a => a.IsManifestModule);
					}
				}
				return manifestModule;
			}
		}
		DbgClrModule manifestModule;

		internal DnAssembly DnAssembly { get; }

		readonly object lockObj;

		public DbgClrAssemblyImpl(DbgClrAppDomain appDomain, DnAssembly dnAssembly) {
			lockObj = new object();
			clrModules = new List<DbgClrModuleImpl>();
			AppDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));
			DnAssembly = dnAssembly ?? throw new ArgumentNullException(nameof(dnAssembly));
		}

		internal void AddModule_DbgThread(DbgClrModuleImpl module) {
			lock (lockObj)
				clrModules.Add(module);
			ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgClrModule>(module, added: true));
		}

		internal void RemoveModule_DbgThread(DbgClrModuleImpl module) {
			bool raiseEvent;
			lock (lockObj) {
				if (manifestModule == module)
					manifestModule = null;
				raiseEvent = clrModules.Remove(module);
			}
			Debug.Assert(raiseEvent || IsClosed);
			if (raiseEvent)
				ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgClrModule>(module, added: false));
		}

		protected override void CloseCore() {
			DbgClrModule[] modules;
			lock (lockObj) {
				manifestModule = null;
				modules = clrModules.ToArray();
				clrModules.Clear();
			}
			if (modules.Length != 0)
				ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgClrModule>(modules, added: false));
		}
	}
}

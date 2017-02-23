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
		readonly DbgManager dbgManager;

		public DbgClrAssemblyImpl(DbgManager dbgManager, DbgClrAppDomain appDomain, DnAssembly dnAssembly) {
			lockObj = new object();
			clrModules = new List<DbgClrModuleImpl>();
			this.dbgManager = dbgManager ?? throw new ArgumentNullException(nameof(dbgManager));
			AppDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));
			DnAssembly = dnAssembly ?? throw new ArgumentNullException(nameof(dnAssembly));
		}

		internal void AddModule(DbgClrModuleImpl module) {
			lock (lockObj) {
				clrModules.Add(module);
				dbgManager.DispatcherThread.BeginInvoke(() => ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgClrModule>(module, added: true)));
			}
		}

		internal void RemoveModule(DbgClrModuleImpl module) {
			lock (lockObj) {
				if (manifestModule == module)
					manifestModule = null;
				bool b = clrModules.Remove(module);
				if (b)
					dbgManager.DispatcherThread.BeginInvoke(() => ModulesChanged?.Invoke(this, new DbgCollectionChangedEventArgs<DbgClrModule>(module, added: false)));
			}
		}

		protected override void CloseCore() {
			lock (lockObj) {
				manifestModule = null;
				clrModules.Clear();
			}
		}
	}
}

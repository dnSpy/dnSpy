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
using System.Diagnostics;
using System.Threading;
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;
using dndbg.DotNet;

namespace dndbg.Engine {
	sealed class DnAssembly {
		readonly DebuggerCollection<ICorDebugModule, DnModule> modules;

		public CorAssembly CorAssembly { get; }

		/// <summary>
		/// Unique id per debugger
		/// </summary>
		public int UniqueId { get; }

		/// <summary>
		/// Unique id per process
		/// </summary>
		public int UniqueIdProcess { get; }

		/// <summary>
		/// Unique id per AppDomain
		/// </summary>
		public int UniqueIdAppDomain { get; }

		/// <summary>
		/// Assembly name, and is usually the full path to the manifest (first) module on disk
		/// (the EXE or DLL file).
		/// </summary>
		public string Name => CorAssembly.Name;

		/// <summary>
		/// Gets the full name, identical to the dnlib assembly full name
		/// </summary>
		public string FullName {
			get {
				if (fullName is null) {
					Debug.Assert(modules.Count != 0);
					if (modules.Count == 0)
						return Name;
					Interlocked.CompareExchange(ref fullName, CorAssembly.FullName, null);
				}
				return fullName!;
			}
		}
		string? fullName;

		public bool HasUnloaded { get; private set; }
		public DnDebugger Debugger => AppDomain.Debugger;
		public DnProcess Process => AppDomain.Process;
		public DnAppDomain AppDomain { get; }

		internal DnAssembly(DnAppDomain appDomain, ICorDebugAssembly assembly, int uniqueId, int uniqueIdProcess, int uniqueIdAppDomain) {
			AppDomain = appDomain;
			modules = new DebuggerCollection<ICorDebugModule, DnModule>(CreateModule);
			CorAssembly = new CorAssembly(assembly);
			UniqueId = uniqueId;
			UniqueIdProcess = uniqueIdProcess;
			UniqueIdAppDomain = uniqueIdAppDomain;
		}

		DnModule CreateModule(ICorDebugModule comModule) =>
			new DnModule(this, comModule, Debugger.GetNextModuleId(), Process.GetNextModuleId(), AppDomain.GetNextModuleId());
		internal void SetHasUnloaded() => HasUnloaded = true;
		internal DnModule? TryAdd(ICorDebugModule? comModule) => modules.Add(comModule);

		public DnModule[] Modules {
			get {
				Debugger.DebugVerifyThread();
				var list = modules.GetAll();
				Array.Sort(list, (a, b) => a.UniqueId.CompareTo(b.UniqueId));
				return list;
			}
		}

		public DnModule? TryGetModule(ICorDebugModule? comModule) {
			Debugger.DebugVerifyThread();
			return modules.TryGet(comModule);
		}

		internal void ModuleUnloaded(DnModule module) {
			module.SetHasUnloaded();
			modules.Remove(module.CorModule.RawObject);
		}

		internal void InitializeAssemblyAndModules() {
			Debugger.DebugVerifyThread();

			// No lock needed, must be called on debugger thread

			var created = new List<DnModule>();
			var modules = this.modules.GetAll();
			for (int i = 0; i < modules.Length; i++) {
				var module = modules[i];
				if (module.CorModuleDef is not null) {
					Debug2.Assert(corAssemblyDef is not null);
					continue;
				}
				module.CorModuleDef = new CorModuleDef(module.CorModule.GetMetaDataInterface<IMetaDataImport>(), new CorModuleDefHelper(module));
				if (corAssemblyDef is null)
					corAssemblyDef = new CorAssemblyDef(module.CorModuleDef, 1);
				corAssemblyDef.Modules.Add(module.CorModuleDef);
				module.CorModuleDef.Initialize();
				created.Add(module);
			}
			Debug.Assert(created.Count != 0);
			foreach (var m in created)
				Debugger.CorModuleDefCreated(m);
		}
		CorAssemblyDef? corAssemblyDef;

		public override string ToString() => $"{UniqueId} {Name}";
	}
}

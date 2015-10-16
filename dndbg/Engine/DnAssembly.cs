/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Diagnostics;
using dndbg.COM.CorDebug;
using dndbg.DotNet;

namespace dndbg.Engine {
	/// <summary>
	/// A loaded assembly
	/// </summary>
	public sealed class DnAssembly {
		readonly DebuggerCollection<ICorDebugModule, DnModule> modules;

		public CorAssembly CorAssembly {
			get { return assembly; }
		}
		readonly CorAssembly assembly;

		/// <summary>
		/// Unique id per AppDomain. Each new created assembly gets an incremented value.
		/// </summary>
		public int IncrementedId {
			get { return incrementedId; }
		}
		readonly int incrementedId;

		/// <summary>
		/// Assembly name, and is usually the full path to the manifest (first) module on disk
		/// (the EXE or DLL file).
		/// </summary>
		public string Name {
			get { return assembly.Name; }
		}

		/// <summary>
		/// true if the assembly has been unloaded
		/// </summary>
		public bool HasUnloaded {
			get { return hasUnloaded; }
		}
		bool hasUnloaded;

		/// <summary>
		/// Gets the owner debugger
		/// </summary>
		public DnDebugger Debugger {
			get { return AppDomain.Debugger; }
		}

		/// <summary>
		/// Gets the owner process
		/// </summary>
		public DnProcess Process {
			get { return AppDomain.Process; }
		}

		/// <summary>
		/// Gets the owner AppDomain
		/// </summary>
		public DnAppDomain AppDomain {
			get { return appDomain; }
		}
		readonly DnAppDomain appDomain;

		internal DnAssembly(DnAppDomain appDomain, ICorDebugAssembly assembly, int incrementedId) {
			this.appDomain = appDomain;
			this.modules = new DebuggerCollection<ICorDebugModule, DnModule>(CreateModule);
			this.assembly = new CorAssembly(assembly);
			this.incrementedId = incrementedId;
		}

		DnModule CreateModule(ICorDebugModule comModule, int id) {
			return new DnModule(this, comModule, id, Debugger.GetNextModuleId());
		}

		internal void SetHasUnloaded() {
			hasUnloaded = true;
		}

		internal DnModule TryAdd(ICorDebugModule comModule) {
			return modules.Add(comModule);
		}

		/// <summary>
		/// Gets all modules, sorted on the order they were created
		/// </summary>
		/// <returns></returns>
		public DnModule[] Modules {
			get {
				Debugger.DebugVerifyThread();
				var list = modules.GetAll();
				Array.Sort(list, (a, b) => a.IncrementedId.CompareTo(b.IncrementedId));
				return list;
			}
		}

		/// <summary>
		/// Gets a module or null
		/// </summary>
		/// <param name="comModule">Module</param>
		/// <returns></returns>
		public DnModule TryGetModule(ICorDebugModule comModule) {
			Debugger.DebugVerifyThread();
			return modules.TryGet(comModule);
		}

		internal void ModuleUnloaded(ICorDebugModule comModule) {
			var module = modules.TryGet(comModule);
			if (module == null)
				return;
			module.SetHasUnloaded();
			modules.Remove(comModule);
		}

		internal CorAssemblyDef GetOrCreateCorAssemblyDef(DnModule module, CorModuleDef corModuleDef) {
			Debugger.DebugVerifyThread();
			if (corAssemblyDef != null)
				return corAssemblyDef;

			// No lock needed, must be called on debugger thread

			Debug.Assert(module.IncrementedId == 0);
			Debug.Assert(module.CorModuleDef == corModuleDef);
			corAssemblyDef = new CorAssemblyDef(corModuleDef, 1);
			return corAssemblyDef;
		}
		CorAssemblyDef corAssemblyDef;

		public override string ToString() {
			return string.Format("{0} {1}", IncrementedId, Name);
		}
	}
}

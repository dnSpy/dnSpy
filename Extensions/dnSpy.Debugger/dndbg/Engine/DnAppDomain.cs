/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.Threading;
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	/// <summary>
	/// A debugged AppDomain in a debugged process
	/// </summary>
	public sealed class DnAppDomain {
		readonly DebuggerCollection<ICorDebugAssembly, DnAssembly> assemblies;

		public CorAppDomain CorAppDomain { get; }

		/// <summary>
		/// Unique id per debugger
		/// </summary>
		public int UniqueId { get; }

		/// <summary>
		/// Unique id per process
		/// </summary>
		public int UniqueIdProcess { get; }

		/// <summary>
		/// AppDomain Id
		/// </summary>
		public int Id => CorAppDomain.Id;

		/// <summary>
		/// true if the AppDomain has exited
		/// </summary>
		public bool HasExited { get; private set; }

		/// <summary>
		/// AppDomain name
		/// </summary>
		public string Name => CorAppDomain.Name ?? string.Empty;

		/// <summary>
		/// Gets the owner debugger
		/// </summary>
		public DnDebugger Debugger => Process.Debugger;

		/// <summary>
		/// Gets the owner process
		/// </summary>
		public DnProcess Process { get; }

		internal DnAppDomain(DnProcess ownerProcess, ICorDebugAppDomain appDomain, int uniqueId, int uniqueIdProcess) {
			this.Process = ownerProcess;
			this.assemblies = new DebuggerCollection<ICorDebugAssembly, DnAssembly>(CreateAssembly);
			this.CorAppDomain = new CorAppDomain(appDomain);
			this.UniqueId = uniqueId;
			this.UniqueIdProcess = uniqueIdProcess;
			NameChanged();
		}

		DnAssembly CreateAssembly(ICorDebugAssembly comAssembly) =>
			new DnAssembly(this, comAssembly, Debugger.GetNextAssemblyId(), Process.GetNextAssemblyId(), Interlocked.Increment(ref nextAssemblyId));
		int nextAssemblyId = -1, nextModuleId = -1;

		internal int GetNextModuleId() => Interlocked.Increment(ref nextModuleId);
		internal void NameChanged() { }
		internal void SetHasExited() => HasExited = true;

		public bool CheckValid() {
			if (HasExited)
				return false;
			int running;
			return CorAppDomain.RawObject.IsRunning(out running) >= 0;
		}

		internal DnAssembly TryAdd(ICorDebugAssembly comAssembly) => assemblies.Add(comAssembly);

		/// <summary>
		/// Gets all Assemblies, sorted on the order they were created
		/// </summary>
		/// <returns></returns>
		public DnAssembly[] Assemblies {
			get {
				Debugger.DebugVerifyThread();
				var list = assemblies.GetAll();
				Array.Sort(list, (a, b) => a.UniqueId.CompareTo(b.UniqueId));
				return list;
			}
		}

		/// <summary>
		/// Gets an Assembly or null
		/// </summary>
		/// <param name="comAssembly">Assembly</param>
		/// <returns></returns>
		public DnAssembly TryGetAssembly(ICorDebugAssembly comAssembly) {
			Debugger.DebugVerifyThread();
			return assemblies.TryGet(comAssembly);
		}

		internal void AssemblyUnloaded(ICorDebugAssembly comAssembly) {
			var assembly = assemblies.TryGet(comAssembly);
			if (assembly == null)
				return;
			assembly.SetHasUnloaded();
			assemblies.Remove(comAssembly);
		}

		/// <summary>
		/// Gets all threads, sorted on the order they were created
		/// </summary>
		/// <returns></returns>
		public DnThread[] Threads => Process.Threads.Where(t => t.AppDomainOrNull == this).ToArray();

		/// <summary>
		/// Gets all modules in this app domain
		/// </summary>
		public IEnumerable<DnModule> Modules {
			get {
				Debugger.DebugVerifyThread();
				foreach (var asm in Assemblies) {
					foreach (var mod in asm.Modules)
						yield return mod;
				}
			}
		}

		public override string ToString() => string.Format("{0} {1} {2}", UniqueId, Id, Name);
	}
}

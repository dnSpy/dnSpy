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
using System.Linq;
using System.Threading;
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	sealed class DnAppDomain {
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

		public int Id => CorAppDomain.Id;
		public bool HasExited { get; private set; }
		public string Name => CorAppDomain.Name ?? string.Empty;
		public DnDebugger Debugger => Process.Debugger;
		public DnProcess Process { get; }

		internal DnAppDomain(DnProcess ownerProcess, ICorDebugAppDomain appDomain, int uniqueId, int uniqueIdProcess) {
			Process = ownerProcess;
			assemblies = new DebuggerCollection<ICorDebugAssembly, DnAssembly>(CreateAssembly);
			CorAppDomain = new CorAppDomain(appDomain);
			UniqueId = uniqueId;
			UniqueIdProcess = uniqueIdProcess;
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
			return CorAppDomain.RawObject.IsRunning(out int running) >= 0;
		}

		internal DnAssembly? TryAdd(ICorDebugAssembly? comAssembly) => assemblies.Add(comAssembly);

		public DnAssembly[] Assemblies {
			get {
				Debugger.DebugVerifyThread();
				var list = assemblies.GetAll();
				Array.Sort(list, (a, b) => a.UniqueId.CompareTo(b.UniqueId));
				return list;
			}
		}

		public DnAssembly? TryGetAssembly(ICorDebugAssembly? comAssembly) {
			Debugger.DebugVerifyThread();
			return assemblies.TryGet(comAssembly);
		}

		internal void AssemblyUnloaded(ICorDebugAssembly? comAssembly) {
			var assembly = assemblies.TryGet(comAssembly);
			if (assembly is null)
				return;
			assembly.SetHasUnloaded();
			assemblies.Remove(comAssembly);
		}

		public DnThread[] Threads => Process.Threads.Where(t => t.AppDomain == this).ToArray();

		public IEnumerable<DnModule> Modules {
			get {
				Debugger.DebugVerifyThread();
				foreach (var asm in Assemblies) {
					foreach (var mod in asm.Modules)
						yield return mod;
				}
			}
		}

		public override string ToString() => $"{UniqueId} {Id} {Name}";
	}
}

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
using System.Linq;
using dndbg.Engine.COM.CorDebug;

namespace dndbg.Engine {
	/// <summary>
	/// A debugged AppDomain in a debugged process
	/// </summary>
	public sealed class DnAppDomain {
		readonly DebuggerCollection<ICorDebugAssembly, DnAssembly> assemblies;

		public CorAppDomain CorAppDomain {
			get { return appDomain; }
		}
		readonly CorAppDomain appDomain;

		/// <summary>
		/// Unique id per process. Each new created AppDomain gets an incremented value.
		/// </summary>
		public int IncrementedId {
			get { return incrementedId; }
		}
		readonly int incrementedId;

		/// <summary>
		/// AppDomain Id
		/// </summary>
		public int Id {
			get { return appDomain.Id; }
		}

		/// <summary>
		/// true if the AppDomain has exited
		/// </summary>
		public bool HasExited {
			get { return hasExited; }
		}
		bool hasExited;

		/// <summary>
		/// AppDomain name
		/// </summary>
		public string Name {
			get { return appDomain.Name ?? string.Empty; }
		}

		/// <summary>
		/// Gets the owner debugger
		/// </summary>
		public DnDebugger Debugger {
			get { return Process.Debugger; }
		}

		/// <summary>
		/// Gets the owner process
		/// </summary>
		public DnProcess Process {
			get { return ownerProcess; }
		}
		readonly DnProcess ownerProcess;

		internal DnAppDomain(DnProcess ownerProcess, ICorDebugAppDomain appDomain, int incrementedId) {
			this.ownerProcess = ownerProcess;
			this.assemblies = new DebuggerCollection<ICorDebugAssembly, DnAssembly>(CreateAssembly);
			this.appDomain = new CorAppDomain(appDomain);
			this.incrementedId = incrementedId;
			NameChanged();
		}

		DnAssembly CreateAssembly(ICorDebugAssembly comAssembly, int id) {
			return new DnAssembly(this, comAssembly, id);
		}

		internal void NameChanged() {
		}

		internal void SetHasExited() {
			hasExited = true;
		}

		public bool CheckValid() {
			if (HasExited)
				return false;
			int running;
			return appDomain.RawObject.IsRunning(out running) >= 0;
		}

		internal DnAssembly TryAdd(ICorDebugAssembly comAssembly) {
			return assemblies.Add(comAssembly);
		}

		/// <summary>
		/// Gets all Assemblies, sorted on the order they were created
		/// </summary>
		/// <returns></returns>
		public DnAssembly[] GetAssemblies() {
			Debugger.DebugVerifyThread();
			var list = assemblies.GetAll();
			Array.Sort(list, (a, b) => a.IncrementedId.CompareTo(b.IncrementedId));
			return list;
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
		public DnThread[] GetThreads() {
			return Process.GetThreads().Where(t => t.AppDomainOrNull == this).ToArray();
		}

		public override string ToString() {
			return string.Format("{0} {1} {2}", IncrementedId, Id, Name);
		}
	}
}

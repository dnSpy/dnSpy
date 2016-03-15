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
using System.Threading;
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	/// <summary>
	/// A debugged process
	/// </summary>
	public sealed class DnProcess {
		readonly DebuggerCollection<ICorDebugAppDomain, DnAppDomain> appDomains;
		readonly DebuggerCollection<ICorDebugThread, DnThread> threads;

		public CorProcess CorProcess {
			get { return process; }
		}
		readonly CorProcess process;

		/// <summary>
		/// Unique id per debugger
		/// </summary>
		public int UniqueId {
			get { return uniqueId; }
		}
		readonly int uniqueId;

		/// <summary>
		/// Gets the process id (pid) of the process
		/// </summary>
		public int ProcessId {
			get { return process.ProcessId; }
		}

		/// <summary>
		/// true if the process has exited
		/// </summary>
		public bool HasExited {
			get { return hasExited; }
		}
		bool hasExited;

		/// <summary>
		/// true if it has been initialized
		/// </summary>
		public bool HasInitialized {
			get { return hasInitialized; }
		}
		bool hasInitialized = false;

		/// <summary>
		/// Filename or empty string
		/// </summary>
		public string Filename {
			get { return filename; }
		}
		string filename = string.Empty;

		public string WorkingDirectory {
			get { return cwd; }
		}
		string cwd = string.Empty;

		public string CommandLine {
			get { return cmdLine; }
		}
		string cmdLine = string.Empty;

		/// <summary>
		/// Returns the value of ICorDebugProcess::GetHelperThreadID(). Don't cache this value since
		/// it can change. 0 is returned if the thread doesn't exist.
		/// </summary>
		public uint HelperThreadId {
			get { return process.HelperThreadId; }
		}

		/// <summary>
		/// Gets the owner debugger
		/// </summary>
		public DnDebugger Debugger {
			get { return ownerDebugger; }
		}
		readonly DnDebugger ownerDebugger;

		internal DnProcess(DnDebugger ownerDebugger, ICorDebugProcess process, int uniqueId) {
			this.ownerDebugger = ownerDebugger;
			this.appDomains = new DebuggerCollection<ICorDebugAppDomain, DnAppDomain>(CreateAppDomain);
			this.threads = new DebuggerCollection<ICorDebugThread, DnThread>(CreateThread);
			this.process = new CorProcess(process);
			this.uniqueId = uniqueId;
		}
		int nextAppDomainId = -1, nextAssemblyId = -1, nextModuleId = -1, nextThreadId = -1;

		internal int GetNextAssemblyId() {
			return Interlocked.Increment(ref nextAssemblyId);
		}

		internal int GetNextModuleId() {
			return Interlocked.Increment(ref nextModuleId);
		}

		DnAppDomain CreateAppDomain(ICorDebugAppDomain appDomain) {
			return new DnAppDomain(this, appDomain, Debugger.GetNextAppDomainId(), Interlocked.Increment(ref nextAppDomainId));
		}

		DnThread CreateThread(ICorDebugThread thread) {
			return new DnThread(this, thread, Debugger.GetNextThreadId(), Interlocked.Increment(ref nextThreadId));
		}

		public bool Terminate(int exitCode) {
			return process.Terminate(exitCode);
		}

		/// <summary>
		/// true if we attached to the process, false if we created the process
		/// </summary>
		public bool WasAttached {
			get { return attached; }
		}
		bool attached;

		internal void Initialize(bool attached, string filename, string cwd, string cmdLine) {
			this.attached = attached;
			this.filename = filename ?? string.Empty;
			this.cwd = cwd ?? string.Empty;
			this.cmdLine = cmdLine ?? string.Empty;
			this.hasInitialized = true;
		}

		internal void SetHasExited() {
			hasExited = true;
		}

		public bool CheckValid() {
			if (HasExited)
				return false;
			int running;
			return process.RawObject.IsRunning(out running) >= 0;
		}

		internal DnAppDomain TryAdd(ICorDebugAppDomain comAppDomain) {
			return appDomains.Add(comAppDomain);
		}

		/// <summary>
		/// Gets all AppDomains, sorted on the order they were created
		/// </summary>
		/// <returns></returns>
		public DnAppDomain[] AppDomains {
			get {
				Debugger.DebugVerifyThread();
				var list = appDomains.GetAll();
				Array.Sort(list, (a, b) => a.UniqueId.CompareTo(b.UniqueId));
				return list;
			}
		}

		internal DnAppDomain TryGetAppDomain(ICorDebugAppDomain comAppDomain) {
			return appDomains.TryGet(comAppDomain);
		}

		/// <summary>
		/// Gets an AppDomain or null if it has exited
		/// </summary>
		/// <param name="comAppDomain">AppDomain</param>
		/// <returns></returns>
		public DnAppDomain TryGetValidAppDomain(ICorDebugAppDomain comAppDomain) {
			Debugger.DebugVerifyThread();
			var appDomain = appDomains.TryGet(comAppDomain);
			if (appDomain == null)
				return null;
			if (!appDomain.CheckValid())
				return null;
			return appDomain;
		}

		internal void AppDomainExited(ICorDebugAppDomain comAppDomain) {
			var appDomain = appDomains.TryGet(comAppDomain);
			if (appDomain == null)
				return;
			appDomain.SetHasExited();
			appDomains.Remove(comAppDomain);
		}

		internal DnThread TryAdd(ICorDebugThread comThread) {
			return threads.Add(comThread);
		}

		/// <summary>
		/// Gets all threads, sorted on the order they were created
		/// </summary>
		/// <returns></returns>
		public DnThread[] Threads {
			get {
				Debugger.DebugVerifyThread();
				var list = threads.GetAll();
				Array.Sort(list, (a, b) => a.UniqueId.CompareTo(b.UniqueId));
				return list;
			}
		}

		internal DnThread TryGetThread(ICorDebugThread comThread) {
			return threads.TryGet(comThread);
		}

		/// <summary>
		/// Gets a thread or null if it has exited
		/// </summary>
		/// <param name="comThread">Thread</param>
		/// <returns></returns>
		public DnThread TryGetValidThread(ICorDebugThread comThread) {
			Debugger.DebugVerifyThread();
			var thread = threads.TryGet(comThread);
			if (thread == null)
				return null;
			if (!thread.CheckValid())
				return null;
			return thread;
		}

		internal void ThreadExited(ICorDebugThread comThread) {
			var thread = threads.TryGet(comThread);
			// Sometimes we don't get a CreateThread message
			if (thread == null)
				return;
			thread.SetHasExited();
			threads.Remove(comThread);
		}

		/// <summary>
		/// Gets the main thread in the main AppDomain, if an AppDomain with threads exist, else
		/// it returns a thread or null
		/// </summary>
		/// <returns></returns>
		public DnThread GetMainThread() {
			var threads = Threads;
			var appDomain = GetMainAppDomain();
			foreach (var thread in threads) {
				if (thread.AppDomainOrNull == appDomain)
					return thread;
			}
			return threads.Length == 0 ? null : threads[0];
		}

		/// <summary>
		/// Gets the main AppDomain or null
		/// </summary>
		/// <returns></returns>
		public DnAppDomain GetMainAppDomain() {
			var appDomains = AppDomains;
			return appDomains.Length == 0 ? null : appDomains[0];
		}

		/// <summary>
		/// Gets all modules from all app domains
		/// </summary>
		public IEnumerable<DnModule> Modules {
			get {
				Debugger.DebugVerifyThread();
				foreach (var ad in AppDomains) {
					foreach (var asm in ad.Assemblies) {
						foreach (var mod in asm.Modules)
							yield return mod;
					}
				}
			}
		}

		/// <summary>
		/// Gets all assemblies from all app domains
		/// </summary>
		public IEnumerable<DnAssembly> Assemblies {
			get {
				Debugger.DebugVerifyThread();
				foreach (var ad in AppDomains) {
					foreach (var asm in ad.Assemblies)
						yield return asm;
				}
			}
		}

		public override string ToString() {
			return string.Format("{0} {1} {2}", UniqueId, ProcessId, Filename);
		}
	}
}

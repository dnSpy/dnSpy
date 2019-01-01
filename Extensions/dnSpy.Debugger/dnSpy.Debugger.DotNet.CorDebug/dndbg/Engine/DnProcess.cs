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
using System.Threading;
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	sealed class DnProcess {
		readonly DebuggerCollection<ICorDebugAppDomain, DnAppDomain> appDomains;
		readonly DebuggerCollection<ICorDebugThread, DnThread> threads;

		public CorProcess CorProcess { get; }

		/// <summary>
		/// Unique id per debugger
		/// </summary>
		public int UniqueId { get; }

		public int ProcessId => CorProcess.ProcessId;
		public bool HasExited { get; private set; }
		public bool HasInitialized { get; private set; }

		public string Filename => filename;
		string filename = string.Empty;

		public string WorkingDirectory => cwd;
		string cwd = string.Empty;

		public string CommandLine => cmdLine;
		string cmdLine = string.Empty;

		public uint HelperThreadId => CorProcess.HelperThreadId;
		public DnDebugger Debugger { get; }

		internal DnProcess(DnDebugger ownerDebugger, ICorDebugProcess process, int uniqueId) {
			Debugger = ownerDebugger;
			appDomains = new DebuggerCollection<ICorDebugAppDomain, DnAppDomain>(CreateAppDomain);
			threads = new DebuggerCollection<ICorDebugThread, DnThread>(CreateThread);
			CorProcess = new CorProcess(process);
			UniqueId = uniqueId;
		}
		int nextAppDomainId = -1, nextAssemblyId = -1, nextModuleId = -1, nextThreadId = -1;

		internal int GetNextAssemblyId() => Interlocked.Increment(ref nextAssemblyId);
		internal int GetNextModuleId() => Interlocked.Increment(ref nextModuleId);
		DnAppDomain CreateAppDomain(ICorDebugAppDomain appDomain) =>
			new DnAppDomain(this, appDomain, Debugger.GetNextAppDomainId(), Interlocked.Increment(ref nextAppDomainId));
		DnThread CreateThread(ICorDebugThread thread) =>
			new DnThread(this, thread, Debugger.GetNextThreadId(), Interlocked.Increment(ref nextThreadId));
		public bool Terminate(int exitCode) => CorProcess.Terminate(exitCode);

		internal void Initialize(string filename, string cwd, string cmdLine) {
			this.filename = filename ?? string.Empty;
			this.cwd = cwd ?? string.Empty;
			this.cmdLine = cmdLine ?? string.Empty;
			HasInitialized = true;
		}

		internal void SetHasExited() => HasExited = true;

		public bool CheckValid() {
			if (HasExited)
				return false;
			return CorProcess.RawObject.IsRunning(out int running) >= 0;
		}

		internal DnAppDomain TryAdd(ICorDebugAppDomain comAppDomain) => appDomains.Add(comAppDomain);

		public DnAppDomain[] AppDomains {
			get {
				Debugger.DebugVerifyThread();
				var list = appDomains.GetAll();
				Array.Sort(list, (a, b) => a.UniqueId.CompareTo(b.UniqueId));
				return list;
			}
		}

		internal DnAppDomain TryGetAppDomain(ICorDebugAppDomain comAppDomain) => appDomains.TryGet(comAppDomain);

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

		internal DnThread TryAdd(ICorDebugThread comThread) => threads.Add(comThread);

		public DnThread[] Threads {
			get {
				Debugger.DebugVerifyThread();
				var list = threads.GetAll();
				Array.Sort(list, (a, b) => a.UniqueId.CompareTo(b.UniqueId));
				return list;
			}
		}

		internal DnThread TryGetThread(ICorDebugThread comThread) => threads.TryGet(comThread);

		public DnThread TryGetValidThread(ICorDebugThread comThread) {
			Debugger.DebugVerifyThread();
			var thread = threads.TryGet(comThread);
			if (thread == null)
				return null;
			if (!thread.CheckValid())
				return null;
			return thread;
		}

		internal DnThread ThreadExited(ICorDebugThread comThread) {
			var thread = threads.TryGet(comThread);
			// Sometimes we don't get a CreateThread message
			if (thread != null) {
				thread.SetHasExited();
				threads.Remove(comThread);
			}
			return thread;
		}

		public DnThread GetMainThread() {
			var threads = Threads;
			var appDomain = GetMainAppDomain();
			foreach (var thread in threads) {
				if (thread.AppDomainOrNull == appDomain)
					return thread;
			}
			return threads.Length == 0 ? null : threads[0];
		}

		public DnAppDomain GetMainAppDomain() {
			var appDomains = AppDomains;
			return appDomains.Length == 0 ? null : appDomains[0];
		}

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

		public IEnumerable<DnAssembly> Assemblies {
			get {
				Debugger.DebugVerifyThread();
				foreach (var ad in AppDomains) {
					foreach (var asm in ad.Assemblies)
						yield return asm;
				}
			}
		}

		public override string ToString() => $"{UniqueId} {ProcessId} {Filename}";
	}
}

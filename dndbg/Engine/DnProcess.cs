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
using dndbg.Engine.COM.CorDebug;

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
		/// Unique id per debugger. Each new created process gets an incremented value.
		/// </summary>
		public int IncrementedId {
			get { return incrementedId; }
		}
		readonly int incrementedId;

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
		string cmdLine;

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

		internal DnProcess(DnDebugger ownerDebugger, ICorDebugProcess process, int incrementedId) {
			this.ownerDebugger = ownerDebugger;
			this.appDomains = new DebuggerCollection<ICorDebugAppDomain, DnAppDomain>(CreateAppDomain);
			this.threads = new DebuggerCollection<ICorDebugThread, DnThread>(CreateThread);
			this.process = new CorProcess(process);
			this.incrementedId = incrementedId;
		}

		DnAppDomain CreateAppDomain(ICorDebugAppDomain appDomain, int id) {
			return new DnAppDomain(this, appDomain, id);
		}

		DnThread CreateThread(ICorDebugThread thread, int id) {
			return new DnThread(this, thread, id);
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
			this.filename = filename;
			this.cwd = cwd;
			this.cmdLine = cmdLine;
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
				Array.Sort(list, (a, b) => a.IncrementedId.CompareTo(b.IncrementedId));
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
				Array.Sort(list, (a, b) => a.IncrementedId.CompareTo(b.IncrementedId));
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

		public override string ToString() {
			return string.Format("{0} {1} {2}", IncrementedId, ProcessId, Filename);
		}
	}
}

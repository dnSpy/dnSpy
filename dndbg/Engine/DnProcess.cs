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
using dndbg.Engine.COM.CorDebug;

namespace dndbg.Engine {
	/// <summary>
	/// A debugged process
	/// </summary>
	public sealed class DnProcess {
		readonly DebuggerCollection<ICorDebugAppDomain, DnAppDomain> appDomains;
		readonly DebuggerCollection<ICorDebugThread, DnThread> threads;

		/// <summary>
		/// Gets the COM object
		/// </summary>
		public ICorDebugProcess RawObject {
			get { return process; }
		}
		readonly ICorDebugProcess process;

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
			get { return pid; }
		}
		readonly int pid;

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
			get {
				uint threadId;
				int hr = process.GetHelperThreadID(out threadId);
				return hr < 0 ? 0 : threadId;
			}
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
			this.process = process;
			this.incrementedId = incrementedId;
			int hr = this.process.GetID(out this.pid);
			if (hr < 0)
				this.pid = 0;
		}

		DnAppDomain CreateAppDomain(ICorDebugAppDomain appDomain, int id) {
			return new DnAppDomain(this, appDomain, id);
		}

		DnThread CreateThread(ICorDebugThread thread, int id) {
			return new DnThread(this, thread, id);
		}

		public bool Terminate(int exitCode) {
			return process.Terminate((uint)exitCode) >= 0;
		}

		internal void Initialize(string filename, string cwd, string cmdLine) {
			this.filename = filename;
			this.cwd = cwd;
			this.cmdLine = cmdLine;
			this.hasInitialized = true;
		}

		/// <summary>
		/// Enable or disable log messages. Must be called after CreateProcess event has occurred
		/// </summary>
		/// <param name="enable"></param>
		public void EnableLogMessages(bool enable) {
			int hr = process.EnableLogMessages(enable ? 1 : 0);
			Debug.Assert(hr >= 0);
		}

		/// <summary>
		/// Calls ICorDebugProcess2::SetDesiredNGENCompilerFlags() if the iface is available
		/// </summary>
		/// <param name="flags">Flags</param>
		public void SetDesiredNGENCompilerFlags(CorDebugJITCompilerFlags flags) {
			var dbg2 = process as ICorDebugProcess2;
			if (dbg2 != null) {
				int hr = dbg2.SetDesiredNGENCompilerFlags(flags);
			}
		}

		/// <summary>
		/// Calls ICorDebugProcess7::SetWriteableMetadataUpdateMode() if the iface is available
		/// </summary>
		/// <param name="mode"></param>
		public void SetWriteableMetadataUpdateMode(WriteableMetadataUpdateMode mode) {
			var dbg7 = process as ICorDebugProcess7;
			if (dbg7 != null) {
				int hr = dbg7.SetWriteableMetadataUpdateMode(mode);
				// 0x80131c4e: CORDBG_E_UNSUPPORTED
			}
		}

		/// <summary>
		/// Calls ICorDebugProcess7::SetWriteableMetadataUpdateMode() if the iface is available
		/// </summary>
		/// <param name="mode"></param>
		public void EnableExceptionCallbacksOutsideOfMyCode(bool value) {
			var dbg8 = process as ICorDebugProcess8;
			if (dbg8 != null)
				dbg8.EnableExceptionCallbacksOutsideOfMyCode(value ? 1 : 0);
		}

		internal void SetHasExited() {
			hasExited = true;
		}

		public bool CheckValid() {
			if (HasExited)
				return false;
			int running;
			return process.IsRunning(out running) >= 0;
		}

		internal DnAppDomain TryAdd(ICorDebugAppDomain comAppDomain) {
			return appDomains.Add(comAppDomain);
		}

		/// <summary>
		/// Gets all AppDomains, sorted on the order they were created
		/// </summary>
		/// <returns></returns>
		public DnAppDomain[] GetAppDomains() {
			Debugger.DebugVerifyThread();
			var list = appDomains.GetAll();
			Array.Sort(list, (a, b) => a.IncrementedId.CompareTo(b.IncrementedId));
			return list;
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
		public DnThread[] GetThreads() {
			Debugger.DebugVerifyThread();
			var list = threads.GetAll();
			Array.Sort(list, (a, b) => a.IncrementedId.CompareTo(b.IncrementedId));
			return list;
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

		public override string ToString() {
			return string.Format("{0} {1} {2}", IncrementedId, ProcessId, Filename);
		}
	}
}

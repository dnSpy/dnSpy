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

using System.Collections.Generic;
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	/// <summary>
	/// A debugged .NET thread
	/// </summary>
	public sealed class DnThread {
		public CorThread CorThread {
			get { return thread; }
		}
		readonly CorThread thread;

		/// <summary>
		/// Unique id per debugger
		/// </summary>
		public int UniqueId {
			get { return uniqueId; }
		}
		readonly int uniqueId;

		/// <summary>
		/// Unique id per process
		/// </summary>
		public int UniqueIdProcess {
			get { return uniqueIdProcess; }
		}
		readonly int uniqueIdProcess;

		/// <summary>
		/// Gets the thread ID (calls ICorDebugThread::GetID()). This is not necessarily the OS
		/// thread ID in V2 or later, see <see cref="VolatileThreadId"/>
		/// </summary>
		public int ThreadId {
			get { return thread.ThreadId; }
		}

		/// <summary>
		/// Gets the OS thread ID (calls ICorDebugThread2::GetVolatileOSThreadID()) or -1. This value
		/// can change during execution of the thread.
		/// </summary>
		public int VolatileThreadId {
			get { return thread.VolatileThreadId; }
		}

		/// <summary>
		/// true if the thread has exited
		/// </summary>
		public bool HasExited {
			get { return hasExited; }
		}
		bool hasExited;

		/// <summary>
		/// Gets the AppDomain or null if none
		/// </summary>
		public DnAppDomain AppDomainOrNull {
			get {
				var comAppDomain = thread.AppDomain;
				return comAppDomain == null ? null : Process.TryGetValidAppDomain(comAppDomain.RawObject);
			}
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

		/// <summary>
		/// Gets all chains
		/// </summary>
		public IEnumerable<CorChain> Chains {
			get { return thread.Chains; }
		}

		/// <summary>
		/// Gets all frames in all chains
		/// </summary>
		public IEnumerable<CorFrame> AllFrames {
			get { return thread.AllFrames; }
		}

		internal DnThread(DnProcess ownerProcess, ICorDebugThread thread, int uniqueId, int uniqueIdProcess) {
			this.ownerProcess = ownerProcess;
			this.thread = new CorThread(thread);
			this.uniqueId = uniqueId;
			this.uniqueIdProcess = uniqueIdProcess;
		}

		internal void SetHasExited() {
			hasExited = true;
		}

		public bool CheckValid() {
			if (HasExited)
				return false;

			return true;
		}

		internal void NameChanged() {
			//TODO:
		}

		public override string ToString() {
			return string.Format("{0} {1} {2}", UniqueId, ThreadId, VolatileThreadId);
		}
	}
}

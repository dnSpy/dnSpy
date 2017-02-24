/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
	sealed class DnThread {
		public CorThread CorThread { get; }

		/// <summary>
		/// Unique id per debugger
		/// </summary>
		public int UniqueId { get; }

		/// <summary>
		/// Unique id per process
		/// </summary>
		public int UniqueIdProcess { get; }

		/// <summary>
		/// Gets the thread ID (calls ICorDebugThread::GetID()). This is not necessarily the OS
		/// thread ID in V2 or later, see <see cref="VolatileThreadId"/>
		/// </summary>
		public int ThreadId => CorThread.ThreadId;

		/// <summary>
		/// Gets the OS thread ID (calls ICorDebugThread2::GetVolatileOSThreadID()) or -1. This value
		/// can change during execution of the thread.
		/// </summary>
		public int VolatileThreadId => CorThread.VolatileThreadId;

		/// <summary>
		/// true if the thread has exited
		/// </summary>
		public bool HasExited { get; private set; }

		/// <summary>
		/// Gets the AppDomain or null if none
		/// </summary>
		public DnAppDomain AppDomainOrNull {
			get {
				var comAppDomain = CorThread.AppDomain;
				return comAppDomain == null ? null : Process.TryGetValidAppDomain(comAppDomain.RawObject);
			}
		}

		/// <summary>
		/// Gets the owner debugger
		/// </summary>
		public DnDebugger Debugger => Process.Debugger;

		/// <summary>
		/// Gets the owner process
		/// </summary>
		public DnProcess Process { get; }

		/// <summary>
		/// Gets all chains
		/// </summary>
		public IEnumerable<CorChain> Chains => CorThread.Chains;

		/// <summary>
		/// Gets all frames in all chains
		/// </summary>
		public IEnumerable<CorFrame> AllFrames => CorThread.AllFrames;

		internal DnThread(DnProcess ownerProcess, ICorDebugThread thread, int uniqueId, int uniqueIdProcess) {
			Process = ownerProcess;
			CorThread = new CorThread(thread);
			UniqueId = uniqueId;
			UniqueIdProcess = uniqueIdProcess;
		}

		internal void SetHasExited() => HasExited = true;

		public bool CheckValid() {
			if (HasExited)
				return false;

			return true;
		}

		internal void NameChanged() {
			//TODO:
		}

		public override string ToString() => string.Format("{0} {1} {2}", UniqueId, ThreadId, VolatileThreadId);
	}
}

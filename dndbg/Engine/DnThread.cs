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
using System.Collections.Generic;
using dndbg.Engine.COM.CorDebug;

namespace dndbg.Engine {
	/// <summary>
	/// A debugged .NET thread
	/// </summary>
	public sealed class DnThread {
		/// <summary>
		/// Gets the COM object
		/// </summary>
		public ICorDebugThread RawObject {
			get { return thread; }
		}
		readonly ICorDebugThread thread;

		/// <summary>
		/// Unique id per process. Each new created thread gets an incremented value.
		/// </summary>
		public int IncrementedId {
			get { return incrementedId; }
		}
		readonly int incrementedId;

		/// <summary>
		/// Gets the thread ID (calls ICorDebugThread::GetID()). This is not necessarily the OS
		/// thread ID in V2 or later, see <see cref="VolatileThreadId"/>
		/// </summary>
		public int ThreadId {
			get {
				int tid;
				int hr = thread.GetID(out tid);
				return hr < 0 ? -1 : tid;
			}
		}

		/// <summary>
		/// Gets the OS thread ID (calls ICorDebugThread2::GetVolatileOSThreadID()) or -1. This value
		/// can change during execution of the thread.
		/// </summary>
		public int VolatileThreadId {
			get {
				var th2 = thread as ICorDebugThread2;
				if (th2 == null)
					return -1;
				int tid;
				int hr = th2.GetVolatileOSThreadID(out tid);
				return hr < 0 ? -1 : tid;
			}
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
				ICorDebugAppDomain comAppDomain;
				int hr = thread.GetAppDomain(out comAppDomain);
				if (hr < 0)
					return null;
				return Process.TryGetValidAppDomain(comAppDomain);
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
		public IEnumerable<DnChain> Chains {
			get {
				ICorDebugChainEnum chainEnum;
				int hr = thread.EnumerateChains(out chainEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					ICorDebugChain chain = null;
					hr = chainEnum.Next(1, out chain, IntPtr.Zero);
					if (hr != 0 || chain == null)
						break;
					yield return new DnChain(chain);
				}
			}
		}

		/// <summary>
		/// Gets all frames in all chains
		/// </summary>
		public IEnumerable<DnFrame> AllFrames {
			get {
				foreach (var chain in Chains) {
					foreach (var frame in chain.Frames)
						yield return frame;
				}
			}
		}

		internal DnThread(DnProcess ownerProcess, ICorDebugThread thread, int incrementedId) {
			this.ownerProcess = ownerProcess;
			this.thread = thread;
			this.incrementedId = incrementedId;
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
			return string.Format("{0} {1} {2}", IncrementedId, ThreadId, VolatileThreadId);
		}
	}
}

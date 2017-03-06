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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Diagnostics.Runtime;

namespace dnSpy.Debugger.CorDebug.DAC {
	sealed class ClrDacImpl : ClrDac {
		DataTarget dataTarget;
		ClrRuntime clrRuntime;
		IClrDacDebugger clrDacDebugger;
		readonly Dictionary<int, ClrThread> toClrThread;
		bool toClrThreadInitd;

		public ClrDacImpl(DataTarget dataTarget, ClrRuntime clrRuntime, IClrDacDebugger clrDacDebugger) {
			this.dataTarget = dataTarget ?? throw new ArgumentNullException(nameof(dataTarget));
			this.clrRuntime = clrRuntime ?? throw new ArgumentNullException(nameof(clrRuntime));
			this.clrDacDebugger = clrDacDebugger ?? throw new ArgumentNullException(nameof(clrDacDebugger));
			toClrThread = new Dictionary<int, ClrThread>();
			clrDacDebugger.ClrDacPaused += ClrDacDebugger_ClrDacPaused;
			clrDacDebugger.ClrDacRunning += ClrDacDebugger_ClrDacRunning;
			clrDacDebugger.ClrDacTerminated += ClrDacDebugger_ClrDacTerminated;
		}

		void Flush() {
			clrRuntime.Flush();
			toClrThread.Clear();
			toClrThreadInitd = false;
		}

		void ClrDacDebugger_ClrDacPaused(object sender, EventArgs e) => Flush();
		void ClrDacDebugger_ClrDacRunning(object sender, EventArgs e) => Flush();

		void ClrDacDebugger_ClrDacTerminated(object sender, EventArgs e) {
			clrDacDebugger.ClrDacPaused -= ClrDacDebugger_ClrDacPaused;
			clrDacDebugger.ClrDacRunning -= ClrDacDebugger_ClrDacRunning;
			clrDacDebugger.ClrDacTerminated -= ClrDacDebugger_ClrDacTerminated;
			Flush();
			dataTarget.Dispose();
			dataTarget = null;
			clrRuntime = null;
			clrDacDebugger = null;
		}

		public override ClrDacThreadInfo? GetThreadInfo(int tid) {
			if (!toClrThreadInitd) {
				toClrThreadInitd = true;
				Debug.Assert(toClrThread.Count == 0);
				foreach (var thread in clrRuntime.Threads) {
					if (thread.OSThreadId == 0)
						continue;
					Debug.Assert(!toClrThread.ContainsKey((int)thread.OSThreadId));
					toClrThread[(int)thread.OSThreadId] = thread;
				}
			}
			if (toClrThread.TryGetValue(tid, out var thread2))
				return CreateClrDacThreadInfo(thread2);
			return null;
		}

		ClrDacThreadInfo CreateClrDacThreadInfo(ClrThread thread) {
			var flags = ClrDacThreadFlags.None;
			if (thread.IsFinalizer) flags |= ClrDacThreadFlags.IsFinalizer;
			if (thread.IsAlive) flags |= ClrDacThreadFlags.IsAlive;
			if (clrRuntime.ServerGC && thread.IsGC) flags |= ClrDacThreadFlags.IsGC;
			if (thread.IsDebuggerHelper) flags |= ClrDacThreadFlags.IsDebuggerHelper;
			if (thread.IsThreadpoolTimer) flags |= ClrDacThreadFlags.IsThreadpoolTimer;
			if (thread.IsThreadpoolCompletionPort) flags |= ClrDacThreadFlags.IsThreadpoolCompletionPort;
			if (thread.IsThreadpoolWorker) flags |= ClrDacThreadFlags.IsThreadpoolWorker;
			if (thread.IsThreadpoolWait) flags |= ClrDacThreadFlags.IsThreadpoolWait;
			if (thread.IsThreadpoolGate) flags |= ClrDacThreadFlags.IsThreadpoolGate;
			if (thread.IsSuspendingEE) flags |= ClrDacThreadFlags.IsSuspendingEE;
			if (thread.IsShutdownHelper) flags |= ClrDacThreadFlags.IsShutdownHelper;
			if (thread.IsAbortRequested) flags |= ClrDacThreadFlags.IsAbortRequested;
			if (thread.IsAborted) flags |= ClrDacThreadFlags.IsAborted;
			if (thread.IsGCSuspendPending) flags |= ClrDacThreadFlags.IsGCSuspendPending;
			if (thread.IsUserSuspended) flags |= ClrDacThreadFlags.IsUserSuspended;
			if (thread.IsDebugSuspended) flags |= ClrDacThreadFlags.IsDebugSuspended;
			if (thread.IsBackground) flags |= ClrDacThreadFlags.IsBackground;
			if (thread.IsUnstarted) flags |= ClrDacThreadFlags.IsUnstarted;
			if (thread.IsCoInitialized) flags |= ClrDacThreadFlags.IsCoInitialized;
			if (thread.IsSTA) flags |= ClrDacThreadFlags.IsSTA;
			if (thread.IsMTA) flags |= ClrDacThreadFlags.IsMTA;
			return new ClrDacThreadInfo(thread.ManagedThreadId, flags);
		}
	}
}

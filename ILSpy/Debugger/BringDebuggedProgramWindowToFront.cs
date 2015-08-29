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
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using dndbg.Engine;

namespace dnSpy.Debugger {
	sealed class BringDebuggedProgramWindowToFront {
		// Millisecs to wait before we bring the debugged process' window to the front
		const int WAIT_TIME_MS = 1000;

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32")]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		public BringDebuggedProgramWindowToFront() {
			DebugManager.Instance.OnProcessStateChanged += DebugManager_OnProcessStateChanged;
		}

		bool isRunning;
		int isRunningId;

		void DebugManager_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			var debugger = DebugManager.Instance.Debugger;
			bool newIsRunning = debugger != null && debugger.ProcessState == DebuggerProcessState.Running;
			if (newIsRunning == isRunning)
				return;
			var dnProcess = debugger.GetProcesses().FirstOrDefault();
			if (dnProcess == null)
				return;

			isRunning = newIsRunning;
			int id = Interlocked.Increment(ref isRunningId);
			if (!isRunning)
				return;

			var process = GetProcessById(dnProcess.ProcessId);
			if (process == null)
				return;

			Timer timer = null;
			timer = new Timer(a => {
				timer.Dispose();
				if (id == isRunningId)
					SwitchToDebuggedProcessWindow(process);
			}, null, WAIT_TIME_MS, Timeout.Infinite);
		}

		Process GetProcessById(int pid) {
			try {
				return Process.GetProcessById(pid);
			}
			catch {
			}
			return null;
		}

		void SwitchToDebuggedProcessWindow(Process process) {
			try {
				var hWnd = process.MainWindowHandle;
				if (hWnd != IntPtr.Zero)
					SetForegroundWindow(hWnd);
			}
			catch {
			}
		}
	}
}

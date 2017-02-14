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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using dndbg.Engine;

namespace dnSpy.Debugger {
	sealed class DebuggedProcessRunningEventArgs : EventArgs {
		public Process Process { get; }

		public DebuggedProcessRunningEventArgs(Process process) => Process = process;
	}

	sealed class DebuggedProcessRunningNotifier {
		const int WAIT_TIME_MS = 1000;
		readonly Dispatcher dispatcher;
		readonly ITheDebugger theDebugger;

		public DebuggedProcessRunningNotifier(ITheDebugger theDebugger) {
			dispatcher = Dispatcher.CurrentDispatcher;
			this.theDebugger = theDebugger;
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
		}

		public event EventHandler<DebuggedProcessRunningEventArgs> ProcessRunning;

		bool isRunning;
		int isRunningId;

		void TheDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			if (theDebugger.Debugger == null)
				return;
			if (theDebugger.Debugger.IsEvaluating)
				return;
			bool newIsRunning = theDebugger.ProcessState == DebuggerProcessState.Running;
			if (newIsRunning == isRunning)
				return;
			var dnProcess = theDebugger.Debugger.Processes.FirstOrDefault();
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
				if (id == isRunningId) {
					if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
						return;
					dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => {
						if (id == isRunningId) {
							ProcessRunning?.Invoke(this, new DebuggedProcessRunningEventArgs(process));
						}
					}));
				}
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
	}
}

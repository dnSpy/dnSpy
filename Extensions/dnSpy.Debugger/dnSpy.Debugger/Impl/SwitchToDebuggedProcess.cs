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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Debugger.Native;

namespace dnSpy.Debugger.Impl {
	[Export(typeof(IDbgManagerStartListener))]
	sealed class SwitchToDebuggedProcess : IDbgManagerStartListener {
		readonly DebuggerSettings debuggerSettings;
		bool ignoreSetForeground;
		DbgProcess currentProcess;

		[ImportingConstructor]
		SwitchToDebuggedProcess(DebuggerSettings debuggerSettings) => this.debuggerSettings = debuggerSettings;

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) {
			dbgManager.DelayedIsRunningChanged += DbgManager_DelayedIsRunningChanged;
			dbgManager.IsRunningChanged += DbgManager_IsRunningChanged;
			dbgManager.IsDebuggingChanged += DbgManager_IsDebuggingChanged;
			dbgManager.CurrentProcessChanged += DbgManager_CurrentProcessChanged;
			dbgManager.ProcessesChanged += DbgManager_ProcessesChanged;
		}

		void DbgManager_ProcessesChanged(object sender, DbgCollectionChangedEventArgs<DbgProcess> e) {
			if (!e.Added && e.Objects.Contains(currentProcess))
				currentProcess = null;
		}

		void DbgManager_CurrentProcessChanged(object sender, DbgCurrentObjectChangedEventArgs<DbgProcess> e) {
			if (!e.CurrentChanged)
				return;
			var newProcess = ((DbgManager)sender).CurrentProcess.Current;
			if (newProcess != null)
				currentProcess = newProcess;
		}

		void DbgManager_IsDebuggingChanged(object sender, EventArgs e) {
			ignoreSetForeground = true;
			currentProcess = null;
		}

		void DbgManager_IsRunningChanged(object sender, EventArgs e) {
			var dbgManager = (DbgManager)sender;
			if (dbgManager.IsRunning != false)
				return;
			ignoreSetForeground = false;
		}

		void DbgManager_DelayedIsRunningChanged(object sender, EventArgs e) {
			var process = currentProcess;
			currentProcess = null;

			// Ignore it the first time because the OS will give the debugged process focus
			if (ignoreSetForeground)
				return;
			if (process == null)
				process = ((DbgManager)sender).Processes.FirstOrDefault(a => a.State == DbgProcessState.Running);
			// Fails if the process hasn't been created yet (eg. the engine hasn't connected to the process yet)
			if (process == null)
				return;
			if (!debuggerSettings.FocusActiveProcess)
				return;
			try {
				using (var p = Process.GetProcessById((int)process.Id)) {
					var hWnd = p.MainWindowHandle;
					if (hWnd != IntPtr.Zero)
						NativeMethods.SetForegroundWindow(hWnd);
				}
			}
			catch {
			}
		}
	}
}

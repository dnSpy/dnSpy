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
using dnSpy.Contracts.Debugger;
using dnSpy.Debugger.Native;

namespace dnSpy.Debugger.Impl {
	[ExportDbgManagerStartListener]
	sealed class SwitchToDebuggedProcess : IDbgManagerStartListener {
		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) => dbgManager.DelayedIsRunningChanged += DbgManager_DelayedIsRunningChanged;

		void DbgManager_DelayedIsRunningChanged(object sender, EventArgs e) {
			var dbgManager = (DbgManager)sender;
			var process = dbgManager.Processes.FirstOrDefault();
			Debug.Assert(process != null);
			if (process == null)
				return;
			try {
				using (var p = Process.GetProcessById(process.Id)) {
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

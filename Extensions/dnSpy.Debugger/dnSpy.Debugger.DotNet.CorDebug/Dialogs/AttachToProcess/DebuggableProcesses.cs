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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using dnSpy.Debugger.DotNet.CorDebug.Native;

namespace dnSpy.Debugger.DotNet.CorDebug.Dialogs.AttachToProcess {
	static class DebuggableProcesses {
		static readonly int currentProcessId = Process.GetCurrentProcess().Id;
		public static IEnumerable<Process> GetProcesses(int[] processIds, Func<Process, bool>? isValidProcess, CancellationToken cancellationToken) {
			var processes = GetProcesses(processIds);
			try {
				foreach (var process in processes) {
					cancellationToken.ThrowIfCancellationRequested();
					int pid;
					try {
						pid = process.Id;
					}
					catch {
						continue;
					}
					if (pid == currentProcessId)
						continue;
					// Prevent slow exceptions by filtering out processes we can't access
					using (var fh = NativeMethods.OpenProcess(NativeMethods.PROCESS_ALL_ACCESS, false, (uint)process.Id)) {
						if (fh.IsInvalid)
							continue;
					}
					if (Environment.Is64BitOperatingSystem) {
						try {
							if (NativeMethods.IsWow64Process(process.Handle, out bool isWow64Process)) {
								if ((IntPtr.Size == 4) != isWow64Process)
									continue;
							}
						}
						catch (InvalidOperationException) {
							continue;
						}
					}
					if (process.HasExited)
						continue;
					if (isValidProcess is not null) {
						if (!isValidProcess(process) || process.HasExited)
							continue;
					}
					yield return process;
				}
			}
			finally {
				foreach (var p in processes)
					p.Dispose();
			}
		}

		static Process[] GetProcesses(int[] processIds) {
			if (processIds.Length == 0)
				return Process.GetProcesses();
			var processes = new Process[processIds.Length];
			try {
				int w = 0;
				for (int i = 0; i < processIds.Length; i++) {
					try {
						processes[w] = Process.GetProcessById(processIds[i]);
					}
					catch (InvalidOperationException) {
						continue;
					}
					catch (ArgumentException) {
						continue;
					}
					w++;
				}
				if (w != processes.Length)
					Array.Resize(ref processes, w);
				return processes;
			}
			catch {
				foreach (var p in processes)
					p?.Dispose();
				throw;
			}
		}
	}
}

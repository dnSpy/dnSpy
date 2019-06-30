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

namespace dnSpy.Debugger.Utilities {
	/// <summary>
	/// Calling <see cref="Process.GetProcessById(int)"/> is slow. This class caches the processes to speed
	/// up code that must get multiple processes.
	/// </summary>
	sealed class ProcessProvider : IDisposable {
		readonly Dictionary<int, Process> toProcess;
		bool processesInitd;

		public ProcessProvider() => toProcess = new Dictionary<int, Process>();

		public Process? GetProcess(int pid) {
			if (!processesInitd)
				ForceInitialize();

			if (toProcess.TryGetValue(pid, out var process))
				return process;

			// Maybe a new one has been created
			ForceInitialize();
			if (toProcess.TryGetValue(pid, out process))
				return process;

			return null;
		}

		void ForceInitialize() {
			ClearProcesses();
			foreach (var p in Process.GetProcesses())
				toProcess[p.Id] = p;
			processesInitd = true;
		}

		void ClearProcesses() {
			foreach (var p in toProcess.Values)
				p.Dispose();
			toProcess.Clear();
			processesInitd = false;
		}

		public void Dispose() => ClearProcesses();
	}
}

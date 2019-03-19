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

using System.Collections.Generic;
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
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

		public int ThreadId => CorThread.ThreadId;
		public int VolatileThreadId => CorThread.VolatileThreadId;
		public bool HasExited { get; private set; }

		public DnAppDomain AppDomainOrNull {
			get {
				var comAppDomain = CorThread.AppDomain;
				return comAppDomain == null ? null : Process.TryGetValidAppDomain(comAppDomain.RawObject);
			}
		}

		public DnDebugger Debugger => Process.Debugger;
		public DnProcess Process { get; }
		public IEnumerable<CorChain> Chains => CorThread.Chains;
		public IEnumerable<CorFrame> AllFrames => CorThread.AllFrames;
		public IEnumerable<CorFrame> GetAllFrames(ICorDebugFrame[] frames) => CorThread.GetAllFrames(frames);

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

		internal void NameChanged() { }

		public override string ToString() => $"{UniqueId} {ThreadId} {VolatileThreadId}";
	}
}

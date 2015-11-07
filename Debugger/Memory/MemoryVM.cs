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
using dndbg.Engine;
using dnSpy.Shared.UI.HexEditor;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Debugger.Memory {
	sealed class MemoryVM : ViewModelBase {
		internal bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled != value) {
					// Don't call OnPropertyChanged() since it's only used internally by the View
					isEnabled = value;
					InitializeMemory();
				}
			}
		}
		bool isEnabled;

		public bool CanNotEditMemory {
			get { return canNotEditMemory; }
			set {
				if (canNotEditMemory != value) {
					canNotEditMemory = value;
					OnPropertyChanged("CanNotEditMemory");
				}
			}
		}
		bool canNotEditMemory;

		public bool IsStopped {
			get { return isStopped; }
			set {
				if (isStopped != value) {
					isStopped = value;
					OnPropertyChanged("IsStopped");
				}
			}
		}
		bool isStopped;

		public HexDocument HexDocument {
			get { return hexDocument; }
			private set {
				if (hexDocument != value) {
					this.hexDocument = value;
					OnPropertyChanged("HexDocument");
				}
			}
		}
		HexDocument hexDocument;
		CachedHexStream cachedHexStream;

		readonly Action refreshLines;

		public MemoryVM(Action refreshLines) {
			this.refreshLines = refreshLines;
			this.canNotEditMemory = true;
			DebugManager.Instance.OnProcessStateChanged += DebugManager_OnProcessStateChanged;
			DebugManager.Instance.ProcessRunning += DebugManager_ProcessRunning;
			InitializeHexDocument();
		}

		void DebugManager_ProcessRunning(object sender, EventArgs e) {
			CanNotEditMemory = true;
		}

		void InitializeHexDocument() {
			cachedHexStream = null;
			if (DebugManager.Instance.ProcessState == DebuggerProcessState.Terminated)
				this.HexDocument = null;
			else {
				var process = DebugManager.Instance.Debugger.Processes.FirstOrDefault();
				Debug.Assert(process != null);
				if (process == null)
					this.HexDocument = null;
				else
					this.HexDocument = new HexDocument(cachedHexStream = new CachedHexStream(new ProcessHexStream(process.CorProcess.Handle)), string.Format("<MEMORY: pid {0}>", process.ProcessId));
			}
		}

		void DebugManager_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			var state = DebugManager.Instance.ProcessState;
			switch (state) {
			case DebuggerProcessState.Starting:
				InitializeHexDocument();
				break;

			case DebuggerProcessState.Continuing:
			case DebuggerProcessState.Running:
			case DebuggerProcessState.Stopped:
				break;

			case DebuggerProcessState.Terminated:
				InitializeHexDocument();
				break;
			}

			IsStopped = state == DebuggerProcessState.Stopped;
			if (state != DebuggerProcessState.Continuing && state != DebuggerProcessState.Running)
				CanNotEditMemory = state != DebuggerProcessState.Stopped;

			InitializeMemory();
		}

		void InitializeMemory() {
			if (cachedHexStream != null)
				cachedHexStream.ClearCache();
			if (IsEnabled)
				refreshLines();
		}
	}
}

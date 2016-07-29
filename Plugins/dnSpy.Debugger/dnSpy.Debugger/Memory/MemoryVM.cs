/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnSpy.Contracts.HexEditor;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.Memory {
	interface IMemoryVM {
		bool IsEnabled { get; set; }
		bool IsVisible { get; set; }
		HexDocument HexDocument { get; }
		void SetRefreshLines(Action refreshLines);
	}

	sealed class MemoryVM : ViewModelBase, IMemoryVM {
		public bool IsEnabled {
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

		public bool IsVisible {//TODO: Use it
			get { return isVisible; }
			set { isVisible = value; }
		}
		bool isVisible;

		public bool CanNotEditMemory {
			get { return canNotEditMemory; }
			set {
				if (canNotEditMemory != value) {
					canNotEditMemory = value;
					OnPropertyChanged(nameof(CanNotEditMemory));
				}
			}
		}
		bool canNotEditMemory;

		public bool IsStopped {
			get { return isStopped; }
			set {
				if (isStopped != value) {
					isStopped = value;
					OnPropertyChanged(nameof(IsStopped));
				}
			}
		}
		bool isStopped;

		public HexDocument HexDocument {
			get { return hexDocument; }
			private set {
				if (hexDocument != value) {
					this.hexDocument = value;
					OnPropertyChanged(nameof(HexDocument));
				}
			}
		}
		HexDocument hexDocument;
		CachedHexStream cachedHexStream;

		readonly ITheDebugger theDebugger;

		public MemoryVM(ITheDebugger theDebugger) {
			this.theDebugger = theDebugger;
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
			theDebugger.ProcessRunning += TheDebugger_ProcessRunning;
			CanNotEditMemory = theDebugger.ProcessState != DebuggerProcessState.Paused;
			IsStopped = theDebugger.ProcessState == DebuggerProcessState.Paused;
			InitializeHexDocument();
		}

		public void SetRefreshLines(Action refreshLines) {
			if (refreshLines == null)
				throw new ArgumentNullException(nameof(refreshLines));
			if (this.refreshLines != null)
				throw new InvalidOperationException();
			this.refreshLines = refreshLines;
		}
		Action refreshLines;

		void TheDebugger_ProcessRunning(object sender, EventArgs e) => CanNotEditMemory = true;

		void InitializeHexDocument() {
			cachedHexStream = null;
			if (theDebugger.ProcessState == DebuggerProcessState.Terminated)
				this.HexDocument = null;
			else {
				var process = theDebugger.Debugger.Processes.FirstOrDefault();
				Debug.Assert(process != null);
				if (process == null)
					this.HexDocument = null;
				else
					this.HexDocument = new HexDocument(cachedHexStream = new CachedHexStream(new ProcessHexStream(process.CorProcess.Handle)), string.Format("<MEMORY: pid {0}>", process.ProcessId));
			}
		}

		void TheDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			var state = theDebugger.ProcessState;
			switch (state) {
			case DebuggerProcessState.Starting:
				InitializeHexDocument();
				break;

			case DebuggerProcessState.Continuing:
			case DebuggerProcessState.Running:
			case DebuggerProcessState.Paused:
				break;

			case DebuggerProcessState.Terminated:
				InitializeHexDocument();
				break;
			}

			IsStopped = state == DebuggerProcessState.Paused;
			if (state != DebuggerProcessState.Continuing && state != DebuggerProcessState.Running)
				CanNotEditMemory = state != DebuggerProcessState.Paused;

			InitializeMemory();
		}

		void InitializeMemory() {
			if (cachedHexStream != null)
				cachedHexStream.ClearCache();
			if (IsEnabled) {
				Debug.Assert(refreshLines != null);
				refreshLines?.Invoke();
			}
		}
	}
}

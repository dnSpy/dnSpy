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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dndbg.Engine;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.Memory {
	interface IMemoryVM {
		HexBuffer Buffer { get; }
		event EventHandler UnderlyingStreamChanged;
	}

	[Export(typeof(IMemoryVM))]
	sealed class MemoryVM : ViewModelBase, IMemoryVM {
		public bool CanNotEditMemory {
			get { return canNotEditMemory; }
			private set {
				if (canNotEditMemory != value) {
					canNotEditMemory = value;
					OnPropertyChanged(nameof(CanNotEditMemory));
				}
			}
		}
		bool canNotEditMemory;

		public bool CanEditMemory {
			get { return canEditMemory; }
			private set {
				if (canEditMemory != value) {
					canEditMemory = value;
					OnPropertyChanged(nameof(CanEditMemory));
				}
			}
		}
		bool canEditMemory;

		public HexBuffer Buffer { get; }

		readonly ITheDebugger theDebugger;
		readonly HexBufferStreamFactoryService hexBufferStreamFactoryService;
		readonly DebuggerHexBufferStream debuggerStream;

		[ImportingConstructor]
		MemoryVM(ITheDebugger theDebugger, HexBufferFactoryService hexBufferFactoryService, HexBufferStreamFactoryService hexBufferStreamFactoryService) {
			this.theDebugger = theDebugger;
			this.hexBufferStreamFactoryService = hexBufferStreamFactoryService;
			debuggerStream = new DebuggerHexBufferStream();
			debuggerStream.UnderlyingStreamChanged += DebuggerStream_UnderlyingStreamChanged;
			Buffer = hexBufferFactoryService.Create(debuggerStream, hexBufferFactoryService.DefaultMemoryTags, disposeStream: true);
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
			InitializeHexStream();
		}

		public event EventHandler UnderlyingStreamChanged;
		void DebuggerStream_UnderlyingStreamChanged(object sender, EventArgs e) => UnderlyingStreamChanged?.Invoke(this, EventArgs.Empty);

		void InitializeHexStream() {
			var stream = CreateHexBufferStream();
			debuggerStream.SetUnderlyingStream(stream);
			CanNotEditMemory = stream == null;
			CanEditMemory = stream != null;
		}

		HexBufferStream CreateHexBufferStream() {
			if (theDebugger.ProcessState == DebuggerProcessState.Terminated)
				return null;

			var process = theDebugger.Debugger.Processes.FirstOrDefault();
			Debug.Assert(process != null);
			if (process == null)
				return null;

			var processStream = hexBufferStreamFactoryService.CreateSimpleProcessStream(process.CorProcess.Handle);
			var cachedStream = hexBufferStreamFactoryService.CreateCached(processStream, disposeStream: true);
			return cachedStream;
		}

		void TheDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			var state = theDebugger.ProcessState;
			switch (state) {
			case DebuggerProcessState.Starting:
				InitializeHexStream();
				break;

			case DebuggerProcessState.Continuing:
			case DebuggerProcessState.Running:
			case DebuggerProcessState.Paused:
				break;

			case DebuggerProcessState.Terminated:
				InitializeHexStream();
				break;
			}

			InitializeMemory();
		}

		void InitializeMemory() => debuggerStream.InvalidateAll();
	}
}

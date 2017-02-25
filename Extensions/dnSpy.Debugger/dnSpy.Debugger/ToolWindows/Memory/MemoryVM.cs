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
using System.ComponentModel.Composition;
using System.Windows.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.MVVM;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.Memory {
	interface IMemoryVM {
		bool CanEditMemory { get; }
	}

	[Export(typeof(IMemoryVM))]
	sealed class MemoryVM : ViewModelBase, IMemoryVM {
		public bool CanNotEditMemory => !CanEditMemory;

		public bool CanEditMemory {
			get => canEditMemory;
			private set {
				if (canEditMemory != value) {
					canEditMemory = value;
					OnPropertyChanged(nameof(CanEditMemory));
					OnPropertyChanged(nameof(CanNotEditMemory));
				}
			}
		}
		bool canEditMemory;

		readonly DbgManager dbgManager;
		readonly DebuggerDispatcher debuggerDispatcher;
		readonly ProcessHexBufferProvider processHexBufferProvider;

		[ImportingConstructor]
		MemoryVM(DbgManager dbgManager, DebuggerDispatcher debuggerDispatcher, ProcessHexBufferProvider processHexBufferProvider) {
			this.dbgManager = dbgManager;
			this.debuggerDispatcher = debuggerDispatcher;
			this.processHexBufferProvider = processHexBufferProvider;
			dbgManager.IsDebuggingChanged += DbgManager_IsDebuggingChanged;
			InitializeCanEditMemory_UI();
		}

		// UI thread
		void InitializeCanEditMemory_UI() {
			debuggerDispatcher.Dispatcher.VerifyAccess();
			CanEditMemory = dbgManager.IsDebugging;
		}

		// random thread
		void UI(Action action) =>
			debuggerDispatcher.Dispatcher.BeginInvoke(DispatcherPriority.Background, action);

		// DbgManager thread
		void DbgManager_IsDebuggingChanged(object sender, EventArgs e) => UI(() => {
			//TODO: Only invalidate the processes that get paused, not every process
			processHexBufferProvider.InvalidateMemory();
			InitializeCanEditMemory_UI();
		});
	}
}

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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using dndbg.Engine;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;

namespace dnSpy.Debugger.Memory {
	//[Export(typeof(ILoadBeforeDebug))]
	sealed class BufferFileCreator : ILoadBeforeDebug {
		readonly ITheDebugger theDebugger;
		readonly HexBufferFileServiceFactory hexBufferFileServiceFactory;
		readonly IDebuggerHexBufferStreamProvider debuggerHexBufferStreamProvider;
		readonly Dictionary<HexPosition, int> moduleReferences;

		[ImportingConstructor]
		BufferFileCreator(ITheDebugger theDebugger, HexBufferFileServiceFactory hexBufferFileServiceFactory, IDebuggerHexBufferStreamProvider debuggerHexBufferStreamProvider) {
			this.theDebugger = theDebugger;
			this.hexBufferFileServiceFactory = hexBufferFileServiceFactory;
			this.debuggerHexBufferStreamProvider = debuggerHexBufferStreamProvider;
			moduleReferences = new Dictionary<HexPosition, int>();
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
		}

		void TheDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			var dbg = (DnDebugger)sender;
			switch (theDebugger.ProcessState) {
			case DebuggerProcessState.Starting:
				dbg.OnModuleAdded += DnDebugger_OnModuleAdded;
				foreach (var mod in dbg.Modules)
					OnModuleAddedRemoved(mod, added: true);
				break;

			case DebuggerProcessState.Continuing:
			case DebuggerProcessState.Running:
			case DebuggerProcessState.Paused:
				break;

			case DebuggerProcessState.Terminated:
				dbg.OnModuleAdded -= DnDebugger_OnModuleAdded;
				ClearAllFiles();
				break;
			}
		}

		void DnDebugger_OnModuleAdded(object sender, ModuleDebuggerEventArgs e) {
			if (theDebugger.Debugger != sender)
				return;
			if (theDebugger.ProcessState == DebuggerProcessState.Terminated)
				return;
			OnModuleAddedRemoved(e.Module, e.Added);
		}

		void OnModuleAddedRemoved(DnModule module, bool added) {
			if (module.Address == 0 || module.Size == 0)
				return;
			var service = hexBufferFileServiceFactory.Create(debuggerHexBufferStreamProvider.Buffer);
			var pos = new HexPosition(module.Address);
			moduleReferences.TryGetValue(pos, out int refCount);
			if (added) {
				if (refCount == 0) {
					var tags = module.IsInMemory ?
						new string[] { PredefinedBufferFileTags.FileLayout } :
						new string[] { PredefinedBufferFileTags.MemoryLayout };
					service.CreateFile(new HexSpan(module.Address, module.Size), module.Name, module.Name, tags);
				}
				refCount++;
			}
			else {
				if (refCount == 1)
					service.RemoveFiles(new HexSpan(module.Address, module.Size));
				refCount--;
			}
			moduleReferences[pos] = refCount;
		}

		void ClearAllFiles() {
			var service = hexBufferFileServiceFactory.Create(debuggerHexBufferStreamProvider.Buffer);
			service.RemoveAllFiles();
			moduleReferences.Clear();
		}
	}
}

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
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Disassembly {
	abstract class DisassemblyOperations {
		public abstract bool IsDebugging { get; }
		public abstract bool CanShowDisassembly_CurrentFrame { get; }
		public abstract void ShowDisassembly_CurrentFrame();
	}

	[Export(typeof(DisassemblyOperations))]
	sealed class DisassemblyOperationsImpl : DisassemblyOperations {
		readonly Lazy<DbgManager> dbgManager;
		readonly Lazy<DbgCallStackService> dbgCallStackService;
		readonly Lazy<DbgShowNativeCodeService> dbgShowNativeCodeService;
		readonly Lazy<IMessageBoxService> messageBoxService;

		[ImportingConstructor]
		DisassemblyOperationsImpl(Lazy<DbgManager> dbgManager, Lazy<DbgCallStackService> dbgCallStackService, Lazy<DbgShowNativeCodeService> dbgShowNativeCodeService, Lazy<IMessageBoxService> messageBoxService) {
			this.dbgManager = dbgManager;
			this.dbgCallStackService = dbgCallStackService;
			this.dbgShowNativeCodeService = dbgShowNativeCodeService;
			this.messageBoxService = messageBoxService;
		}

		public override bool IsDebugging => dbgManager.Value.IsDebugging;

		public override bool CanShowDisassembly_CurrentFrame =>
			dbgManager.Value.CurrentThread.Current?.Process.State == DbgProcessState.Paused &&
			dbgCallStackService.Value.ActiveFrame is not null;

		public override void ShowDisassembly_CurrentFrame() {
			if (!CanShowDisassembly_CurrentFrame)
				return;

			var frame = dbgCallStackService.Value.ActiveFrame;
			if (frame is not null) {
				if (!dbgShowNativeCodeService.Value.ShowNativeCode(frame))
					messageBoxService.Value.Show(dnSpy_Debugger_Resources.Error_CouldNotShowDisassembly);
			}
		}
	}
}

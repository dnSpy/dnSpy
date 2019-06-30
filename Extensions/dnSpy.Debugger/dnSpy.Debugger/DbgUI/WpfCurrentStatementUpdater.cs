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
using System.Windows.Interop;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Documents;
using dnSpy.Debugger.Native;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.DbgUI {
	[Export(typeof(IDbgManagerStartListener))]
	sealed class WpfCurrentStatementUpdater : CurrentStatementUpdater {
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<IAppWindow> appWindow;

		[ImportingConstructor]
		WpfCurrentStatementUpdater(UIDispatcher uiDispatcher, Lazy<IAppWindow> appWindow, DbgCallStackService dbgCallStackService, Lazy<ReferenceNavigatorService> referenceNavigatorService, Lazy<DebuggerSettings> debuggerSettings)
			: base(dbgCallStackService, referenceNavigatorService, debuggerSettings) {
			this.uiDispatcher = uiDispatcher;
			this.appWindow = appWindow;
		}

		protected override void ActivateMainWindow() => uiDispatcher.UI(() => ActivateMainWindow_UI());

		void ActivateMainWindow_UI() {
			uiDispatcher.VerifyAccess();
			if (mainWindowHandle == IntPtr.Zero)
				mainWindowHandle = new WindowInteropHelper(appWindow.Value.MainWindow).Handle;

			// SetForegroundWindow() must be called first or we won't get focus...
			NativeMethods.SetForegroundWindow(mainWindowHandle);
			NativeMethods.SetWindowPos(mainWindowHandle, IntPtr.Zero, 0, 0, 0, 0, 3);
			appWindow.Value.MainWindow.Activate();
		}
		IntPtr mainWindowHandle;
	}
}

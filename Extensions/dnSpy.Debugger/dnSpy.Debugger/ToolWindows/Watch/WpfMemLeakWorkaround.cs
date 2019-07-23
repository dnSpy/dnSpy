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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.MVVM;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.Watch {
	[Export(typeof(IDbgManagerStartListener))]
	sealed class WpfMemLeakWorkaround : IDbgManagerStartListener {
		readonly UIDispatcher uiDispatcher;
		readonly WatchContentFactory watchContentFactory;

		[ImportingConstructor]
		WpfMemLeakWorkaround(UIDispatcher uiDispatcher, WatchContentFactory watchContentFactory) {
			this.uiDispatcher = uiDispatcher;
			this.watchContentFactory = watchContentFactory;
		}

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) => dbgManager.IsDebuggingChanged += DbgManager_IsDebuggingChanged;

		void DbgManager_IsDebuggingChanged(object? sender, EventArgs e) {
			var dbgManager = (DbgManager)sender!;
			if (!dbgManager.IsDebugging)
				uiDispatcher.UIBackground(() => MemLeakFix());
		}

		void MemLeakFix() {
			uiDispatcher.VerifyAccess();
			for (int i = 0; i < WatchWindowsHelper.NUMBER_OF_WATCH_WINDOWS; i++) {
				if (watchContentFactory.TryGetContent(i, out var watchContent)) {
					var listView = watchContent.VariablesWindowControl.ListView;
					if (!(listView is null))
						AutomationPeerMemoryLeakWorkaround.ClearAll(listView);
				}
			}
		}
	}
}

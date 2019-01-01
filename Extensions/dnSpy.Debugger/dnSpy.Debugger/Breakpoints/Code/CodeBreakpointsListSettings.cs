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
using System.Linq;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Settings;
using dnSpy.Debugger.Impl;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.Breakpoints.Code {
	[Export(typeof(IDbgCodeBreakpointsServiceListener))]
	sealed class CodeBreakpointsListSettingsListener : IDbgCodeBreakpointsServiceListener {
		readonly DbgDispatcherProvider dbgDispatcherProvider;
		readonly ISettingsService settingsService;
		readonly DbgCodeLocationSerializerService dbgCodeLocationSerializerService;
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<IAppWindow> appWindow;

		[ImportingConstructor]
		CodeBreakpointsListSettingsListener(DbgDispatcherProvider dbgDispatcherProvider, ISettingsService settingsService, DbgCodeLocationSerializerService dbgCodeLocationSerializerService, UIDispatcher uiDispatcher, Lazy<IAppWindow> appWindow) {
			this.dbgDispatcherProvider = dbgDispatcherProvider;
			this.settingsService = settingsService;
			this.dbgCodeLocationSerializerService = dbgCodeLocationSerializerService;
			this.uiDispatcher = uiDispatcher;
			this.appWindow = appWindow;
		}

		void IDbgCodeBreakpointsServiceListener.Initialize(DbgCodeBreakpointsService dbgCodeBreakpointsService) =>
			new CodeBreakpointsListSettings(dbgDispatcherProvider, settingsService, dbgCodeLocationSerializerService, dbgCodeBreakpointsService, uiDispatcher, appWindow);
	}

	sealed class CodeBreakpointsListSettings {
		readonly DbgDispatcherProvider dbgDispatcherProvider;
		readonly ISettingsService settingsService;
		readonly DbgCodeLocationSerializerService dbgCodeLocationSerializerService;
		readonly DbgCodeBreakpointsService dbgCodeBreakpointsService;
		readonly UIDispatcher uiDispatcher;
		volatile bool saveBreakpoints;

		public CodeBreakpointsListSettings(DbgDispatcherProvider dbgDispatcherProvider, ISettingsService settingsService, DbgCodeLocationSerializerService dbgCodeLocationSerializerService, DbgCodeBreakpointsService dbgCodeBreakpointsService, UIDispatcher uiDispatcher, Lazy<IAppWindow> appWindow) {
			this.dbgDispatcherProvider = dbgDispatcherProvider ?? throw new ArgumentNullException(nameof(dbgDispatcherProvider));
			this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
			this.dbgCodeLocationSerializerService = dbgCodeLocationSerializerService ?? throw new ArgumentNullException(nameof(dbgCodeLocationSerializerService));
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService ?? throw new ArgumentNullException(nameof(dbgCodeBreakpointsService));
			this.uiDispatcher = uiDispatcher;
			uiDispatcher.UIBackground(() => appWindow.Value.MainWindowClosed += AppWindow_MainWindowClosed);
			dbgCodeBreakpointsService.BreakpointsChanged += DbgCodeBreakpointsService_BreakpointsChanged;
			dbgCodeBreakpointsService.BreakpointsModified += DbgCodeBreakpointsService_BreakpointsModified;
			dbgDispatcherProvider.Dbg(() => Load());
		}

		void Load() {
			dbgDispatcherProvider.VerifyAccess();
			ignoreSave = true;
			dbgCodeBreakpointsService.Add(new BreakpointsSerializer(settingsService, dbgCodeLocationSerializerService).Load());
			dbgDispatcherProvider.Dbg(() => ignoreSave = false);
		}

		void DbgCodeBreakpointsService_BreakpointsChanged(object sender, DbgCollectionChangedEventArgs<DbgCodeBreakpoint> e) => BreakpointsModified();
		void DbgCodeBreakpointsService_BreakpointsModified(object sender, DbgBreakpointsModifiedEventArgs e) => BreakpointsModified();

		void BreakpointsModified() {
			dbgDispatcherProvider.VerifyAccess();
			if (ignoreSave)
				return;
			saveBreakpoints = true;
		}
		bool ignoreSave;

		void AppWindow_MainWindowClosed(object sender, EventArgs e) {
			if (!saveBreakpoints)
				return;
			// Don't save temporary and hidden BPs. They should only be created by code, not by the user.
			// The options aren't serialized so don't save any BP that has a non-zero Options prop.
			new BreakpointsSerializer(settingsService, dbgCodeLocationSerializerService).Save(dbgCodeBreakpointsService.Breakpoints.Where(a => a.Options == 0).ToArray());
		}
	}
}

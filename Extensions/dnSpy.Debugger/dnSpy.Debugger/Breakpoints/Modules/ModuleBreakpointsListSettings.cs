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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Modules;
using dnSpy.Contracts.Settings;
using dnSpy.Debugger.Impl;

namespace dnSpy.Debugger.Breakpoints.Modules {
	[Export(typeof(IDbgModuleBreakpointsServiceListener))]
	sealed class ModuleBreakpointsListSettingsListener : IDbgModuleBreakpointsServiceListener {
		readonly DbgDispatcher dbgDispatcher;
		readonly ISettingsService settingsService;

		[ImportingConstructor]
		ModuleBreakpointsListSettingsListener(DbgDispatcher dbgDispatcher, ISettingsService settingsService) {
			this.dbgDispatcher = dbgDispatcher;
			this.settingsService = settingsService;
		}

		void IDbgModuleBreakpointsServiceListener.Initialize(DbgModuleBreakpointsService dbgModuleBreakpointsService) =>
			new ModuleBreakpointsListSettings(dbgDispatcher, settingsService, dbgModuleBreakpointsService);
	}

	sealed class ModuleBreakpointsListSettings {
		readonly DbgDispatcher dbgDispatcher;
		readonly ISettingsService settingsService;
		readonly DbgModuleBreakpointsService dbgModuleBreakpointsService;

		public ModuleBreakpointsListSettings(DbgDispatcher dbgDispatcher, ISettingsService settingsService, DbgModuleBreakpointsService dbgModuleBreakpointsService) {
			this.dbgDispatcher = dbgDispatcher ?? throw new ArgumentNullException(nameof(dbgDispatcher));
			this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
			this.dbgModuleBreakpointsService = dbgModuleBreakpointsService ?? throw new ArgumentNullException(nameof(dbgModuleBreakpointsService));
			dbgModuleBreakpointsService.BreakpointsChanged += DbgModuleBreakpointsService_BreakpointsChanged;
			dbgModuleBreakpointsService.BreakpointsModified += DbgModuleBreakpointsService_BreakpointsModified;
			dbgDispatcher.Dbg(() => Load());
		}

		void Load() {
			dbgDispatcher.VerifyAccess();
			ignoreSave = true;
			dbgModuleBreakpointsService.Add(new BreakpointsSerializer(settingsService).Load());
			dbgDispatcher.Dbg(() => ignoreSave = false);
		}

		void DbgModuleBreakpointsService_BreakpointsChanged(object sender, DbgCollectionChangedEventArgs<DbgModuleBreakpoint> e) => Save();
		void DbgModuleBreakpointsService_BreakpointsModified(object sender, DbgBreakpointsModifiedEventArgs e) => Save();

		void Save() {
			dbgDispatcher.VerifyAccess();
			if (ignoreSave)
				return;
			new BreakpointsSerializer(settingsService).Save(dbgModuleBreakpointsService.Breakpoints);
		}
		bool ignoreSave;
	}
}

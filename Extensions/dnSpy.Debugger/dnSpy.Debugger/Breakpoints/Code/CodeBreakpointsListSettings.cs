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
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Settings;
using dnSpy.Debugger.Impl;

namespace dnSpy.Debugger.Breakpoints.Code {
	[Export(typeof(IDbgCodeBreakpointsServiceListener))]
	sealed class CodeBreakpointsListSettingsListener : IDbgCodeBreakpointsServiceListener {
		readonly DbgDispatcher dbgDispatcher;
		readonly ISettingsService settingsService;
		readonly DbgBreakpointLocationSerializerService dbgBreakpointLocationSerializerService;

		[ImportingConstructor]
		CodeBreakpointsListSettingsListener(DbgDispatcher dbgDispatcher, ISettingsService settingsService, DbgBreakpointLocationSerializerService dbgBreakpointLocationSerializerService) {
			this.dbgDispatcher = dbgDispatcher;
			this.settingsService = settingsService;
			this.dbgBreakpointLocationSerializerService = dbgBreakpointLocationSerializerService;
		}

		void IDbgCodeBreakpointsServiceListener.Initialize(DbgCodeBreakpointsService dbgCodeBreakpointsService) =>
			new CodeBreakpointsListSettings(dbgDispatcher, settingsService, dbgBreakpointLocationSerializerService, dbgCodeBreakpointsService);
	}

	sealed class CodeBreakpointsListSettings {
		readonly DbgDispatcher dbgDispatcher;
		readonly ISettingsService settingsService;
		readonly DbgBreakpointLocationSerializerService dbgBreakpointLocationSerializerService;
		readonly DbgCodeBreakpointsService dbgCodeBreakpointsService;

		public CodeBreakpointsListSettings(DbgDispatcher dbgDispatcher, ISettingsService settingsService, DbgBreakpointLocationSerializerService dbgBreakpointLocationSerializerService, DbgCodeBreakpointsService dbgCodeBreakpointsService) {
			this.dbgDispatcher = dbgDispatcher ?? throw new ArgumentNullException(nameof(dbgDispatcher));
			this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
			this.dbgBreakpointLocationSerializerService = dbgBreakpointLocationSerializerService ?? throw new ArgumentNullException(nameof(dbgBreakpointLocationSerializerService));
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService ?? throw new ArgumentNullException(nameof(dbgCodeBreakpointsService));
			dbgCodeBreakpointsService.BreakpointsChanged += DbgCodeBreakpointsService_BreakpointsChanged;
			dbgCodeBreakpointsService.BreakpointsModified += DbgCodeBreakpointsService_BreakpointsModified;
			dbgDispatcher.Dbg(() => Load());
		}

		void Load() {
			dbgDispatcher.VerifyAccess();
			ignoreSave = true;
			dbgCodeBreakpointsService.Add(new BreakpointsSerializer(settingsService, dbgBreakpointLocationSerializerService).Load());
			dbgDispatcher.Dbg(() => ignoreSave = false);
		}

		void DbgCodeBreakpointsService_BreakpointsChanged(object sender, DbgCollectionChangedEventArgs<DbgCodeBreakpoint> e) => Save();
		void DbgCodeBreakpointsService_BreakpointsModified(object sender, DbgBreakpointsModifiedEventArgs e) => Save();

		void Save() {
			dbgDispatcher.VerifyAccess();
			if (ignoreSave)
				return;
			new BreakpointsSerializer(settingsService, dbgBreakpointLocationSerializerService).Save(dbgCodeBreakpointsService.Breakpoints);
		}
		bool ignoreSave;
	}
}

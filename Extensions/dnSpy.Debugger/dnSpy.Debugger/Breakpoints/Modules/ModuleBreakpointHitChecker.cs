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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Modules;

namespace dnSpy.Debugger.Breakpoints.Modules {
	[Export(typeof(IDbgManagerStartListener))]
	sealed class ModuleBreakpointHitChecker : IDbgManagerStartListener {
		readonly DbgModuleBreakpointsService dbgModuleBreakpointsService;

		[ImportingConstructor]
		ModuleBreakpointHitChecker(DbgModuleBreakpointsService dbgModuleBreakpointsService) =>
			this.dbgModuleBreakpointsService = dbgModuleBreakpointsService;

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) {
			dbgManager.MessageModuleLoaded += DbgManager_MessageModuleLoaded;
			dbgManager.MessageModuleUnloaded += DbgManager_MessageModuleUnloaded;
		}

		void DbgManager_MessageModuleLoaded(object? sender, DbgMessageModuleLoadedEventArgs e) {
			if (dbgModuleBreakpointsService.IsMatch(new DbgModuleBreakpointInfo(e.Module, isLoaded: true)))
				e.Pause = true;
		}

		void DbgManager_MessageModuleUnloaded(object? sender, DbgMessageModuleUnloadedEventArgs e) {
			if (dbgModuleBreakpointsService.IsMatch(new DbgModuleBreakpointInfo(e.Module, isLoaded: false)))
				e.Pause = true;
		}
	}
}

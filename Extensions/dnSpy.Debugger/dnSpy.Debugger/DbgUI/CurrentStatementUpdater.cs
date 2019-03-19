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
using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Documents;

namespace dnSpy.Debugger.DbgUI {
	abstract class CurrentStatementUpdater : IDbgManagerStartListener {
		readonly DbgCallStackService dbgCallStackService;
		readonly Lazy<ReferenceNavigatorService> referenceNavigatorService;

		protected CurrentStatementUpdater(DbgCallStackService dbgCallStackService, Lazy<ReferenceNavigatorService> referenceNavigatorService) {
			this.dbgCallStackService = dbgCallStackService;
			this.referenceNavigatorService = referenceNavigatorService;
		}

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) => dbgManager.ProcessPaused += DbgManager_ProcessPaused;

		protected abstract void ActivateMainWindow();

		void DbgManager_ProcessPaused(object sender, ProcessPausedEventArgs e) {
			Debug.Assert(dbgCallStackService.Thread == e.Thread);
			var info = GetLocation();
			if (info.location != null) {
				dbgCallStackService.ActiveFrameIndex = info.frameIndex;
				referenceNavigatorService.Value.GoTo(info.location);
			}
			ActivateMainWindow();
		}

		(DbgCodeLocation location, int frameIndex) GetLocation() {
			var frames = dbgCallStackService.Frames.Frames;
			for (int i = 0; i < frames.Count; i++) {
				var location = frames[i].Location;
				if (location != null)
					return (location, i);
			}
			return (null, -1);
		}
	}
}

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
using System.ComponentModel;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Documents;

namespace dnSpy.Debugger.Code {
	[Export(typeof(IDbgManagerStartListener))]
	sealed class GoToInMemoryModuleStatement : IDbgManagerStartListener {
		readonly Lazy<DbgCallStackService> dbgCallStackService;
		readonly Lazy<ReferenceNavigatorService> referenceNavigatorService;

		[ImportingConstructor]
		GoToInMemoryModuleStatement(Lazy<DbgCallStackService> dbgCallStackService, Lazy<ReferenceNavigatorService> referenceNavigatorService, DebuggerSettings debuggerSettings) {
			this.dbgCallStackService = dbgCallStackService;
			this.referenceNavigatorService = referenceNavigatorService;
			debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
		}

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) { }

		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(DebuggerSettings.UseMemoryModules)) {
				var debuggerSettings = (DebuggerSettings)sender;
				if (debuggerSettings.UseMemoryModules)
					UpdateLocation();
			}
		}

		void UpdateLocation() {
			var location = dbgCallStackService.Value.ActiveFrame?.Location;
			if (location == null)
				return;
			referenceNavigatorService.Value.GoTo(location);
		}
	}
}

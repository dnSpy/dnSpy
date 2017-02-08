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

namespace dnSpy.Debugger.DbgUI {
	[Export(typeof(Debugger))]
	sealed class DebuggerImpl : Debugger {
		readonly Lazy<DbgManager> dbgManager;
		readonly Lazy<StartDebuggingOptionsProvider> startDebuggingOptionsProvider;

		public override bool IsDebugging => false;//TODO:

		[ImportingConstructor]
		DebuggerImpl(Lazy<DbgManager> dbgManager, Lazy<StartDebuggingOptionsProvider> startDebuggingOptionsProvider) {
			this.dbgManager = dbgManager;
			this.startDebuggingOptionsProvider = startDebuggingOptionsProvider;
		}

		public override void DebugProgram() {
			var options = startDebuggingOptionsProvider.Value.GetStartDebuggingOptions();
			if (options == null)
				return;

			dbgManager.Value.Start(options);
		}

		public override void Continue() {
			//TODO:
		}

		public override void BreakAll() {
			//TODO:
		}

		public override void Stop() {
			//TODO:
		}

		public override void Restart() {
			//TODO:
		}

		public override void ShowNextStatement() {
			//TODO:
		}

		public override void StepInto() {
			//TODO:
		}

		public override void StepOver() {
			//TODO:
		}

		public override void StepOut() {
			//TODO:
		}
	}
}

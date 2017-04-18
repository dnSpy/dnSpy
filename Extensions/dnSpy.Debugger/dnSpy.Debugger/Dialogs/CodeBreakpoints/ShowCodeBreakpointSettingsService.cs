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
using System.Linq;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger.Breakpoints.Code;

namespace dnSpy.Debugger.Dialogs.CodeBreakpoints {
	abstract class ShowCodeBreakpointSettingsService {
		public abstract DbgCodeBreakpointSettings? Show(DbgCodeBreakpointSettings settings);
		public void Edit(DbgCodeBreakpoint breakpoint) => Edit(new[] { breakpoint ?? throw new ArgumentNullException(nameof(breakpoint)) });
		public abstract void Edit(DbgCodeBreakpoint[] breakpoints);
	}

	[Export(typeof(ShowCodeBreakpointSettingsService))]
	sealed class ShowCodeBreakpointSettingsServiceImpl : ShowCodeBreakpointSettingsService {
		readonly IAppWindow appWindow;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		readonly IMessageBoxService messageBoxService;

		[ImportingConstructor]
		ShowCodeBreakpointSettingsServiceImpl(IAppWindow appWindow, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService, IMessageBoxService messageBoxService) {
			this.appWindow = appWindow;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			this.messageBoxService = messageBoxService;
		}

		public override DbgCodeBreakpointSettings? Show(DbgCodeBreakpointSettings settings) {
			var dlg = new ShowCodeBreakpointSettingsDlg();
			var vm = new ShowCodeBreakpointSettingsVM(settings, s => messageBoxService.Show(s, ownerWindow: dlg));
			dlg.DataContext = vm;
			dlg.Owner = appWindow.MainWindow;
			var res = dlg.ShowDialog();
			if (res != true)
				return null;
			return vm.GetSettings();
		}

		public override void Edit(DbgCodeBreakpoint[] breakpoints) {
			if (breakpoints == null)
				throw new ArgumentNullException(nameof(breakpoints));
			if (breakpoints.Length == 0)
				return;
			var newSettings = Show(breakpoints[0].Settings);
			if (newSettings != null)
				dbgCodeBreakpointsService.Value.Modify(breakpoints.Select(a => new DbgCodeBreakpointAndSettings(a, newSettings.Value)).ToArray());
		}
	}
}

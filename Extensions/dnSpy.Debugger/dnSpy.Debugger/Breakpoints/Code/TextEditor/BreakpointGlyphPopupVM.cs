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
using System.Windows.Input;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Breakpoints.Code.Dialogs;
using dnSpy.Contracts.MVVM;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Breakpoints.Code.TextEditor {
	sealed class BreakpointGlyphPopupVM : ViewModelBase {
		public ICommand SettingsCommand => new RelayCommand(a => ShowSettings(), a => CanShowSettings);
		public ICommand ToggleBreakpointCommand => new RelayCommand(a => ToggleBreakpoint(), a => CanToggleBreakpoint);

		public string SettingsToolTip => dnSpy_Debugger_Resources.Breakpoints_GlyphMargin_ShowSettingsToolTip;
		public string ToggleBreakpointToolTip => breakpoint.IsEnabled ? dnSpy_Debugger_Resources.Breakpoints_GlyphMargin_DisableBreakpoint : dnSpy_Debugger_Resources.Breakpoints_GlyphMargin_EnableBreakpoint;

		public event EventHandler BeforeExecuteCommand;

		readonly ShowCodeBreakpointSettingsService showCodeBreakpointSettingsService;
		readonly DbgCodeBreakpoint breakpoint;

		public BreakpointGlyphPopupVM(ShowCodeBreakpointSettingsService showCodeBreakpointSettingsService, DbgCodeBreakpoint breakpoint) {
			this.showCodeBreakpointSettingsService = showCodeBreakpointSettingsService ?? throw new ArgumentNullException(nameof(showCodeBreakpointSettingsService));
			this.breakpoint = breakpoint ?? throw new ArgumentNullException(nameof(breakpoint));
		}

		bool CanShowSettings => true;
		void ShowSettings() {
			BeforeExecuteCommand?.Invoke(this, EventArgs.Empty);
			showCodeBreakpointSettingsService.Edit(breakpoint);
		}

		bool CanToggleBreakpoint => true;
		void ToggleBreakpoint() {
			BeforeExecuteCommand?.Invoke(this, EventArgs.Empty);
			breakpoint.IsEnabled = !breakpoint.IsEnabled;
		}
	}
}

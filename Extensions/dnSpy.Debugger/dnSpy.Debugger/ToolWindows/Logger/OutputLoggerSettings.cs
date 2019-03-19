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
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;

namespace dnSpy.Debugger.ToolWindows.Logger {
	class OutputLoggerSettings : ViewModelBase {
		protected virtual void OnModified() { }

		public bool ShowExceptionMessages {
			get => showExceptionMessages;
			set {
				if (showExceptionMessages != value) {
					showExceptionMessages = value;
					OnPropertyChanged(nameof(ShowExceptionMessages));
					OnModified();
				}
			}
		}
		bool showExceptionMessages = true;

		public bool ShowStepFilteringMessages {
			get => showStepFilteringMessages;
			set {
				if (showStepFilteringMessages != value) {
					showStepFilteringMessages = value;
					OnPropertyChanged(nameof(ShowStepFilteringMessages));
					OnModified();
				}
			}
		}
		bool showStepFilteringMessages = true;

		public bool ShowModuleLoadMessages {
			get => showModuleLoadMessages;
			set {
				if (showModuleLoadMessages != value) {
					showModuleLoadMessages = value;
					OnPropertyChanged(nameof(ShowModuleLoadMessages));
					OnModified();
				}
			}
		}
		bool showModuleLoadMessages = true;

		public bool ShowModuleUnloadMessages {
			get => showModuleUnloadMessages;
			set {
				if (showModuleUnloadMessages != value) {
					showModuleUnloadMessages = value;
					OnPropertyChanged(nameof(ShowModuleUnloadMessages));
					OnModified();
				}
			}
		}
		bool showModuleUnloadMessages = true;

		public bool ShowProcessExitMessages {
			get => showProcessExitMessages;
			set {
				if (showProcessExitMessages != value) {
					showProcessExitMessages = value;
					OnPropertyChanged(nameof(ShowProcessExitMessages));
					OnModified();
				}
			}
		}
		bool showProcessExitMessages = true;

		public bool ShowThreadExitMessages {
			get => showThreadExitMessages;
			set {
				if (showThreadExitMessages != value) {
					showThreadExitMessages = value;
					OnPropertyChanged(nameof(ShowThreadExitMessages));
					OnModified();
				}
			}
		}
		bool showThreadExitMessages = true;

		public bool ShowProgramOutputMessages {
			get => showProgramOutputMessages;
			set {
				if (showProgramOutputMessages != value) {
					showProgramOutputMessages = value;
					OnPropertyChanged(nameof(ShowProgramOutputMessages));
					OnModified();
				}
			}
		}
		bool showProgramOutputMessages = true;

		public bool ShowMDAMessages {
			get => showMDAMessages;
			set {
				if (showMDAMessages != value) {
					showMDAMessages = value;
					OnPropertyChanged(nameof(ShowMDAMessages));
					OnModified();
				}
			}
		}
		bool showMDAMessages = true;

		public bool ShowDebugOutputLog {
			get => showDebugOutputLog;
			set {
				if (showDebugOutputLog != value) {
					showDebugOutputLog = value;
					OnPropertyChanged(nameof(ShowDebugOutputLog));
					OnModified();
				}
			}
		}
		bool showDebugOutputLog = true;
	}

	[Export(typeof(OutputLoggerSettings))]
	sealed class OutputLoggerSettingsImpl : OutputLoggerSettings {
		static readonly Guid SETTINGS_GUID = new Guid("87C84585-355B-4BF2-B5EE-C61BC1975552");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		OutputLoggerSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			disableSave = true;
			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			ShowExceptionMessages = sect.Attribute<bool?>(nameof(ShowExceptionMessages)) ?? ShowExceptionMessages;
			ShowStepFilteringMessages = sect.Attribute<bool?>(nameof(ShowStepFilteringMessages)) ?? ShowStepFilteringMessages;
			ShowModuleLoadMessages = sect.Attribute<bool?>(nameof(ShowModuleLoadMessages)) ?? ShowModuleLoadMessages;
			ShowModuleUnloadMessages = sect.Attribute<bool?>(nameof(ShowModuleUnloadMessages)) ?? ShowModuleUnloadMessages;
			ShowProcessExitMessages = sect.Attribute<bool?>(nameof(ShowProcessExitMessages)) ?? ShowProcessExitMessages;
			ShowThreadExitMessages = sect.Attribute<bool?>(nameof(ShowThreadExitMessages)) ?? ShowThreadExitMessages;
			ShowProgramOutputMessages = sect.Attribute<bool?>(nameof(ShowProgramOutputMessages)) ?? ShowProgramOutputMessages;
			ShowMDAMessages = sect.Attribute<bool?>(nameof(ShowMDAMessages)) ?? ShowMDAMessages;
			ShowDebugOutputLog = sect.Attribute<bool?>(nameof(ShowDebugOutputLog)) ?? ShowDebugOutputLog;
			disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(ShowExceptionMessages), ShowExceptionMessages);
			sect.Attribute(nameof(ShowStepFilteringMessages), ShowStepFilteringMessages);
			sect.Attribute(nameof(ShowModuleLoadMessages), ShowModuleLoadMessages);
			sect.Attribute(nameof(ShowModuleUnloadMessages), ShowModuleUnloadMessages);
			sect.Attribute(nameof(ShowProcessExitMessages), ShowProcessExitMessages);
			sect.Attribute(nameof(ShowThreadExitMessages), ShowThreadExitMessages);
			sect.Attribute(nameof(ShowProgramOutputMessages), ShowProgramOutputMessages);
			sect.Attribute(nameof(ShowMDAMessages), ShowMDAMessages);
			sect.Attribute(nameof(ShowDebugOutputLog), ShowDebugOutputLog);
		}
	}
}

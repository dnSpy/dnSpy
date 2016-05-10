/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnSpy.Contracts.Settings;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.Logger {
	interface IOutputLoggerSettings : INotifyPropertyChanged {
		bool ShowExceptionMessages { get; }
		bool ShowStepFilteringMessages { get; }
		bool ShowModuleLoadMessages { get; }
		bool ShowModuleUnloadMessages { get; }
		bool ShowProcessExitMessages { get; }
		bool ShowThreadExitMessages { get; }
		bool ShowProgramOutputMessages { get; }
		bool ShowMDAMessages { get; }
		bool ShowDebugOutputLog { get; }
	}

	class OutputLoggerSettings : ViewModelBase, IOutputLoggerSettings {
		protected virtual void OnModified() { }

		public bool ShowExceptionMessages {
			get { return showExceptionMessages; }
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
			get { return showStepFilteringMessages; }
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
			get { return showModuleLoadMessages; }
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
			get { return showModuleUnloadMessages; }
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
			get { return showProcessExitMessages; }
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
			get { return showThreadExitMessages; }
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
			get { return showProgramOutputMessages; }
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
			get { return showMDAMessages; }
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
			get { return showDebugOutputLog; }
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

	[Export, Export(typeof(IOutputLoggerSettings))]
	sealed class OutputLoggerSettingsImpl : OutputLoggerSettings {
		static readonly Guid SETTINGS_GUID = new Guid("87C84585-355B-4BF2-B5EE-C61BC1975552");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		OutputLoggerSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			this.ShowExceptionMessages = sect.Attribute<bool?>(nameof(ShowExceptionMessages)) ?? this.ShowExceptionMessages;
			this.ShowStepFilteringMessages = sect.Attribute<bool?>(nameof(ShowStepFilteringMessages)) ?? this.ShowStepFilteringMessages;
			this.ShowModuleLoadMessages = sect.Attribute<bool?>(nameof(ShowModuleLoadMessages)) ?? this.ShowModuleLoadMessages;
			this.ShowModuleUnloadMessages = sect.Attribute<bool?>(nameof(ShowModuleUnloadMessages)) ?? this.ShowModuleUnloadMessages;
			this.ShowProcessExitMessages = sect.Attribute<bool?>(nameof(ShowProcessExitMessages)) ?? this.ShowProcessExitMessages;
			this.ShowThreadExitMessages = sect.Attribute<bool?>(nameof(ShowThreadExitMessages)) ?? this.ShowThreadExitMessages;
			this.ShowProgramOutputMessages = sect.Attribute<bool?>(nameof(ShowProgramOutputMessages)) ?? this.ShowProgramOutputMessages;
			this.ShowMDAMessages = sect.Attribute<bool?>(nameof(ShowMDAMessages)) ?? this.ShowMDAMessages;
			this.ShowDebugOutputLog = sect.Attribute<bool?>(nameof(ShowDebugOutputLog)) ?? this.ShowDebugOutputLog;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
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

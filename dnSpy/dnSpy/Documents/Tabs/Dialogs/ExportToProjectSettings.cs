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
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;
using dnSpy.Decompiler.MSBuild;

namespace dnSpy.Documents.Tabs.Dialogs {
	interface IExportToProjectSettings : INotifyPropertyChanged {
		ProjectVersion ProjectVersion { get; set; }
	}

	class ExportToProjectSettings : ViewModelBase, IExportToProjectSettings {
		public ProjectVersion ProjectVersion {
			get => projectVersion;
			set {
				if (projectVersion != value) {
					projectVersion = value;
					OnPropertyChanged(nameof(ProjectVersion));
				}
			}
		}
		ProjectVersion projectVersion = ProjectVersion.VS2010;
	}

	[Export(typeof(IExportToProjectSettings))]
	sealed class ExportToProjectSettingsImpl : ExportToProjectSettings {
		static readonly Guid SETTINGS_GUID = new Guid("EF5C4F77-AC84-413B-93AB-4773F0013514");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		ExportToProjectSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			ProjectVersion = sect.Attribute<ProjectVersion?>(nameof(ProjectVersion)) ?? ProjectVersion;
			PropertyChanged += ExportToProjectSettingsImpl_PropertyChanged;
		}

		void ExportToProjectSettingsImpl_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(ProjectVersion), ProjectVersion);
		}
	}
}

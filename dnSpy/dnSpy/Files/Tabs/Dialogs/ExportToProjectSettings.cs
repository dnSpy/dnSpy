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
using dnSpy.Languages.MSBuild;
using dnSpy.Shared.MVVM;

namespace dnSpy.Files.Tabs.Dialogs {
	interface IExportToProjectSettings : INotifyPropertyChanged {
		ProjectVersion ProjectVersion { get; set; }
	}

	class ExportToProjectSettings : ViewModelBase, IExportToProjectSettings {
		protected virtual void OnModified() { }

		public ProjectVersion ProjectVersion {
			get { return projectVersion; }
			set {
				if (projectVersion != value) {
					projectVersion = value;
					OnPropertyChanged("ProjectVersion");
					OnModified();
				}
			}
		}
		ProjectVersion projectVersion = ProjectVersion.VS2010;
	}

	[Export, Export(typeof(IExportToProjectSettings)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class ExportToProjectSettingsImpl : ExportToProjectSettings {
		static readonly Guid SETTINGS_GUID = new Guid("EF5C4F77-AC84-413B-93AB-4773F0013514");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		ExportToProjectSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			this.ProjectVersion = sect.Attribute<ProjectVersion?>("ProjectVersion") ?? this.ProjectVersion;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute("ProjectVersion", ProjectVersion);
		}
	}
}

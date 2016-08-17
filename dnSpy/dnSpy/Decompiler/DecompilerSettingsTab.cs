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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Properties;

namespace dnSpy.Decompiler {
	sealed class DecompilerAppSettingsTabSettings {
		public string LastSelectedSettingsName { get; set; }
	}

	[Export(typeof(IAppSettingsTabProvider))]
	sealed class DecompilerAppSettingsTabProvider : IAppSettingsTabProvider {
		readonly Lazy<IDecompilerSettingsTabProvider>[] decompilerSettingsTabProviders;
		readonly DecompilerAppSettingsTabSettings decompilerAppSettingsTabSettings;

		[ImportingConstructor]
		DecompilerAppSettingsTabProvider([ImportMany] IEnumerable<Lazy<IDecompilerSettingsTabProvider>> decompilerSettingsTabProviders) {
			this.decompilerSettingsTabProviders = decompilerSettingsTabProviders.ToArray();
			this.decompilerAppSettingsTabSettings = new DecompilerAppSettingsTabSettings();
		}

		public IEnumerable<IAppSettingsTab> Create() {
			var settings = decompilerSettingsTabProviders.SelectMany(a => a.Value.Create()).OrderBy(a => a.Order).ToArray();
			if (settings.Length > 0)
				yield return new DecompilerAppSettingsTab(settings, decompilerAppSettingsTabSettings);
		}
	}

	sealed class DecompilerAppSettingsTab : ViewModelBase, IAppSettingsTab {
		public double Order => AppSettingsConstants.ORDER_SETTINGS_TAB_DECOMPILER;
		public string Title => dnSpy_Resources.DecompilerDlgTabTitle;
		public object UIObject => this;
		public bool HasMoreThanOneSetting => settings.Length > 1;
		public IDecompilerSettingsTab[] LanguageSettings => settings;
		public object CurrentUIObject => selectedLanguageSetting?.UIObject;

		public IDecompilerSettingsTab SelectedLanguageSetting {
			get { return selectedLanguageSetting; }
			set {
				if (selectedLanguageSetting != value) {
					selectedLanguageSetting = value;
					OnPropertyChanged(nameof(SelectedLanguageSetting));
					OnPropertyChanged(nameof(CurrentUIObject));
				}
			}
		}
		IDecompilerSettingsTab selectedLanguageSetting;

		readonly IDecompilerSettingsTab[] settings;
		readonly DecompilerAppSettingsTabSettings decompilerAppSettingsTabSettings;

		public DecompilerAppSettingsTab(IDecompilerSettingsTab[] settings, DecompilerAppSettingsTabSettings decompilerAppSettingsTabSettings) {
			this.settings = settings;
			this.decompilerAppSettingsTabSettings = decompilerAppSettingsTabSettings;
			this.selectedLanguageSetting = settings.FirstOrDefault(a => StringComparer.Ordinal.Equals(a.Name, decompilerAppSettingsTabSettings.LastSelectedSettingsName));
			if (this.selectedLanguageSetting == null)
				this.selectedLanguageSetting = settings.Length == 0 ? null : settings[0];
		}

		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
			decompilerAppSettingsTabSettings.LastSelectedSettingsName = selectedLanguageSetting == null ? null : selectedLanguageSetting.Name;
			foreach (var setting in settings)
				setting.OnClosed(saveSettings, appRefreshSettings);
		}
	}
}

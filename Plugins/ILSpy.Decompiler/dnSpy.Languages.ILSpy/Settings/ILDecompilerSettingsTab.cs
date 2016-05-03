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

using System.ComponentModel;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Languages.ILSpy.Properties;

namespace dnSpy.Languages.ILSpy.Settings {
	sealed class ILDecompilerSettingsTab : IDecompilerSettingsTab, INotifyPropertyChanged {
		readonly ILSettings _global_ilSettings;
		readonly ILSettings ilSettings;

		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		public double Order => LanguageConstants.ORDER_DECOMPILER_SETTINGS_ILSPY_IL;
		public string Name => dnSpy_Languages_ILSpy_Resources.ILDecompilerSettingsTabName;
		public ILSettings Settings => ilSettings;
		public object UIObject => this;

		public ILDecompilerSettingsTab(ILSettings ilSettings) {
			this._global_ilSettings = ilSettings;
			this.ilSettings = ilSettings.Clone();
		}

		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
			if (!saveSettings)
				return;

			if (!_global_ilSettings.Equals(ilSettings))
				appRefreshSettings.Add(SettingsConstants.REDISASSEMBLE_IL_ILSPY_CODE);

			ilSettings.CopyTo(_global_ilSettings);
		}
	}
}

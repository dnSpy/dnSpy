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
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Decompiler.ILSpy.Core.Settings;
using dnSpy.Decompiler.ILSpy.Properties;

namespace dnSpy.Decompiler.ILSpy.Settings {
	sealed class ILDecompilerSettingsPage : IAppSettingsPage, INotifyPropertyChanged {
		readonly ILSettings _global_ilSettings;
		readonly ILSettings ilSettings;

		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		public double Order => AppSettingsConstants.ORDER_DECOMPILER_SETTINGS_ILSPY_IL;
		public string Name => dnSpy_Decompiler_ILSpy_Resources.ILDecompilerSettingsTabName;
		public ILSettings Settings => ilSettings;
		public Guid ParentGuid => new Guid(AppSettingsConstants.GUID_DECOMPILER);
		public Guid Guid => new Guid("0F8FBD3F-01DA-4AF0-9316-B7B5C8901A74");
		public string Title => "IL (ILSpy)";
		public ImageReference Icon => ImageReference.None;
		public object UIObject => this;

		public ILDecompilerSettingsPage(ILSettings ilSettings) {
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

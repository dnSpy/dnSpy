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
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Decompiler.ILSpy.Core.Settings;

namespace dnSpy.Decompiler.ILSpy.Settings {
	sealed class ILDecompilerSettingsPage : AppSettingsPage, IAppSettingsPage2 {
		readonly ILSettings _global_ilSettings;
		readonly ILSettings ilSettings;

		public override double Order => AppSettingsConstants.ORDER_DECOMPILER_SETTINGS_ILSPY_IL;
		public ILSettings Settings => ilSettings;
		public override Guid ParentGuid => new Guid(AppSettingsConstants.GUID_DECOMPILER);
		public override Guid Guid => new Guid("0F8FBD3F-01DA-4AF0-9316-B7B5C8901A74");
		public override string Title => "IL (ILSpy)";
		public override object? UIObject => this;

		public ILDecompilerSettingsPage(ILSettings ilSettings) {
			_global_ilSettings = ilSettings;
			this.ilSettings = ilSettings.Clone();
		}

		public override void OnApply() => throw new InvalidOperationException();
		public void OnApply(IAppRefreshSettings appRefreshSettings) {
			if (!_global_ilSettings.Equals(ilSettings))
				appRefreshSettings.Add(SettingsConstants.REDISASSEMBLE_IL_ILSPY_CODE);

			ilSettings.CopyTo(_global_ilSettings);
		}
	}
}

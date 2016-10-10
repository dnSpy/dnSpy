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
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Properties;
using dnSpy.Text.Editor;

namespace dnSpy.Output.Settings {
	sealed class TabsAppSettingsPage : ViewModelBase, IAppSettingsPage {
		public Guid ParentGuid => new Guid(AppSettingsConstants.GUID_OUTPUT);
		public Guid Guid => new Guid("2F191061-E9E0-48DB-8673-E9500E6A811A");
		public double Order => AppSettingsConstants.ORDER_OUTPUT_DEFAULT_TABS;
		public string Title => dnSpy_Resources.TabsSettings;
		public ImageReference Icon => ImageReference.None;
		public object UIObject => this;

		public Int32VM TabSizeVM { get; }
		public Int32VM IndentSizeVM { get; }

		public bool ConvertTabsToSpaces {
			get { return convertTabsToSpaces; }
			set {
				if (convertTabsToSpaces != value) {
					convertTabsToSpaces = value;
					OnPropertyChanged(nameof(ConvertTabsToSpaces));
				}
			}
		}
		bool convertTabsToSpaces;

		readonly IOutputWindowOptions options;

		public TabsAppSettingsPage(IOutputWindowOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			this.options = options;
			TabSizeVM = new Int32VM(options.TabSize, a => { }, true) { Min = OptionsHelpers.MinimumTabSize, Max = OptionsHelpers.MaximumTabSize };
			IndentSizeVM = new Int32VM(options.IndentSize, a => { }, true) { Min = OptionsHelpers.MinimumIndentSize, Max = OptionsHelpers.MaximumIndentSize };
			ConvertTabsToSpaces = options.ConvertTabsToSpaces;
		}

		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
			if (!saveSettings)
				return;

			if (!TabSizeVM.HasError)
				options.TabSize = TabSizeVM.Value;
			if (!IndentSizeVM.HasError)
				options.IndentSize = IndentSizeVM.Value;
			options.ConvertTabsToSpaces = ConvertTabsToSpaces;
		}
	}
}

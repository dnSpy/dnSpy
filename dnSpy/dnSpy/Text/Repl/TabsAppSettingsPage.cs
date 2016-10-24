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
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Properties;
using dnSpy.Text.Editor;

namespace dnSpy.Text.Repl {
	sealed class TabsAppSettingsPage : AppSettingsPage, INotifyPropertyChanged {
		public override Guid ParentGuid => options.Guid;
		public override Guid Guid => guid;
		public override double Order => AppSettingsConstants.ORDER_REPL_LANGUAGES_TABS;
		public override string Title => dnSpy_Resources.TabsSettings;
		public override object UIObject => this;
		readonly Guid guid;

		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

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

		readonly IReplOptions options;

		public TabsAppSettingsPage(IReplOptions options, Guid guid) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			this.options = options;
			this.guid = guid;
			TabSizeVM = new Int32VM(options.TabSize, a => { }, true) { Min = OptionsHelpers.MinimumTabSize, Max = OptionsHelpers.MaximumTabSize };
			IndentSizeVM = new Int32VM(options.IndentSize, a => { }, true) { Min = OptionsHelpers.MinimumIndentSize, Max = OptionsHelpers.MaximumIndentSize };
			ConvertTabsToSpaces = options.ConvertTabsToSpaces;
		}

		public override void OnApply() {
			if (!TabSizeVM.HasError)
				options.TabSize = TabSizeVM.Value;
			if (!IndentSizeVM.HasError)
				options.IndentSize = IndentSizeVM.Value;
			options.ConvertTabsToSpaces = ConvertTabsToSpaces;
		}
	}
}

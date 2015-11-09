/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using dnSpy.Contracts;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Debugger.Breakpoints {
	sealed class BreakpointSettings : ViewModelBase {
		public static readonly BreakpointSettings Instance = new BreakpointSettings();
		int disableSaveCounter;

		BreakpointSettings() {
			Load();
		}

		public bool ShowTokens {
			get { return showTokens; }
			set {
				if (showTokens != value) {
					showTokens = value;
					Save();
					OnPropertyChanged("ShowTokens");
				}
			}
		}
		bool showTokens;

		const string SETTINGS_NAME = "42CB1310-641D-4EB7-971D-16DC5CF9A40D";

		void Load() {
			try {
				disableSaveCounter++;

				var section = DnSpy.App.SettingsManager.GetOrCreateSection(SETTINGS_NAME);
				ShowTokens = section.Attribute<bool?>("ShowTokens") ?? true;
			}
			finally {
				disableSaveCounter--;
			}
		}

		void Save() {
			if (this != BreakpointSettings.Instance)
				return;
			if (disableSaveCounter != 0)
				return;

			var section = DnSpy.App.SettingsManager.CreateSection(SETTINGS_NAME);

			section.Attribute("ShowTokens", ShowTokens);
		}
	}
}

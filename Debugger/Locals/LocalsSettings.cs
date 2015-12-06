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

using System;
using dnSpy.Contracts;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Debugger.Locals {
	sealed class LocalsSettings : ViewModelBase {
		public static readonly LocalsSettings Instance = new LocalsSettings();
		int disableSaveCounter;

		LocalsSettings() {
			Load();
		}

		public bool ShowNamespaces {
			get { return showNamespaces; }
			set {
				if (showNamespaces != value) {
					showNamespaces = value;
					Save();
					OnPropertyChanged("ShowNamespaces");
				}
			}
		}
		bool showNamespaces;

		public bool ShowTypeKeywords {
			get { return showTypeKeywords; }
			set {
				if (showTypeKeywords != value) {
					showTypeKeywords = value;
					Save();
					OnPropertyChanged("ShowTypeKeywords");
				}
			}
		}
		bool showTypeKeywords;

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

		static readonly Guid SETTINGS_GUID = new Guid("33608C69-6696-4721-8011-81ECCCC80C64");

		void Load() {
			try {
				disableSaveCounter++;

				var section = DnSpy.App.SettingsManager.GetOrCreateSection(SETTINGS_GUID);
				ShowNamespaces = section.Attribute<bool?>("ShowNamespaces") ?? true;
				ShowTypeKeywords = section.Attribute<bool?>("ShowTypeKeywords") ?? true;
				ShowTokens = section.Attribute<bool?>("ShowTokens") ?? false;
			}
			finally {
				disableSaveCounter--;
			}
		}

		void Save() {
			if (this != LocalsSettings.Instance)
				return;
			if (disableSaveCounter != 0)
				return;

			var section = DnSpy.App.SettingsManager.CreateSection(SETTINGS_GUID);

			section.Attribute("ShowNamespaces", ShowNamespaces);
			section.Attribute("ShowTypeKeywords", ShowTypeKeywords);
			section.Attribute("ShowTokens", ShowTokens);
		}
	}
}

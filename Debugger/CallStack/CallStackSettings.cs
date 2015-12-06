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

namespace dnSpy.Debugger.CallStack {
	sealed class CallStackSettings : ViewModelBase {
		public static readonly CallStackSettings Instance = new CallStackSettings();
		int disableSaveCounter;

		CallStackSettings() {
			Load();
		}

		public bool ShowModuleNames {
			get { return showModuleNames; }
			set {
				if (showModuleNames != value) {
					showModuleNames = value;
					Save();
					OnPropertyChanged("ShowModuleNames");
				}
			}
		}
		bool showModuleNames;

		public bool ShowParameterTypes {
			get { return showParameterTypes; }
			set {
				if (showParameterTypes != value) {
					showParameterTypes = value;
					Save();
					OnPropertyChanged("ShowParameterTypes");
				}
			}
		}
		bool showParameterTypes;

		public bool ShowParameterNames {
			get { return showParameterNames; }
			set {
				if (showParameterNames != value) {
					showParameterNames = value;
					Save();
					OnPropertyChanged("ShowParameterNames");
				}
			}
		}
		bool showParameterNames;

		public bool ShowParameterValues {
			get { return showParameterValues; }
			set {
				if (showParameterValues != value) {
					showParameterValues = value;
					Save();
					OnPropertyChanged("ShowParameterValues");
				}
			}
		}
		bool showParameterValues;

		public bool ShowIP {
			get { return showIP; }
			set {
				if (showIP != value) {
					showIP = value;
					Save();
					OnPropertyChanged("ShowIP");
				}
			}
		}
		bool showIP;

		public bool ShowOwnerTypes {
			get { return showOwnerTypes; }
			set {
				if (showOwnerTypes != value) {
					showOwnerTypes = value;
					Save();
					OnPropertyChanged("ShowOwnerTypes");
				}
			}
		}
		bool showOwnerTypes;

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

		public bool ShowReturnTypes {
			get { return showReturnTypes; }
			set {
				if (showReturnTypes != value) {
					showReturnTypes = value;
					Save();
					OnPropertyChanged("ShowReturnTypes");
				}
			}
		}
		bool showReturnTypes;

		static readonly Guid SETTINGS_GUID = new Guid("7280C4EB-1135-4F39-B6E0-57BD0A2454D6");

		void Load() {
			try {
				disableSaveCounter++;

				var section = DnSpy.App.SettingsManager.GetOrCreateSection(SETTINGS_GUID);
				ShowModuleNames = section.Attribute<bool?>("ShowModuleNames") ?? true;
				ShowParameterTypes = section.Attribute<bool?>("ShowParameterTypes") ?? true;
				ShowParameterNames = section.Attribute<bool?>("ShowParameterNames") ?? true;
				ShowParameterValues = section.Attribute<bool?>("ShowParameterValues") ?? false;
				ShowIP = section.Attribute<bool?>("ShowIP") ?? true;
				ShowOwnerTypes = section.Attribute<bool?>("ShowOwnerTypes") ?? true;
				ShowNamespaces = section.Attribute<bool?>("ShowNamespaces") ?? true;
				ShowTypeKeywords = section.Attribute<bool?>("ShowTypeKeywords") ?? true;
				ShowTokens = section.Attribute<bool?>("ShowTokens") ?? false;
				ShowReturnTypes = section.Attribute<bool?>("ShowReturnTypes") ?? false;
			}
			finally {
				disableSaveCounter--;
			}
		}

		void Save() {
			if (this != CallStackSettings.Instance)
				return;
			if (disableSaveCounter != 0)
				return;

			var section = DnSpy.App.SettingsManager.CreateSection(SETTINGS_GUID);

			section.Attribute("ShowModuleNames", ShowModuleNames);
			section.Attribute("ShowParameterTypes", ShowParameterTypes);
			section.Attribute("ShowParameterNames", ShowParameterNames);
			section.Attribute("ShowParameterValues", ShowParameterValues);
			section.Attribute("ShowIP", ShowIP);
			section.Attribute("ShowOwnerTypes", ShowOwnerTypes);
			section.Attribute("ShowNamespaces", ShowNamespaces);
			section.Attribute("ShowTypeKeywords", ShowTypeKeywords);
			section.Attribute("ShowTokens", ShowTokens);
			section.Attribute("ShowReturnTypes", ShowReturnTypes);
		}
	}
}

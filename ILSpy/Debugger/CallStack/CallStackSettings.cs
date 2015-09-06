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

using System.Xml.Linq;
using dnSpy.MVVM;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger.CallStack {
	sealed class CallStackSettings : ViewModelBase {
		public static readonly CallStackSettings Instance = new CallStackSettings();
		int disableSaveCounter;

		CallStackSettings() {
			LoadSettings();
		}

		public bool ShowModuleNames {
			get { return showModuleNames; }
			set {
				if (showModuleNames != value) {
					showModuleNames = value;
					SaveSettings();
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
					SaveSettings();
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
					SaveSettings();
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
					SaveSettings();
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
					SaveSettings();
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
					SaveSettings();
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
					SaveSettings();
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
					SaveSettings();
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
					SaveSettings();
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
					SaveSettings();
					OnPropertyChanged("ShowReturnTypes");
				}
			}
		}
		bool showReturnTypes;

		const string SETTINGS_NAME = "CallStack";

		void LoadSettings() {
			try {
				disableSaveCounter++;

				var settings = ILSpySettings.Load();
				var csx = settings[SETTINGS_NAME];
				ShowModuleNames = (bool?)csx.Attribute("ShowModuleNames") ?? true;
				ShowParameterTypes = (bool?)csx.Attribute("ShowParameterTypes") ?? true;
				ShowParameterNames = (bool?)csx.Attribute("ShowParameterNames") ?? true;
				ShowParameterValues = (bool?)csx.Attribute("ShowParameterValues") ?? false;
				ShowIP = (bool?)csx.Attribute("ShowIP") ?? false;
				ShowOwnerTypes = (bool?)csx.Attribute("ShowOwnerTypes") ?? true;
				ShowNamespaces = (bool?)csx.Attribute("ShowNamespaces") ?? true;
				ShowTypeKeywords = (bool?)csx.Attribute("ShowTypeKeywords") ?? true;
				ShowTokens = (bool?)csx.Attribute("ShowTokens") ?? false;
				ShowReturnTypes = (bool?)csx.Attribute("ShowReturnTypes") ?? false;
			}
			finally {
				disableSaveCounter--;
			}
		}

		void SaveSettings() {
			ILSpySettings.Update(root => SaveSettings(root));
		}

		void SaveSettings(XElement root) {
			if (disableSaveCounter != 0)
				return;

			var csx = new XElement(SETTINGS_NAME);
			var existingElement = root.Element(SETTINGS_NAME);
			if (existingElement != null)
				existingElement.ReplaceWith(csx);
			else
				root.Add(csx);

			csx.SetAttributeValue("ShowModuleNames", ShowModuleNames);
			csx.SetAttributeValue("ShowParameterTypes", ShowParameterTypes);
			csx.SetAttributeValue("ShowParameterNames", ShowParameterNames);
			csx.SetAttributeValue("ShowParameterValues", ShowParameterValues);
			csx.SetAttributeValue("ShowIP", ShowIP);
			csx.SetAttributeValue("ShowOwnerTypes", ShowOwnerTypes);
			csx.SetAttributeValue("ShowNamespaces", ShowNamespaces);
			csx.SetAttributeValue("ShowTypeKeywords", ShowTypeKeywords);
			csx.SetAttributeValue("ShowTokens", ShowTokens);
			csx.SetAttributeValue("ShowReturnTypes", ShowReturnTypes);
		}
	}
}

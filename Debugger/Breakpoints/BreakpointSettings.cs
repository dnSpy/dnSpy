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
using dnSpy.Shared.UI.MVVM;
using ICSharpCode.ILSpy;

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

		const string SETTINGS_NAME = "BreakpointSettings";

		void Load() {
			try {
				disableSaveCounter++;

				var settings = DNSpySettings.Load();
				var csx = settings[SETTINGS_NAME];
				ShowTokens = (bool?)csx.Attribute("ShowTokens") ?? true;
			}
			finally {
				disableSaveCounter--;
			}
		}

		void Save() {
			if (this != BreakpointSettings.Instance)
				return;
			DNSpySettings.Update(root => Save(root));
		}

		void Save(XElement root) {
			if (this != BreakpointSettings.Instance)
				return;
			if (disableSaveCounter != 0)
				return;

			var csx = new XElement(SETTINGS_NAME);
			var existingElement = root.Element(SETTINGS_NAME);
			if (existingElement != null)
				existingElement.ReplaceWith(csx);
			else
				root.Add(csx);

			csx.SetAttributeValue("ShowTokens", ShowTokens);
		}
	}
}

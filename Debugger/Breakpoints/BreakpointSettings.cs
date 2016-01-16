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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Settings;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.Breakpoints {
	interface IBreakpointSettings : INotifyPropertyChanged {
		bool ShowTokens { get; }
	}

	class BreakpointSettings : ViewModelBase, IBreakpointSettings {
		protected virtual void OnModified() {
		}

		public bool ShowTokens {
			get { return showTokens; }
			set {
				if (showTokens != value) {
					showTokens = value;
					OnPropertyChanged("ShowTokens");
					OnModified();
				}
			}
		}
		bool showTokens = true;
	}

	[Export, Export(typeof(IBreakpointSettings)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class BreakpointSettingsImpl : BreakpointSettings {
		static readonly Guid SETTINGS_GUID = new Guid("42CB1310-641D-4EB7-971D-16DC5CF9A40D");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		BreakpointSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			ShowTokens = sect.Attribute<bool?>("ShowTokens") ?? ShowTokens;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute("ShowTokens", ShowTokens);
		}
	}
}

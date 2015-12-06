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
using System.ComponentModel;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Settings;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Files.Tabs.Dialogs {
	interface ITabsVMSettings : INotifyPropertyChanged {
		bool SyntaxHighlight { get; }
	}

	class TabsVMSettings : ViewModelBase, ITabsVMSettings {
		protected virtual void OnModified() {
		}

		public bool SyntaxHighlight {
			get { return syntaxHighlight; }
			set {
				if (syntaxHighlight != value) {
					syntaxHighlight = value;
					OnPropertyChanged("SyntaxHighlight");
					OnModified();
				}
			}
		}
		bool syntaxHighlight = true;
	}

	[Export, Export(typeof(ITabsVMSettings)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class TabsVMSettingsImpl : TabsVMSettings {
		static readonly Guid SETTINGS_GUID = new Guid("EB2D9511-93B9-4985-BB99-1758BF2A5ADE");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		TabsVMSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			this.SyntaxHighlight = sect.Attribute<bool?>("SyntaxHighlight") ?? this.SyntaxHighlight;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute("SyntaxHighlight", SyntaxHighlight);
		}
	}
}

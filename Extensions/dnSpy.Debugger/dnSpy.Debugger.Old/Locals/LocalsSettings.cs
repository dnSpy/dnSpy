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
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;

namespace dnSpy.Debugger.Locals {
	interface ILocalsSettings : INotifyPropertyChanged {
		bool ShowNamespaces { get;}
		bool ShowTypeKeywords { get; }
		bool ShowTokens { get; }
	}

	class LocalsSettings : ViewModelBase, ILocalsSettings {
		protected virtual void OnModified() { }

		public bool ShowNamespaces {
			get { return showNamespaces; }
			set {
				if (showNamespaces != value) {
					showNamespaces = value;
					OnPropertyChanged(nameof(ShowNamespaces));
					OnModified();
				}
			}
		}
		bool showNamespaces = true;

		public bool ShowTypeKeywords {
			get { return showTypeKeywords; }
			set {
				if (showTypeKeywords != value) {
					showTypeKeywords = value;
					OnPropertyChanged(nameof(ShowTypeKeywords));
					OnModified();
				}
			}
		}
		bool showTypeKeywords = true;

		public bool ShowTokens {
			get { return showTokens; }
			set {
				if (showTokens != value) {
					showTokens = value;
					OnPropertyChanged(nameof(ShowTokens));
					OnModified();
				}
			}
		}
		bool showTokens = false;
	}

	//[Export, Export(typeof(ILocalsSettings))]
	sealed class LocalsSettingsImpl : LocalsSettings {
		static readonly Guid SETTINGS_GUID = new Guid("33608C69-6696-4721-8011-81ECCCC80C64");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		LocalsSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			disableSave = true;
			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			ShowNamespaces = sect.Attribute<bool?>(nameof(ShowNamespaces)) ?? ShowNamespaces;
			ShowTypeKeywords = sect.Attribute<bool?>(nameof(ShowTypeKeywords)) ?? ShowTypeKeywords;
			ShowTokens = sect.Attribute<bool?>(nameof(ShowTokens)) ?? ShowTokens;
			disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(ShowNamespaces), ShowNamespaces);
			sect.Attribute(nameof(ShowTypeKeywords), ShowTypeKeywords);
			sect.Attribute(nameof(ShowTokens), ShowTokens);
		}
	}
}

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

namespace dnSpy.Debugger.Locals {
	interface ILocalsSettings : INotifyPropertyChanged {
		bool ShowNamespaces { get;}
		bool ShowTypeKeywords { get; }
		bool ShowTokens { get; }
	}

	class LocalsSettings : ViewModelBase, ILocalsSettings {
		protected virtual void OnModified() {
		}

		public bool ShowNamespaces {
			get { return showNamespaces; }
			set {
				if (showNamespaces != value) {
					showNamespaces = value;
					OnPropertyChanged("ShowNamespaces");
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
					OnPropertyChanged("ShowTypeKeywords");
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
					OnPropertyChanged("ShowTokens");
					OnModified();
				}
			}
		}
		bool showTokens = false;
	}

	[Export, Export(typeof(ILocalsSettings)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class LocalsSettingsImpl : LocalsSettings {
		static readonly Guid SETTINGS_GUID = new Guid("33608C69-6696-4721-8011-81ECCCC80C64");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		LocalsSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			ShowNamespaces = sect.Attribute<bool?>("ShowNamespaces") ?? ShowNamespaces;
			ShowTypeKeywords = sect.Attribute<bool?>("ShowTypeKeywords") ?? ShowTypeKeywords;
			ShowTokens = sect.Attribute<bool?>("ShowTokens") ?? ShowTokens;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var secti = settingsManager.RecreateSection(SETTINGS_GUID);
			secti.Attribute("ShowNamespaces", ShowNamespaces);
			secti.Attribute("ShowTypeKeywords", ShowTypeKeywords);
			secti.Attribute("ShowTokens", ShowTokens);
		}
	}
}

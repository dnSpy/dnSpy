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
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Search {
	interface ISearchSettings : INotifyPropertyChanged {
		bool SyntaxHighlight { get; }
	}

	class SearchSettings : ViewModelBase, ISearchSettings {
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

		public SearchSettings Clone() {
			return CopyTo(new SearchSettings());
		}

		public SearchSettings CopyTo(SearchSettings other) {
			other.SyntaxHighlight = this.SyntaxHighlight;
			return other;
		}
	}

	[Export, Export(typeof(ISearchSettings)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class SearchSettingsImpl : SearchSettings {
		static readonly Guid SETTINGS_GUID = new Guid("68377C1D-228A-4317-AB10-11796F6DEB18");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		SearchSettingsImpl(ISettingsManager settingsManager) {
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

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

namespace dnSpy.Search {
	interface ISearchSettings : INotifyPropertyChanged {
		bool SyntaxHighlight { get; set; }
		bool MatchWholeWords { get; set; }
		bool CaseSensitive { get; set; }
		bool MatchAnySearchTerm { get; set; }
		bool SearchDecompiledData { get; set; }
		bool SearchGacAssemblies { get; set; }
	}

	class SearchSettings : ViewModelBase, ISearchSettings {
		protected virtual void OnModified() { }

		public bool SyntaxHighlight {
			get { return syntaxHighlight; }
			set {
				if (syntaxHighlight != value) {
					syntaxHighlight = value;
					OnPropertyChanged(nameof(SyntaxHighlight));
					OnModified();
				}
			}
		}
		bool syntaxHighlight = true;

		public bool MatchWholeWords {
			get { return matchWholeWords; }
			set {
				if (matchWholeWords != value) {
					matchWholeWords = value;
					OnPropertyChanged(nameof(MatchWholeWords));
					OnModified();
				}
			}
		}
		bool matchWholeWords = false;

		public bool CaseSensitive {
			get { return caseSensitive; }
			set {
				if (caseSensitive != value) {
					caseSensitive = value;
					OnPropertyChanged(nameof(CaseSensitive));
					OnModified();
				}
			}
		}
		bool caseSensitive = false;

		public bool MatchAnySearchTerm {
			get { return matchAnySearchTerm; }
			set {
				if (matchAnySearchTerm != value) {
					matchAnySearchTerm = value;
					OnPropertyChanged(nameof(MatchAnySearchTerm));
					OnModified();
				}
			}
		}
		bool matchAnySearchTerm = false;

		public bool SearchDecompiledData {
			get { return searchDecompiledData; }
			set {
				if (searchDecompiledData != value) {
					searchDecompiledData = value;
					OnPropertyChanged(nameof(SearchDecompiledData));
					OnModified();
				}
			}
		}
		bool searchDecompiledData = true;

		public bool SearchGacAssemblies {
			get { return searchGacAssemblies; }
			set {
				if (searchGacAssemblies != value) {
					searchGacAssemblies = value;
					OnPropertyChanged(nameof(SearchGacAssemblies));
					OnModified();
				}
			}
		}
		bool searchGacAssemblies = true;

		public SearchSettings Clone() => CopyTo(new SearchSettings());

		public SearchSettings CopyTo(SearchSettings other) {
			other.SyntaxHighlight = this.SyntaxHighlight;
			other.MatchWholeWords = this.MatchWholeWords;
			other.CaseSensitive = this.CaseSensitive;
			other.MatchAnySearchTerm = this.MatchAnySearchTerm;
			other.SearchDecompiledData = this.SearchDecompiledData;
			other.SearchGacAssemblies = this.SearchGacAssemblies;
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
			this.SyntaxHighlight = sect.Attribute<bool?>(nameof(SyntaxHighlight)) ?? this.SyntaxHighlight;
			this.MatchWholeWords = sect.Attribute<bool?>(nameof(MatchWholeWords)) ?? this.MatchWholeWords;
			this.CaseSensitive = sect.Attribute<bool?>(nameof(CaseSensitive)) ?? this.CaseSensitive;
			this.MatchAnySearchTerm = sect.Attribute<bool?>(nameof(MatchAnySearchTerm)) ?? this.MatchAnySearchTerm;
			this.SearchDecompiledData = sect.Attribute<bool?>(nameof(SearchDecompiledData)) ?? this.SearchDecompiledData;
			this.SearchGacAssemblies = sect.Attribute<bool?>(nameof(SearchGacAssemblies)) ?? this.SearchGacAssemblies;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(SyntaxHighlight), SyntaxHighlight);
			sect.Attribute(nameof(MatchWholeWords), MatchWholeWords);
			sect.Attribute(nameof(CaseSensitive), CaseSensitive);
			sect.Attribute(nameof(MatchAnySearchTerm), MatchAnySearchTerm);
			sect.Attribute(nameof(SearchDecompiledData), SearchDecompiledData);
			sect.Attribute(nameof(SearchGacAssemblies), SearchGacAssemblies);
		}
	}
}

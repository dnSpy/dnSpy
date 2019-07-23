/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

namespace dnSpy.Search {
	interface ISearchSettings : INotifyPropertyChanged {
		SearchLocation SearchLocation { get; set; }
		SearchType SearchType { get; set; }
		bool SyntaxHighlight { get; set; }
		bool MatchWholeWords { get; set; }
		bool CaseSensitive { get; set; }
		bool MatchAnySearchTerm { get; set; }
		bool SearchDecompiledData { get; set; }
		bool SearchFrameworkAssemblies { get; set; }
	}

	class SearchSettings : ViewModelBase, ISearchSettings {
		public SearchLocation SearchLocation {
			get => searchLocation;
			set {
				if (searchLocation != value) {
					searchLocation = value;
					OnPropertyChanged(nameof(SearchLocation));
				}
			}
		}
		SearchLocation searchLocation = SearchLocation.AllFiles;

		public SearchType SearchType {
			get => searchType;
			set {
				if (searchType != value) {
					searchType = value;
					OnPropertyChanged(nameof(SearchType));
				}
			}
		}
		SearchType searchType = SearchType.Any;

		public bool SyntaxHighlight {
			get => syntaxHighlight;
			set {
				if (syntaxHighlight != value) {
					syntaxHighlight = value;
					OnPropertyChanged(nameof(SyntaxHighlight));
				}
			}
		}
		bool syntaxHighlight = true;

		public bool MatchWholeWords {
			get => matchWholeWords;
			set {
				if (matchWholeWords != value) {
					matchWholeWords = value;
					OnPropertyChanged(nameof(MatchWholeWords));
				}
			}
		}
		bool matchWholeWords = false;

		public bool CaseSensitive {
			get => caseSensitive;
			set {
				if (caseSensitive != value) {
					caseSensitive = value;
					OnPropertyChanged(nameof(CaseSensitive));
				}
			}
		}
		bool caseSensitive = false;

		public bool MatchAnySearchTerm {
			get => matchAnySearchTerm;
			set {
				if (matchAnySearchTerm != value) {
					matchAnySearchTerm = value;
					OnPropertyChanged(nameof(MatchAnySearchTerm));
				}
			}
		}
		bool matchAnySearchTerm = false;

		public bool SearchDecompiledData {
			get => searchDecompiledData;
			set {
				if (searchDecompiledData != value) {
					searchDecompiledData = value;
					OnPropertyChanged(nameof(SearchDecompiledData));
				}
			}
		}
		bool searchDecompiledData = true;

		public bool SearchFrameworkAssemblies {
			get => searchFrameworkAssemblies;
			set {
				if (searchFrameworkAssemblies != value) {
					searchFrameworkAssemblies = value;
					OnPropertyChanged(nameof(SearchFrameworkAssemblies));
				}
			}
		}
		bool searchFrameworkAssemblies = true;

		public SearchSettings Clone() => CopyTo(new SearchSettings());

		public SearchSettings CopyTo(SearchSettings other) {
			other.SearchLocation = SearchLocation;
			other.SearchType = SearchType;
			other.SyntaxHighlight = SyntaxHighlight;
			other.MatchWholeWords = MatchWholeWords;
			other.CaseSensitive = CaseSensitive;
			other.MatchAnySearchTerm = MatchAnySearchTerm;
			other.SearchDecompiledData = SearchDecompiledData;
			other.SearchFrameworkAssemblies = SearchFrameworkAssemblies;
			return other;
		}
	}

	[Export, Export(typeof(ISearchSettings))]
	sealed class SearchSettingsImpl : SearchSettings {
		static readonly Guid SETTINGS_GUID = new Guid("68377C1D-228A-4317-AB10-11796F6DEB18");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		SearchSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			SearchLocation = sect.Attribute<SearchLocation?>(nameof(SearchLocation)) ?? SearchLocation;
			SearchType = sect.Attribute<SearchType?>(nameof(SearchType)) ?? SearchType;
			SyntaxHighlight = sect.Attribute<bool?>(nameof(SyntaxHighlight)) ?? SyntaxHighlight;
			MatchWholeWords = sect.Attribute<bool?>(nameof(MatchWholeWords)) ?? MatchWholeWords;
			CaseSensitive = sect.Attribute<bool?>(nameof(CaseSensitive)) ?? CaseSensitive;
			MatchAnySearchTerm = sect.Attribute<bool?>(nameof(MatchAnySearchTerm)) ?? MatchAnySearchTerm;
			SearchDecompiledData = sect.Attribute<bool?>(nameof(SearchDecompiledData)) ?? SearchDecompiledData;
			SearchFrameworkAssemblies = sect.Attribute<bool?>(nameof(SearchFrameworkAssemblies)) ?? SearchFrameworkAssemblies;
			PropertyChanged += SearchSettingsImpl_PropertyChanged;
		}

		void SearchSettingsImpl_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(SearchLocation), SearchLocation);
			sect.Attribute(nameof(SearchType), SearchType);
			sect.Attribute(nameof(SyntaxHighlight), SyntaxHighlight);
			sect.Attribute(nameof(MatchWholeWords), MatchWholeWords);
			sect.Attribute(nameof(CaseSensitive), CaseSensitive);
			sect.Attribute(nameof(MatchAnySearchTerm), MatchAnySearchTerm);
			sect.Attribute(nameof(SearchDecompiledData), SearchDecompiledData);
			sect.Attribute(nameof(SearchFrameworkAssemblies), SearchFrameworkAssemblies);
		}
	}
}

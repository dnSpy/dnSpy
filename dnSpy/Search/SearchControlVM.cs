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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Data;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Search;
using dnSpy.Properties;
using dnSpy.Shared.MVVM;
using dnSpy.Shared.Search;

namespace dnSpy.Search {
	sealed class SearchControlVM : ViewModelBase {
		const int DEFAULT_DELAY_SEARCH_MS = 500;

		public bool CanSearch {
			get { return canSearch; }
			set { canSearch = value; }
		}
		bool canSearch;

		public bool MatchWholeWords {
			get { return matchWholeWords; }
			set {
				if (matchWholeWords != value) {
					matchWholeWords = value;
					OnPropertyChanged("MatchWholeWords");
					Restart();
				}
			}
		}
		bool matchWholeWords;

		public bool CaseSensitive {
			get { return caseSensitive; }
			set {
				if (caseSensitive != value) {
					caseSensitive = value;
					OnPropertyChanged("CaseSensitive");
					Restart();
				}
			}
		}
		bool caseSensitive;

		public bool MatchAnySearchTerm {
			get { return matchAnySearchTerm; }
			set {
				if (matchAnySearchTerm != value) {
					matchAnySearchTerm = value;
					OnPropertyChanged("MatchAnySearchTerm");
					Restart();
				}
			}
		}
		bool matchAnySearchTerm;

		public bool SearchDecompiledData {
			get { return searchDecompiledData; }
			set {
				if (searchDecompiledData != value) {
					searchDecompiledData = value;
					OnPropertyChanged("SearchDecompiledData");
					Restart();
				}
			}
		}
		bool searchDecompiledData;

		public bool TooManyResults {
			get { return tooManyResults; }
			set {
				if (tooManyResults != value) {
					tooManyResults = value;
					OnPropertyChanged("TooManyResults");
				}
			}
		}
		bool tooManyResults;

		public ObservableCollection<SearchTypeVM> SearchTypeVMs {
			get { return searchTypeVMs; }
		}
		readonly ObservableCollection<SearchTypeVM> searchTypeVMs;

		public SearchTypeVM SelectedSearchTypeVM {
			get { return selectedSearchTypeVM; }
			set {
				if (selectedSearchTypeVM != value) {
					selectedSearchTypeVM = value;
					OnPropertyChanged("SelectedSearchTypeVM");
					Restart();
				}
			}
		}
		SearchTypeVM selectedSearchTypeVM;

		public ICollectionView SearchResultsCollectionView {
			get { return searchResultsCollectionView; }
		}
		readonly ListCollectionView searchResultsCollectionView;

		public ObservableCollection<ISearchResult> SearchResults {
			get { return searchResults; }
		}
		readonly ObservableCollection<ISearchResult> searchResults;

		public ISearchResult SelectedSearchResult {
			get { return selectedSearchResult; }
			set {
				if (selectedSearchResult != value) {
					selectedSearchResult = value;
					OnPropertyChanged("SelectedSearchResult");
				}
			}
		}
		ISearchResult selectedSearchResult;

		public string SearchText {
			get { return searchText; }
			set {
				if (searchText != value) {
					searchText = value;
					OnPropertyChanged("SearchText");
					delayedSearch.Start();
				}
			}
		}
		string searchText;
		readonly DelayedAction delayedSearch;

		public bool SyntaxHighlight {
			get { return syntaxHighlight; }
			set {
				if (syntaxHighlight != value) {
					syntaxHighlight = value;
					if (fileSearcher != null)
						fileSearcher.SyntaxHighlight = value;
				}
			}
		}
		bool syntaxHighlight;

		public ILanguage Language {
			get { return language; }
			set {
				if (language != value) {
					language = value;
					if (fileSearcher != null)
						fileSearcher.Language = language;
				}
			}
		}
		ILanguage language;

		public BackgroundType BackgroundType {
			get { return backgroundType; }
			set {
				if (backgroundType != value) {
					backgroundType = value;
					if (fileSearcher != null)
						fileSearcher.BackgroundType = value;
				}
			}
		}
		BackgroundType backgroundType;

		readonly IImageManager imageManager;
		readonly IFileSearcherCreator fileSearcherCreator;
		readonly IFileTreeView fileTreeView;

		public SearchControlVM(IImageManager imageManager, IFileSearcherCreator fileSearcherCreator, IFileTreeView fileTreeView) {
			this.imageManager = imageManager;
			this.fileSearcherCreator = fileSearcherCreator;
			this.fileTreeView = fileTreeView;
			this.delayedSearch = new DelayedAction(DEFAULT_DELAY_SEARCH_MS, DelayStartSearch);
			this.searchTypeVMs = new ObservableCollection<SearchTypeVM>();
			this.searchResults = new ObservableCollection<ISearchResult>();
			this.searchResultsCollectionView = (ListCollectionView)CollectionViewSource.GetDefaultView(searchResults);
			this.searchResultsCollectionView.CustomSort = new SearchResult_Comparer();

			Add(SearchType.AssemblyDef, dnSpy_Resources.SearchWindow_Assembly, "Assembly", null, VisibleMembersFlags.AssemblyDef);
			Add(SearchType.ModuleDef, dnSpy_Resources.SearchWindow_Module, "AssemblyModule", null, VisibleMembersFlags.ModuleDef);
			Add(SearchType.Namespace, dnSpy_Resources.SearchWindow_Namespace, "Namespace", null, VisibleMembersFlags.Namespace);
			Add(SearchType.TypeDef, dnSpy_Resources.SearchWindow_Type, "Class", dnSpy_Resources.SearchWindow_Type_Key, VisibleMembersFlags.TypeDef);
			Add(SearchType.FieldDef, dnSpy_Resources.SearchWindow_Field, "Field", dnSpy_Resources.SearchWindow_Field_Key, VisibleMembersFlags.FieldDef);
			Add(SearchType.MethodDef, dnSpy_Resources.SearchWindow_Method, "Method", dnSpy_Resources.SearchWindow_Method_Key, VisibleMembersFlags.MethodDef);
			Add(SearchType.PropertyDef, dnSpy_Resources.SearchWindow_Property, "Property", dnSpy_Resources.SearchWindow_Property_Key, VisibleMembersFlags.PropertyDef);
			Add(SearchType.EventDef, dnSpy_Resources.SearchWindow_Event, "Event", dnSpy_Resources.SearchWindow_Event_Key, VisibleMembersFlags.EventDef);
			Add(SearchType.ParamDef, dnSpy_Resources.SearchWindow_Parameter, "Parameter", dnSpy_Resources.SearchWindow_Parameter_Key, VisibleMembersFlags.ParamDef);
			Add(SearchType.Local, dnSpy_Resources.SearchWindow_Local, "Local", dnSpy_Resources.SearchWindow_Local_Key, VisibleMembersFlags.Local);
			Add(SearchType.ParamLocal, dnSpy_Resources.SearchWindow_ParameterLocal, "Parameter", dnSpy_Resources.SearchWindow_ParameterLocal_Key, VisibleMembersFlags.ParamDef | VisibleMembersFlags.Local);
			Add(SearchType.AssemblyRef, dnSpy_Resources.SearchWindow_AssemblyRef, "AssemblyReference", null, VisibleMembersFlags.AssemblyRef);
			Add(SearchType.ModuleRef, dnSpy_Resources.SearchWindow_ModuleRef, "ModuleReference", null, VisibleMembersFlags.ModuleRef);
			Add(SearchType.Resource, dnSpy_Resources.SearchWindow_Resource, "Resource", dnSpy_Resources.SearchWindow_Resource_Key, VisibleMembersFlags.Resource | VisibleMembersFlags.ResourceElement);
			Add(SearchType.GenericTypeDef, dnSpy_Resources.SearchWindow_Generic, "Generic", null, VisibleMembersFlags.GenericTypeDef);
			Add(SearchType.NonGenericTypeDef, dnSpy_Resources.SearchWindow_NonGeneric, "Class", null, VisibleMembersFlags.NonGenericTypeDef);
			Add(SearchType.EnumTypeDef, dnSpy_Resources.SearchWindow_Enum, "Enum", null, VisibleMembersFlags.EnumTypeDef);
			Add(SearchType.InterfaceTypeDef, dnSpy_Resources.SearchWindow_Interface, "Interface", null, VisibleMembersFlags.InterfaceTypeDef);
			Add(SearchType.ClassTypeDef, dnSpy_Resources.SearchWindow_Class, "Class", null, VisibleMembersFlags.ClassTypeDef);
			Add(SearchType.StructTypeDef, dnSpy_Resources.SearchWindow_Struct, "Struct", null, VisibleMembersFlags.StructTypeDef);
			Add(SearchType.DelegateTypeDef, dnSpy_Resources.SearchWindow_Delegate, "Delegate", null, VisibleMembersFlags.DelegateTypeDef);
			Add(SearchType.Member, dnSpy_Resources.SearchWindow_Member, "Property", dnSpy_Resources.SearchWindow_Member_Key, VisibleMembersFlags.MethodDef | VisibleMembersFlags.FieldDef | VisibleMembersFlags.PropertyDef | VisibleMembersFlags.EventDef);
			Add(SearchType.Any, dnSpy_Resources.SearchWindow_AllAbove, "Class", dnSpy_Resources.SearchWindow_AllAbove_Key, VisibleMembersFlags.TreeViewAll | VisibleMembersFlags.ParamDef | VisibleMembersFlags.Local);
			Add(SearchType.Literal, dnSpy_Resources.SearchWindow_Literal, "Literal", dnSpy_Resources.SearchWindow_Literal_Key, VisibleMembersFlags.MethodBody | VisibleMembersFlags.FieldDef | VisibleMembersFlags.ParamDef | VisibleMembersFlags.PropertyDef | VisibleMembersFlags.Resource | VisibleMembersFlags.ResourceElement);

			this.SelectedSearchTypeVM = SearchTypeVMs.First(a => a.SearchType == SearchType.TypeDef);
		}

		void Add(SearchType searchType, string name, string icon, string toolTip, VisibleMembersFlags flags) {
			searchTypeVMs.Add(new SearchTypeVM(imageManager, searchType, name, toolTip, icon, flags));
		}

		void DelayStartSearch() {
			Restart();
		}

		void StartSearch() {
			if (!CanSearch) {
				Clear();
				return;
			}

			CancelSearch();
			if (string.IsNullOrEmpty(SearchText))
				SearchResults.Clear();
			else {
				var options = new FileSearcherOptions {
					SearchComparer = CreateSearchComparer(),
					Filter = new FlagsFileTreeNodeFilter(selectedSearchTypeVM.Flags),
					SearchDecompiledData = SearchDecompiledData,
				};
				fileSearcher = fileSearcherCreator.Create(options);
				fileSearcher.SyntaxHighlight = SyntaxHighlight;
				fileSearcher.Language = Language;
				fileSearcher.BackgroundType = BackgroundType;
				fileSearcher.OnSearchCompleted += FileSearcher_OnSearchCompleted;
				fileSearcher.OnNewSearchResults += FileSearcher_OnNewSearchResults;
				fileSearcher.Start(fileTreeView.TreeView.Root.DataChildren.OfType<IDnSpyFileNode>());
			}
		}
		IFileSearcher fileSearcher;
		bool searchCompleted;

		void FileSearcher_OnSearchCompleted(object sender, EventArgs e) {
			if (sender == null || sender != fileSearcher || searchCompleted)
				return;
			searchCompleted = true;
			searchResults.Remove(fileSearcher.SearchingResult);
			TooManyResults = fileSearcher.TooManyResults;
		}

		void FileSearcher_OnNewSearchResults(object sender, SearchResultEventArgs e) {
			if (sender == null || sender != fileSearcher)
				return;
			Debug.Assert(!searchCompleted);
			if (searchCompleted)
				return;
			foreach (var vm in e.Results)
				searchResults.Add(vm);
		}

		ISearchComparer CreateSearchComparer() {
			if (SelectedSearchTypeVM.SearchType == SearchType.Literal)
				return SearchComparerFactory.CreateLiteral(SearchText, CaseSensitive, MatchWholeWords, MatchAnySearchTerm);
			return SearchComparerFactory.Create(SearchText, CaseSensitive, MatchWholeWords, MatchAnySearchTerm);
		}

		public void Restart() {
			StopSearch();
			SearchResults.Clear();
			StartSearch();
		}

		void StopSearch() {
			CancelSearch();
			delayedSearch.Cancel();
		}

		public void Clear() {
			SearchText = string.Empty;
			StopSearch();
			SearchResults.Clear();
		}

		void CancelSearch() {
			TooManyResults = false;
			delayedSearch.Cancel();
			if (fileSearcher != null) {
				fileSearcher.Cancel();
				fileSearcher = null;
			}
			searchCompleted = false;
		}
	}

	sealed class SearchResult_Comparer : System.Collections.IComparer {
		public int Compare(object x, object y) {
			var a = x as ISearchResult;
			var b = y as ISearchResult;
			if (a == null)
				return 1;
			if (b == null)
				return -1;
			if (a == b)
				return 0;
			return a.CompareTo(b);
		}
	}
}

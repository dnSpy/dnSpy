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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Data;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Utilities;
using dnSpy.Properties;

namespace dnSpy.Search {
	enum SearchLocation {
		AllFiles,
		SelectedFiles,
		AllFilesInSameDir,
		SelectedType,

		Last,
	}

	sealed class SearchControlVM : ViewModelBase {
		const int DEFAULT_DELAY_SEARCH_MS = 500;

		public bool CanSearch { get; set; }
		public ISearchSettings SearchSettings { get; }

		public bool TooManyResults {
			get { return tooManyResults; }
			set {
				if (tooManyResults != value) {
					tooManyResults = value;
					OnPropertyChanged(nameof(TooManyResults));
				}
			}
		}
		bool tooManyResults;

		static readonly EnumVM[] searchLocationList = new EnumVM[(int)SearchLocation.Last] {
			new EnumVM(SearchLocation.AllFiles, dnSpy_Resources.SearchWindow_Where_AllFiles),
			new EnumVM(SearchLocation.SelectedFiles, dnSpy_Resources.SearchWindow_Where_SelectedFiles),
			new EnumVM(SearchLocation.AllFilesInSameDir, dnSpy_Resources.SearchWindow_Where_FilesInSameFolder),
			new EnumVM(SearchLocation.SelectedType, dnSpy_Resources.SearchWindow_Where_SelectedType),
		};
		public EnumListVM SearchLocationVM { get; }

		public ObservableCollection<SearchTypeVM> SearchTypeVMs { get; }

		public SearchTypeVM SelectedSearchTypeVM {
			get { return selectedSearchTypeVM; }
			set {
				if (selectedSearchTypeVM != value) {
					selectedSearchTypeVM = value;
					OnPropertyChanged(nameof(SelectedSearchTypeVM));
					Restart();
				}
			}
		}
		SearchTypeVM selectedSearchTypeVM;

		public ICollectionView SearchResultsCollectionView => searchResultsCollectionView;
		readonly ListCollectionView searchResultsCollectionView;

		public ObservableCollection<ISearchResult> SearchResults { get; }

		public ISearchResult SelectedSearchResult {
			get { return selectedSearchResult; }
			set {
				if (selectedSearchResult != value) {
					selectedSearchResult = value;
					OnPropertyChanged(nameof(SelectedSearchResult));
				}
			}
		}
		ISearchResult selectedSearchResult;

		public string SearchText {
			get { return searchText; }
			set {
				if (searchText != value) {
					searchText = value;
					OnPropertyChanged(nameof(SearchText));
					delayedSearch.Start();
				}
			}
		}
		string searchText;
		readonly DelayedAction delayedSearch;

		public IDecompiler Decompiler {
			get { return decompiler; }
			set {
				if (decompiler != value) {
					decompiler = value;
					if (fileSearcher != null)
						fileSearcher.Decompiler = decompiler;
				}
			}
		}
		IDecompiler decompiler;

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
		readonly IFileSearcherProvider fileSearcherProvider;
		readonly IFileTreeView fileTreeView;

		public SearchControlVM(IImageManager imageManager, IFileSearcherProvider fileSearcherProvider, IFileTreeView fileTreeView, ISearchSettings searchSettings) {
			this.imageManager = imageManager;
			this.fileSearcherProvider = fileSearcherProvider;
			this.fileTreeView = fileTreeView;
			this.SearchSettings = searchSettings;
			searchSettings.PropertyChanged += SearchSettings_PropertyChanged;
			this.delayedSearch = new DelayedAction(DEFAULT_DELAY_SEARCH_MS, DelayStartSearch);
			this.SearchTypeVMs = new ObservableCollection<SearchTypeVM>();
			this.SearchResults = new ObservableCollection<ISearchResult>();
			this.searchResultsCollectionView = (ListCollectionView)CollectionViewSource.GetDefaultView(SearchResults);
			this.searchResultsCollectionView.CustomSort = new SearchResult_Comparer();
			this.SearchLocationVM = new EnumListVM(searchLocationList, (a, b) => Restart());
			this.SearchLocationVM.SelectedItem = SearchLocation.AllFiles;

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
			Add(SearchType.Literal, dnSpy_Resources.SearchWindow_Literal, "Literal", dnSpy_Resources.SearchWindow_Literal_Key, VisibleMembersFlags.MethodBody | VisibleMembersFlags.FieldDef | VisibleMembersFlags.ParamDef | VisibleMembersFlags.PropertyDef | VisibleMembersFlags.Resource | VisibleMembersFlags.ResourceElement | VisibleMembersFlags.Attributes);

			this.SelectedSearchTypeVM = SearchTypeVMs.First(a => a.SearchType == SearchType.Any);
		}

		void Add(SearchType searchType, string name, string icon, string toolTip, VisibleMembersFlags flags) =>
			SearchTypeVMs.Add(new SearchTypeVM(imageManager, searchType, name, toolTip, icon, flags));
		void DelayStartSearch() => Restart();

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
					SearchDecompiledData = SearchSettings.SearchDecompiledData,
				};
				fileSearcher = fileSearcherProvider.Create(options);
				fileSearcher.SyntaxHighlight = SearchSettings.SyntaxHighlight;
				fileSearcher.Decompiler = Decompiler;
				fileSearcher.BackgroundType = BackgroundType;
				fileSearcher.OnSearchCompleted += FileSearcher_OnSearchCompleted;
				fileSearcher.OnNewSearchResults += FileSearcher_OnNewSearchResults;

				switch ((SearchLocation)SearchLocationVM.SelectedItem) {
				case SearchLocation.AllFiles:
					fileSearcher.Start(GetAllFilesToSearch());
					break;

				case SearchLocation.SelectedFiles:
					fileSearcher.Start(GetSelectedFilesToSearch());
					break;

				case SearchLocation.AllFilesInSameDir:
					fileSearcher.Start(GetAllFilesInSameDirToSearch());
					break;

				case SearchLocation.SelectedType:
					fileSearcher.Start(GetSelectedTypeToSearch());
					break;

				default:
					throw new InvalidOperationException();
				}
			}
		}
		IFileSearcher fileSearcher;
		bool searchCompleted;

		bool CanSearchFile(IDnSpyFileNode node) =>
			SearchSettings.SearchGacAssemblies || !GacInfo.IsGacPath(node.DnSpyFile.Filename);
		IEnumerable<IDnSpyFileNode> GetAllFilesToSearch() =>
			fileTreeView.TreeView.Root.DataChildren.OfType<IDnSpyFileNode>().Where(a => CanSearchFile(a));
		IEnumerable<IDnSpyFileNode> GetSelectedFilesToSearch() =>
			fileTreeView.TreeView.TopLevelSelection.Select(a => a.GetTopNode()).Where(a => a != null && CanSearchFile(a)).Distinct();

		IEnumerable<IDnSpyFileNode> GetAllFilesInSameDirToSearch() {
			var dirsEnum = GetSelectedFilesToSearch().Where(a => File.Exists(a.DnSpyFile.Filename)).Select(a => Path.GetDirectoryName(a.DnSpyFile.Filename));
			var dirs = new HashSet<string>(dirsEnum, StringComparer.OrdinalIgnoreCase);
			return GetAllFilesToSearch().Where(a => File.Exists(a.DnSpyFile.Filename) && dirs.Contains(Path.GetDirectoryName(a.DnSpyFile.Filename)));
		}

		IEnumerable<SearchTypeInfo> GetSelectedTypeToSearch() {
			foreach (var node in fileTreeView.TreeView.TopLevelSelection.Select(a => a.GetAncestorOrSelf<ITypeNode>()).Where(a => a != null).Distinct()) {
				var fileNode = node.GetDnSpyFileNode();
				Debug.Assert(fileNode != null);
				if (fileNode == null)
					continue;
				yield return new SearchTypeInfo(fileNode.DnSpyFile, node.TypeDef);
			}
		}

		void FileSearcher_OnSearchCompleted(object sender, EventArgs e) {
			if (sender == null || sender != fileSearcher || searchCompleted)
				return;
			searchCompleted = true;
			SearchResults.Remove(fileSearcher.SearchingResult);
			TooManyResults = fileSearcher.TooManyResults;
		}

		void FileSearcher_OnNewSearchResults(object sender, SearchResultEventArgs e) {
			if (sender == null || sender != fileSearcher)
				return;
			Debug.Assert(!searchCompleted);
			if (searchCompleted)
				return;
			foreach (var vm in e.Results)
				SearchResults.Add(vm);
		}

		ISearchComparer CreateSearchComparer() {
			if (SelectedSearchTypeVM.SearchType == SearchType.Literal)
				return SearchComparerFactory.CreateLiteral(SearchText, SearchSettings.CaseSensitive, SearchSettings.MatchWholeWords, SearchSettings.MatchAnySearchTerm);
			return SearchComparerFactory.Create(SearchText, SearchSettings.CaseSensitive, SearchSettings.MatchWholeWords, SearchSettings.MatchAnySearchTerm);
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

		void SearchSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			switch (e.PropertyName) {
			case nameof(SearchSettings.SyntaxHighlight):
				if (fileSearcher != null)
					fileSearcher.SyntaxHighlight = SearchSettings.SyntaxHighlight;
				break;
			case nameof(SearchSettings.MatchWholeWords):
			case nameof(SearchSettings.CaseSensitive):
			case nameof(SearchSettings.MatchAnySearchTerm):
			case nameof(SearchSettings.SearchDecompiledData):
			case nameof(SearchSettings.SearchGacAssemblies):
				Restart();
				break;
			}
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

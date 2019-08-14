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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Data;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
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
			get => tooManyResults;
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
			get => selectedSearchTypeVM;
			set {
				if (selectedSearchTypeVM != value) {
					selectedSearchTypeVM = value;
					OnPropertyChanged(nameof(SelectedSearchTypeVM));
					SearchSettings.SearchType = selectedSearchTypeVM.SearchType;
				}
			}
		}
		SearchTypeVM selectedSearchTypeVM;

		public ICollectionView SearchResultsCollectionView => searchResultsCollectionView;
		readonly ListCollectionView searchResultsCollectionView;

		public ObservableCollection<ISearchResult> SearchResults { get; }

		public ISearchResult? SelectedSearchResult {
			get => selectedSearchResult;
			set {
				if (selectedSearchResult != value) {
					selectedSearchResult = value;
					OnPropertyChanged(nameof(SelectedSearchResult));
				}
			}
		}
		ISearchResult? selectedSearchResult;

		public string SearchText {
			get => searchText;
			set {
				if (searchText != value) {
					searchText = value;
					OnPropertyChanged(nameof(SearchText));
					delayedSearch.Start();
				}
			}
		}
		string searchText = string.Empty;
		readonly DelayedAction delayedSearch;

		public IDecompiler Decompiler {
			get => decompiler;
			set {
				if (decompiler != value) {
					decompiler = value;
					if (!(fileSearcher is null))
						fileSearcher.Decompiler = decompiler;
				}
			}
		}
		IDecompiler decompiler;

		readonly IDocumentSearcherProvider fileSearcherProvider;
		readonly IDocumentTreeView documentTreeView;

		public SearchControlVM(IDocumentSearcherProvider fileSearcherProvider, IDocumentTreeView documentTreeView, ISearchSettings searchSettings, IDecompiler decompiler) {
			this.selectedSearchTypeVM = null!;
			this.fileSearcherProvider = fileSearcherProvider;
			this.documentTreeView = documentTreeView;
			this.decompiler = decompiler;
			SearchSettings = searchSettings;
			delayedSearch = new DelayedAction(DEFAULT_DELAY_SEARCH_MS, DelayStartSearch);
			SearchTypeVMs = new ObservableCollection<SearchTypeVM>();
			SearchResults = new ObservableCollection<ISearchResult>();
			searchResultsCollectionView = (ListCollectionView)CollectionViewSource.GetDefaultView(SearchResults);
			searchResultsCollectionView.CustomSort = new SearchResult_Comparer();
			SearchLocationVM = new EnumListVM(searchLocationList, (a, b) => SearchSettings.SearchLocation = (SearchLocation)SearchLocationVM.SelectedItem!);
			SearchLocationVM.SelectedItem = SearchSettings.SearchLocation;

			Add(SearchType.AssemblyDef, dnSpy_Resources.SearchWindow_Assembly, DsImages.Assembly, null, VisibleMembersFlags.AssemblyDef);
			Add(SearchType.ModuleDef, dnSpy_Resources.SearchWindow_Module, DsImages.ModulePublic, null, VisibleMembersFlags.ModuleDef);
			Add(SearchType.Namespace, dnSpy_Resources.SearchWindow_Namespace, DsImages.Namespace, null, VisibleMembersFlags.Namespace);
			Add(SearchType.TypeDef, dnSpy_Resources.SearchWindow_Type, DsImages.ClassPublic, dnSpy_Resources.SearchWindow_Type_Key, VisibleMembersFlags.TypeDef);
			Add(SearchType.FieldDef, dnSpy_Resources.SearchWindow_Field, DsImages.FieldPublic, dnSpy_Resources.SearchWindow_Field_Key, VisibleMembersFlags.FieldDef);
			Add(SearchType.MethodDef, dnSpy_Resources.SearchWindow_Method, DsImages.MethodPublic, dnSpy_Resources.SearchWindow_Method_Key, VisibleMembersFlags.MethodDef);
			Add(SearchType.PropertyDef, dnSpy_Resources.SearchWindow_Property, DsImages.Property, dnSpy_Resources.SearchWindow_Property_Key, VisibleMembersFlags.PropertyDef);
			Add(SearchType.EventDef, dnSpy_Resources.SearchWindow_Event, DsImages.EventPublic, dnSpy_Resources.SearchWindow_Event_Key, VisibleMembersFlags.EventDef);
			Add(SearchType.ParamDef, dnSpy_Resources.SearchWindow_Parameter, DsImages.Parameter, dnSpy_Resources.SearchWindow_Parameter_Key, VisibleMembersFlags.ParamDef);
			Add(SearchType.Local, dnSpy_Resources.SearchWindow_Local, DsImages.LocalVariable, dnSpy_Resources.SearchWindow_Local_Key, VisibleMembersFlags.Local);
			Add(SearchType.ParamLocal, dnSpy_Resources.SearchWindow_ParameterLocal, DsImages.LocalVariable, dnSpy_Resources.SearchWindow_ParameterLocal_Key, VisibleMembersFlags.ParamDef | VisibleMembersFlags.Local);
			Add(SearchType.AssemblyRef, dnSpy_Resources.SearchWindow_AssemblyRef, DsImages.Reference, null, VisibleMembersFlags.AssemblyRef);
			Add(SearchType.ModuleRef, dnSpy_Resources.SearchWindow_ModuleRef, DsImages.Reference, null, VisibleMembersFlags.ModuleRef);
			Add(SearchType.Resource, dnSpy_Resources.SearchWindow_Resource, DsImages.Dialog, dnSpy_Resources.SearchWindow_Resource_Key, VisibleMembersFlags.Resource | VisibleMembersFlags.ResourceElement);
			Add(SearchType.GenericTypeDef, dnSpy_Resources.SearchWindow_Generic, DsImages.Template, null, VisibleMembersFlags.GenericTypeDef);
			Add(SearchType.NonGenericTypeDef, dnSpy_Resources.SearchWindow_NonGeneric, DsImages.ClassPublic, null, VisibleMembersFlags.NonGenericTypeDef);
			Add(SearchType.EnumTypeDef, dnSpy_Resources.SearchWindow_Enum, DsImages.EnumerationPublic, null, VisibleMembersFlags.EnumTypeDef);
			Add(SearchType.InterfaceTypeDef, dnSpy_Resources.SearchWindow_Interface, DsImages.InterfacePublic, null, VisibleMembersFlags.InterfaceTypeDef);
			Add(SearchType.ClassTypeDef, dnSpy_Resources.SearchWindow_Class, DsImages.ClassPublic, null, VisibleMembersFlags.ClassTypeDef);
			Add(SearchType.StructTypeDef, dnSpy_Resources.SearchWindow_Struct, DsImages.StructurePublic, null, VisibleMembersFlags.StructTypeDef);
			Add(SearchType.DelegateTypeDef, dnSpy_Resources.SearchWindow_Delegate, DsImages.DelegatePublic, null, VisibleMembersFlags.DelegateTypeDef);
			Add(SearchType.Member, dnSpy_Resources.SearchWindow_Member, DsImages.Property, dnSpy_Resources.SearchWindow_Member_Key, VisibleMembersFlags.MethodDef | VisibleMembersFlags.FieldDef | VisibleMembersFlags.PropertyDef | VisibleMembersFlags.EventDef);
			Add(SearchType.Any, dnSpy_Resources.SearchWindow_AllAbove, DsImages.ClassPublic, dnSpy_Resources.SearchWindow_AllAbove_Key, VisibleMembersFlags.TreeViewAll | VisibleMembersFlags.ParamDef | VisibleMembersFlags.Local);
			Add(SearchType.Literal, dnSpy_Resources.SearchWindow_Literal, DsImages.ConstantPublic, dnSpy_Resources.SearchWindow_Literal_Key, VisibleMembersFlags.MethodBody | VisibleMembersFlags.FieldDef | VisibleMembersFlags.ParamDef | VisibleMembersFlags.PropertyDef | VisibleMembersFlags.Resource | VisibleMembersFlags.ResourceElement | VisibleMembersFlags.Attributes);

			UpdateSearchType();
			searchSettings.PropertyChanged += SearchSettings_PropertyChanged;
		}

		void UpdateSearchType() =>
			SelectedSearchTypeVM = SearchTypeVMs.FirstOrDefault(a => a.SearchType == SearchSettings.SearchType) ??
			SearchTypeVMs.First(a => a.SearchType == SearchType.Any);

		void Add(SearchType searchType, string name, ImageReference icon, string? toolTip, VisibleMembersFlags flags) =>
			SearchTypeVMs.Add(new SearchTypeVM(searchType, name, toolTip, icon, flags));
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
				var options = new DocumentSearcherOptions {
					SearchComparer = CreateSearchComparer(),
					Filter = new FlagsDocumentTreeNodeFilter(selectedSearchTypeVM.Flags),
					SearchDecompiledData = SearchSettings.SearchDecompiledData,
				};
				fileSearcher = fileSearcherProvider.Create(options, documentTreeView);
				fileSearcher.SyntaxHighlight = SearchSettings.SyntaxHighlight;
				fileSearcher.Decompiler = Decompiler;
				fileSearcher.OnSearchCompleted += FileSearcher_OnSearchCompleted;
				fileSearcher.OnNewSearchResults += FileSearcher_OnNewSearchResults;

				switch ((SearchLocation)SearchLocationVM.SelectedItem!) {
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
		IDocumentSearcher? fileSearcher;
		bool searchCompleted;

		bool CanSearchFile(DsDocumentNode node) =>
			SearchSettings.SearchFrameworkAssemblies || !FrameworkFileUtils.IsFrameworkAssembly(node.Document.Filename, node.Document.AssemblyDef?.Name);
		IEnumerable<DsDocumentNode> GetAllFilesToSearch() =>
			documentTreeView.TreeView.Root.DataChildren.OfType<DsDocumentNode>().Where(a => CanSearchFile(a));
		IEnumerable<DsDocumentNode> GetSelectedFilesToSearch() =>
			documentTreeView.TreeView.TopLevelSelection.Select(a => a.GetDocumentNode()).Where(a => !(a is null) && CanSearchFile(a)).Distinct()!;

		IEnumerable<DsDocumentNode> GetAllFilesInSameDirToSearch() {
			var dirsEnum = GetSelectedFilesToSearch().Where(a => File.Exists(a.Document.Filename)).Select(a => Path.GetDirectoryName(a.Document.Filename));
			var dirs = new HashSet<string>(dirsEnum!, StringComparer.OrdinalIgnoreCase);
			return GetAllFilesToSearch().Where(a => File.Exists(a.Document.Filename) && dirs.Contains(Path.GetDirectoryName(a.Document.Filename)!));
		}

		IEnumerable<SearchTypeInfo> GetSelectedTypeToSearch() {
			foreach (var node in documentTreeView.TreeView.TopLevelSelection.Select(a => a.GetAncestorOrSelf<TypeNode>()).Where(a => !(a is null)).Distinct()) {
				var fileNode = node.GetDocumentNode();
				Debug2.Assert(!(fileNode is null));
				if (fileNode is null)
					continue;
				yield return new SearchTypeInfo(fileNode.Document, node!.TypeDef);
			}
		}

		void FileSearcher_OnSearchCompleted(object? sender, EventArgs e) {
			if (sender is null || sender != fileSearcher || searchCompleted)
				return;
			Debug2.Assert(!(fileSearcher is null));
			searchCompleted = true;
			SearchResults.Remove(fileSearcher.SearchingResult!);
			TooManyResults = fileSearcher.TooManyResults;
		}

		void FileSearcher_OnNewSearchResults(object? sender, SearchResultEventArgs e) {
			if (sender is null || sender != fileSearcher)
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
			if (!(fileSearcher is null)) {
				fileSearcher.Cancel();
				fileSearcher = null;
			}
			searchCompleted = false;
		}

		void SearchSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			switch (e.PropertyName) {
			case nameof(SearchSettings.SearchLocation):
				SearchLocationVM.SelectedItem = SearchSettings.SearchLocation;
				Restart();
				break;
			case nameof(SearchSettings.SearchType):
				UpdateSearchType();
				Restart();
				break;
			case nameof(SearchSettings.SyntaxHighlight):
				if (!(fileSearcher is null))
					fileSearcher.SyntaxHighlight = SearchSettings.SyntaxHighlight;
				break;
			case nameof(SearchSettings.MatchWholeWords):
			case nameof(SearchSettings.CaseSensitive):
			case nameof(SearchSettings.MatchAnySearchTerm):
			case nameof(SearchSettings.SearchDecompiledData):
			case nameof(SearchSettings.SearchFrameworkAssemblies):
				Restart();
				break;
			}
		}
	}

	sealed class SearchResult_Comparer : System.Collections.IComparer {
		public int Compare(object? x, object? y) {
			var a = x as ISearchResult;
			var b = y as ISearchResult;
			if (a is null)
				return 1;
			if (b is null)
				return -1;
			if (a == b)
				return 0;
			return a.CompareTo(b);
		}
	}
}

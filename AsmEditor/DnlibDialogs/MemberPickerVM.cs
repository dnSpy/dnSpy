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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.TreeView;
using dnSpy.Shared.UI.MVVM;
using dnSpy.Shared.UI.Search;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class MemberPickerVM : ViewModelBase {
		const int DEFAULT_DELAY_SEARCH_MS = 100;

		public IOpenAssembly OpenAssembly {
			set { openAssembly = value; }
		}
		IOpenAssembly openAssembly;

		public ICommand OpenCommand {
			get { return new RelayCommand(a => OpenNewAssembly(), a => CanOpenAssembly); }
		}

		public bool CanOpenAssembly {
			get { return true; }
			set {
				if (canOpenAssembly != value) {
					canOpenAssembly = value;
					OnPropertyChanged("CanOpenAssembly");
				}
			}
		}
		bool canOpenAssembly = true;

		public object SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnPropertyChanged("SelectedItem");
					if (value != null) {
						searchResult = null;
						OnPropertyChanged("SearchResult");
					}
					HasErrorUpdated();
				}
			}
		}
		object selectedItem;

		public object SelectedDnlibObject {
			get {
				var res = SearchResult;
				if (res != null) {
					var obj = res.Object;

					if (obj is AssemblyDef && filter.GetResult(obj as AssemblyDef).IsMatch)
						return res.DnSpyFile;
					if (obj is ModuleDef && filter.GetResult(obj as ModuleDef).IsMatch)
						return res.DnSpyFile;
					if (obj is IDnSpyFile && filter.GetResult(obj as IDnSpyFile).IsMatch)
						return (IDnSpyFile)obj;
					if (obj is string && filter.GetResult((string)obj, res.DnSpyFile).IsMatch)
						return (string)obj;
					if (obj is TypeDef && filter.GetResult(obj as TypeDef).IsMatch)
						return obj;
					if (obj is FieldDef && filter.GetResult(obj as FieldDef).IsMatch)
						return obj;
					if (obj is MethodDef && filter.GetResult(obj as MethodDef).IsMatch)
						return obj;
					if (obj is PropertyDef && filter.GetResult(obj as PropertyDef).IsMatch)
						return obj;
					if (obj is EventDef && filter.GetResult(obj as EventDef).IsMatch)
						return obj;
					if (obj is AssemblyRef && filter.GetResult((AssemblyRef)obj).IsMatch)
						return (AssemblyRef)obj;
					if (obj is ModuleRef && filter.GetResult((ModuleRef)obj).IsMatch)
						return (ModuleRef)obj;
				}

				var item = fileTreeView.TreeView.FromImplNode(SelectedItem);
				if (item != null) {
					if (item is IAssemblyFileNode && filter.GetResult((item as IAssemblyFileNode).DnSpyFile.AssemblyDef).IsMatch)
						return ((IAssemblyFileNode)item).DnSpyFile;
					else if (item is IModuleFileNode && filter.GetResult((item as IModuleFileNode).DnSpyFile.ModuleDef).IsMatch)
						return ((IModuleFileNode)item).DnSpyFile;
					else if (item is IDnSpyFileNode && filter.GetResult((item as IDnSpyFileNode).DnSpyFile).IsMatch)
						return ((IDnSpyFileNode)item).DnSpyFile;
					if (item is INamespaceNode && filter.GetResult((item as INamespaceNode).Name, ((item as INamespaceNode).TreeNode.Parent.Data as IModuleFileNode).DnSpyFile).IsMatch)
						return ((INamespaceNode)item).Name;
					if (item is ITypeNode && filter.GetResult((item as ITypeNode).TypeDef).IsMatch)
						return ((ITypeNode)item).TypeDef;
					if (item is IFieldNode && filter.GetResult((item as IFieldNode).FieldDef).IsMatch)
						return ((IFieldNode)item).FieldDef;
					if (item is IMethodNode && filter.GetResult((item as IMethodNode).MethodDef).IsMatch)
						return ((IMethodNode)item).MethodDef;
					if (item is IPropertyNode && filter.GetResult((item as IPropertyNode).PropertyDef).IsMatch)
						return ((IPropertyNode)item).PropertyDef;
					if (item is IEventNode && filter.GetResult((item as IEventNode).EventDef).IsMatch)
						return ((IEventNode)item).EventDef;
					if (item is IAssemblyReferenceNode && filter.GetResult((item as IAssemblyReferenceNode).AssemblyRef).IsMatch)
						return ((IAssemblyReferenceNode)item).AssemblyRef;
					if (item is IModuleReferenceNode && filter.GetResult((item as IModuleReferenceNode).ModuleRef).IsMatch)
						return ((IModuleReferenceNode)item).ModuleRef;
				}

				return null;
			}
		}

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
					bool hasSearchTextChanged = string.IsNullOrEmpty(searchText) != string.IsNullOrEmpty(value);
					searchText = value;
					OnPropertyChanged("SearchText");
					if (hasSearchTextChanged)
						OnPropertyChanged("HasSearchText");
					delayedSearch.Start();
				}
			}
		}
		string searchText = string.Empty;
		readonly DelayedAction delayedSearch;

		public bool HasSearchText {
			get { return !string.IsNullOrEmpty(searchText); }
		}

		public ISearchResult SearchResult {
			get { return searchResult; }
			set {
				if (searchResult != value) {
					searchResult = value;
					OnPropertyChanged("SearchResult");
					if (value != null) {
						selectedItem = null;
						OnPropertyChanged("SelectedItem");
					}
					HasErrorUpdated();
				}
			}
		}
		ISearchResult searchResult;

		public IEnumerable<ILanguage> AllLanguages {
			get { return languageManager.Languages; }
		}

		public ILanguage Language {
			get { return language; }
			set {
				if (language != value) {
					language = value;
					OnPropertyChanged("Language");
					RefreshTreeView();
				}
			}
		}
		ILanguage language;
		readonly ILanguageManager languageManager;
		readonly IFileTreeView fileTreeView;
		readonly IFileTreeNodeFilter filter;
		readonly IFileSearcherCreator fileSearcherCreator;

		public bool SyntaxHighlight { get; set; }

		public string Title {
			get {
				var text = filter.Description;
				if (!string.IsNullOrEmpty(text)) {
					if (StartsWithVowel(text))
						return string.Format("Pick an {0}", text);
					return string.Format("Pick a {0}", text);
				}
				return "Pick a Node";
			}
		}

		static bool StartsWithVowel(string text) {
			if (string.IsNullOrEmpty(text))
				return false;
			var c = char.ToUpperInvariant(text[0]);
			return c == 'A' || c == 'E' || c == 'I' || c == 'O' || c == 'U';
		}

		bool CaseSensitive { get; set; }
		bool MatchWholeWords { get; set; }
		bool MatchAnySearchTerm { get; set; }

		public MemberPickerVM(IFileSearcherCreator fileSearcherCreator, IFileTreeView fileTreeView, ILanguageManager languageManager, IFileTreeNodeFilter filter, IEnumerable<IDnSpyFile> assemblies) {
			this.fileSearcherCreator = fileSearcherCreator;
			this.languageManager = languageManager;
			this.fileTreeView = fileTreeView;
			this.language = languageManager.SelectedLanguage;
			this.filter = filter;
			this.delayedSearch = new DelayedAction(DEFAULT_DELAY_SEARCH_MS, DelayStartSearch);
			this.searchResults = new ObservableCollection<ISearchResult>();
			this.searchResultsCollectionView = (ListCollectionView)CollectionViewSource.GetDefaultView(searchResults);
			this.searchResultsCollectionView.CustomSort = new SearchResult_Comparer();

			foreach (var file in assemblies)
				fileTreeView.FileManager.ForceAdd(file, false, null);

			fileTreeView.FileManager.CollectionChanged += (s, e) => Restart();

			this.CaseSensitive = false;
			this.MatchWholeWords = false;
			this.MatchAnySearchTerm = false;
			RefreshTreeView();
		}

		public bool SelectItem(object item) {
			var node = fileTreeView.FindNode(item);
			if (node == null)
				return false;

			fileTreeView.TreeView.SelectItems(new ITreeNodeData[] { node });
			SelectedItem = fileTreeView.TreeView.ToImplNode(node);

			return true;
		}

		void RefreshTreeView() {
			fileTreeView.SetLanguage(Language);
			Restart();
		}

		void OpenNewAssembly() {
			if (openAssembly == null)
				throw new InvalidOperationException();

			var file = openAssembly.Open();
			if (file == null)
				return;

			fileTreeView.FileManager.GetOrAdd(file);
		}

		void DelayStartSearch() {
			Restart();
		}

		void StartSearch() {
			CancelSearch();
			if (string.IsNullOrEmpty(SearchText))
				SearchResults.Clear();
			else {
				var options = new FileSearcherOptions {
					SearchComparer = SearchComparerFactory.Create(SearchText, CaseSensitive, MatchWholeWords, MatchAnySearchTerm),
					Filter = filter,
					SearchDecompiledData = false,
				};
				fileSearcher = fileSearcherCreator.Create(options);
				fileSearcher.SyntaxHighlight = SyntaxHighlight;
				fileSearcher.Language = Language;
				fileSearcher.BackgroundType = BackgroundType.Search;
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

		protected override string Verify(string columnName) {
			if (columnName == "SelectedItem" || columnName == "SearchResult") {
				if (SelectedItem == null && SearchResult == null)
					return "A type must be selected.";
				if (SelectedDnlibObject == null)
					return GetErrorMessage();
				return string.Empty;
			}
			return string.Empty;
		}

		string GetErrorMessage() {
			string s = filter.Description;
			return string.IsNullOrEmpty(s) ?
				"You must select a correct node" :
				string.Format("You must select: {0}", s);
		}

		public override bool HasError {
			get {
				if (!string.IsNullOrEmpty(Verify("SelectedItem")))
					return true;
				if (!string.IsNullOrEmpty(Verify("SearchResult")))
					return true;

				return false;
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

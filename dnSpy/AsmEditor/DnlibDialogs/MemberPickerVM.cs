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
using System.Linq;
using System.Threading;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.MVVM;
using dnSpy.TreeNodes;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor.DnlibDialogs {
	sealed class MemberPickerVM : ViewModelBase {
		public IOpenAssembly OpenAssembly {
			set { openAssembly = value; }
		}
		IOpenAssembly openAssembly;

		public IMakeVisible MakeVisible {
			set { makeVisible = value; }
		}
		IMakeVisible makeVisible;

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

					if (obj is AssemblyTreeNode && filter.GetFilterResult((obj as AssemblyTreeNode).LoadedAssembly, (obj as AssemblyTreeNode).AssemblyFilterType).IsMatch)
						return ((AssemblyTreeNode)obj).LoadedAssembly;
					if (obj is LoadedAssembly && filter.GetFilterResult(obj as LoadedAssembly, (obj as LoadedAssembly).ModuleDefinition != null ? AssemblyFilterType.NetModule : AssemblyFilterType.NonNetFile).IsMatch)
						return (LoadedAssembly)obj;
					if (obj is string && filter.GetFilterResult((string)obj, res.LoadedAssembly).IsMatch)
						return (string)obj;
					if (obj is TypeDef && filter.GetFilterResult(obj as TypeDef).IsMatch)
						return obj;
					if (obj is FieldDef && filter.GetFilterResult(obj as FieldDef).IsMatch)
						return obj;
					if (obj is MethodDef && filter.GetFilterResult(obj as MethodDef).IsMatch)
						return obj;
					if (obj is PropertyDef && filter.GetFilterResult(obj as PropertyDef).IsMatch)
						return obj;
					if (obj is EventDef && filter.GetFilterResult(obj as EventDef).IsMatch)
						return obj;
					if (obj is AssemblyRef && filter.GetFilterResult((AssemblyRef)obj).IsMatch)
						return (AssemblyRef)obj;
					if (obj is ModuleRef && filter.GetFilterResult((ModuleRef)obj).IsMatch)
						return (ModuleRef)obj;
				}

				var item = SelectedItem;
				if (item != null) {
					if (item is AssemblyTreeNode && filter.GetFilterResult((item as AssemblyTreeNode).LoadedAssembly, (item as AssemblyTreeNode).AssemblyFilterType).IsMatch)
						return ((AssemblyTreeNode)item).LoadedAssembly;
					if (item is NamespaceTreeNode && filter.GetFilterResult((item as NamespaceTreeNode).Name, ((item as NamespaceTreeNode).Parent as AssemblyTreeNode).LoadedAssembly).IsMatch)
						return ((NamespaceTreeNode)item).Name;
					if (item is TypeTreeNode && filter.GetFilterResult((item as TypeTreeNode).TypeDefinition).IsMatch)
						return ((TypeTreeNode)item).TypeDefinition;
					if (item is FieldTreeNode && filter.GetFilterResult((item as FieldTreeNode).FieldDefinition).IsMatch)
						return ((FieldTreeNode)item).FieldDefinition;
					if (item is MethodTreeNode && filter.GetFilterResult((item as MethodTreeNode).MethodDefinition).IsMatch)
						return ((MethodTreeNode)item).MethodDefinition;
					if (item is PropertyTreeNode && filter.GetFilterResult((item as PropertyTreeNode).PropertyDefinition).IsMatch)
						return ((PropertyTreeNode)item).PropertyDefinition;
					if (item is EventTreeNode && filter.GetFilterResult((item as EventTreeNode).EventDefinition).IsMatch)
						return ((EventTreeNode)item).EventDefinition;
					if (item is AssemblyReferenceTreeNode && filter.GetFilterResult((item as AssemblyReferenceTreeNode).AssemblyNameReference).IsMatch)
						return ((AssemblyReferenceTreeNode)item).AssemblyNameReference;
					if (item is ModuleReferenceTreeNode && filter.GetFilterResult((item as ModuleReferenceTreeNode).ModuleReference).IsMatch)
						return ((ModuleReferenceTreeNode)item).ModuleReference;
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

		public string SearchText {
			get { return searchText; }
			set {
				if (searchText != value) {
					bool hasSearchTextChanged = string.IsNullOrEmpty(searchText) != string.IsNullOrEmpty(value);
					searchText = value;
					OnPropertyChanged("SearchText");
					if (hasSearchTextChanged)
						OnPropertyChanged("HasSearchText");
					RestartSearch();
				}
			}
		}
		string searchText = string.Empty;

		public bool HasSearchText {
			get { return !string.IsNullOrEmpty(searchText); }
		}

		public object SearchItemsSource {
			get { return searchItemsSource; }
			set {
				if (searchItemsSource != value) {
					searchItemsSource = value;
					OnPropertyChanged("SearchItemsSource");
				}
			}
		}
		object searchItemsSource;

		public SearchResult SearchResult {
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
		SearchResult searchResult;

		public IEnumerable<Language> AllLanguages {
			get { return Languages.AllLanguages; }
		}

		public Language Language {
			get { return language; }
			set {
				if (language != value) {
					language = value;
					OnPropertyChanged("Language");
					CreateNewFilterSettings();
				}
			}
		}
		Language language;

		public AssemblyListTreeNode AssemblyListTreeNode {
			get { return assemblyListTreeNode; }
		}
		AssemblyListTreeNode assemblyListTreeNode;
		readonly AssemblyList assemblyList;

		ITreeViewNodeFilter filter;
		readonly ITreeViewNodeFilter origFilter;

		public string Title {
			get {
				var text = filter.Text;
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

		public bool ShowInternalApi {
			get { return showInternalApi; }
			set {
				if (showInternalApi != value) {
					showInternalApi = value;
					OnPropertyChanged("ShowInternalApi");
					CreateNewFilterSettings();
					if (SelectedItem != null) {
						if (makeVisible == null)
							throw new InvalidOperationException();
						makeVisible.ScrollIntoView(SelectedItem);
					}
				}
			}
		}
		bool showInternalApi;

		public MemberPickerVM(Language language, ITreeViewNodeFilter filter, IEnumerable<LoadedAssembly> assemblies) {
			this.Language = language;
			this.ShowInternalApi = true;
			this.filter = filter;
			this.origFilter = filter;

			assemblyList = new AssemblyList("Member Picker List", false);
			foreach (var asm in assemblies)
				assemblyList.ForceAddAssemblyToList(asm, true, false, -1, false);

			this.assemblyListTreeNode = new AssemblyListTreeNode(assemblyList);
			this.assemblyListTreeNode.DisableDrop = true;
			if (assemblyListTreeNode.Children.Count > 0)
				SelectedItem = assemblyListTreeNode.Children[0];

			// Make sure we don't hook this event before the assembly list node because we depend
			// on the new asm node being present when we restart the search.
			assemblyList.CollectionChanged += (s, e) => RestartSearch();

			CreateNewFilterSettings();
		}

		public bool SelectItem(object item) {
			if (makeVisible == null)
				throw new InvalidOperationException("Call SelectItem(item) after DataContext has been initialized!");

			var node = assemblyListTreeNode.FindTreeNode(item);
			if (node == null)
				return false;

			SelectedItem = node;
			makeVisible.ScrollIntoView(node);
			return true;
		}

		void CreateNewFilterSettings() {
			if (assemblyListTreeNode != null) {
				assemblyListTreeNode.FilterSettings = new FilterSettings(origFilter, Language, ShowInternalApi);
				filter = assemblyListTreeNode.FilterSettings.Filter;
				RestartSearch();
			}
		}

		void OpenNewAssembly() {
			if (openAssembly == null)
				throw new InvalidOperationException();

			var asm = openAssembly.Open();
			if (asm == null)
				return;

			assemblyList.AddAssembly(asm, true, false);
		}

		RunningSearch currentSearch;
		void StartSearch(string searchTerm) {
			TooManyResults = false;
			if (currentSearch != null)
				currentSearch.Cancel();
			if (string.IsNullOrEmpty(searchTerm)) {
				currentSearch = null;
				SearchItemsSource = null;
			}
			else {
				currentSearch = new RunningSearch(AssemblyListTreeNode.Children.Cast<AssemblyTreeNode>(), RunningSearch.CreateSearchComparer(searchTerm), filter, Language);
				SearchItemsSource = currentSearch.Results;
				currentSearch.OnSearchEnded += RunningSearch_OnSearchEnded;
				new Thread(currentSearch.Run).Start();
			}
		}

		void RunningSearch_OnSearchEnded(object sender, EventArgs e) {
			if (currentSearch == null || currentSearch != sender)
				return;

			TooManyResults = currentSearch.TooManyResults;
		}

		void RestartSearch() {
			StartSearch(SearchText);
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
			string s = filter.Text;
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
}

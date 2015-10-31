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
using dnSpy.Files;
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

					if (obj is AssemblyTreeNode && filter.GetFilterResult((obj as AssemblyTreeNode).DnSpyFile, (obj as AssemblyTreeNode).AssemblyFilterType).IsMatch)
						return ((AssemblyTreeNode)obj).DnSpyFile;
					if (obj is DnSpyFile && filter.GetFilterResult(obj as DnSpyFile, (obj as DnSpyFile).ModuleDef != null ? AssemblyFilterType.NetModule : AssemblyFilterType.NonNetFile).IsMatch)
						return (DnSpyFile)obj;
					if (obj is string && filter.GetFilterResult((string)obj, res.DnSpyFile).IsMatch)
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
					if (item is AssemblyTreeNode && filter.GetFilterResult((item as AssemblyTreeNode).DnSpyFile, (item as AssemblyTreeNode).AssemblyFilterType).IsMatch)
						return ((AssemblyTreeNode)item).DnSpyFile;
					if (item is NamespaceTreeNode && filter.GetFilterResult((item as NamespaceTreeNode).Name, ((item as NamespaceTreeNode).Parent as AssemblyTreeNode).DnSpyFile).IsMatch)
						return ((NamespaceTreeNode)item).Name;
					if (item is TypeTreeNode && filter.GetFilterResult((item as TypeTreeNode).TypeDef).IsMatch)
						return ((TypeTreeNode)item).TypeDef;
					if (item is FieldTreeNode && filter.GetFilterResult((item as FieldTreeNode).FieldDef).IsMatch)
						return ((FieldTreeNode)item).FieldDef;
					if (item is MethodTreeNode && filter.GetFilterResult((item as MethodTreeNode).MethodDef).IsMatch)
						return ((MethodTreeNode)item).MethodDef;
					if (item is PropertyTreeNode && filter.GetFilterResult((item as PropertyTreeNode).PropertyDef).IsMatch)
						return ((PropertyTreeNode)item).PropertyDef;
					if (item is EventTreeNode && filter.GetFilterResult((item as EventTreeNode).EventDef).IsMatch)
						return ((EventTreeNode)item).EventDef;
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

		public DnSpyFileListTreeNode DnSpyFileListTreeNode {
			get { return dnSpyFileListTreeNode; }
		}
		DnSpyFileListTreeNode dnSpyFileListTreeNode;
		readonly DnSpyFileList dnSpyFileList;

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

		public MemberPickerVM(IDnSpyFileListOptions options, Language language, ITreeViewNodeFilter filter, IEnumerable<DnSpyFile> assemblies) {
			this.Language = language;
			this.filter = filter;
			this.origFilter = filter;

			dnSpyFileList = new DnSpyFileList(options, "Member Picker List");
			foreach (var file in assemblies)
				dnSpyFileList.ForceAddFileToList(file, true, false, -1, false);

			this.dnSpyFileListTreeNode = new DnSpyFileListTreeNode(dnSpyFileList);
			this.dnSpyFileListTreeNode.DisableDrop = true;
			if (dnSpyFileListTreeNode.Children.Count > 0)
				SelectedItem = dnSpyFileListTreeNode.Children[0];

			// Make sure we don't hook this event before the assembly list node because we depend
			// on the new asm node being present when we restart the search.
			dnSpyFileList.CollectionChanged += (s, e) => RestartSearch();

			CreateNewFilterSettings();
		}

		public bool SelectItem(object item) {
			if (makeVisible == null)
				throw new InvalidOperationException("Call SelectItem(item) after DataContext has been initialized!");

			var node = dnSpyFileListTreeNode.FindTreeNode(item);
			if (node == null)
				return false;

			SelectedItem = node;
			makeVisible.ScrollIntoView(node);
			return true;
		}

		void CreateNewFilterSettings() {
			if (dnSpyFileListTreeNode != null) {
				dnSpyFileListTreeNode.FilterSettings = new FilterSettings(origFilter, Language);
				filter = dnSpyFileListTreeNode.FilterSettings.Filter;
				RestartSearch();
			}
		}

		void OpenNewAssembly() {
			if (openAssembly == null)
				throw new InvalidOperationException();

			var file = openAssembly.Open();
			if (file == null)
				return;

			dnSpyFileList.AddFile(file, true, false);
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
				currentSearch = new RunningSearch(DnSpyFileListTreeNode.Children.Cast<AssemblyTreeNode>(), RunningSearch.CreateSearchComparer(searchTerm), filter, Language);
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

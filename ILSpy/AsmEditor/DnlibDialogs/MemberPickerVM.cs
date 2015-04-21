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
using System.Threading;
using System.Windows.Input;
using dnlib.DotNet;
using ICSharpCode.ILSpy.AsmEditor.ViewHelpers;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.ILSpy.TreeNodes.Filters;

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	sealed class MemberPickerVM : ViewModelBase
	{
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
				}
			}
		}
		object selectedItem;

		public object SelectedDnlibObject {
			get {
				var res = SearchResult;
				if (res != null) {
					var mr = res.Member;

					if (mr is TypeDef && filter.GetFilterResult(mr as TypeDef).IsMatch)
						return mr;
					if (mr is FieldDef && filter.GetFilterResult(mr as FieldDef).IsMatch)
						return mr;
					if (mr is MethodDef && filter.GetFilterResult(mr as MethodDef).IsMatch)
						return mr;
					if (mr is PropertyDef && filter.GetFilterResult(mr as PropertyDef).IsMatch)
						return mr;
					if (mr is EventDef && filter.GetFilterResult(mr as EventDef).IsMatch)
						return mr;
				}

				var item = SelectedItem;
				if (item != null) {
					if (item is AssemblyTreeNode && filter.GetFilterResult((item as AssemblyTreeNode).LoadedAssembly, (item as AssemblyTreeNode).AssemblyFilterType).IsMatch)
						return ((AssemblyTreeNode)item).LoadedAssembly;
					if (item is NamespaceTreeNode && filter.GetFilterResult((item as NamespaceTreeNode).Name).IsMatch)
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

		public string SearchText {
			get { return searchText; }
			set {
				if (searchText != value) {
					bool hasSearchTextChanged = string.IsNullOrEmpty(searchText) != string.IsNullOrEmpty(value);
					searchText = value;
					OnPropertyChanged("SearchText");
					if (hasSearchTextChanged)
						OnPropertyChanged("HasSearchText");
					StartSearch(SearchText);
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

		readonly ITreeViewNodeFilter filter;

		public string Title {
			get {
				var text = filter.Text;
				if (!string.IsNullOrEmpty(text))
					return string.Format("Pick a {0}", text);
				return "Pick a Node";
			}
		}

		public bool ShowInternalApi {
			get { return showInternalApi; }
			set {
				if (showInternalApi != value) {
					showInternalApi = value;
					OnPropertyChanged("ShowInternalApi");
					CreateNewFilterSettings();
				}
			}
		}
		bool showInternalApi;

		public MemberPickerVM(Language language, ITreeViewNodeFilter filter)
			: this(language, filter, MainWindow.Instance.CurrentAssemblyList.GetAssemblies())
		{
		}

		public MemberPickerVM(Language language, ITreeViewNodeFilter filter, IEnumerable<LoadedAssembly> assemblies)
		{
			this.Language = language;
			this.ShowInternalApi = true;
			this.filter = filter;

			assemblyList = new AssemblyList("Member Picker List", false);
			foreach (var asm in assemblies)
				assemblyList.ForceAddAssemblyToList(asm, true, false);

			this.assemblyListTreeNode = new AssemblyListTreeNode(assemblyList);
			this.assemblyListTreeNode.DisableDrop = true;
			if (assemblyListTreeNode.Children.Count > 0)
				SelectedItem = assemblyListTreeNode.Children[0];
			CreateNewFilterSettings();
		}

		void CreateNewFilterSettings()
		{
			if (assemblyListTreeNode != null)
				assemblyListTreeNode.FilterSettings = new FilterSettings(filter, Language, ShowInternalApi);
		}

		void OpenNewAssembly()
		{
			if (openAssembly == null)
				throw new InvalidOperationException();

			var asm = openAssembly.Open();
			if (asm == null)
				return;

			assemblyList.AddAssembly(asm, true, false);
		}

		SearchPane.RunningSearch currentSearch;
		void StartSearch(string searchTerm)
		{
			if (currentSearch != null)
				currentSearch.Cancel();
			if (string.IsNullOrEmpty(searchTerm)) {
				currentSearch = null;
				SearchItemsSource = null;
			}
			else {
				currentSearch = new SearchPane.RunningSearch(assemblyList.GetAllModules(), searchTerm, GetSearchMode(), Language, false);
				SearchItemsSource = currentSearch.Results;
				new Thread(currentSearch.Run).Start();
			}
		}

		SearchMode GetSearchMode()
		{
			//TODO: Update searcher. SearchMode should be flags and it should support more stuff.
			//		Eg. the Member value should be split up into Field, Method, etc. It also shouldn't
			//		be a nested class of SearchPane!
			if (filter.GetFilterResult((TypeDef)null).IsMatch)//TODO: Hack until above has been fixed
				return SearchMode.Type;
			return SearchMode.Member;
		}

		protected override string Verify(string columnName)
		{
			if (columnName == "SelectedItem" || columnName == "SearchResult") {
				if (SelectedItem == null && SearchResult == null)
					return "A type must be selected.";
				if (SelectedDnlibObject == null)
					return GetErrorMessage();
				return string.Empty;
			}
			return string.Empty;
		}

		string GetErrorMessage()
		{
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

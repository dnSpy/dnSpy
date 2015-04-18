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
using System.ComponentModel;
using System.Threading;
using System.Windows.Input;
using dnlib.DotNet;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.AsmEditor.TypeSig
{
	sealed class MemberPickerVM : ViewModelBase
	{
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

					if ((visibleMembersFlags & VisibleMembersFlags.TypeDef) != 0 && mr is TypeDef)
						return mr;
					if ((visibleMembersFlags & VisibleMembersFlags.FieldDef) != 0 && mr is FieldDef)
						return mr;
					if ((visibleMembersFlags & VisibleMembersFlags.MethodDef) != 0 && mr is MethodDef)
						return mr;
					if ((visibleMembersFlags & VisibleMembersFlags.PropertyDef) != 0 && mr is PropertyDef)
						return mr;
					if ((visibleMembersFlags & VisibleMembersFlags.EventDef) != 0 && mr is EventDef)
						return mr;
				}

				var item = SelectedItem;
				if (item != null) {
					if ((visibleMembersFlags & VisibleMembersFlags.AssemblyDef) != 0 && item is AssemblyTreeNode && ((AssemblyTreeNode)item).IsAssembly)
						return ((AssemblyTreeNode)item).LoadedAssembly;
					if ((visibleMembersFlags & VisibleMembersFlags.ModuleDef) != 0 && item is AssemblyTreeNode && ((AssemblyTreeNode)item).IsModule)
						return ((AssemblyTreeNode)item).LoadedAssembly;
					if ((visibleMembersFlags & VisibleMembersFlags.Namespace) != 0 && item is NamespaceTreeNode)
						return ((NamespaceTreeNode)item).Name;
					if ((visibleMembersFlags & VisibleMembersFlags.TypeDef) != 0 && item is TypeTreeNode)
						return ((TypeTreeNode)item).TypeDefinition;
					if ((visibleMembersFlags & VisibleMembersFlags.FieldDef) != 0 && item is FieldTreeNode)
						return ((FieldTreeNode)item).FieldDefinition;
					if ((visibleMembersFlags & VisibleMembersFlags.MethodDef) != 0 && item is MethodTreeNode)
						return ((MethodTreeNode)item).MethodDefinition;
					if ((visibleMembersFlags & VisibleMembersFlags.PropertyDef) != 0 && item is PropertyTreeNode)
						return ((PropertyTreeNode)item).PropertyDefinition;
					if ((visibleMembersFlags & VisibleMembersFlags.EventDef) != 0 && item is EventTreeNode)
						return ((EventTreeNode)item).EventDefinition;
					if ((visibleMembersFlags & VisibleMembersFlags.AssemblyRef) != 0 && item is AssemblyReferenceTreeNode)
						return ((AssemblyReferenceTreeNode)item).AssemblyNameReference;
					if ((visibleMembersFlags & VisibleMembersFlags.ModuleRef) != 0 && item is ModuleReferenceTreeNode)
						return ((ModuleReferenceTreeNode)item).ModuleReference;
				}

				return null;
			}
		}

		public string SearchText {
			get { return searchText; }
			set {
				if (searchText != value) {
					searchText = value;
					OnPropertyChanged("SearchText");
					StartSearch(SearchText);
				}
			}
		}
		string searchText;

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

		public VisibleMembersFlags VisibleMembersFlags {
			get { return visibleMembersFlags; }
		}
		readonly VisibleMembersFlags visibleMembersFlags;

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

		public MemberPickerVM(Language language, VisibleMembersFlags visibleMembersFlags)
			: this(language, visibleMembersFlags, MainWindow.Instance.CurrentAssemblyList.GetAssemblies())
		{
		}

		public MemberPickerVM(Language language, VisibleMembersFlags visibleMembersFlags, IEnumerable<LoadedAssembly> assemblies)
		{
			this.language = language;
			this.showInternalApi = true;
			this.visibleMembersFlags = visibleMembersFlags;

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
			this.assemblyListTreeNode.FilterSettings = new FilterSettings(visibleMembersFlags, language, ShowInternalApi);
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
			if ((VisibleMembersFlags & ILSpy.VisibleMembersFlags.TypeDef) != 0)
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
			int count;
			string s = visibleMembersFlags.GetListString(out count);
			return count == 1 ?
				string.Format("You must select a {0}", s) :
				string.Format("You must select one of {0}", s);
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

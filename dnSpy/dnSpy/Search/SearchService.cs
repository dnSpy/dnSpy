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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Search {
	interface ISearchService : IUIObjectProvider {
		/// <summary>
		/// Called when it's been added to the UI
		/// </summary>
		void OnShow();

		/// <summary>
		/// Called when it's been closed
		/// </summary>
		void OnClose();

		/// <summary>
		/// Gives focus to the focused element
		/// </summary>
		void Focus();

		/// <summary>
		/// Follows the reference
		/// </summary>
		/// <param name="searchResult">Search result</param>
		/// <param name="newTab">true to show it in a new tab</param>
		void FollowResult(ISearchResult searchResult, bool newTab);

		SearchLocation SearchLocation { get; set; }
		SearchType SearchType { get; set; }
		string SearchText { get; set; }
	}

	[Export(typeof(ISearchService))]
	sealed class SearchService : ISearchService {
		readonly SearchControl searchControl;
		readonly SearchControlVM vmSearch;
		readonly IDocumentTabService documentTabService;

		public SearchLocation SearchLocation {
			get { return (SearchLocation)vmSearch.SearchLocationVM.SelectedItem; }
			set { vmSearch.SearchLocationVM.SelectedItem = value; }
		}

		public SearchType SearchType {
			get { return vmSearch.SelectedSearchTypeVM.SearchType; }
			set { vmSearch.SelectedSearchTypeVM = vmSearch.SearchTypeVMs.First(a => a.SearchType == value); }
		}

		public string SearchText {
			get { return vmSearch.SearchText; }
			set { vmSearch.SearchText = value; }
		}

		public IInputElement FocusedElement => searchControl.SearchTextBox;

		public FrameworkElement ZoomElement => searchControl;

		public object UIObject => searchControl;

		sealed class GuidObjectsProvider : IGuidObjectsProvider {
			public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsProviderArgs args) {
				var listBox = (ListBox)args.CreatorObject.Object;
				var searchResult = listBox.SelectedItem as ISearchResult;
				if (searchResult != null) {
					yield return new GuidObject(MenuConstants.GUIDOBJ_SEARCHRESULT_GUID, searchResult);
					var @ref = searchResult.Reference;
					if (@ref != null)
						yield return new GuidObject(MenuConstants.GUIDOBJ_CODE_REFERENCE_GUID, new TextReference(@ref));
				}
			}
		}

		[ImportingConstructor]
		SearchService(IDecompilerService decompilerService, ISearchSettings searchSettings, IDocumentSearcherProvider fileSearcherProvider, IMenuService menuService, IWpfCommandService wpfCommandService, IDocumentTabService documentTabService, IClassificationFormatMapService classificationFormatMapService) {
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.Search);
			this.documentTabService = documentTabService;
			this.searchControl = new SearchControl();
			this.vmSearch = new SearchControlVM(fileSearcherProvider, documentTabService.DocumentTreeView, searchSettings) {
				Decompiler = decompilerService.Decompiler,
			};
			this.searchControl.DataContext = this.vmSearch;

			menuService.InitializeContextMenu(this.searchControl.ListBox, MenuConstants.GUIDOBJ_SEARCH_GUID, new GuidObjectsProvider());
			wpfCommandService.Add(ControlConstants.GUID_SEARCH_CONTROL, this.searchControl);
			wpfCommandService.Add(ControlConstants.GUID_SEARCH_LISTBOX, this.searchControl.ListBox);
			decompilerService.DecompilerChanged += DecompilerService_DecompilerChanged;
			classificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
			searchSettings.PropertyChanged += SearchSettings_PropertyChanged;
			documentTabService.DocumentTreeView.DocumentService.CollectionChanged += DocumentService_CollectionChanged;

			this.searchControl.SearchListBoxDoubleClick += (s, e) => FollowSelectedReference();
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_SEARCH_LISTBOX);
			var command = new RelayCommand(a => FollowSelectedReference());
			cmds.Add(command, ModifierKeys.None, Key.Enter);
			cmds.Add(command, ModifierKeys.Control, Key.Enter);
			cmds.Add(command, ModifierKeys.Shift, Key.Enter);

			Add(SearchType.TypeDef, Key.T);
			Add(SearchType.FieldDef, Key.F);
			Add(SearchType.MethodDef, Key.M);
			Add(SearchType.PropertyDef, Key.P);
			Add(SearchType.EventDef, Key.E);
			Add(SearchType.ParamDef, Key.J);
			Add(SearchType.Local, Key.I);
			Add(SearchType.ParamLocal, Key.N);
			Add(SearchType.Resource, Key.R);
			Add(SearchType.Member, Key.U);
			Add(SearchType.Any, Key.B);
			Add(SearchType.Literal, Key.L);

			Add(SearchLocation.AllFiles, Key.G);
			Add(SearchLocation.SelectedFiles, Key.S);
			Add(SearchLocation.AllFilesInSameDir, Key.D);
			Add(SearchLocation.SelectedType, Key.Q);
		}

		void Add(SearchType searchType, Key key) {
			var command = new RelayCommand(a => {
				this.vmSearch.SelectedSearchTypeVM = this.vmSearch.SearchTypeVMs.First(b => b.SearchType == searchType);
				if (!this.searchControl.SearchTextBox.IsKeyboardFocusWithin)
					this.searchControl.SearchTextBox.SelectAll();
				this.searchControl.SearchTextBox.Focus();
			});
			this.searchControl.InputBindings.Add(new KeyBinding(command, new KeyGesture(key, ModifierKeys.Control)));
		}

		void Add(SearchLocation loc, Key key) {
			var command = new RelayCommand(a => {
				this.vmSearch.SearchLocationVM.SelectedItem = loc;
				if (!this.searchControl.SearchTextBox.IsKeyboardFocusWithin)
					this.searchControl.SearchTextBox.SelectAll();
				this.searchControl.SearchTextBox.Focus();
			});
			this.searchControl.InputBindings.Add(new KeyBinding(command, new KeyGesture(key, ModifierKeys.Control)));
		}

		void DocumentService_CollectionChanged(object sender, NotifyDocumentCollectionChangedEventArgs e) {
			switch (e.Type) {
			case NotifyDocumentCollectionType.Clear:
				vmSearch.Clear();
				break;

			case NotifyDocumentCollectionType.Add:
				// Only restart the search if the file was explicitly loaded by the user. Assembly
				// resolves shouldn't restart the search since it happens too often.
				if (e.Documents.Any(a => !a.IsAutoLoaded))
					vmSearch.Restart();
				break;

			case NotifyDocumentCollectionType.Remove:
				// We only need to restart the search if the search has not completed or if any of
				// the search results contain a reference to the assembly.
				vmSearch.Restart();
				break;

			default:
				Debug.Fail("Unknown NotifyFileCollectionType");
				break;
			}
		}

		void DecompilerService_DecompilerChanged(object sender, EventArgs e) {
			var decompilerService = (IDecompilerService)sender;
			vmSearch.Decompiler = decompilerService.Decompiler;
			RefreshSearchResults();
		}

		void RefreshUI() {
			RefreshSearchResults();
			RefreshComboBox();
		}

		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) => RefreshUI();

		void SearchSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var searchSettings = (ISearchSettings)sender;
			if (e.PropertyName == nameof(searchSettings.SyntaxHighlight))
				RefreshSearchResults();
		}

		void RefreshSearchResults() {
			foreach (var vm in vmSearch.SearchResults)
				vm.RefreshUI();
		}

		void RefreshComboBox() {
			foreach (var vm in vmSearch.SearchTypeVMs)
				vm.RefreshUI();
		}

		public void Focus() {
			this.searchControl.SearchTextBox.SelectAll();
			this.searchControl.SearchTextBox.Focus();
		}

		public void OnShow() {
			this.vmSearch.CanSearch = true;
		}

		public void OnClose() {
			this.vmSearch.CanSearch = false;
			this.vmSearch.Clear();
		}

		void FollowSelectedReference() {
			bool newTab = Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Shift;
			FollowResult(this.searchControl.ListBox.SelectedItem as ISearchResult, newTab);
		}

		public void FollowResult(ISearchResult searchResult, bool newTab) {
			var @ref = searchResult == null ? null : searchResult.Reference;
			if (@ref != null) {
				documentTabService.FollowReference(@ref, newTab, true, a => {
					if (!a.HasMovedCaret && a.Success) {
						var bodyResult = searchResult.ObjectInfo as BodyResult;
						if (bodyResult != null)
							a.HasMovedCaret = GoTo(a.Tab, searchResult.Object as MethodDef, bodyResult.ILOffset);
					}
				});
			}
		}

		bool GoTo(IDocumentTab tab, MethodDef method, uint ilOffset) {
			var documentViewer = tab.TryGetDocumentViewer();
			if (documentViewer == null || method == null)
				return false;
			var methodDebugService = documentViewer.GetMethodDebugService();
			var methodStatement = methodDebugService.FindByCodeOffset(method, ilOffset);
			if (methodStatement == null)
				return false;

			documentViewer.MoveCaretToPosition(methodStatement.Value.Statement.TextSpan.Start);
			return true;
		}
	}
}

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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.Themes;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Search {
	interface ISearchManager {
		/// <summary>
		/// The UI object
		/// </summary>
		object UIObject { get; }

		/// <summary>
		/// Gets the element that should get focus when the content is selected
		/// </summary>
		IInputElement FocusedElement { get; }

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
	}

	[Export, Export(typeof(ISearchManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class SearchManager : ISearchManager {
		readonly SearchControl searchControl;
		readonly SearchControlVM vmSearch;
		readonly IFileTabManager fileTabManager;

		public IInputElement FocusedElement {
			get { return searchControl.SearchTextBox; }
		}

		public object UIObject {
			get { return searchControl; }
		}

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			public IEnumerable<GuidObject> GetGuidObjects(GuidObject creatorObject, bool openedFromKeyboard) {
				var listBox = (ListBox)creatorObject.Object;
				var searchResult = listBox.SelectedItem as ISearchResult;
				if (searchResult != null) {
					var @ref = searchResult.Reference;
					if (@ref != null)
						yield return new GuidObject(MenuConstants.GUIDOBJ_CODE_REFERENCE_GUID, new CodeReference(@ref));
				}
			}
		}

		[ImportingConstructor]
		SearchManager(IImageManager imageManager, ILanguageManager languageManager, IThemeManager themeManager, ISearchSettings searchSettings, IFileSearcherCreator fileSearcherCreator, IMenuManager menuManager, IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager) {
			this.fileTabManager = fileTabManager;
			this.searchControl = new SearchControl();
			this.vmSearch = new SearchControlVM(imageManager, fileSearcherCreator, fileTabManager.FileTreeView) {
				SyntaxHighlight = searchSettings.SyntaxHighlight,
				Language = languageManager.SelectedLanguage,
				BackgroundType = BackgroundType.Search,
				SearchDecompiledData = true,
			};
			this.searchControl.DataContext = this.vmSearch;

			menuManager.InitializeContextMenu(this.searchControl.ListBox, MenuConstants.GUIDOBJ_SEARCH_GUID, new GuidObjectsCreator());
			wpfCommandManager.Add(CommandConstants.GUID_SEARCH_CONTROL, this.searchControl);
			wpfCommandManager.Add(CommandConstants.GUID_SEARCH_LISTBOX, this.searchControl.ListBox);
			languageManager.LanguageChanged += LanguageManager_LanguageChanged;
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;
			searchSettings.PropertyChanged += SearchSettings_PropertyChanged;
			fileTabManager.FileTreeView.FileManager.CollectionChanged += FileManager_CollectionChanged;

			this.searchControl.SearchListBoxDoubleClick += (s, e) => FollowSelectedReference();
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_SEARCH_LISTBOX);
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

		void FileManager_CollectionChanged(object sender, NotifyFileCollectionChangedEventArgs e) {
			switch (e.Type) {
			case NotifyFileCollectionType.Clear:
				vmSearch.Clear();
				break;

			case NotifyFileCollectionType.Add:
				// Only restart the search if the file was explicitly loaded by the user. Assembly
				// resolves shouldn't restart the search since it happens too often.
				if (e.Files.Any(a => !a.IsAutoLoaded))
					vmSearch.Restart();
				break;

			case NotifyFileCollectionType.Remove:
				// We only need to restart the search if the search has not completed or if any of
				// the search results contain a reference to the assembly.
				vmSearch.Restart();
				break;

			default:
				Debug.Fail("Unknown NotifyFileCollectionType");
				break;
			}
		}

		void LanguageManager_LanguageChanged(object sender, EventArgs e) {
			var languageManager = (ILanguageManager)sender;
			vmSearch.Language = languageManager.SelectedLanguage;
			RefreshSearchResults();
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			RefreshSearchResults();
			RefreshComboBox();
		}

		void SearchSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var searchSettings = (ISearchSettings)sender;
			if (e.PropertyName == "SyntaxHighlight") {
				vmSearch.SyntaxHighlight = searchSettings.SyntaxHighlight;
				RefreshSearchResults();
			}
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
			var res = this.searchControl.ListBox.SelectedItem as ISearchResult;
			var @ref = res == null ? null : res.Reference;
			if (@ref != null) {
				bool newTab = Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Shift;
				fileTabManager.FollowReference(@ref, newTab);
			}
		}
	}
}

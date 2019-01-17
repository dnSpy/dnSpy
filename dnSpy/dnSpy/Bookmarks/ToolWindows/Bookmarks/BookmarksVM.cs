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
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Bookmarks.Impl;
using dnSpy.Bookmarks.UI;
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Controls.ToolWindows;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.ToolWindows.Search;
using dnSpy.Properties;
using dnSpy.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Bookmarks.ToolWindows.Bookmarks {
	interface IBookmarksVM : IGridViewColumnDescsProvider {
		bool IsOpen { get; set; }
		bool IsVisible { get; set; }
		BulkObservableCollection<BookmarkVM> AllItems { get; }
		ObservableCollection<BookmarkVM> SelectedItems { get; }
		void ResetSearchSettings();
		string GetSearchHelpText();
		event EventHandler OnShowChanged;
		event EventHandler AllItemsFiltered;
		IEnumerable<BookmarkVM> Sort(IEnumerable<BookmarkVM> bookmarks);
	}

	[Export(typeof(IBookmarksVM))]
	sealed class BookmarksVM : ViewModelBase, IBookmarksVM, ILazyToolWindowVM, IComparer<BookmarkVM> {
		public BulkObservableCollection<BookmarkVM> AllItems { get; }
		public ObservableCollection<BookmarkVM> SelectedItems { get; }
		public GridViewColumnDescs Descs { get; }

		public bool IsOpen {
			get => lazyToolWindowVMHelper.IsOpen;
			set => lazyToolWindowVMHelper.IsOpen = value;
		}

		public bool IsVisible {
			get => lazyToolWindowVMHelper.IsVisible;
			set => lazyToolWindowVMHelper.IsVisible = value;
		}

		public string FilterText {
			get => filterText;
			set {
				if (filterText == value)
					return;
				filterText = value;
				OnPropertyChanged(nameof(FilterText));
				FilterList_UI(filterText);
			}
		}
		string filterText = string.Empty;

		public bool SomethingMatched => !nothingMatched;
		public bool NothingMatched {
			get => nothingMatched;
			set {
				if (nothingMatched == value)
					return;
				nothingMatched = value;
				OnPropertyChanged(nameof(NothingMatched));
				OnPropertyChanged(nameof(SomethingMatched));
			}
		}
		bool nothingMatched;

		IEditValueProvider NameEditValueProvider {
			get {
				bookmarkContext.UIDispatcher.VerifyAccess();
				if (nameEditValueProvider == null)
					nameEditValueProvider = editValueProviderService.Create(ContentTypes.BookmarksWindowName, Array.Empty<string>());
				return nameEditValueProvider;
			}
		}
		IEditValueProvider nameEditValueProvider;

		IEditValueProvider LabelsEditValueProvider {
			get {
				bookmarkContext.UIDispatcher.VerifyAccess();
				if (labelsEditValueProvider == null)
					labelsEditValueProvider = editValueProviderService.Create(ContentTypes.BookmarksWindowLabels, Array.Empty<string>());
				return labelsEditValueProvider;
			}
		}
		IEditValueProvider labelsEditValueProvider;

		public event EventHandler OnShowChanged;
		public event EventHandler AllItemsFiltered;

		readonly UIDispatcher uiDispatcher;
		readonly BookmarkContext bookmarkContext;
		readonly BookmarkFormatterProvider bookmarkFormatterProvider;
		readonly BookmarksSettings bookmarksSettings;
		readonly BookmarkDisplaySettings bookmarkDisplaySettings;
		readonly LazyToolWindowVMHelper lazyToolWindowVMHelper;
		readonly Lazy<BookmarksService> bookmarksService;
		readonly Lazy<BookmarkLocationFormatterService> bookmarkLocationFormatterService;
		readonly EditValueProviderService editValueProviderService;
		readonly Dictionary<Bookmark, BookmarkVM> bmToVM;
		readonly List<BookmarkVM> realAllItems;
		int bookmarkOrder;

		[ImportingConstructor]
		BookmarksVM(BookmarksSettings bookmarksSettings, BookmarkDisplaySettings bookmarkDisplaySettings, UIDispatcher uiDispatcher, BookmarkFormatterProvider bookmarkFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider, Lazy<BookmarksService> bookmarksService, Lazy<BookmarkLocationFormatterService> bookmarkLocationFormatterService, EditValueProviderService editValueProviderService) {
			uiDispatcher.VerifyAccess();
			this.uiDispatcher = uiDispatcher;
			sbOutput = new StringBuilderTextColorOutput();
			realAllItems = new List<BookmarkVM>();
			AllItems = new BulkObservableCollection<BookmarkVM>();
			SelectedItems = new ObservableCollection<BookmarkVM>();
			bmToVM = new Dictionary<Bookmark, BookmarkVM>();
			this.bookmarkFormatterProvider = bookmarkFormatterProvider;
			this.bookmarksSettings = bookmarksSettings;
			this.bookmarkDisplaySettings = bookmarkDisplaySettings;
			lazyToolWindowVMHelper = new LazyToolWindowVMHelper(this, uiDispatcher);
			this.bookmarksService = bookmarksService;
			this.bookmarkLocationFormatterService = bookmarkLocationFormatterService;
			this.editValueProviderService = editValueProviderService;
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			bookmarkContext = new BookmarkContext(uiDispatcher, classificationFormatMap, textElementProvider, new SearchMatcher(searchColumnDefinitions)) {
				SyntaxHighlight = bookmarksSettings.SyntaxHighlight,
				Formatter = bookmarkFormatterProvider.Create(),
			};
			Descs = new GridViewColumnDescs {
				Columns = new GridViewColumnDesc[] {
					new GridViewColumnDesc(BookmarksWindowColumnIds.Name, dnSpy_Resources.Column_Name),
					new GridViewColumnDesc(BookmarksWindowColumnIds.Labels, dnSpy_Resources.Column_Labels),
					new GridViewColumnDesc(BookmarksWindowColumnIds.Location, dnSpy_Resources.Column_Location),
					new GridViewColumnDesc(BookmarksWindowColumnIds.Module, dnSpy_Resources.Column_Module),
				},
			};
			Descs.SortedColumnChanged += (a, b) => SortList();
		}

		// Don't change the order of these instances without also updating input passed to SearchMatcher.IsMatchAll()
		static readonly SearchColumnDefinition[] searchColumnDefinitions = new SearchColumnDefinition[] {
			new SearchColumnDefinition(PredefinedTextClassifierTags.BookmarksWindowName, "n", dnSpy_Resources.Column_Name),
			new SearchColumnDefinition(PredefinedTextClassifierTags.BookmarksWindowLabels, "l", dnSpy_Resources.Column_Labels),
			new SearchColumnDefinition(PredefinedTextClassifierTags.BookmarksWindowLocation, "o", dnSpy_Resources.Column_Location),
			new SearchColumnDefinition(PredefinedTextClassifierTags.BookmarksWindowModule, "m", dnSpy_Resources.Column_Module),
		};

		// UI thread
		public string GetSearchHelpText() {
			bookmarkContext.UIDispatcher.VerifyAccess();
			return bookmarkContext.SearchMatcher.GetHelpText();
		}

		// random thread
		void BMThread(Action callback) => uiDispatcher.UI(callback);
		void BMThread_VerifyAccess() => uiDispatcher.VerifyAccess();

		// UI thread
		void ILazyToolWindowVM.Show() {
			bookmarkContext.UIDispatcher.VerifyAccess();
			Initialize_UI(enable: true);
		}

		// UI thread
		void ILazyToolWindowVM.Hide() {
			bookmarkContext.UIDispatcher.VerifyAccess();
			Initialize_UI(enable: false);
		}

		// UI thread
		void Initialize_UI(bool enable) {
			bookmarkContext.UIDispatcher.VerifyAccess();
			ResetSearchSettings();
			if (enable) {
				bookmarkContext.ClassificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
				bookmarksSettings.PropertyChanged += BookmarksSettings_PropertyChanged;
				bookmarkDisplaySettings.PropertyChanged += BookmarkDisplaySettings_PropertyChanged;
				bookmarkContext.FormatterOptions = GetBookmarkLocationFormatterOptions();
				RecreateFormatter_UI();
				bookmarkContext.SyntaxHighlight = bookmarksSettings.SyntaxHighlight;
			}
			else {
				bookmarkContext.ClassificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
				bookmarksSettings.PropertyChanged -= BookmarksSettings_PropertyChanged;
				bookmarkDisplaySettings.PropertyChanged -= BookmarkDisplaySettings_PropertyChanged;
			}
			BMThread(() => Initialize_BMThread(enable));
		}

		// BM thread
		void Initialize_BMThread(bool enable) {
			BMThread_VerifyAccess();
			if (enable) {
				bookmarksService.Value.BookmarksChanged += BookmarksService_BookmarksChanged;
				bookmarksService.Value.BookmarksModified += BookmarksService_BookmarksModified;
				var bookmarks = bookmarksService.Value.Bookmarks;
				if (bookmarks.Length > 0)
					UI(() => AddItems_UI(bookmarks));
			}
			else {
				bookmarksService.Value.BookmarksChanged -= BookmarksService_BookmarksChanged;
				bookmarksService.Value.BookmarksModified -= BookmarksService_BookmarksModified;
				UI(() => RemoveAllBookmarks_UI());
			}
			UI(() => OnShowChanged?.Invoke(this, EventArgs.Empty));
		}

		// UI thread
		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			bookmarkContext.UIDispatcher.VerifyAccess();
			RefreshThemeFields_UI();
		}

		// random thread
		void BookmarksSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => BookmarksSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void BookmarksSettings_PropertyChanged_UI(string propertyName) {
			bookmarkContext.UIDispatcher.VerifyAccess();
			if (propertyName == nameof(BookmarksSettings.SyntaxHighlight)) {
				bookmarkContext.SyntaxHighlight = bookmarksSettings.SyntaxHighlight;
				RefreshThemeFields_UI();
			}
		}

		// random thread
		void BookmarkDisplaySettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => BookmarkDisplaySettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void BookmarkDisplaySettings_PropertyChanged_UI(string propertyName) {
			bookmarkContext.UIDispatcher.VerifyAccess();
			switch (propertyName) {
			case nameof(BookmarkDisplaySettings.ShowTokens):
			case nameof(BookmarkDisplaySettings.ShowModuleNames):
			case nameof(BookmarkDisplaySettings.ShowParameterTypes):
			case nameof(BookmarkDisplaySettings.ShowParameterNames):
			case nameof(BookmarkDisplaySettings.ShowDeclaringTypes):
			case nameof(BookmarkDisplaySettings.ShowReturnTypes):
			case nameof(BookmarkDisplaySettings.ShowNamespaces):
			case nameof(BookmarkDisplaySettings.ShowIntrinsicTypeKeywords):
				bookmarkContext.FormatterOptions = GetBookmarkLocationFormatterOptions();
				RefreshLocationColumn_UI();
				break;

			default:
				Debug.Fail($"Unknown property: {propertyName}");
				break;
			}
		}

		BookmarkLocationFormatterOptions GetBookmarkLocationFormatterOptions() {
			var options = BookmarkLocationFormatterOptions.None;
			if (bookmarkDisplaySettings.ShowTokens)
				options |= BookmarkLocationFormatterOptions.Tokens;
			if (bookmarkDisplaySettings.ShowModuleNames)
				options |= BookmarkLocationFormatterOptions.ModuleNames;
			if (bookmarkDisplaySettings.ShowParameterTypes)
				options |= BookmarkLocationFormatterOptions.ParameterTypes;
			if (bookmarkDisplaySettings.ShowParameterNames)
				options |= BookmarkLocationFormatterOptions.ParameterNames;
			if (bookmarkDisplaySettings.ShowDeclaringTypes)
				options |= BookmarkLocationFormatterOptions.DeclaringTypes;
			if (bookmarkDisplaySettings.ShowReturnTypes)
				options |= BookmarkLocationFormatterOptions.ReturnTypes;
			if (bookmarkDisplaySettings.ShowNamespaces)
				options |= BookmarkLocationFormatterOptions.Namespaces;
			if (bookmarkDisplaySettings.ShowIntrinsicTypeKeywords)
				options |= BookmarkLocationFormatterOptions.IntrinsicTypeKeywords;
			bool useDigitSeparators = false;
			if (useDigitSeparators)
				options |= BookmarkLocationFormatterOptions.DigitSeparators;
			bool useHexadecimal = true;
			if (!useHexadecimal)
				options |= BookmarkLocationFormatterOptions.Decimal;
			return options;
		}

		// UI thread
		void RefreshThemeFields_UI() {
			bookmarkContext.UIDispatcher.VerifyAccess();
			foreach (var vm in realAllItems)
				vm.RefreshThemeFields_UI();
		}

		// UI thread
		void RefreshLocationColumn_UI() {
			bookmarkContext.UIDispatcher.VerifyAccess();
			foreach (var vm in realAllItems)
				vm.RefreshLocationColumn_UI();
		}

		// UI thread
		void RecreateFormatter_UI() {
			bookmarkContext.UIDispatcher.VerifyAccess();
			bookmarkContext.Formatter = bookmarkFormatterProvider.Create();
		}

		// random thread
		void UI(Action callback) => bookmarkContext.UIDispatcher.UI(callback);

		// BM thread
		void BookmarksService_BookmarksChanged(object sender, CollectionChangedEventArgs<Bookmark> e) {
			BMThread_VerifyAccess();
			if (e.Added)
				UI(() => AddItems_UI(e.Objects));
			else {
				UI(() => {
					var coll = realAllItems;
					for (int i = coll.Count - 1; i >= 0; i--) {
						if (e.Objects.Contains(coll[i].Bookmark))
							RemoveBookmarkAt_UI(i);
					}
					InitializeNothingMatched();
				});
			}
		}

		// BM thread
		void BookmarksService_BookmarksModified(object sender, BookmarksModifiedEventArgs e) {
			BMThread_VerifyAccess();
			UI(() => {
				foreach (var info in e.Bookmarks) {
					bool b = bmToVM.TryGetValue(info.Bookmark, out var vm);
					Debug.Assert(b);
					if (b)
						vm.UpdateSettings_UI(info.Bookmark.Settings);
				}
			});
		}

		// UI thread
		void AddItems_UI(IList<Bookmark> bookmarks) {
			bookmarkContext.UIDispatcher.VerifyAccess();
			foreach (var bm in bookmarks) {
				var vm = new BookmarkVM(bm, bookmarkLocationFormatterService.Value.GetFormatter(bm.Location), bookmarkContext, bookmarkOrder++, NameEditValueProvider, LabelsEditValueProvider);
				Debug.Assert(!bmToVM.ContainsKey(bm));
				bmToVM[bm] = vm;
				realAllItems.Add(vm);
				if (IsMatch_UI(vm, filterText)) {
					int insertionIndex = GetInsertionIndex_UI(vm);
					AllItems.Insert(insertionIndex, vm);
				}
			}
			if (NothingMatched && AllItems.Count != 0)
				NothingMatched = false;
		}

		// UI thread
		int GetInsertionIndex_UI(BookmarkVM vm) {
			Debug.Assert(bookmarkContext.UIDispatcher.CheckAccess());
			var list = AllItems;
			int lo = 0, hi = list.Count - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				int c = Compare(vm, list[index]);
				if (c < 0)
					hi = index - 1;
				else if (c > 0)
					lo = index + 1;
				else
					return index;
			}
			return hi + 1;
		}

		// UI thread
		void FilterList_UI(string filterText) {
			bookmarkContext.UIDispatcher.VerifyAccess();
			if (string.IsNullOrWhiteSpace(filterText))
				filterText = string.Empty;
			bookmarkContext.SearchMatcher.SetSearchText(filterText);
			SortList(filterText);
		}

		// UI thread
		void SortList() {
			bookmarkContext.UIDispatcher.VerifyAccess();
			SortList(filterText);
		}

		// UI thread
		void SortList(string filterText) {
			bookmarkContext.UIDispatcher.VerifyAccess();
			var newList = new List<BookmarkVM>(GetFilteredItems_UI(filterText));
			newList.Sort(this);
			AllItems.Reset(newList);
			InitializeNothingMatched(filterText);
			AllItemsFiltered?.Invoke(this, EventArgs.Empty);
		}

		// UI thread
		IEnumerable<BookmarkVM> IBookmarksVM.Sort(IEnumerable<BookmarkVM> bookmarks) {
			bookmarkContext.UIDispatcher.VerifyAccess();
			var list = new List<BookmarkVM>(bookmarks);
			list.Sort(this);
			return list;
		}

		void InitializeNothingMatched() => InitializeNothingMatched(filterText);
		void InitializeNothingMatched(string filterText) =>
			NothingMatched = AllItems.Count == 0 && !string.IsNullOrWhiteSpace(filterText);

		public int Compare(BookmarkVM x, BookmarkVM y) {
			Debug.Assert(bookmarkContext.UIDispatcher.CheckAccess());
			var (desc, dir) = Descs.SortedColumn;

			int id;
			if (desc == null || dir == GridViewSortDirection.Default) {
				id = BookmarksWindowColumnIds.Default_Order;
				dir = GridViewSortDirection.Ascending;
			}
			else
				id = desc.Id;

			int diff;
			switch (id) {
			case BookmarksWindowColumnIds.Default_Order:
				diff = x.Order - y.Order;
				break;

			case BookmarksWindowColumnIds.Name:
				diff = StringComparer.OrdinalIgnoreCase.Compare(GetName_UI(x), GetName_UI(y));
				break;

			case BookmarksWindowColumnIds.Labels:
				diff = StringComparer.OrdinalIgnoreCase.Compare(GetLabels_UI(x), GetLabels_UI(y));
				break;

			case BookmarksWindowColumnIds.Location:
				diff = StringComparer.OrdinalIgnoreCase.Compare(GetLocation_UI(x), GetLocation_UI(y));
				break;

			case BookmarksWindowColumnIds.Module:
				diff = StringComparer.OrdinalIgnoreCase.Compare(GetModule_UI(x), GetModule_UI(y));
				break;

			default:
				throw new InvalidOperationException();
			}

			if (diff == 0)
				diff = x.Order - y.Order;
			Debug.Assert(dir == GridViewSortDirection.Ascending || dir == GridViewSortDirection.Descending);
			if (dir == GridViewSortDirection.Descending)
				diff = -diff;
			return diff;
		}

		// UI thread
		IEnumerable<BookmarkVM> GetFilteredItems_UI(string filterText) {
			bookmarkContext.UIDispatcher.VerifyAccess();
			foreach (var vm in realAllItems) {
				if (IsMatch_UI(vm, filterText))
					yield return vm;
			}
		}

		// UI thread
		bool IsMatch_UI(BookmarkVM vm, string filterText) {
			Debug.Assert(bookmarkContext.UIDispatcher.CheckAccess());
			// Common case check, we don't need to allocate any strings
			if (filterText == string.Empty)
				return true;
			// The order must match searchColumnDefinitions
			var allStrings = new string[] {
				GetName_UI(vm),
				GetLabels_UI(vm),
				GetLocation_UI(vm),
				GetModule_UI(vm),
			};
			sbOutput.Reset();
			return bookmarkContext.SearchMatcher.IsMatchAll(allStrings);
		}
		readonly StringBuilderTextColorOutput sbOutput;

		// UI thread
		string GetName_UI(BookmarkVM vm) {
			Debug.Assert(bookmarkContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			bookmarkContext.Formatter.WriteName(sbOutput, vm);
			return sbOutput.ToString();
		}

		// UI thread
		string GetLabels_UI(BookmarkVM vm) {
			Debug.Assert(bookmarkContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			bookmarkContext.Formatter.WriteLabels(sbOutput, vm);
			return sbOutput.ToString();
		}

		// UI thread
		string GetLocation_UI(BookmarkVM vm) {
			Debug.Assert(bookmarkContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			bookmarkContext.Formatter.WriteLocation(sbOutput, vm);
			return sbOutput.ToString();
		}

		// UI thread
		string GetModule_UI(BookmarkVM vm) {
			Debug.Assert(bookmarkContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			bookmarkContext.Formatter.WriteModule(sbOutput, vm);
			return sbOutput.ToString();
		}

		// UI thread
		void RemoveBookmarkAt_UI(int i) {
			bookmarkContext.UIDispatcher.VerifyAccess();
			Debug.Assert(0 <= i && i < realAllItems.Count);
			var vm = realAllItems[i];
			bool b = bmToVM.Remove(vm.Bookmark);
			Debug.Assert(b);
			vm.Dispose();
			realAllItems.RemoveAt(i);
			AllItems.Remove(vm);
		}

		// UI thread
		void RemoveAllBookmarks_UI() {
			bookmarkContext.UIDispatcher.VerifyAccess();
			AllItems.Reset(Array.Empty<BookmarkVM>());
			var coll = realAllItems;
			for (int i = coll.Count - 1; i >= 0; i--)
				RemoveBookmarkAt_UI(i);
			Debug.Assert(bmToVM.Count == 0);
		}

		// UI thread
		public void ResetSearchSettings() {
			bookmarkContext.UIDispatcher.VerifyAccess();
			FilterText = string.Empty;
		}
	}
}

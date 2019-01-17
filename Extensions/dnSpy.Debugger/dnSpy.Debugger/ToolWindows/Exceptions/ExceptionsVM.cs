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
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.ToolWindows.Search;
using dnSpy.Debugger.Exceptions;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.Exceptions {
	interface IExceptionsVM : IGridViewColumnDescsProvider {
		bool IsOpen { get; set; }
		bool IsVisible { get; set; }
		BulkObservableCollection<ExceptionVM> AllItems { get; }
		ObservableCollection<ExceptionVM> SelectedItems { get; }
		bool ShowOnlyEnabledExceptions { get; set; }
		void ResetSearchSettings();
		IReadOnlyCollection<ExceptionCategoryVM> ExceptionCategoryCollection { get; }
		ExceptionCategoryVM SelectedCategory { get; set; }
		bool IsAddingExceptions { get; set; }
		string GetSearchHelpText();
		IEnumerable<ExceptionVM> Sort(IEnumerable<ExceptionVM> exceptions);
	}

	[Export(typeof(IExceptionsVM))]
	sealed class ExceptionsVM : ViewModelBase, IExceptionsVM, ILazyToolWindowVM, IComparer<ExceptionsVM.ExceptionVMCached> {
		public BulkObservableCollection<ExceptionVM> AllItems { get; }
		public ObservableCollection<ExceptionVM> SelectedItems { get; }
		public GridViewColumnDescs Descs { get; }

		public bool IsOpen {
			get => lazyToolWindowVMHelper.IsOpen;
			set => lazyToolWindowVMHelper.IsOpen = value;
		}

		public bool IsVisible {
			get => lazyToolWindowVMHelper.IsVisible;
			set => lazyToolWindowVMHelper.IsVisible = value;
		}

		public bool IsAddingExceptionsEnabled => dbgExceptionSettingsService.Value.CategoryDefinitions.Count > 0;
		public bool IsAddingExceptions {
			get => isAddingExceptions;
			set {
				if (isAddingExceptions == value)
					return;
				isAddingExceptions = value;
				OnPropertyChanged(nameof(IsAddingExceptions));
			}
		}
		bool isAddingExceptions;

		public bool ShowOnlyEnabledExceptions {
			get => showOnlyEnabledExceptions;
			set {
				if (showOnlyEnabledExceptions == value)
					return;
				showOnlyEnabledExceptions = value;
				OnPropertyChanged(nameof(ShowOnlyEnabledExceptions));
				FilterList_UI(filterText, showOnlyEnabledExceptions, selectedCategory);
			}
		}
		bool showOnlyEnabledExceptions;

		public string FilterText {
			get => filterText;
			set {
				if (filterText == value)
					return;
				filterText = value;
				OnPropertyChanged(nameof(FilterText));
				FilterList_UI(filterText, showOnlyEnabledExceptions, selectedCategory);
			}
		}
		string filterText = string.Empty;

		public ExceptionCategoryVM SelectedCategory {
			get => selectedCategory;
			set {
				if (selectedCategory == value)
					return;
				selectedCategory = value;
				OnPropertyChanged(nameof(SelectedCategory));
				FilterList_UI(filterText, showOnlyEnabledExceptions, selectedCategory);
			}
		}
		ExceptionCategoryVM selectedCategory;

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

		public IReadOnlyCollection<ExceptionCategoryVM> ExceptionCategoryCollection => exceptionCategories;
		readonly ObservableCollection<ExceptionCategoryVM> exceptionCategories;

		readonly Lazy<DbgManager> dbgManager;
		readonly Lazy<DbgExceptionSettingsService> dbgExceptionSettingsService;
		readonly ExceptionContext exceptionContext;
		readonly ExceptionFormatterProvider exceptionFormatterProvider;
		readonly DebuggerSettings debuggerSettings;
		readonly LazyToolWindowVMHelper lazyToolWindowVMHelper;
		readonly Dictionary<DbgExceptionId, ExceptionVM> toVM;
		readonly List<ExceptionVM> realAllItems;
		int exceptionOrder;

		[ImportingConstructor]
		ExceptionsVM(Lazy<DbgManager> dbgManager, Lazy<DbgExceptionSettingsService> dbgExceptionSettingsService, DebuggerSettings debuggerSettings, UIDispatcher uiDispatcher, ExceptionFormatterProvider exceptionFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider, DbgExceptionSettingsService exceptionSettingsService, DbgExceptionFormatterService exceptionFormatterService) {
			uiDispatcher.VerifyAccess();
			AllItems = new BulkObservableCollection<ExceptionVM>();
			SelectedItems = new ObservableCollection<ExceptionVM>();
			exceptionCategories = new ObservableCollection<ExceptionCategoryVM>();
			this.dbgManager = dbgManager;
			this.dbgExceptionSettingsService = dbgExceptionSettingsService;
			this.exceptionFormatterProvider = exceptionFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			lazyToolWindowVMHelper = new LazyToolWindowVMHelper(this, uiDispatcher);
			toVM = new Dictionary<DbgExceptionId, ExceptionVM>();
			realAllItems = new List<ExceptionVM>();
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			exceptionContext = new ExceptionContext(uiDispatcher, classificationFormatMap, textElementProvider, exceptionSettingsService, exceptionFormatterService, new SearchMatcher(searchColumnDefinitions)) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlight,
				Formatter = exceptionFormatterProvider.Create(),
			};
			Descs = new GridViewColumnDescs {
				Columns = new GridViewColumnDesc[] {
					new GridViewColumnDesc(ExceptionsWindowColumnIds.BreakWhenThrown, dnSpy_Debugger_Resources.Column_BreakWhenThrown),
					new GridViewColumnDesc(ExceptionsWindowColumnIds.Category, dnSpy_Debugger_Resources.Column_Category),
					new GridViewColumnDesc(ExceptionsWindowColumnIds.Conditions, dnSpy_Debugger_Resources.Column_Conditions),
				},
			};
			Descs.SortedColumnChanged += (a, b) => SortList();
		}

		// Don't change the order of these instances without also updating input passed to SearchMatcher.IsMatchAll()
		static readonly SearchColumnDefinition[] searchColumnDefinitions = new SearchColumnDefinition[] {
			new SearchColumnDefinition(PredefinedTextClassifierTags.ExceptionSettingsWindowName, "n", dnSpy_Debugger_Resources.Column_BreakWhenThrown),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ExceptionSettingsWindowCategory, "cat", dnSpy_Debugger_Resources.Column_Category),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ExceptionSettingsWindowConditions, "c", dnSpy_Debugger_Resources.Column_Conditions),
		};

		// UI thread
		public string GetSearchHelpText() {
			exceptionContext.UIDispatcher.VerifyAccess();
			return exceptionContext.SearchMatcher.GetHelpText();
		}

		// random thread
		void DbgThread(Action callback) =>
			dbgManager.Value.Dispatcher.BeginInvoke(callback);

		// UI thread
		void ILazyToolWindowVM.Show() {
			exceptionContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: true);
		}

		// UI thread
		void ILazyToolWindowVM.Hide() {
			exceptionContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: false);
		}

		// UI thread
		void InitializeDebugger_UI(bool enable) {
			exceptionContext.UIDispatcher.VerifyAccess();
			if (exceptionCategories.Count == 0)
				InitializeExceptionCategories_UI();
			IsAddingExceptions = false;
			ResetSearchSettings();
			if (enable) {
				exceptionContext.ClassificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
				RecreateFormatter_UI();
				exceptionContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
			}
			else {
				exceptionContext.ClassificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged -= DebuggerSettings_PropertyChanged;
			}
			DbgThread(() => InitializeDebugger_DbgThread(enable));
		}

		// UI thread
		void InitializeExceptionCategories_UI() {
			exceptionContext.UIDispatcher.VerifyAccess();
			if (exceptionCategories.Count != 0)
				return;
			foreach (var g in dbgExceptionSettingsService.Value.CategoryDefinitions.Select(a => new ExceptionCategoryVM(a)).OrderBy(a => a.ShortDisplayName, StringComparer.CurrentCultureIgnoreCase))
				exceptionCategories.Add(g);
			exceptionCategories.Insert(0, new ExceptionCategoryVM(dnSpy_Debugger_Resources.Exceptions_AllCategories));
			SelectedCategory = exceptionCategories[0];
		}

		// DbgManager thread
		void InitializeDebugger_DbgThread(bool enable) {
			dbgManager.Value.Dispatcher.VerifyAccess();
			if (enable) {
				dbgExceptionSettingsService.Value.ExceptionsChanged += DbgExceptionSettingsService_ExceptionsChanged;
				dbgExceptionSettingsService.Value.ExceptionSettingsModified += DbgExceptionSettingsService_ExceptionSettingsModified;
				var exceptions = dbgExceptionSettingsService.Value.Exceptions;
				if (exceptions.Length > 0)
					UI(() => AddItems_UI(exceptions));
			}
			else {
				dbgExceptionSettingsService.Value.ExceptionsChanged -= DbgExceptionSettingsService_ExceptionsChanged;
				dbgExceptionSettingsService.Value.ExceptionSettingsModified -= DbgExceptionSettingsService_ExceptionSettingsModified;
				UI(() => RemoveAllExceptions_UI());
			}
		}

		// DbgManager thread
		void DbgExceptionSettingsService_ExceptionsChanged(object sender, DbgCollectionChangedEventArgs<DbgExceptionSettingsInfo> e) {
			dbgManager.Value.Dispatcher.VerifyAccess();
			if (e.Added)
				UI(() => AddItems_UI(e.Objects));
			else {
				UI(() => {
					foreach (var info in e.Objects) {
						if (toVM.TryGetValue(info.Definition.Id, out var ex)) {
							var index = realAllItems.IndexOf(ex);
							Debug.Assert(index >= 0);
							if (index >= 0)
								RemoveExceptionAt_UI(index);
						}
						InitializeNothingMatched();
					}
				});
			}
		}

		// DbgManager thread
		void DbgExceptionSettingsService_ExceptionSettingsModified(object sender, DbgExceptionSettingsModifiedEventArgs e) {
			dbgManager.Value.Dispatcher.VerifyAccess();
			UI(() => {
				foreach (var info in e.IdAndSettings) {
					if (toVM.TryGetValue(info.Id, out var ex))
						ex.Settings = info.Settings;
				}
			});
		}

		// UI thread
		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			exceptionContext.UIDispatcher.VerifyAccess();
			RefreshThemeFields_UI();
		}

		// random thread
		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => DebuggerSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DebuggerSettings_PropertyChanged_UI(string propertyName) {
			exceptionContext.UIDispatcher.VerifyAccess();
			if (propertyName == nameof(DebuggerSettings.SyntaxHighlight)) {
				exceptionContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				RefreshThemeFields_UI();
			}
		}

		// UI thread
		void RefreshThemeFields_UI() {
			exceptionContext.UIDispatcher.VerifyAccess();
			foreach (var vm in realAllItems)
				vm.RefreshThemeFields_UI();
		}

		// UI thread
		void RecreateFormatter_UI() {
			exceptionContext.UIDispatcher.VerifyAccess();
			exceptionContext.Formatter = exceptionFormatterProvider.Create();
		}

		// random thread
		void UI(Action callback) => exceptionContext.UIDispatcher.UI(callback);

		// UI thread
		void AddItems_UI(IList<DbgExceptionSettingsInfo> exceptions) {
			exceptionContext.UIDispatcher.VerifyAccess();
			foreach (var ex in exceptions) {
				var vm = new ExceptionVM(ex, exceptionContext, exceptionOrder++);
				Debug.Assert(!toVM.ContainsKey(ex.Definition.Id));
				toVM[ex.Definition.Id] = vm;
				realAllItems.Add(vm);
				var vmc = CreateCached_UI(vm);
				if (IsMatch_UI(vmc, filterText, showOnlyEnabledExceptions)) {
					int insertionIndex = GetInsertionIndex_UI(vmc);
					AllItems.Insert(insertionIndex, vm);
				}
			}
			if (NothingMatched && AllItems.Count != 0)
				NothingMatched = false;
		}

		// UI thread
		int GetInsertionIndex_UI(ExceptionVMCached vmc) {
			Debug.Assert(exceptionContext.UIDispatcher.CheckAccess());
			var list = AllItems;
			int lo = 0, hi = list.Count - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				var otherc = CreateCached_UI(list[index]);
				int c = Compare(vmc, otherc);
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
		void RemoveExceptionAt_UI(int i) {
			exceptionContext.UIDispatcher.VerifyAccess();
			Debug.Assert(0 <= i && i < realAllItems.Count);
			var vm = realAllItems[i];
			vm.Dispose();
			realAllItems.RemoveAt(i);
			AllItems.Remove(vm);
			toVM.Remove(vm.Definition.Id);
		}

		// UI thread
		void RemoveAllExceptions_UI() {
			exceptionContext.UIDispatcher.VerifyAccess();
			AllItems.Reset(Array.Empty<ExceptionVM>());
			toVM.Clear();
			var coll = realAllItems;
			for (int i = coll.Count - 1; i >= 0; i--)
				RemoveExceptionAt_UI(i);
		}

		// UI thread
		void FilterList_UI(string filterText, bool showOnlyEnabledExceptions, ExceptionCategoryVM selectedCategory) {
			exceptionContext.UIDispatcher.VerifyAccess();
			if (string.IsNullOrWhiteSpace(filterText))
				filterText = string.Empty;
			exceptionContext.SearchMatcher.SetSearchText(filterText);
			SortList(filterText, showOnlyEnabledExceptions, selectedCategory);
		}

		// UI thread
		void SortList() {
			exceptionContext.UIDispatcher.VerifyAccess();
			SortList(filterText, showOnlyEnabledExceptions, selectedCategory);
		}
 
		// UI thread
		void SortList(string filterText, bool showOnlyEnabledExceptions, ExceptionCategoryVM selectedCategory) {
			exceptionContext.UIDispatcher.VerifyAccess();
			var newList = new List<ExceptionVMCached>(GetFilteredItems_UI(selectedCategory, filterText, showOnlyEnabledExceptions));
			newList.Sort(this);
			AllItems.Reset(newList.Select(a => a.VM));
			InitializeNothingMatched(filterText, showOnlyEnabledExceptions, selectedCategory);
		}

		// UI thread
		IEnumerable<ExceptionVM> IExceptionsVM.Sort(IEnumerable<ExceptionVM> exceptions) {
			exceptionContext.UIDispatcher.VerifyAccess();
			var list = new List<ExceptionVMCached>(exceptions.Select(a => CreateCached_UI(a)));
			list.Sort(this);
			return list.Select(a => a.VM);
		}

		void InitializeNothingMatched() => InitializeNothingMatched(filterText, showOnlyEnabledExceptions, selectedCategory);
		void InitializeNothingMatched(string filterText, bool showOnlyEnabledExceptions, ExceptionCategoryVM selectedCategory) =>
			NothingMatched = AllItems.Count == 0 && !(string.IsNullOrWhiteSpace(filterText) && !showOnlyEnabledExceptions && selectedCategory == exceptionCategories.FirstOrDefault());

		int IComparer<ExceptionVMCached>.Compare(ExceptionVMCached x, ExceptionVMCached y) => Compare(x, y);
		int Compare(ExceptionVMCached x, ExceptionVMCached y) {
			Debug.Assert(exceptionContext.UIDispatcher.CheckAccess());
			var (desc, dir) = Descs.SortedColumn;

			int id;
			if (desc == null || dir == GridViewSortDirection.Default) {
				id = ExceptionsWindowColumnIds.Default_Order;
				dir = GridViewSortDirection.Ascending;
			}
			else
				id = desc.Id;

			int diff;
			switch (id) {
			case ExceptionsWindowColumnIds.Default_Order:
				diff = GetDefaultOrder(x, y);
				break;

			case ExceptionsWindowColumnIds.BreakWhenThrown:
				diff = StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
				break;

			case ExceptionsWindowColumnIds.Category:
				diff = StringComparer.OrdinalIgnoreCase.Compare(x.Category, y.Category);
				break;

			case ExceptionsWindowColumnIds.Conditions:
				diff = StringComparer.OrdinalIgnoreCase.Compare(x.Conditions, y.Conditions);
				break;

			default:
				throw new InvalidOperationException();
			}

			if (diff == 0 && id != ExceptionsWindowColumnIds.Default_Order)
				diff = GetDefaultOrder(x, y);
			Debug.Assert(dir == GridViewSortDirection.Ascending || dir == GridViewSortDirection.Descending);
			if (dir == GridViewSortDirection.Descending)
				diff = -diff;
			return diff;
		}

		static int GetDefaultOrder(ExceptionVMCached x, ExceptionVMCached y) {
			if (x == y)
				return 0;

			var c = StringComparer.OrdinalIgnoreCase.Compare(x.Category, y.Category);
			if (c != 0)
				return c;

			var xid = x.VM.Definition.Id;
			var yid = y.VM.Definition.Id;
			if (xid.IsDefaultId != yid.IsDefaultId) {
				if (xid.IsDefaultId)
					return -1;
				Debug.Assert(yid.IsDefaultId);
				return 1;
			}
			else if (xid.IsDefaultId)
				return StringComparer.OrdinalIgnoreCase.Compare(x.Category, y.Category);

			return StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
		}

		sealed class ExceptionVMCached {
			public ExceptionVM VM { get; }
			public string Name => name ?? (name = GetName_UI(VM));
			public string Category => category ?? (category = GetCategory_UI(VM));
			public string Conditions => conditions ?? (conditions = GetConditions_UI(VM));

			// The order must match searchColumnDefinitions
			public string[] AllStrings => allStrings ?? (allStrings = new[] { Name, Category, Conditions });

			string name;
			string category;
			string conditions;
			string[] allStrings;
			public ExceptionVMCached(ExceptionVM vm) => VM = vm;
		}

		// UI thread
		IEnumerable<ExceptionVMCached> GetFilteredItems_UI(ExceptionCategoryVM selectedCategory, string filterText, bool showOnlyEnabledExceptions) {
			exceptionContext.UIDispatcher.VerifyAccess();
			var category = selectedCategory?.Definition?.Name;
			foreach (var item in realAllItems) {
				if (category != null && item.Definition.Id.Category != category)
					continue;
				var vmc = CreateCached_UI(item);
				if (IsMatch_UI(vmc, filterText, showOnlyEnabledExceptions))
					yield return vmc;
			}
		}

		// UI thread
		ExceptionVMCached CreateCached_UI(ExceptionVM vm) {
			Debug.Assert(exceptionContext.UIDispatcher.CheckAccess());
			return new ExceptionVMCached(vm);
		}

		// UI thread
		bool IsMatch_UI(ExceptionVMCached vmc, string filterText, bool showOnlyEnabledExceptions) {
			Debug.Assert(exceptionContext.UIDispatcher.CheckAccess());
			if (showOnlyEnabledExceptions && !vmc.VM.BreakWhenThrown)
				return false;
			// Common case check. Prevents initializing props in 'vmc'
			if (filterText == string.Empty)
				return true;
			return exceptionContext.SearchMatcher.IsMatchAll(vmc.AllStrings);
		}

		// UI thread
		static string GetName_UI(ExceptionVM vm) {
			Debug.Assert(vm.Context.UIDispatcher.CheckAccess());
			var writer = vm.Context.TextClassifierTextColorWriter;
			writer.Clear();
			var formatter = vm.Context.Formatter;
			formatter.WriteName(writer, vm);
			return writer.Text;
		}

		// UI thread
		static string GetCategory_UI(ExceptionVM vm) {
			Debug.Assert(vm.Context.UIDispatcher.CheckAccess());
			var writer = vm.Context.TextClassifierTextColorWriter;
			writer.Clear();
			var formatter = vm.Context.Formatter;
			formatter.WriteCategory(writer, vm);
			return writer.Text;
		}

		// UI thread
		static string GetConditions_UI(ExceptionVM vm) {
			Debug.Assert(vm.Context.UIDispatcher.CheckAccess());
			var writer = vm.Context.TextClassifierTextColorWriter;
			writer.Clear();
			var formatter = vm.Context.Formatter;
			formatter.WriteConditions(writer, vm);
			return writer.Text;
		}

		// UI thread
		public void ResetSearchSettings() {
			exceptionContext.UIDispatcher.VerifyAccess();
			ShowOnlyEnabledExceptions = false;
			FilterText = string.Empty;
			if (exceptionCategories.Count > 0)
				SelectedCategory = exceptionCategories[0];
		}
	}
}

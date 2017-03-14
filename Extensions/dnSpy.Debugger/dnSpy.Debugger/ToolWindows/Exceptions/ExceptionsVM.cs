/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.ToolWindows.Text;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.Exceptions {
	interface IExceptionsVM {
		bool IsEnabled { get; set; }
		bool IsVisible { get; set; }
		BulkObservableCollection<ExceptionVM> AllItems { get; }
		ObservableCollection<ExceptionVM> SelectedItems { get; }
		bool ShowOnlyEnabledExceptions { get; set; }
		void ResetSearchSettings();
		IReadOnlyCollection<ExceptionGroupVM> ExceptionGroupCollection { get; }
		ExceptionGroupVM SelectedGroup { get; set; }
		bool IsAddingExceptions { get; set; }
	}

	[Export(typeof(IExceptionsVM))]
	sealed class ExceptionsVM : ViewModelBase, IExceptionsVM, ILazyToolWindowVM {
		public BulkObservableCollection<ExceptionVM> AllItems { get; }
		public ObservableCollection<ExceptionVM> SelectedItems { get; }

		public bool IsEnabled {
			get => lazyToolWindowVMHelper.IsEnabled;
			set => lazyToolWindowVMHelper.IsEnabled = value;
		}

		public bool IsVisible {
			get => lazyToolWindowVMHelper.IsVisible;
			set => lazyToolWindowVMHelper.IsVisible = value;
		}

		public bool IsAddingExceptionsEnabled => dbgExceptionSettingsService.Value.GroupDefinitions.Count > 0;
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
				FilterTreeView_UI(filterText, showOnlyEnabledExceptions, selectedGroup);
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
				FilterTreeView_UI(filterText, showOnlyEnabledExceptions, selectedGroup);
			}
		}
		string filterText = string.Empty;

		public ExceptionGroupVM SelectedGroup {
			get => selectedGroup;
			set {
				if (selectedGroup == value)
					return;
				selectedGroup = value;
				OnPropertyChanged(nameof(SelectedGroup));
				FilterTreeView_UI(filterText, showOnlyEnabledExceptions, selectedGroup);
			}
		}
		ExceptionGroupVM selectedGroup;

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

		public IReadOnlyCollection<ExceptionGroupVM> ExceptionGroupCollection => exceptionGroups;
		readonly ObservableCollection<ExceptionGroupVM> exceptionGroups;

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
			exceptionGroups = new ObservableCollection<ExceptionGroupVM>();
			this.dbgManager = dbgManager;
			this.dbgExceptionSettingsService = dbgExceptionSettingsService;
			this.exceptionFormatterProvider = exceptionFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			lazyToolWindowVMHelper = new LazyToolWindowVMHelper(this, uiDispatcher);
			toVM = new Dictionary<DbgExceptionId, ExceptionVM>();
			realAllItems = new List<ExceptionVM>();
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			exceptionContext = new ExceptionContext(uiDispatcher, classificationFormatMap, textElementProvider, exceptionSettingsService, exceptionFormatterService, new SearchMatcher()) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlight,
				Formatter = exceptionFormatterProvider.Create(),
			};
		}

		// random thread
		void DbgThread(Action action) =>
			dbgManager.Value.DispatcherThread.BeginInvoke(action);

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
			if (exceptionGroups.Count == 0)
				InitializeExceptionGroups_UI();
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
		void InitializeExceptionGroups_UI() {
			exceptionContext.UIDispatcher.VerifyAccess();
			if (exceptionGroups.Count != 0)
				return;
			foreach (var g in dbgExceptionSettingsService.Value.GroupDefinitions.Select(a => new ExceptionGroupVM(a)).OrderBy(a => a.ShortDisplayName, StringComparer.CurrentCultureIgnoreCase))
				exceptionGroups.Add(g);
			exceptionGroups.Insert(0, new ExceptionGroupVM(dnSpy_Debugger_Resources.Exceptions_AllGroups));
			SelectedGroup = exceptionGroups[0];
		}

		// DbgManager thread
		void InitializeDebugger_DbgThread(bool enable) {
			dbgManager.Value.DispatcherThread.VerifyAccess();
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
			dbgManager.Value.DispatcherThread.VerifyAccess();
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
					}
				});
			}
		}

		// DbgManager thread
		void DbgExceptionSettingsService_ExceptionSettingsModified(object sender, DbgExceptionSettingsModifiedEventArgs e) {
			dbgManager.Value.DispatcherThread.VerifyAccess();
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
		void UI(Action action) => exceptionContext.UIDispatcher.UI(action);

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
			var comparer = ExceptionVMCachedComparer.Instance;
			var list = AllItems;
			int lo = 0, hi = list.Count - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				var otherc = CreateCached_UI(list[index]);
				int c = comparer.Compare(vmc, otherc);
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
		void FilterTreeView_UI(string filterText, bool showOnlyEnabledExceptions, ExceptionGroupVM selectedGroup) {
			exceptionContext.UIDispatcher.VerifyAccess();
			if (string.IsNullOrWhiteSpace(filterText))
				filterText = string.Empty;
			exceptionContext.SearchMatcher.SetSearchText(filterText);

			var newList = new List<ExceptionVMCached>(GetFilteredItems_UI(selectedGroup, filterText, showOnlyEnabledExceptions));
			newList.Sort(ExceptionVMCachedComparer.Instance);
			AllItems.Reset(newList.Select(a => a.VM));
			NothingMatched = AllItems.Count == 0 && !(filterText == string.Empty && !showOnlyEnabledExceptions && selectedGroup == exceptionGroups.FirstOrDefault());
		}

		sealed class ExceptionVMCached {
			public ExceptionVM VM { get; }
			public string Name => name ?? (name = GetName_UI(VM));
			public string Group => group ?? (group = GetGroup_UI(VM));
			public string Conditions => conditions ?? (conditions = GetConditions_UI(VM));
			public string[] AllStrings => allStrings ?? (allStrings = new[] { Name, Group, Conditions });
			string name;
			string group;
			string conditions;
			string[] allStrings;
			public ExceptionVMCached(ExceptionVM vm) => VM = vm;
		}

		sealed class ExceptionVMCachedComparer : IComparer<ExceptionVMCached> {
			public static readonly IComparer<ExceptionVMCached> Instance = new ExceptionVMCachedComparer();
			public int Compare(ExceptionVMCached x, ExceptionVMCached y) {
				if (x == y)
					return 0;

				var c = StringComparer.OrdinalIgnoreCase.Compare(x.Group, y.Group);
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
					return StringComparer.OrdinalIgnoreCase.Compare(x.Group, y.Group);

				return StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
			}
		}

		// UI thread
		IEnumerable<ExceptionVMCached> GetFilteredItems_UI(ExceptionGroupVM selectedGroup, string filterText, bool showOnlyEnabledExceptions) {
			exceptionContext.UIDispatcher.VerifyAccess();
			var groupName = selectedGroup?.Definition?.Name;
			foreach (var item in realAllItems) {
				if (groupName != null && item.Definition.Id.Group != groupName)
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
			formatter.WriteName(writer, vm.Context.DebugOutputWriter, vm);
			return writer.Text;
		}

		// UI thread
		static string GetGroup_UI(ExceptionVM vm) {
			Debug.Assert(vm.Context.UIDispatcher.CheckAccess());
			var writer = vm.Context.TextClassifierTextColorWriter;
			writer.Clear();
			var formatter = vm.Context.Formatter;
			formatter.WriteGroup(writer, vm);
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
			if (exceptionGroups.Count > 0)
				SelectedGroup = exceptionGroups[0];
		}
	}
}

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
using dnSpy.Contracts.Controls.ToolWindows;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Modules;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.ToolWindows.Search;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.ModuleBreakpoints {
	interface IModuleBreakpointsVM : IGridViewColumnDescsProvider {
		bool IsOpen { get; set; }
		bool IsVisible { get; set; }
		BulkObservableCollection<ModuleBreakpointVM> AllItems { get; }
		ObservableCollection<ModuleBreakpointVM> SelectedItems { get; }
		void ResetSearchSettings();
		string GetSearchHelpText();
		IEnumerable<ModuleBreakpointVM> Sort(IEnumerable<ModuleBreakpointVM> moduleBreakpoints);
	}

	[Export(typeof(IModuleBreakpointsVM))]
	sealed class ModuleBreakpointsVM : ViewModelBase, IModuleBreakpointsVM, ILazyToolWindowVM, IComparer<ModuleBreakpointVM> {
		public BulkObservableCollection<ModuleBreakpointVM> AllItems { get; }
		public ObservableCollection<ModuleBreakpointVM> SelectedItems { get; }
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

		IEditValueProvider ModuleNameEditValueProvider {
			get {
				moduleBreakpointContext.UIDispatcher.VerifyAccess();
				if (moduleNameEditValueProvider == null)
					moduleNameEditValueProvider = editValueProviderService.Create(ContentTypes.ModuleBreakpointsWindowModuleName, Array.Empty<string>());
				return moduleNameEditValueProvider;
			}
		}
		IEditValueProvider moduleNameEditValueProvider;

		IEditValueProvider OrderEditValueProvider {
			get {
				moduleBreakpointContext.UIDispatcher.VerifyAccess();
				if (orderEditValueProvider == null)
					orderEditValueProvider = editValueProviderService.Create(ContentTypes.ModuleBreakpointsWindowOrder, Array.Empty<string>());
				return orderEditValueProvider;
			}
		}
		IEditValueProvider orderEditValueProvider;

		IEditValueProvider ProcessNameEditValueProvider {
			get {
				moduleBreakpointContext.UIDispatcher.VerifyAccess();
				if (processNameEditValueProvider == null)
					processNameEditValueProvider = editValueProviderService.Create(ContentTypes.ModuleBreakpointsWindowProcessName, Array.Empty<string>());
				return processNameEditValueProvider;
			}
		}
		IEditValueProvider processNameEditValueProvider;

		IEditValueProvider AppDomainNameEditValueProvider {
			get {
				moduleBreakpointContext.UIDispatcher.VerifyAccess();
				if (appDomainNameEditValueProvider == null)
					appDomainNameEditValueProvider = editValueProviderService.Create(ContentTypes.ModuleBreakpointsWindowAppDomainName, Array.Empty<string>());
				return appDomainNameEditValueProvider;
			}
		}
		IEditValueProvider appDomainNameEditValueProvider;

		readonly Lazy<DbgManager> dbgManager;
		readonly ModuleBreakpointContext moduleBreakpointContext;
		readonly ModuleBreakpointFormatterProvider moduleBreakpointFormatterProvider;
		readonly DebuggerSettings debuggerSettings;
		readonly EditValueProviderService editValueProviderService;
		readonly LazyToolWindowVMHelper lazyToolWindowVMHelper;
		readonly Lazy<DbgModuleBreakpointsService> dbgModuleBreakpointsService;
		readonly Dictionary<DbgModuleBreakpoint, ModuleBreakpointVM> bpToVM;
		readonly List<ModuleBreakpointVM> realAllItems;
		int moduleBreakpointOrder;

		[ImportingConstructor]
		ModuleBreakpointsVM(Lazy<DbgManager> dbgManager, DebuggerSettings debuggerSettings, UIDispatcher uiDispatcher, ModuleBreakpointFormatterProvider moduleBreakpointFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider, EditValueProviderService editValueProviderService, Lazy<DbgModuleBreakpointsService> dbgModuleBreakpointsService) {
			uiDispatcher.VerifyAccess();
			realAllItems = new List<ModuleBreakpointVM>();
			AllItems = new BulkObservableCollection<ModuleBreakpointVM>();
			SelectedItems = new ObservableCollection<ModuleBreakpointVM>();
			bpToVM = new Dictionary<DbgModuleBreakpoint, ModuleBreakpointVM>();
			this.dbgManager = dbgManager;
			this.moduleBreakpointFormatterProvider = moduleBreakpointFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			lazyToolWindowVMHelper = new LazyToolWindowVMHelper(this, uiDispatcher);
			this.editValueProviderService = editValueProviderService;
			this.dbgModuleBreakpointsService = dbgModuleBreakpointsService;
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			moduleBreakpointContext = new ModuleBreakpointContext(uiDispatcher, classificationFormatMap, textElementProvider, new SearchMatcher(searchColumnDefinitions)) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlight,
				Formatter = moduleBreakpointFormatterProvider.Create(),
			};
			Descs = new GridViewColumnDescs {
				Columns = new GridViewColumnDesc[] {
					new GridViewColumnDesc(ModuleBreakpointsWindowColumnIds.IsEnabled, string.Empty) { CanBeSorted = true },
					new GridViewColumnDesc(ModuleBreakpointsWindowColumnIds.Name, dnSpy_Debugger_Resources.Column_Name),
					new GridViewColumnDesc(ModuleBreakpointsWindowColumnIds.DynamicModule, dnSpy_Debugger_Resources.Column_DynamicModule),
					new GridViewColumnDesc(ModuleBreakpointsWindowColumnIds.InMemoryModule, dnSpy_Debugger_Resources.Column_InMemoryModule),
					new GridViewColumnDesc(ModuleBreakpointsWindowColumnIds.Order, dnSpy_Debugger_Resources.Column_Order),
					new GridViewColumnDesc(ModuleBreakpointsWindowColumnIds.Process, dnSpy_Debugger_Resources.Column_Process),
					new GridViewColumnDesc(ModuleBreakpointsWindowColumnIds.AppDomain, dnSpy_Debugger_Resources.Column_AppDomain),
				},
			};
			Descs.SortedColumnChanged += (a, b) => SortList();
		}
		// Don't change the order of these instances without also updating input passed to SearchMatcher.IsMatchAll()
		static readonly SearchColumnDefinition[] searchColumnDefinitions = new SearchColumnDefinition[] {
			new SearchColumnDefinition(PredefinedTextClassifierTags.ModuleBreakpointsWindowModuleName, "n", dnSpy_Debugger_Resources.Column_Name),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ModuleBreakpointsWindowOrder, "o", dnSpy_Debugger_Resources.Column_Order),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ModuleBreakpointsWindowProcessName, "p", dnSpy_Debugger_Resources.Column_Process),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ModuleBreakpointsWindowModuleAppDomainName, "ad", dnSpy_Debugger_Resources.Column_AppDomain),
		};

		// UI thread
		public string GetSearchHelpText() {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			return moduleBreakpointContext.SearchMatcher.GetHelpText();
		}

		// random thread
		void DbgThread(Action callback) =>
			dbgManager.Value.Dispatcher.BeginInvoke(callback);

		// UI thread
		void ILazyToolWindowVM.Show() {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: true);
		}

		// UI thread
		void ILazyToolWindowVM.Hide() {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: false);
		}

		// UI thread
		void InitializeDebugger_UI(bool enable) {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			ResetSearchSettings();
			if (enable) {
				moduleBreakpointContext.ClassificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
				RecreateFormatter_UI();
				moduleBreakpointContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
			}
			else {
				moduleBreakpointContext.ClassificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged -= DebuggerSettings_PropertyChanged;
			}
			DbgThread(() => InitializeDebugger_DbgThread(enable));
		}

		// DbgManager thread
		void InitializeDebugger_DbgThread(bool enable) {
			dbgManager.Value.Dispatcher.VerifyAccess();
			if (enable) {
				dbgModuleBreakpointsService.Value.BreakpointsChanged += DbgModuleBreakpointsService_BreakpointsChanged;
				dbgModuleBreakpointsService.Value.BreakpointsModified += DbgModuleBreakpointsService_BreakpointsModified;
				var breakpoints = dbgModuleBreakpointsService.Value.Breakpoints;
				if (breakpoints.Length > 0)
					UI(() => AddItems_UI(breakpoints));
			}
			else {
				dbgModuleBreakpointsService.Value.BreakpointsChanged -= DbgModuleBreakpointsService_BreakpointsChanged;
				dbgModuleBreakpointsService.Value.BreakpointsModified -= DbgModuleBreakpointsService_BreakpointsModified;
				UI(() => RemoveAllModuleBreakpoints_UI());
			}
		}

		// UI thread
		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			RefreshThemeFields_UI();
		}

		// random thread
		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => DebuggerSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DebuggerSettings_PropertyChanged_UI(string propertyName) {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			if (propertyName == nameof(DebuggerSettings.SyntaxHighlight)) {
				moduleBreakpointContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				RefreshThemeFields_UI();
			}
		}

		// UI thread
		void RefreshThemeFields_UI() {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			foreach (var vm in realAllItems)
				vm.RefreshThemeFields_UI();
		}

		// UI thread
		void RecreateFormatter_UI() {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			moduleBreakpointContext.Formatter = moduleBreakpointFormatterProvider.Create();
		}

		// random thread
		void UI(Action callback) => moduleBreakpointContext.UIDispatcher.UI(callback);

		// DbgManager thread
		void DbgModuleBreakpointsService_BreakpointsChanged(object sender, DbgCollectionChangedEventArgs<DbgModuleBreakpoint> e) {
			dbgManager.Value.Dispatcher.VerifyAccess();
			if (e.Added)
				UI(() => AddItems_UI(e.Objects));
			else {
				UI(() => {
					var coll = realAllItems;
					for (int i = coll.Count - 1; i >= 0; i--) {
						if (e.Objects.Contains(coll[i].ModuleBreakpoint))
							RemoveModuleBreakpointAt_UI(i);
					}
					InitializeNothingMatched();
				});
			}
		}

		// DbgManager thread
		void DbgModuleBreakpointsService_BreakpointsModified(object sender, DbgBreakpointsModifiedEventArgs e) {
			dbgManager.Value.Dispatcher.VerifyAccess();
			UI(() => {
				foreach (var info in e.Breakpoints) {
					bool b = bpToVM.TryGetValue(info.Breakpoint, out var vm);
					Debug.Assert(b);
					if (b)
						vm.UpdateSettings_UI(info.Breakpoint.Settings);
				}
			});
		}

		// UI thread
		void AddItems_UI(IList<DbgModuleBreakpoint> moduleBreakpoints) {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			foreach (var bp in moduleBreakpoints) {
				var vm = new ModuleBreakpointVM(bp, moduleBreakpointContext, moduleBreakpointOrder++, ModuleNameEditValueProvider, OrderEditValueProvider, ProcessNameEditValueProvider, AppDomainNameEditValueProvider);
				Debug.Assert(!bpToVM.ContainsKey(bp));
				bpToVM[bp] = vm;
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
		int GetInsertionIndex_UI(ModuleBreakpointVM vm) {
			Debug.Assert(moduleBreakpointContext.UIDispatcher.CheckAccess());
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
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			if (string.IsNullOrWhiteSpace(filterText))
				filterText = string.Empty;
			moduleBreakpointContext.SearchMatcher.SetSearchText(filterText);
			SortList(filterText);
		}

		// UI thread
		void SortList() {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			SortList(filterText);
		}

		// UI thread
		void SortList(string filterText) {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			var newList = new List<ModuleBreakpointVM>(GetFilteredItems_UI(filterText));
			newList.Sort(this);
			AllItems.Reset(newList);
			InitializeNothingMatched(filterText);
		}

		// UI thread
		IEnumerable<ModuleBreakpointVM> IModuleBreakpointsVM.Sort(IEnumerable<ModuleBreakpointVM> moduleBreakpoints) {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			var list = new List<ModuleBreakpointVM>(moduleBreakpoints);
			list.Sort(this);
			return list;
		}

		void InitializeNothingMatched() => InitializeNothingMatched(filterText);
		void InitializeNothingMatched(string filterText) =>
			NothingMatched = AllItems.Count == 0 && !string.IsNullOrWhiteSpace(filterText);

		public int Compare(ModuleBreakpointVM x, ModuleBreakpointVM y) {
			Debug.Assert(moduleBreakpointContext.UIDispatcher.CheckAccess());
			var (desc, dir) = Descs.SortedColumn;

			int id;
			if (desc == null || dir == GridViewSortDirection.Default) {
				id = ModuleBreakpointsWindowColumnIds.Default_Order;
				dir = GridViewSortDirection.Ascending;
			}
			else
				id = desc.Id;

			int diff;
			switch (id) {
			case ModuleBreakpointsWindowColumnIds.Default_Order:
				diff = x.Order - y.Order;
				break;

			case ModuleBreakpointsWindowColumnIds.IsEnabled:
				diff = Comparer<bool>.Default.Compare(x.IsEnabled, y.IsEnabled);
				break;

			case ModuleBreakpointsWindowColumnIds.Name:
				diff = StringComparer.OrdinalIgnoreCase.Compare(x.ModuleBreakpoint.ModuleName, y.ModuleBreakpoint.ModuleName);
				break;

			case ModuleBreakpointsWindowColumnIds.DynamicModule:
				diff = Comparer<bool?>.Default.Compare(x.IsDynamic, y.IsDynamic);
				break;

			case ModuleBreakpointsWindowColumnIds.InMemoryModule:
				diff = Comparer<bool?>.Default.Compare(x.IsInMemory, y.IsInMemory);
				break;

			case ModuleBreakpointsWindowColumnIds.Order:
				diff = Comparer<int?>.Default.Compare(x.ModuleBreakpoint.Order, y.ModuleBreakpoint.Order);
				break;

			case ModuleBreakpointsWindowColumnIds.Process:
				diff = StringComparer.OrdinalIgnoreCase.Compare(x.ModuleBreakpoint.ProcessName, y.ModuleBreakpoint.ProcessName);
				break;

			case ModuleBreakpointsWindowColumnIds.AppDomain:
				diff = StringComparer.OrdinalIgnoreCase.Compare(x.ModuleBreakpoint.AppDomainName, y.ModuleBreakpoint.AppDomainName);
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
		IEnumerable<ModuleBreakpointVM> GetFilteredItems_UI(string filterText) {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			foreach (var vm in realAllItems) {
				if (IsMatch_UI(vm, filterText))
					yield return vm;
			}
		}

		// UI thread
		bool IsMatch_UI(ModuleBreakpointVM vm, string filterText) {
			Debug.Assert(moduleBreakpointContext.UIDispatcher.CheckAccess());
			// Common case check, we don't need to allocate any strings
			if (filterText == string.Empty)
				return true;
			// The order must match searchColumnDefinitions
			var allStrings = new string[] {
				GetModuleName_UI(vm),
				GetOrder_UI(vm),
				GetProcessName_UI(vm),
				GetAppDomainName_UI(vm),
			};
			sbOutput.Reset();
			return moduleBreakpointContext.SearchMatcher.IsMatchAll(allStrings);
		}
		readonly DbgStringBuilderTextWriter sbOutput = new DbgStringBuilderTextWriter();

		// UI thread
		string GetModuleName_UI(ModuleBreakpointVM vm) {
			Debug.Assert(moduleBreakpointContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			moduleBreakpointContext.Formatter.WriteModuleName(sbOutput, vm.ModuleBreakpoint);
			return sbOutput.ToString();
		}

		// UI thread
		string GetOrder_UI(ModuleBreakpointVM vm) {
			Debug.Assert(moduleBreakpointContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			moduleBreakpointContext.Formatter.WriteOrder(sbOutput, vm.ModuleBreakpoint);
			return sbOutput.ToString();
		}

		// UI thread
		string GetProcessName_UI(ModuleBreakpointVM vm) {
			Debug.Assert(moduleBreakpointContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			moduleBreakpointContext.Formatter.WriteProcessName(sbOutput, vm.ModuleBreakpoint);
			return sbOutput.ToString();
		}

		// UI thread
		string GetAppDomainName_UI(ModuleBreakpointVM vm) {
			Debug.Assert(moduleBreakpointContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			moduleBreakpointContext.Formatter.WriteAppDomainName(sbOutput, vm.ModuleBreakpoint);
			return sbOutput.ToString();
		}

		// UI thread
		void RemoveModuleBreakpointAt_UI(int i) {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			Debug.Assert(0 <= i && i < realAllItems.Count);
			var vm = realAllItems[i];
			bool b = bpToVM.Remove(vm.ModuleBreakpoint);
			Debug.Assert(b);
			vm.Dispose();
			realAllItems.RemoveAt(i);
			AllItems.Remove(vm);
		}

		// UI thread
		void RemoveAllModuleBreakpoints_UI() {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			AllItems.Reset(Array.Empty<ModuleBreakpointVM>());
			var coll = realAllItems;
			for (int i = coll.Count - 1; i >= 0; i--)
				RemoveModuleBreakpointAt_UI(i);
			Debug.Assert(bpToVM.Count == 0);
		}

		// UI thread
		public void ResetSearchSettings() {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			FilterText = string.Empty;
		}
	}
}

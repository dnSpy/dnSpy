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
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.ToolWindows.Search;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.Modules {
	interface IModulesVM : IGridViewColumnDescsProvider {
		bool IsOpen { get; set; }
		bool IsVisible { get; set; }
		BulkObservableCollection<ModuleVM> AllItems { get; }
		ObservableCollection<ModuleVM> SelectedItems { get; }
		void ResetSearchSettings();
		string GetSearchHelpText();
		IEnumerable<ModuleVM> Sort(IEnumerable<ModuleVM> modules);
	}

	[Export(typeof(IModulesVM))]
	sealed class ModulesVM : ViewModelBase, IModulesVM, ILazyToolWindowVM, IComparer<ModuleVM> {
		public BulkObservableCollection<ModuleVM> AllItems { get; }
		public ObservableCollection<ModuleVM> SelectedItems { get; }
		public GridViewColumnDescs Descs { get; }

		public bool IsOpen {
			get => lazyToolWindowVMHelper.IsOpen;
			set => lazyToolWindowVMHelper.IsOpen = value;
		}

		public bool IsVisible {
			get => lazyToolWindowVMHelper.IsVisible;
			set => lazyToolWindowVMHelper.IsVisible = value;
		}

		public object ProcessCollection => processes;
		readonly ObservableCollection<SimpleProcessVM> processes;

		public object SelectedProcess {
			get => selectedProcess;
			set {
				if (selectedProcess != value) {
					selectedProcess = (SimpleProcessVM)value;
					OnPropertyChanged(nameof(SelectedProcess));
					FilterList_UI(filterText, selectedProcess);
				}
			}
		}
		SimpleProcessVM selectedProcess;

		public string FilterText {
			get => filterText;
			set {
				if (filterText == value)
					return;
				filterText = value;
				OnPropertyChanged(nameof(FilterText));
				FilterList_UI(filterText, selectedProcess);
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

		readonly Lazy<DbgManager> dbgManager;
		readonly ModuleContext moduleContext;
		readonly ModuleFormatterProvider moduleFormatterProvider;
		readonly DebuggerSettings debuggerSettings;
		readonly LazyToolWindowVMHelper lazyToolWindowVMHelper;
		readonly List<ModuleVM> realAllItems;
		int moduleOrder;

		[ImportingConstructor]
		ModulesVM(Lazy<DbgManager> dbgManager, DebuggerSettings debuggerSettings, UIDispatcher uiDispatcher, ModuleFormatterProvider moduleFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider) {
			uiDispatcher.VerifyAccess();
			realAllItems = new List<ModuleVM>();
			AllItems = new BulkObservableCollection<ModuleVM>();
			SelectedItems = new ObservableCollection<ModuleVM>();
			processes = new ObservableCollection<SimpleProcessVM>();
			this.dbgManager = dbgManager;
			this.moduleFormatterProvider = moduleFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			lazyToolWindowVMHelper = new DebuggerLazyToolWindowVMHelper(this, uiDispatcher, dbgManager);
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			moduleContext = new ModuleContext(uiDispatcher, classificationFormatMap, textElementProvider, new SearchMatcher(searchColumnDefinitions)) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlight,
				Formatter = moduleFormatterProvider.Create(),
			};
			Descs = new GridViewColumnDescs {
				Columns = new GridViewColumnDesc[] {
					new GridViewColumnDesc(ModulesWindowColumnIds.Icon, string.Empty),
					new GridViewColumnDesc(ModulesWindowColumnIds.Name, dnSpy_Debugger_Resources.Column_Name),
					new GridViewColumnDesc(ModulesWindowColumnIds.OptimizedModule, dnSpy_Debugger_Resources.Column_OptimizedModule),
					new GridViewColumnDesc(ModulesWindowColumnIds.DynamicModule, dnSpy_Debugger_Resources.Column_DynamicModule),
					new GridViewColumnDesc(ModulesWindowColumnIds.InMemoryModule, dnSpy_Debugger_Resources.Column_InMemoryModule),
					new GridViewColumnDesc(ModulesWindowColumnIds.Order, dnSpy_Debugger_Resources.Column_Order),
					new GridViewColumnDesc(ModulesWindowColumnIds.Version, dnSpy_Debugger_Resources.Column_Version),
					new GridViewColumnDesc(ModulesWindowColumnIds.Timestamp, dnSpy_Debugger_Resources.Column_Timestamp),
					new GridViewColumnDesc(ModulesWindowColumnIds.Address, dnSpy_Debugger_Resources.Column_Address),
					new GridViewColumnDesc(ModulesWindowColumnIds.Process, dnSpy_Debugger_Resources.Column_Process),
					new GridViewColumnDesc(ModulesWindowColumnIds.AppDomain, dnSpy_Debugger_Resources.Column_AppDomain),
					new GridViewColumnDesc(ModulesWindowColumnIds.Path, dnSpy_Debugger_Resources.Column_Path),
				},
			};
			Descs.SortedColumnChanged += (a, b) => SortList();
		}

		// Don't change the order of these instances without also updating input passed to SearchMatcher.IsMatchAll()
		static readonly SearchColumnDefinition[] searchColumnDefinitions = new SearchColumnDefinition[] {
			new SearchColumnDefinition(PredefinedTextClassifierTags.ModulesWindowName, "n", dnSpy_Debugger_Resources.Column_Name),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ModulesWindowOptimized, "opt", dnSpy_Debugger_Resources.Column_OptimizedModule),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ModulesWindowDynamic, "dyn", dnSpy_Debugger_Resources.Column_DynamicModule),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ModulesWindowInMemory, "mem", dnSpy_Debugger_Resources.Column_InMemoryModule),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ModulesWindowOrder, "o", dnSpy_Debugger_Resources.Column_Order),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ModulesWindowVersion, "v", dnSpy_Debugger_Resources.Column_Version),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ModulesWindowTimestamp, "ts", dnSpy_Debugger_Resources.Column_Timestamp),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ModulesWindowAddress, "a", dnSpy_Debugger_Resources.Column_Address),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ModulesWindowProcess, "p", dnSpy_Debugger_Resources.Column_Process),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ModulesWindowAppDomain, "ad", dnSpy_Debugger_Resources.Column_AppDomain),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ModulesWindowPath, "f", dnSpy_Debugger_Resources.Column_Path),
		};

		// UI thread
		public string GetSearchHelpText() {
			moduleContext.UIDispatcher.VerifyAccess();
			return moduleContext.SearchMatcher.GetHelpText();
		}

		// random thread
		void DbgThread(Action callback) =>
			dbgManager.Value.Dispatcher.BeginInvoke(callback);

		// UI thread
		void ILazyToolWindowVM.Show() {
			moduleContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: true);
		}

		// UI thread
		void ILazyToolWindowVM.Hide() {
			moduleContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: false);
		}

		// UI thread
		void InitializeDebugger_UI(bool enable) {
			moduleContext.UIDispatcher.VerifyAccess();
			if (processes.Count == 0)
				InitializeProcesses_UI();
			ResetSearchSettings();
			if (enable) {
				moduleContext.ClassificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
				RecreateFormatter_UI();
				moduleContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
			}
			else {
				processes.Clear();
				moduleContext.ClassificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged -= DebuggerSettings_PropertyChanged;
			}
			DbgThread(() => InitializeDebugger_DbgThread(enable));
		}

		// UI thread
		void InitializeProcesses_UI() {
			moduleContext.UIDispatcher.VerifyAccess();
			if (processes.Count != 0)
				return;
			processes.Add(new SimpleProcessVM(dnSpy_Debugger_Resources.Modules_AllProcesses));
			SelectedProcess = processes[0];
		}

		// DbgManager thread
		void InitializeDebugger_DbgThread(bool enable) {
			dbgManager.Value.Dispatcher.VerifyAccess();
			if (enable) {
				dbgManager.Value.ProcessesChanged += DbgManager_ProcessesChanged;
				var modules = new List<DbgModule>();
				var processes = dbgManager.Value.Processes;
				foreach (var p in processes) {
					InitializeProcess_DbgThread(p);
					foreach (var r in p.Runtimes) {
						InitializeRuntime_DbgThread(r);
						modules.AddRange(r.Modules);
						foreach (var a in r.AppDomains)
							InitializeAppDomain_DbgThread(a);
					}
				}
				if (modules.Count > 0 || processes.Length > 0) {
					UI(() => {
						AddItems_UI(modules);
						AddItems_UI(processes);
					});
				}
			}
			else {
				dbgManager.Value.ProcessesChanged -= DbgManager_ProcessesChanged;
				foreach (var p in dbgManager.Value.Processes) {
					DeinitializeProcess_DbgThread(p);
					foreach (var r in p.Runtimes) {
						DeinitializeRuntime_DbgThread(r);
						foreach (var a in r.AppDomains)
							DeinitializeAppDomain_DbgThread(a);
					}
				}
				UI(() => RemoveAllModules_UI());
			}
		}

		// DbgManager thread
		void InitializeProcess_DbgThread(DbgProcess process) {
			process.DbgManager.Dispatcher.VerifyAccess();
			process.RuntimesChanged += DbgProcess_RuntimesChanged;
		}

		// DbgManager thread
		void DeinitializeProcess_DbgThread(DbgProcess process) {
			process.DbgManager.Dispatcher.VerifyAccess();
			process.RuntimesChanged -= DbgProcess_RuntimesChanged;
		}

		// DbgManager thread
		void InitializeRuntime_DbgThread(DbgRuntime runtime) {
			runtime.Process.DbgManager.Dispatcher.VerifyAccess();
			runtime.AppDomainsChanged += DbgRuntime_AppDomainsChanged;
			runtime.ModulesChanged += DbgRuntime_ModulesChanged;
		}

		// DbgManager thread
		void DeinitializeRuntime_DbgThread(DbgRuntime runtime) {
			runtime.Process.DbgManager.Dispatcher.VerifyAccess();
			runtime.AppDomainsChanged -= DbgRuntime_AppDomainsChanged;
			runtime.ModulesChanged -= DbgRuntime_ModulesChanged;
		}

		// DbgManager thread
		void InitializeAppDomain_DbgThread(DbgAppDomain appDomain) {
			appDomain.Process.DbgManager.Dispatcher.VerifyAccess();
			appDomain.PropertyChanged += DbgAppDomain_PropertyChanged;
		}

		// DbgManager thread
		void DeinitializeAppDomain_DbgThread(DbgAppDomain appDomain) {
			appDomain.Process.DbgManager.Dispatcher.VerifyAccess();
			appDomain.PropertyChanged -= DbgAppDomain_PropertyChanged;
		}

		// UI thread
		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			moduleContext.UIDispatcher.VerifyAccess();
			RefreshThemeFields_UI();
		}

		// random thread
		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => DebuggerSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DebuggerSettings_PropertyChanged_UI(string propertyName) {
			moduleContext.UIDispatcher.VerifyAccess();
			if (propertyName == nameof(DebuggerSettings.UseHexadecimal))
				RefreshHexFields_UI();
			else if (propertyName == nameof(DebuggerSettings.SyntaxHighlight)) {
				moduleContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				RefreshThemeFields_UI();
			}
		}

		// UI thread
		void RefreshThemeFields_UI() {
			moduleContext.UIDispatcher.VerifyAccess();
			foreach (var vm in realAllItems)
				vm.RefreshThemeFields_UI();
		}

		// UI thread
		void RecreateFormatter_UI() {
			moduleContext.UIDispatcher.VerifyAccess();
			moduleContext.Formatter = moduleFormatterProvider.Create();
		}

		// UI thread
		void RefreshHexFields_UI() {
			moduleContext.UIDispatcher.VerifyAccess();
			RecreateFormatter_UI();
			foreach (var vm in realAllItems)
				vm.RefreshHexFields_UI();
			foreach (var vm in processes)
				vm.UpdateName(debuggerSettings.UseHexadecimal);
		}

		// random thread
		void UI(Action callback) => moduleContext.UIDispatcher.UI(callback);

		// DbgManager thread
		void DbgManager_ProcessesChanged(object sender, DbgCollectionChangedEventArgs<DbgProcess> e) {
			if (e.Added) {
				foreach (var p in e.Objects)
					InitializeProcess_DbgThread(p);
				UI(() => AddItems_UI(e.Objects));
			}
			else {
				foreach (var p in e.Objects)
					DeinitializeProcess_DbgThread(p);
				UI(() => {
					var coll = realAllItems;
					for (int i = coll.Count - 1; i >= 0; i--) {
						var moduleProcess = coll[i].Module.Process;
						foreach (var p in e.Objects) {
							if (p == moduleProcess) {
								RemoveModuleAt_UI(i);
								break;
							}
						}
					}
					foreach (var p in e.Objects)
						RemoveProcess_UI(p);
					InitializeNothingMatched();
				});
			}
		}

		// DbgManager thread
		void DbgProcess_RuntimesChanged(object sender, DbgCollectionChangedEventArgs<DbgRuntime> e) {
			if (e.Added) {
				foreach (var r in e.Objects)
					InitializeRuntime_DbgThread(r);
			}
			else {
				foreach (var r in e.Objects)
					DeinitializeRuntime_DbgThread(r);
				UI(() => {
					var coll = realAllItems;
					for (int i = coll.Count - 1; i >= 0; i--) {
						var moduleRuntime = coll[i].Module.Runtime;
						foreach (var r in e.Objects) {
							if (r == moduleRuntime) {
								RemoveModuleAt_UI(i);
								break;
							}
						}
					}
					InitializeNothingMatched();
				});
			}
		}

		// DbgManager thread
		void DbgRuntime_AppDomainsChanged(object sender, DbgCollectionChangedEventArgs<DbgAppDomain> e) {
			if (e.Added) {
				foreach (var a in e.Objects)
					InitializeAppDomain_DbgThread(a);
			}
			else {
				foreach (var a in e.Objects)
					DeinitializeAppDomain_DbgThread(a);
				UI(() => {
					var coll = realAllItems;
					for (int i = coll.Count - 1; i >= 0; i--) {
						var moduleAppDomain = coll[i].Module.AppDomain;
						if (moduleAppDomain == null)
							continue;
						foreach (var a in e.Objects) {
							if (a == moduleAppDomain) {
								RemoveModuleAt_UI(i);
								break;
							}
						}
					}
					InitializeNothingMatched();
				});
			}
		}

		// DbgManager thread
		void DbgRuntime_ModulesChanged(object sender, DbgCollectionChangedEventArgs<DbgModule> e) {
			if (e.Added)
				UI(() => AddItems_UI(e.Objects));
			else {
				UI(() => {
					foreach (var m in e.Objects)
						RemoveModule_UI(m);
					InitializeNothingMatched();
				});
			}
		}

		// DbgManager thread
		void DbgAppDomain_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(DbgAppDomain.Name) || e.PropertyName == nameof(DbgAppDomain.Id)) {
				UI(() => {
					var appDomain = (DbgAppDomain)sender;
					foreach (var vm in realAllItems)
						vm.RefreshAppDomainNames_UI(appDomain);
				});
			}
		}

		// UI thread
		void AddItems_UI(IList<DbgModule> modules) {
			moduleContext.UIDispatcher.VerifyAccess();
			foreach (var m in modules) {
				var vm = new ModuleVM(m, moduleContext, moduleOrder++);
				realAllItems.Add(vm);
				if (IsMatch_UI(vm, filterText, selectedProcess)) {
					int insertionIndex = GetInsertionIndex_UI(vm);
					AllItems.Insert(insertionIndex, vm);
				}
			}
			if (NothingMatched && AllItems.Count != 0)
				NothingMatched = false;
		}

		// UI thread
		int GetInsertionIndex_UI(ModuleVM vm) {
			Debug.Assert(moduleContext.UIDispatcher.CheckAccess());
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
		void FilterList_UI(string filterText, SimpleProcessVM selectedProcess) {
			moduleContext.UIDispatcher.VerifyAccess();
			if (string.IsNullOrWhiteSpace(filterText))
				filterText = string.Empty;
			moduleContext.SearchMatcher.SetSearchText(filterText);
			SortList(filterText, selectedProcess);
		}

		// UI thread
		void SortList() {
			moduleContext.UIDispatcher.VerifyAccess();
			SortList(filterText, selectedProcess);
		}

		// UI thread
		void SortList(string filterText, SimpleProcessVM selectedProcess) {
			moduleContext.UIDispatcher.VerifyAccess();
			var newList = new List<ModuleVM>(GetFilteredItems_UI(filterText, selectedProcess));
			newList.Sort(this);
			AllItems.Reset(newList);
			InitializeNothingMatched(filterText, selectedProcess);
		}

		// UI thread
		IEnumerable<ModuleVM> IModulesVM.Sort(IEnumerable<ModuleVM> modules) {
			moduleContext.UIDispatcher.VerifyAccess();
			var list = new List<ModuleVM>(modules);
			list.Sort(this);
			return list;
		}

		void InitializeNothingMatched() => InitializeNothingMatched(filterText, selectedProcess);
		void InitializeNothingMatched(string filterText, SimpleProcessVM selectedProcess) =>
			NothingMatched = AllItems.Count == 0 && !(string.IsNullOrWhiteSpace(filterText) && selectedProcess?.Process == null);

		public int Compare(ModuleVM x, ModuleVM y) {
			Debug.Assert(moduleContext.UIDispatcher.CheckAccess());
			var (desc, dir) = Descs.SortedColumn;

			int id;
			if (desc == null || dir == GridViewSortDirection.Default) {
				id = ModulesWindowColumnIds.Default_Order;
				dir = GridViewSortDirection.Ascending;
			}
			else
				id = desc.Id;

			int diff;
			switch (id) {
			case ModulesWindowColumnIds.Default_Order:
				diff = x.Order - y.Order;
				break;

			case ModulesWindowColumnIds.Icon:
				Debug.Fail("Icon column can't be sorted");
				diff = 0;
				break;

			case ModulesWindowColumnIds.Name:
				diff = StringComparer.OrdinalIgnoreCase.Compare(GetName_UI(x), GetName_UI(y));
				break;

			case ModulesWindowColumnIds.OptimizedModule:
				diff = Comparer<bool?>.Default.Compare(x.Module.IsOptimized, y.Module.IsOptimized);
				break;

			case ModulesWindowColumnIds.DynamicModule:
				diff = Comparer<bool?>.Default.Compare(x.Module.IsDynamic, y.Module.IsDynamic);
				break;

			case ModulesWindowColumnIds.InMemoryModule:
				diff = Comparer<bool?>.Default.Compare(x.Module.IsInMemory, y.Module.IsInMemory);
				break;

			case ModulesWindowColumnIds.Order:
				diff = x.Module.Order - y.Module.Order;
				break;

			case ModulesWindowColumnIds.Version:
				diff = new ModuleVersion(x.Module.Version).CompareTo(new ModuleVersion(y.Module.Version));
				break;

			case ModulesWindowColumnIds.Timestamp:
				diff = Comparer<DateTime?>.Default.Compare(x.Module.Timestamp, y.Module.Timestamp);
				break;

			case ModulesWindowColumnIds.Address:
				diff = x.Module.Address.CompareTo(y.Module.Address);
				if (diff == 0)
					diff = x.Module.Size.CompareTo(y.Module.Size);
				break;

			case ModulesWindowColumnIds.Process:
				diff = StringComparer.OrdinalIgnoreCase.Compare(GetProcess_UI(x), GetProcess_UI(y));
				break;

			case ModulesWindowColumnIds.AppDomain:
				diff = StringComparer.OrdinalIgnoreCase.Compare(GetAppDomain_UI(x), GetAppDomain_UI(y));
				break;

			case ModulesWindowColumnIds.Path:
				diff = StringComparer.OrdinalIgnoreCase.Compare(x.Module.Filename, y.Module.Filename);
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
		IEnumerable<ModuleVM> GetFilteredItems_UI(string filterText, SimpleProcessVM selectedProcess) {
			moduleContext.UIDispatcher.VerifyAccess();
			foreach (var vm in realAllItems) {
				if (IsMatch_UI(vm, filterText, selectedProcess))
					yield return vm;
			}
		}

		// UI thread
		bool IsMatch_UI(ModuleVM vm, string filterText, SimpleProcessVM selectedProcess) {
			Debug.Assert(moduleContext.UIDispatcher.CheckAccess());
			if (selectedProcess?.Process != null && selectedProcess.Process != vm.Module.Process)
				return false;
			// Common case check, we don't need to allocate any strings
			if (filterText == string.Empty)
				return true;
			// The order must match searchColumnDefinitions
			var allStrings = new string[] {
				GetName_UI(vm),
				GetOptimized_UI(vm),
				GetDynamic_UI(vm),
				GetInMemory_UI(vm),
				GetOrder_UI(vm),
				GetVersion_UI(vm),
				GetTimestamp_UI(vm),
				GetAddress_UI(vm),
				GetProcess_UI(vm),
				GetAppDomain_UI(vm),
				GetPath_UI(vm),
			};
			sbOutput.Reset();
			return moduleContext.SearchMatcher.IsMatchAll(allStrings);
		}
		readonly DbgStringBuilderTextWriter sbOutput = new DbgStringBuilderTextWriter();

		// UI thread
		string GetName_UI(ModuleVM vm) {
			Debug.Assert(moduleContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			moduleContext.Formatter.WriteName(sbOutput, vm.Module);
			return sbOutput.ToString();
		}

		// UI thread
		string GetPath_UI(ModuleVM vm) {
			Debug.Assert(moduleContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			moduleContext.Formatter.WritePath(sbOutput, vm.Module);
			return sbOutput.ToString();
		}

		// UI thread
		string GetOptimized_UI(ModuleVM vm) {
			Debug.Assert(moduleContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			moduleContext.Formatter.WriteOptimized(sbOutput, vm.Module);
			return sbOutput.ToString();
		}

		// UI thread
		string GetDynamic_UI(ModuleVM vm) {
			Debug.Assert(moduleContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			moduleContext.Formatter.WriteDynamic(sbOutput, vm.Module);
			return sbOutput.ToString();
		}

		// UI thread
		string GetInMemory_UI(ModuleVM vm) {
			Debug.Assert(moduleContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			moduleContext.Formatter.WriteInMemory(sbOutput, vm.Module);
			return sbOutput.ToString();
		}

		// UI thread
		string GetOrder_UI(ModuleVM vm) {
			Debug.Assert(moduleContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			moduleContext.Formatter.WriteOrder(sbOutput, vm.Module);
			return sbOutput.ToString();
		}

		// UI thread
		string GetVersion_UI(ModuleVM vm) {
			Debug.Assert(moduleContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			moduleContext.Formatter.WriteVersion(sbOutput, vm.Module);
			return sbOutput.ToString();
		}

		// UI thread
		string GetTimestamp_UI(ModuleVM vm) {
			Debug.Assert(moduleContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			moduleContext.Formatter.WriteTimestamp(sbOutput, vm.Module);
			return sbOutput.ToString();
		}

		// UI thread
		string GetAddress_UI(ModuleVM vm) {
			Debug.Assert(moduleContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			moduleContext.Formatter.WriteAddress(sbOutput, vm.Module);
			return sbOutput.ToString();
		}

		// UI thread
		string GetProcess_UI(ModuleVM vm) {
			Debug.Assert(moduleContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			moduleContext.Formatter.WriteProcess(sbOutput, vm.Module);
			return sbOutput.ToString();
		}

		// UI thread
		string GetAppDomain_UI(ModuleVM vm) {
			Debug.Assert(moduleContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			moduleContext.Formatter.WriteAppDomain(sbOutput, vm.Module);
			return sbOutput.ToString();
		}

		// UI thread
		void RemoveModuleAt_UI(int i) {
			moduleContext.UIDispatcher.VerifyAccess();
			Debug.Assert(0 <= i && i < realAllItems.Count);
			var vm = realAllItems[i];
			vm.Dispose();
			realAllItems.RemoveAt(i);
			AllItems.Remove(vm);
		}

		// UI thread
		void RemoveModule_UI(DbgModule module) {
			moduleContext.UIDispatcher.VerifyAccess();
			var coll = realAllItems;
			for (int i = 0; i < coll.Count; i++) {
				if (coll[i].Module == module) {
					RemoveModuleAt_UI(i);
					break;
				}
			}
		}

		// UI thread
		void RemoveAllModules_UI() {
			moduleContext.UIDispatcher.VerifyAccess();
			AllItems.Reset(Array.Empty<ModuleVM>());
			var coll = realAllItems;
			for (int i = coll.Count - 1; i >= 0; i--)
				RemoveModuleAt_UI(i);
		}

		// UI thread
		void AddItems_UI(IList<DbgProcess> newProcesses) {
			moduleContext.UIDispatcher.VerifyAccess();
			foreach (var p in newProcesses) {
				var vm = new SimpleProcessVM(p, debuggerSettings.UseHexadecimal);
				int insertionIndex = GetInsertionIndex_UI(vm);
				processes.Insert(insertionIndex, vm);
			}
		}

		// UI thread
		void RemoveProcess_UI(DbgProcess process) {
			moduleContext.UIDispatcher.VerifyAccess();
			for (int i = 0; i < processes.Count; i++) {
				if (processes[i].Process == process) {
					processes.RemoveAt(i);
					break;
				}
			}
			if (selectedProcess == null || selectedProcess.Process == process)
				SelectedProcess = processes.FirstOrDefault();
		}

		// UI thread
		int GetInsertionIndex_UI(SimpleProcessVM vm) {
			Debug.Assert(moduleContext.UIDispatcher.CheckAccess());
			var comparer = SimpleProcessVMComparer.Instance;
			var list = processes;
			int lo = 0, hi = list.Count - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				int c = comparer.Compare(vm, list[index]);
				if (c < 0)
					hi = index - 1;
				else if (c > 0)
					lo = index + 1;
				else
					return index;
			}
			return hi + 1;
		}

		sealed class SimpleProcessVMComparer : IComparer<SimpleProcessVM> {
			public static readonly SimpleProcessVMComparer Instance = new SimpleProcessVMComparer();
			SimpleProcessVMComparer() { }
			public int Compare(SimpleProcessVM x, SimpleProcessVM y) {
				bool x1 = x.Process == null;
				bool y1 = y.Process == null;
				if (x1 != y1) {
					if (x1)
						return -1;
					return 1;
				}
				else if (x1)
					return 0;

				int c = StringComparer.OrdinalIgnoreCase.Compare(x.Process.Name, y.Process.Name);
				if (c != 0)
					return c;
				return x.Process.Id.CompareTo(y.Process.Id);
			}
		}

		// UI thread
		public void ResetSearchSettings() {
			moduleContext.UIDispatcher.VerifyAccess();
			FilterText = string.Empty;
			SelectedProcess = processes.FirstOrDefault();
		}
	}
}

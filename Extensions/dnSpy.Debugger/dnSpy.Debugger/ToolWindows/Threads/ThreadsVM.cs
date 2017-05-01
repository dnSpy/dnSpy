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
using dnSpy.Contracts.Controls.ToolWindows;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.ToolWindows.Search;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.Threads {
	interface IThreadsVM {
		bool IsOpen { get; set; }
		bool IsVisible { get; set; }
		BulkObservableCollection<ThreadVM> AllItems { get; }
		ObservableCollection<ThreadVM> SelectedItems { get; }
		void ResetSearchSettings();
		string GetSearchHelpText();
	}

	[Export(typeof(IThreadsVM))]
	sealed class ThreadsVM : ViewModelBase, IThreadsVM, ILazyToolWindowVM {
		public BulkObservableCollection<ThreadVM> AllItems { get; }
		public ObservableCollection<ThreadVM> SelectedItems { get; }

		public bool IsOpen {
			get => lazyToolWindowVMHelper.IsOpen;
			set => lazyToolWindowVMHelper.IsOpen = value;
		}

		public bool IsVisible {
			get => lazyToolWindowVMHelper.IsVisible;
			set => lazyToolWindowVMHelper.IsVisible = value;
		}

		IEditValueProvider NameEditValueProvider {
			get {
				threadContext.UIDispatcher.VerifyAccess();
				if (nameEditValueProvider == null)
					nameEditValueProvider = editValueProviderService.Create(ContentTypes.ThreadsWindowName, Array.Empty<string>());
				return nameEditValueProvider;
			}
		}
		IEditValueProvider nameEditValueProvider;

		public object ProcessCollection => processes;
		readonly ObservableCollection<SimpleProcessVM> processes;

		public object SelectedProcess {
			get => selectedProcess;
			set {
				if (selectedProcess != value) {
					selectedProcess = (SimpleProcessVM)value;
					OnPropertyChanged(nameof(SelectedProcess));
					delayedSearch.Start();
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
				delayedSearch.Start();
			}
		}
		string filterText = string.Empty;
		readonly DelayedAction delayedSearch;

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

		sealed class ProcessState {
			/// <summary>
			/// Set to true when <see cref="DbgProcess.DelayedIsRunningChanged"/> gets raised
			/// and cleared when the process is paused.
			/// </summary>
			public bool IgnoreThreadsChangedEvent { get; set; }
		}

		readonly Lazy<DbgManager> dbgManager;
		readonly ThreadContext threadContext;
		readonly ThreadFormatterProvider threadFormatterProvider;
		readonly DebuggerSettings debuggerSettings;
		readonly ThreadCategoryService threadCategoryService;
		readonly EditValueProviderService editValueProviderService;
		readonly LazyToolWindowVMHelper lazyToolWindowVMHelper;
		readonly List<ThreadVM> realAllItems;
		int threadOrder;

		[ImportingConstructor]
		ThreadsVM(Lazy<DbgManager> dbgManager, DebuggerSettings debuggerSettings, UIDispatcher uiDispatcher, ThreadFormatterProvider threadFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider, ThreadCategoryService threadCategoryService, EditValueProviderService editValueProviderService) {
			uiDispatcher.VerifyAccess();
			realAllItems = new List<ThreadVM>();
			AllItems = new BulkObservableCollection<ThreadVM>();
			SelectedItems = new ObservableCollection<ThreadVM>();
			processes = new ObservableCollection<SimpleProcessVM>();
			delayedSearch = new DelayedAction(uiDispatcher, SearchConstants.DefaultSearchDelayMilliSeconds, DelayStartSearch_UI);
			this.dbgManager = dbgManager;
			this.threadFormatterProvider = threadFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			lazyToolWindowVMHelper = new DebuggerLazyToolWindowVMHelper(this, uiDispatcher, dbgManager);
			this.threadCategoryService = threadCategoryService;
			this.editValueProviderService = editValueProviderService;
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			threadContext = new ThreadContext(uiDispatcher, classificationFormatMap, textElementProvider, new SearchMatcher(searchColumnDefinitions)) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlight,
				UseHexadecimal = debuggerSettings.UseHexadecimal,
				Formatter = threadFormatterProvider.Create(),
			};
		}
		// Don't change the order of these instances without also updating input passed to SearchMatcher.IsMatchAll()
		static readonly SearchColumnDefinition[] searchColumnDefinitions = new SearchColumnDefinition[] {
			new SearchColumnDefinition(PredefinedTextClassifierTags.ThreadsWindowId, "i", dnSpy_Debugger_Resources.Column_ThreadID),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ThreadsWindowManagedId, "m", dnSpy_Debugger_Resources.Column_ThreadManagedId),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ThreadsWindowCategoryText, "cat", dnSpy_Debugger_Resources.Column_ThreadCategory),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ThreadsWindowName, "n", dnSpy_Debugger_Resources.Column_Name),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ThreadsWindowLocation, "o", dnSpy_Debugger_Resources.Column_ThreadLocation),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ThreadsWindowPriority, "pri", dnSpy_Debugger_Resources.Column_ThreadPriority),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ThreadsWindowAffinityMask, "a", dnSpy_Debugger_Resources.Column_ThreadAffinityMask),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ThreadsWindowSuspended, "sc", dnSpy_Debugger_Resources.Column_ThreadSuspendedCount),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ThreadsWindowProcess, "p", dnSpy_Debugger_Resources.Column_ProcessName),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ThreadsWindowAppDomain, "ad", dnSpy_Debugger_Resources.Column_AppDomain),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ThreadsWindowUserState, "s", dnSpy_Debugger_Resources.Column_ThreadState),
		};

		// UI thread
		void DelayStartSearch_UI() {
			threadContext.UIDispatcher.VerifyAccess();
			delayedSearch.Cancel();
			if (!IsOpen)
				return;
			FilterList_UI(filterText, selectedProcess);
		}

		// UI thread
		public string GetSearchHelpText() {
			threadContext.UIDispatcher.VerifyAccess();
			return threadContext.SearchMatcher.GetHelpText();
		}

		// random thread
		void DbgThread(Action callback) =>
			dbgManager.Value.Dispatcher.BeginInvoke(callback);

		// UI thread
		void ILazyToolWindowVM.Show() {
			threadContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: true);
		}

		// UI thread
		void ILazyToolWindowVM.Hide() {
			threadContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: false);
		}

		// UI thread
		void InitializeDebugger_UI(bool enable) {
			threadContext.UIDispatcher.VerifyAccess();
			if (processes.Count == 0)
				InitializeProcesses_UI();
			ResetSearchSettings();
			delayedSearch.Cancel();
			if (enable) {
				threadContext.ClassificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
				RecreateFormatter_UI();
				threadContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				threadContext.UseHexadecimal = debuggerSettings.UseHexadecimal;
			}
			else {
				processes.Clear();
				threadContext.ClassificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged -= DebuggerSettings_PropertyChanged;
			}
			DbgThread(() => InitializeDebugger_DbgThread(enable));
		}

		// UI thread
		void InitializeProcesses_UI() {
			threadContext.UIDispatcher.VerifyAccess();
			if (processes.Count != 0)
				return;
			processes.Add(new SimpleProcessVM(dnSpy_Debugger_Resources.Threads_AllProcesses));
			SelectedProcess = processes[0];
		}

		// DbgManager thread
		void InitializeDebugger_DbgThread(bool enable) {
			dbgManager.Value.Dispatcher.VerifyAccess();
			if (enable) {
				dbgManager.Value.ProcessesChanged += DbgManager_ProcessesChanged;
				dbgManager.Value.CurrentThreadChanged += DbgManager_CurrentThreadChanged;
				dbgManager.Value.DelayedIsRunningChanged += DbgManager_DelayedIsRunningChanged;
				var threads = new List<DbgThread>();
				var processes = dbgManager.Value.Processes;
				foreach (var p in processes) {
					InitializeProcess_DbgThread(p);
					if (!p.IsRunning)
						threads.AddRange(p.Threads);
					foreach (var r in p.Runtimes) {
						InitializeRuntime_DbgThread(r);
						foreach (var a in r.AppDomains)
							InitializeAppDomain_DbgThread(a);
					}
				}
				if (threads.Count > 0 || processes.Length > 0) {
					UI(() => {
						AddItems_UI(threads);
						AddItems_UI(processes);
					});
				}
			}
			else {
				dbgManager.Value.ProcessesChanged -= DbgManager_ProcessesChanged;
				dbgManager.Value.CurrentThreadChanged -= DbgManager_CurrentThreadChanged;
				dbgManager.Value.DelayedIsRunningChanged -= DbgManager_DelayedIsRunningChanged;
				foreach (var p in dbgManager.Value.Processes) {
					DeinitializeProcess_DbgThread(p);
					foreach (var r in p.Runtimes) {
						DeinitializeRuntime_DbgThread(r);
						foreach (var a in r.AppDomains)
							DeinitializeAppDomain_DbgThread(a);
					}
				}
				UI(() => RemoveAllThreads_UI());
			}
		}

		// DbgManager thread
		void DbgManager_DelayedIsRunningChanged(object sender, EventArgs e) {
			// If all processes are running and the window is hidden, hide it now
			if (!IsVisible)
				UI(() => lazyToolWindowVMHelper.TryHideWindow());
		}

		// DbgManager thread
		void InitializeProcess_DbgThread(DbgProcess process) {
			process.DbgManager.Dispatcher.VerifyAccess();
			var state = process.GetOrCreateData<ProcessState>();
			state.IgnoreThreadsChangedEvent = process.IsRunning;
			process.IsRunningChanged += DbgProcess_IsRunningChanged;
			process.DelayedIsRunningChanged += DbgProcess_DelayedIsRunningChanged;
			process.ThreadsChanged += DbgProcess_ThreadsChanged;
			process.RuntimesChanged += DbgProcess_RuntimesChanged;
		}

		// DbgManager thread
		void DeinitializeProcess_DbgThread(DbgProcess process) {
			process.DbgManager.Dispatcher.VerifyAccess();
			process.IsRunningChanged -= DbgProcess_IsRunningChanged;
			process.DelayedIsRunningChanged -= DbgProcess_DelayedIsRunningChanged;
			process.ThreadsChanged -= DbgProcess_ThreadsChanged;
			process.RuntimesChanged -= DbgProcess_RuntimesChanged;
		}

		// DbgManager thread
		void InitializeRuntime_DbgThread(DbgRuntime runtime) {
			runtime.Process.DbgManager.Dispatcher.VerifyAccess();
			runtime.AppDomainsChanged += DbgRuntime_AppDomainsChanged;
		}

		// DbgManager thread
		void DeinitializeRuntime_DbgThread(DbgRuntime runtime) {
			runtime.Process.DbgManager.Dispatcher.VerifyAccess();
			runtime.AppDomainsChanged -= DbgRuntime_AppDomainsChanged;
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
			threadContext.UIDispatcher.VerifyAccess();
			RefreshThemeFields_UI();
		}

		// random thread
		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => DebuggerSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DebuggerSettings_PropertyChanged_UI(string propertyName) {
			threadContext.UIDispatcher.VerifyAccess();
			if (propertyName == nameof(DebuggerSettings.UseHexadecimal))
				RefreshHexFields_UI();
			else if (propertyName == nameof(DebuggerSettings.SyntaxHighlight)) {
				threadContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				RefreshThemeFields_UI();
			}
		}

		// UI thread
		void RefreshThemeFields_UI() {
			threadContext.UIDispatcher.VerifyAccess();
			foreach (var vm in realAllItems)
				vm.RefreshThemeFields_UI();
		}

		// UI thread
		void RecreateFormatter_UI() {
			threadContext.UIDispatcher.VerifyAccess();
			threadContext.Formatter = threadFormatterProvider.Create();
		}

		// UI thread
		void RefreshHexFields_UI() {
			threadContext.UIDispatcher.VerifyAccess();
			threadContext.UseHexadecimal = debuggerSettings.UseHexadecimal;
			RecreateFormatter_UI();
			foreach (var vm in realAllItems)
				vm.RefreshHexFields_UI();
			foreach (var vm in processes)
				vm.UpdateName(debuggerSettings.UseHexadecimal);
		}

		// random thread
		void UI(Action callback) => threadContext.UIDispatcher.UI(callback);

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
			}
		}

		// DbgManager thread
		void DbgProcess_IsRunningChanged(object sender, EventArgs e) {
			var process = (DbgProcess)sender;
			if (process.State == DbgProcessState.Terminated)
				return;

			if (!process.IsRunning) {
				var state = process.GetOrCreateData<ProcessState>();
				if (state.IgnoreThreadsChangedEvent) {
					state.IgnoreThreadsChangedEvent = false;
					var threads = process.Threads;
					UI(() => AddItems_UI(threads));
				}
				else {
					// The process paused quickly, most likely it was a step operation. Update all thread fields
					UI(() => UpdateFields_UI());
				}
			}
		}

		// DbgManager thread
		void DbgManager_CurrentThreadChanged(object sender, DbgCurrentObjectChangedEventArgs<DbgThread> e) =>
			UI(() => UpdateCurrentThread_UI());

		// UI thread
		void UpdateCurrentThread_UI() {
			threadContext.UIDispatcher.VerifyAccess();
			var currentThread = dbgManager.Value.CurrentThread.Current;
			var breakThread = dbgManager.Value.CurrentThread.Break;
			foreach (var vm in realAllItems) {
				vm.IsCurrentThread = vm.Thread == currentThread;
				vm.IsBreakThread = vm.Thread == breakThread;
			}
		}

		// UI thread
		void UpdateFields_UI() {
			threadContext.UIDispatcher.VerifyAccess();
			foreach (var vm in realAllItems)
				vm.UpdateFields_UI();
		}

		// DbgManager thread
		void DbgProcess_DelayedIsRunningChanged(object sender, EventArgs e) {
			var process = (DbgProcess)sender;
			var state = process.GetOrCreateData<ProcessState>();
			Debug.Assert(process.IsRunning);
			state.IgnoreThreadsChangedEvent = process.IsRunning;
			UI(() => {
				RemoveThread_UI(process);
				InitializeNothingMatched();
			});
		}

		// DbgManager thread
		void DbgProcess_ThreadsChanged(object sender, DbgCollectionChangedEventArgs<DbgThread> e) {
			var process = (DbgProcess)sender;
			var state = process.GetOrCreateData<ProcessState>();
			if (state.IgnoreThreadsChangedEvent)
				return;
			if (e.Added)
				UI(() => AddItems_UI(e.Objects));
			else {
				UI(() => {
					var coll = realAllItems;
					for (int i = coll.Count - 1; i >= 0; i--) {
						if (e.Objects.Contains(coll[i].Thread))
							RemoveThreadAt_UI(i);
					}
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
		void AddItems_UI(IList<DbgThread> threads) {
			threadContext.UIDispatcher.VerifyAccess();
			foreach (var t in threads) {
				var vm = new ThreadVM(t, threadContext, threadOrder++, threadCategoryService, NameEditValueProvider);
				vm.IsCurrentThread = t == dbgManager.Value.CurrentThread.Current;
				vm.IsBreakThread = t == dbgManager.Value.CurrentThread.Break;
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
		int GetInsertionIndex_UI(ThreadVM vm) {
			Debug.Assert(threadContext.UIDispatcher.CheckAccess());
			var comparer = ThreadVMComparer.Instance;
			var list = AllItems;
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

		// UI thread
		void FilterList_UI(string filterText, SimpleProcessVM selectedProcess) {
			threadContext.UIDispatcher.VerifyAccess();
			if (string.IsNullOrWhiteSpace(filterText))
				filterText = string.Empty;
			threadContext.SearchMatcher.SetSearchText(filterText);

			var newList = new List<ThreadVM>(GetFilteredItems_UI(filterText, selectedProcess));
			newList.Sort(ThreadVMComparer.Instance);
			AllItems.Reset(newList);
			InitializeNothingMatched(filterText, selectedProcess);
		}

		void InitializeNothingMatched() => InitializeNothingMatched(filterText, selectedProcess);
		void InitializeNothingMatched(string filterText, SimpleProcessVM selectedProcess) =>
			NothingMatched = AllItems.Count == 0 && !(string.IsNullOrWhiteSpace(filterText) && selectedProcess?.Process == null);

		sealed class ThreadVMComparer : IComparer<ThreadVM> {
			public static readonly IComparer<ThreadVM> Instance = new ThreadVMComparer();
			public int Compare(ThreadVM x, ThreadVM y) => x.Order - y.Order;
		}

		// UI thread
		IEnumerable<ThreadVM> GetFilteredItems_UI(string filterText, SimpleProcessVM selectedProcess) {
			threadContext.UIDispatcher.VerifyAccess();
			foreach (var vm in realAllItems) {
				if (IsMatch_UI(vm, filterText, selectedProcess))
					yield return vm;
			}
		}

		// UI thread
		bool IsMatch_UI(ThreadVM vm, string filterText, SimpleProcessVM selectedProcess) {
			Debug.Assert(threadContext.UIDispatcher.CheckAccess());
			if (selectedProcess?.Process != null && selectedProcess.Process != vm.Thread.Process)
				return false;
			// Common case check, we don't need to allocate any strings
			if (filterText == string.Empty)
				return true;
			// The order must match searchColumnDefinitions
			var allStrings = new string[] {
				GetId_UI(vm),
				GetManagedId_UI(vm),
				GetCategory_UI(vm),
				GetName_UI(vm),
				GetLocation_UI(vm),
				GetPriority_UI(vm),
				GetAffinityMask_UI(vm),
				GetSuspendedCount_UI(vm),
				GetProcess_UI(vm),
				GetAppDomain_UI(vm),
				GetThreadState_UI(vm),
			};
			sbOutput.Reset();
			return threadContext.SearchMatcher.IsMatchAll(allStrings);
		}
		readonly StringBuilderTextColorOutput sbOutput = new StringBuilderTextColorOutput();

		// UI thread
		string GetId_UI(ThreadVM vm) {
			Debug.Assert(threadContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			threadContext.Formatter.WriteId(sbOutput, vm.Thread);
			return sbOutput.ToString();
		}

		// UI thread
		string GetManagedId_UI(ThreadVM vm) {
			Debug.Assert(threadContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			threadContext.Formatter.WriteManagedId(sbOutput, vm.Thread);
			return sbOutput.ToString();
		}

		// UI thread
		string GetCategory_UI(ThreadVM vm) {
			Debug.Assert(threadContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			threadContext.Formatter.WriteCategoryText(sbOutput, vm);
			return sbOutput.ToString();
		}

		// UI thread
		string GetName_UI(ThreadVM vm) {
			Debug.Assert(threadContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			threadContext.Formatter.WriteName(sbOutput, vm.Thread);
			return sbOutput.ToString();
		}

		// UI thread
		string GetLocation_UI(ThreadVM vm) {
			Debug.Assert(threadContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			threadContext.Formatter.WriteLocation(sbOutput, vm);
			return sbOutput.ToString();
		}

		// UI thread
		string GetPriority_UI(ThreadVM vm) {
			Debug.Assert(threadContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			threadContext.Formatter.WritePriority(sbOutput, vm);
			return sbOutput.ToString();
		}

		// UI thread
		string GetAffinityMask_UI(ThreadVM vm) {
			Debug.Assert(threadContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			threadContext.Formatter.WriteAffinityMask(sbOutput, vm);
			return sbOutput.ToString();
		}

		// UI thread
		string GetSuspendedCount_UI(ThreadVM vm) {
			Debug.Assert(threadContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			threadContext.Formatter.WriteSuspendedCount(sbOutput, vm.Thread);
			return sbOutput.ToString();
		}

		// UI thread
		string GetProcess_UI(ThreadVM vm) {
			Debug.Assert(threadContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			threadContext.Formatter.WriteProcessName(sbOutput, vm.Thread);
			return sbOutput.ToString();
		}

		// UI thread
		string GetAppDomain_UI(ThreadVM vm) {
			Debug.Assert(threadContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			threadContext.Formatter.WriteAppDomain(sbOutput, vm.Thread);
			return sbOutput.ToString();
		}

		// UI thread
		string GetThreadState_UI(ThreadVM vm) {
			Debug.Assert(threadContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			threadContext.Formatter.WriteState(sbOutput, vm.Thread);
			return sbOutput.ToString();
		}

		// UI thread
		void RemoveThreadAt_UI(int i) {
			threadContext.UIDispatcher.VerifyAccess();
			Debug.Assert(0 <= i && i < realAllItems.Count);
			var vm = realAllItems[i];
			vm.Dispose();
			realAllItems.RemoveAt(i);
			AllItems.Remove(vm);
		}

		// UI thread
		void RemoveThread_UI(DbgProcess process) {
			threadContext.UIDispatcher.VerifyAccess();
			var coll = realAllItems;
			for (int i = coll.Count - 1; i >= 0; i--) {
				if (coll[i].Thread.Process == process)
					RemoveThreadAt_UI(i);
			}
		}

		// UI thread
		void RemoveAllThreads_UI() {
			threadContext.UIDispatcher.VerifyAccess();
			AllItems.Reset(Array.Empty<ThreadVM>());
			var coll = realAllItems;
			for (int i = coll.Count - 1; i >= 0; i--)
				RemoveThreadAt_UI(i);
		}

		// UI thread
		void AddItems_UI(IList<DbgProcess> newProcesses) {
			threadContext.UIDispatcher.VerifyAccess();
			foreach (var p in newProcesses) {
				var vm = new SimpleProcessVM(p, debuggerSettings.UseHexadecimal);
				int insertionIndex = GetInsertionIndex_UI(vm);
				processes.Insert(insertionIndex, vm);
			}
		}

		// UI thread
		void RemoveProcess_UI(DbgProcess process) {
			threadContext.UIDispatcher.VerifyAccess();
			if (selectedProcess?.Process == process)
				SelectedProcess = processes.FirstOrDefault();
			for (int i = 0; i < processes.Count; i++) {
				if (processes[i].Process == process) {
					processes.RemoveAt(i);
					break;
				}
			}
		}

		// UI thread
		int GetInsertionIndex_UI(SimpleProcessVM vm) {
			Debug.Assert(threadContext.UIDispatcher.CheckAccess());
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
			threadContext.UIDispatcher.VerifyAccess();
			var newProcess = processes.FirstOrDefault();
			if (FilterText != string.Empty || SelectedProcess != newProcess) {
				FilterText = string.Empty;
				SelectedProcess = processes.FirstOrDefault();
				delayedSearch.Cancel();
				DelayStartSearch_UI();
			}
		}
	}
}

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
using System.Windows.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.ToolWindows.Controls;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.Threads {
	interface IThreadsVM {
		bool IsEnabled { get; set; }
		bool IsVisible { get; set; }
		ObservableCollection<ThreadVM> AllItems { get; }
		ObservableCollection<ThreadVM> SelectedItems { get; }
	}

	[Export(typeof(IThreadsVM))]
	sealed class ThreadsVM : ViewModelBase, IThreadsVM {
		public ObservableCollection<ThreadVM> AllItems { get; }
		public ObservableCollection<ThreadVM> SelectedItems { get; }

		public bool IsEnabled {
			get => isEnabled;
			set {
				if (isEnabled == value)
					return;
				isEnabled = value;
				InitializeDebugger_UI(isEnabled);
			}
		}
		bool isEnabled;

		IEditValueProvider NameEditValueProvider {
			get {
				threadContext.Dispatcher.VerifyAccess();
				if (nameEditValueProvider == null)
					nameEditValueProvider = editValueProviderService.Create(ContentTypes.ThreadsWindowName, Array.Empty<string>());
				return nameEditValueProvider;
			}
		}
		IEditValueProvider nameEditValueProvider;

		public bool IsVisible { get; set; }

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
		int threadOrder;

		[ImportingConstructor]
		ThreadsVM(Lazy<DbgManager> dbgManager, DebuggerSettings debuggerSettings, DebuggerDispatcher debuggerDispatcher, ThreadFormatterProvider threadFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider, ThreadCategoryService threadCategoryService, EditValueProviderService editValueProviderService) {
			debuggerDispatcher.Dispatcher.VerifyAccess();
			AllItems = new ObservableCollection<ThreadVM>();
			SelectedItems = new ObservableCollection<ThreadVM>();
			this.dbgManager = dbgManager;
			this.threadFormatterProvider = threadFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			this.threadCategoryService = threadCategoryService;
			this.editValueProviderService = editValueProviderService;
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			threadContext = new ThreadContext(debuggerDispatcher.Dispatcher, classificationFormatMap, textElementProvider) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlight,
				Formatter = threadFormatterProvider.Create(),
			};
		}

		// random thread
		void DbgThread(Action action) =>
			dbgManager.Value.DispatcherThread.BeginInvoke(action);

		// UI thread
		void InitializeDebugger_UI(bool enable) {
			threadContext.Dispatcher.VerifyAccess();
			if (enable) {
				threadContext.ClassificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
				RecreateFormatter_UI();
				threadContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
			}
			else {
				threadContext.ClassificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged -= DebuggerSettings_PropertyChanged;
			}
			DbgThread(() => InitializeDebugger_DbgThread(enable));
		}

		// DbgManager thread
		void InitializeDebugger_DbgThread(bool enable) {
			dbgManager.Value.DispatcherThread.VerifyAccess();
			if (enable) {
				dbgManager.Value.ProcessesChanged += DbgManager_ProcessesChanged;
				var threads = new List<DbgThread>();
				foreach (var p in dbgManager.Value.Processes) {
					InitializeProcess_DbgThread(p);
					if (!p.IsRunning)
						threads.AddRange(p.Threads);
					foreach (var r in p.Runtimes) {
						InitializeRuntime_DbgThread(r);
						foreach (var a in r.AppDomains)
							InitializeAppDomain_DbgThread(a);
					}
				}
				if (threads.Count > 0)
					UI(() => AddItems_UI(threads));
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
				UI(() => RemoveAllThreads_UI());
			}
		}

		// DbgManager thread
		void InitializeProcess_DbgThread(DbgProcess process) {
			process.DbgManager.DispatcherThread.VerifyAccess();
			var state = process.GetOrCreateData<ProcessState>();
			state.IgnoreThreadsChangedEvent = process.IsRunning;
			process.IsRunningChanged += DbgProcess_IsRunningChanged;
			process.DelayedIsRunningChanged += DbgProcess_DelayedIsRunningChanged;
			process.ThreadsChanged += DbgProcess_ThreadsChanged;
			process.RuntimesChanged += DbgProcess_RuntimesChanged;
		}

		// DbgManager thread
		void DeinitializeProcess_DbgThread(DbgProcess process) {
			process.DbgManager.DispatcherThread.VerifyAccess();
			process.IsRunningChanged -= DbgProcess_IsRunningChanged;
			process.DelayedIsRunningChanged -= DbgProcess_DelayedIsRunningChanged;
			process.ThreadsChanged -= DbgProcess_ThreadsChanged;
			process.RuntimesChanged -= DbgProcess_RuntimesChanged;
		}

		// DbgManager thread
		void InitializeRuntime_DbgThread(DbgRuntime runtime) {
			runtime.Process.DbgManager.DispatcherThread.VerifyAccess();
			runtime.AppDomainsChanged += DbgRuntime_AppDomainsChanged;
		}

		// DbgManager thread
		void DeinitializeRuntime_DbgThread(DbgRuntime runtime) {
			runtime.Process.DbgManager.DispatcherThread.VerifyAccess();
			runtime.AppDomainsChanged -= DbgRuntime_AppDomainsChanged;
		}

		// DbgManager thread
		void InitializeAppDomain_DbgThread(DbgAppDomain appDomain) {
			appDomain.Process.DbgManager.DispatcherThread.VerifyAccess();
			appDomain.PropertyChanged += DbgAppDomain_PropertyChanged;
		}

		// DbgManager thread
		void DeinitializeAppDomain_DbgThread(DbgAppDomain appDomain) {
			appDomain.Process.DbgManager.DispatcherThread.VerifyAccess();
			appDomain.PropertyChanged -= DbgAppDomain_PropertyChanged;
		}

		// UI thread
		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			threadContext.Dispatcher.VerifyAccess();
			RefreshThemeFields_UI();
		}

		// random thread
		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => DebuggerSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DebuggerSettings_PropertyChanged_UI(string propertyName) {
			threadContext.Dispatcher.VerifyAccess();
			if (propertyName == nameof(DebuggerSettings.UseHexadecimal))
				RefreshHexFields_UI();
			else if (propertyName == nameof(DebuggerSettings.SyntaxHighlight)) {
				threadContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				RefreshThemeFields_UI();
			}
			else if (propertyName == nameof(debuggerSettings.PropertyEvalAndFunctionCalls))
				RefreshEvalFields_UI();
		}

		// UI thread
		void RefreshThemeFields_UI() {
			threadContext.Dispatcher.VerifyAccess();
			foreach (var vm in AllItems)
				vm.RefreshThemeFields_UI();
		}

		// UI thread
		void RecreateFormatter_UI() {
			threadContext.Dispatcher.VerifyAccess();
			threadContext.Formatter = threadFormatterProvider.Create();
		}

		// UI thread
		void RefreshHexFields_UI() {
			threadContext.Dispatcher.VerifyAccess();
			RecreateFormatter_UI();
			foreach (var vm in AllItems)
				vm.RefreshHexFields_UI();
		}

		// UI thread
		void RefreshEvalFields_UI() {
			threadContext.Dispatcher.VerifyAccess();
			foreach (var vm in AllItems)
				vm.RefreshEvalFields_UI();
		}

		// random thread
		void UI(Action action) =>
			// Use Send so the window is updated as fast as possible when adding new items
			threadContext.Dispatcher.BeginInvoke(DispatcherPriority.Send, action);

		// DbgManager thread
		void DbgManager_ProcessesChanged(object sender, DbgCollectionChangedEventArgs<DbgProcess> e) {
			if (e.Added) {
				foreach (var p in e.Objects)
					InitializeProcess_DbgThread(p);
			}
			else {
				foreach (var p in e.Objects)
					DeinitializeProcess_DbgThread(p);
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

		// UI thread
		void UpdateFields_UI() {
			threadContext.Dispatcher.VerifyAccess();
			foreach (var vm in AllItems)
				vm.UpdateFields_UI();
		}

		// DbgManager thread
		void DbgProcess_DelayedIsRunningChanged(object sender, EventArgs e) {
			var process = (DbgProcess)sender;
			var state = process.GetOrCreateData<ProcessState>();
			Debug.Assert(process.IsRunning);
			state.IgnoreThreadsChangedEvent = process.IsRunning;
			UI(() => RemoveThreads_UI(process));
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
					var coll = AllItems;
					for (int i = coll.Count - 1; i >= 0; i--) {
						if (e.Objects.Contains(coll[i].Thread))
							RemoveThreadAt_UI(i);
					}
				});
			}
		}

		// DbgManager thread
		void DbgAppDomain_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(DbgAppDomain.Name) || e.PropertyName == nameof(DbgAppDomain.Id)) {
				UI(() => {
					var appDomain = (DbgAppDomain)sender;
					foreach (var vm in AllItems)
						vm.RefreshAppDomainNames_UI(appDomain);
				});
			}
		}

		// UI thread
		void AddItems_UI(IList<DbgThread> threads) {
			threadContext.Dispatcher.VerifyAccess();
			foreach (var t in threads)
				AllItems.Add(new ThreadVM(t, threadContext, threadOrder++, threadCategoryService, NameEditValueProvider));
		}

		// UI thread
		void RemoveThreadAt_UI(int i) {
			threadContext.Dispatcher.VerifyAccess();
			Debug.Assert(0 <= i && i < AllItems.Count);
			var vm = AllItems[i];
			vm.Dispose();
			AllItems.RemoveAt(i);
		}

		// UI thread
		void RemoveThreads_UI(DbgProcess process) {
			threadContext.Dispatcher.VerifyAccess();
			var coll = AllItems;
			for (int i = coll.Count - 1; i >= 0; i--) {
				if (coll[i].Thread.Process == process)
					RemoveThreadAt_UI(i);
			}
		}

		// UI thread
		void RemoveAllThreads_UI() {
			threadContext.Dispatcher.VerifyAccess();
			var coll = AllItems;
			for (int i = coll.Count - 1; i >= 0; i--)
				RemoveThreadAt_UI(i);
		}
	}
}

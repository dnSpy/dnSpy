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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.Processes {
	interface IProcessesVM {
		bool IsOpen { get; set; }
		bool IsVisible { get; set; }
		ObservableCollection<ProcessVM> AllItems { get; }
		ObservableCollection<ProcessVM> SelectedItems { get; }
	}

	[Export(typeof(IProcessesVM))]
	sealed class ProcessesVM : ViewModelBase, IProcessesVM, ILazyToolWindowVM {
		public ObservableCollection<ProcessVM> AllItems { get; }
		public ObservableCollection<ProcessVM> SelectedItems { get; }

		public bool IsOpen {
			get => lazyToolWindowVMHelper.IsOpen;
			set => lazyToolWindowVMHelper.IsOpen = value;
		}

		public bool IsVisible {
			get => lazyToolWindowVMHelper.IsVisible;
			set => lazyToolWindowVMHelper.IsVisible = value;
		}

		readonly Lazy<DbgManager> dbgManager;
		readonly ProcessContext processContext;
		readonly ProcessFormatterProvider processFormatterProvider;
		readonly DebuggerSettings debuggerSettings;
		readonly LazyToolWindowVMHelper lazyToolWindowVMHelper;
		int processOrder;

		[ImportingConstructor]
		ProcessesVM(Lazy<DbgManager> dbgManager, DebuggerSettings debuggerSettings, UIDispatcher uiDispatcher, ProcessFormatterProvider processFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider) {
			uiDispatcher.VerifyAccess();
			AllItems = new ObservableCollection<ProcessVM>();
			SelectedItems = new ObservableCollection<ProcessVM>();
			this.dbgManager = dbgManager;
			this.processFormatterProvider = processFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			lazyToolWindowVMHelper = new DebuggerLazyToolWindowVMHelper(this, uiDispatcher, dbgManager);
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			processContext = new ProcessContext(uiDispatcher, classificationFormatMap, textElementProvider) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlight,
				Formatter = processFormatterProvider.Create(),
			};
		}

		// random thread
		void DbgThread(Action callback) =>
			dbgManager.Value.Dispatcher.BeginInvoke(callback);

		// UI thread
		void ILazyToolWindowVM.Show() {
			processContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: true);
		}

		// UI thread
		void ILazyToolWindowVM.Hide() {
			processContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: false);
		}

		// UI thread
		void InitializeDebugger_UI(bool enable) {
			processContext.UIDispatcher.VerifyAccess();
			if (enable) {
				processContext.ClassificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
				RecreateFormatter_UI();
				processContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
			}
			else {
				processContext.ClassificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged -= DebuggerSettings_PropertyChanged;
			}
			DbgThread(() => InitializeDebugger_DbgThread(enable));
		}

		// DbgManager thread
		void InitializeDebugger_DbgThread(bool enable) {
			dbgManager.Value.Dispatcher.VerifyAccess();
			if (enable) {
				dbgManager.Value.ProcessesChanged += DbgManager_ProcessesChanged;
				dbgManager.Value.DelayedIsRunningChanged += DbgManager_DelayedIsRunningChanged;
				dbgManager.Value.CurrentProcessChanged += DbgManager_CurrentProcessChanged;

				var processes = dbgManager.Value.Processes;
				if (processes.Length > 0)
					UI(() => AddItems_UI(processes));
			}
			else {
				dbgManager.Value.ProcessesChanged -= DbgManager_ProcessesChanged;
				dbgManager.Value.DelayedIsRunningChanged -= DbgManager_DelayedIsRunningChanged;
				dbgManager.Value.CurrentProcessChanged -= DbgManager_CurrentProcessChanged;

				UI(() => RemoveAllProcesses_UI());
			}
		}

		// UI thread
		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			processContext.UIDispatcher.VerifyAccess();
			RefreshThemeFields_UI();
		}

		// random thread
		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => DebuggerSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DebuggerSettings_PropertyChanged_UI(string propertyName) {
			processContext.UIDispatcher.VerifyAccess();
			if (propertyName == nameof(DebuggerSettings.UseHexadecimal))
				RefreshHexFields_UI();
			else if (propertyName == nameof(DebuggerSettings.SyntaxHighlight)) {
				processContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				RefreshThemeFields_UI();
			}
		}

		// UI thread
		void RefreshThemeFields_UI() {
			processContext.UIDispatcher.VerifyAccess();
			foreach (var vm in AllItems)
				vm.RefreshThemeFields_UI();
		}

		// UI thread
		void RecreateFormatter_UI() {
			processContext.UIDispatcher.VerifyAccess();
			processContext.Formatter = processFormatterProvider.Create();
		}

		// UI thread
		void RefreshHexFields_UI() {
			processContext.UIDispatcher.VerifyAccess();
			RecreateFormatter_UI();
			foreach (var vm in AllItems)
				vm.RefreshHexFields_UI();
		}

		// random thread
		void UI(Action callback) => processContext.UIDispatcher.UI(callback);

		// random thread
		void UI(TimeSpan delay, Action callback) => processContext.UIDispatcher.UI(delay, callback);

		// DbgManager thread
		void DbgManager_DelayedIsRunningChanged(object sender, EventArgs e) => UI(() => {
			// If all processes are running and the window is hidden, hide it now
			if (!IsVisible)
				lazyToolWindowVMHelper.TryHideWindow();
		});

		// DbgManager thread
		void DbgManager_ProcessesChanged(object sender, DbgCollectionChangedEventArgs<DbgProcess> e) {
			if (e.Added)
				UI(() => AddItems_UI(e.Objects));
			else {
				UI(() => {
					var coll = AllItems;
					for (int i = coll.Count - 1; i >= 0; i--) {
						if (e.Objects.Contains(coll[i].Process))
							RemoveProcessAt_UI(i);
					}
				});
			}
		}

		// DbgManager thread
		void DbgManager_CurrentProcessChanged(object sender, DbgCurrentObjectChangedEventArgs<DbgProcess> e) =>
			UI(() => UpdateCurrentProcess_UI());

		// UI thread
		void UpdateCurrentProcess_UI() {
			processContext.UIDispatcher.VerifyAccess();
			var currentProcess = dbgManager.Value.CurrentProcess.Current;
			var breakProcess = dbgManager.Value.CurrentProcess.Break;
			foreach (var vm in AllItems) {
				vm.IsCurrentProcess = vm.Process == currentProcess;
				vm.IsBreakProcess = vm.Process == breakProcess;
			}
		}

		// UI thread
		void AddItems_UI(IList<DbgProcess> processes) {
			processContext.UIDispatcher.VerifyAccess();
			var currentProcess = dbgManager.Value.CurrentProcess.Current;
			var breakProcess = dbgManager.Value.CurrentProcess.Break;
			foreach (var p in processes) {
				var vm = new ProcessVM(this, p, processContext, processOrder++);
				vm.IsCurrentProcess = vm.Process == currentProcess;
				vm.IsBreakProcess = vm.Process == breakProcess;
				AllItems.Add(vm);
			}
		}

		// UI thread
		void RemoveProcessAt_UI(int i) {
			processContext.UIDispatcher.VerifyAccess();
			Debug.Assert(0 <= i && i < AllItems.Count);
			var vm = AllItems[i];
			vm.Dispose();
			AllItems.RemoveAt(i);
		}

		// UI thread
		void RemoveAllProcesses_UI() {
			processContext.UIDispatcher.VerifyAccess();
			var coll = AllItems;
			for (int i = coll.Count - 1; i >= 0; i--)
				RemoveProcessAt_UI(i);
		}
	}
}

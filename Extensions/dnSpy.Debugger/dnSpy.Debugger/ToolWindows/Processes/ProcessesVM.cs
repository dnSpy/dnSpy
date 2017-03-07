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
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.Processes {
	interface IProcessesVM {
		bool IsEnabled { get; set; }
		bool IsVisible { get; set; }
		ObservableCollection<ProcessVM> AllItems { get; }
		ObservableCollection<ProcessVM> SelectedItems { get; }
	}

	[Export(typeof(IProcessesVM))]
	sealed class ProcessesVM : ViewModelBase, IProcessesVM {
		public ObservableCollection<ProcessVM> AllItems { get; }
		public ObservableCollection<ProcessVM> SelectedItems { get; }

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

		public bool IsVisible {
			get => processContext.IsVisible;
			set {
				if (processContext.IsVisible != value) {
					processContext.IsVisible = value;
					if (processContext.IsVisible) {
						RecreateFormatter_UI();
						RefreshTitles_UI();
						RefreshThemeFields_UI();
					}
				}
			}
		}

		readonly Lazy<DbgManager> dbgManager;
		readonly ProcessContext processContext;
		readonly ProcessFormatterProvider processFormatterProvider;
		readonly DebuggerSettings debuggerSettings;
		int processOrder;
		bool refreshTitlesOnPause;

		[ImportingConstructor]
		ProcessesVM(Lazy<DbgManager> dbgManager, DebuggerSettings debuggerSettings, DebuggerDispatcher debuggerDispatcher, ProcessFormatterProvider processFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider) {
			debuggerDispatcher.Dispatcher.VerifyAccess();
			AllItems = new ObservableCollection<ProcessVM>();
			SelectedItems = new ObservableCollection<ProcessVM>();
			this.dbgManager = dbgManager;
			this.processFormatterProvider = processFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			processContext = new ProcessContext(debuggerDispatcher.Dispatcher, classificationFormatMap, textElementProvider) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlight,
				Formatter = processFormatterProvider.Create(),
			};
		}

		// random thread
		void DbgThread(Action action) =>
			dbgManager.Value.DispatcherThread.BeginInvoke(action);

		// UI thread
		void InitializeDebugger_UI(bool enable) {
			processContext.Dispatcher.VerifyAccess();
			if (enable) {
				processContext.ClassificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
				RecreateFormatter_UI();
				processContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				refreshTitlesOnPause = false;
			}
			else {
				processContext.ClassificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged -= DebuggerSettings_PropertyChanged;
			}
			DbgThread(() => InitializeDebugger_DbgThread(enable));
		}

		// DbgManager thread
		void InitializeDebugger_DbgThread(bool enable) {
			dbgManager.Value.DispatcherThread.VerifyAccess();
			if (enable) {
				dbgManager.Value.ProcessesChanged += DbgManager_ProcessesChanged;
				dbgManager.Value.DelayedIsRunningChanged += DbgManager_DelayedIsRunningChanged;
				dbgManager.Value.IsRunningChanged += DbgManager_IsRunningChanged;

				var processes = dbgManager.Value.Processes;
				if (processes.Length > 0)
					UI(() => AddItems_UI(processes));
			}
			else {
				dbgManager.Value.ProcessesChanged -= DbgManager_ProcessesChanged;
				dbgManager.Value.DelayedIsRunningChanged -= DbgManager_DelayedIsRunningChanged;
				dbgManager.Value.IsRunningChanged -= DbgManager_IsRunningChanged;

				UI(() => RemoveAllProcesses_UI());
			}
		}

		// UI thread
		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			processContext.Dispatcher.VerifyAccess();
			RefreshThemeFields_UI();
		}

		// random thread
		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => DebuggerSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DebuggerSettings_PropertyChanged_UI(string propertyName) {
			processContext.Dispatcher.VerifyAccess();
			if (propertyName == nameof(DebuggerSettings.UseHexadecimal))
				RefreshHexFields_UI();
			else if (propertyName == nameof(DebuggerSettings.SyntaxHighlight)) {
				processContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				RefreshThemeFields_UI();
			}
		}

		// UI thread
		void RefreshTitles_UI() {
			processContext.Dispatcher.VerifyAccess();
			if (!processContext.IsVisible)
				return;
			foreach (var vm in AllItems)
				vm.RefreshTitle_UI();
		}

		// UI thread
		void RefreshThemeFields_UI() {
			processContext.Dispatcher.VerifyAccess();
			if (!processContext.IsVisible)
				return;
			foreach (var vm in AllItems)
				vm.RefreshThemeFields_UI();
		}

		// UI thread
		void RecreateFormatter_UI() {
			processContext.Dispatcher.VerifyAccess();
			processContext.Formatter = processFormatterProvider.Create();
		}

		// UI thread
		void RefreshHexFields_UI() {
			processContext.Dispatcher.VerifyAccess();
			if (!processContext.IsVisible)
				return;
			RecreateFormatter_UI();
			foreach (var vm in AllItems)
				vm.RefreshHexFields_UI();
		}

		// random thread
		void UI(Action action) =>
			// Use Send so the window is updated as fast as possible when adding new items
			processContext.Dispatcher.BeginInvoke(DispatcherPriority.Send, action);

		// DbgManager thread
		void DbgManager_DelayedIsRunningChanged(object sender, EventArgs e) => UI(() => refreshTitlesOnPause = true);

		// DbgManager thread
		void DbgManager_IsRunningChanged(object sender, EventArgs e) {
			if (refreshTitlesOnPause && !((DbgManager)sender).IsRunning) {
				refreshTitlesOnPause = false;
				UI(() => RefreshTitles_UI());
			}
		}

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

		// UI thread
		void AddItems_UI(IList<DbgProcess> processes) {
			processContext.Dispatcher.VerifyAccess();
			foreach (var p in processes)
				AllItems.Add(new ProcessVM(p, processContext, processOrder++));
		}

		// UI thread
		void RemoveProcessAt_UI(int i) {
			processContext.Dispatcher.VerifyAccess();
			Debug.Assert(0 <= i && i < AllItems.Count);
			var vm = AllItems[i];
			vm.Dispose();
			AllItems.RemoveAt(i);
		}

		// UI thread
		void RemoveAllProcesses_UI() {
			processContext.Dispatcher.VerifyAccess();
			var coll = AllItems;
			for (int i = coll.Count - 1; i >= 0; i--)
				RemoveProcessAt_UI(i);
		}
	}
}

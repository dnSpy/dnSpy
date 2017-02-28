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
	[ExportDbgManagerStartListener]
	sealed class ProcessesVM : ViewModelBase, IProcessesVM, IDbgManagerStartListener {
		public ObservableCollection<ProcessVM> AllItems { get; }
		public ObservableCollection<ProcessVM> SelectedItems { get; }

		public bool IsEnabled { get; set; }

		public bool IsVisible {
			get => processContext.IsVisible;
			set {
				if (processContext.IsVisible != value) {
					processContext.IsVisible = value;
					if (processContext.IsVisible) {
						RecreateFormatter();
						RefreshTitles_UI();
						RefreshThemeFields_UI();
					}
				}
			}
		}

		readonly ProcessContext processContext;
		readonly ProcessFormatterProvider processFormatterProvider;
		readonly DebuggerSettings debuggerSettings;
		int processOrder;
		bool refreshTitlesOnPause;

		[ImportingConstructor]
		ProcessesVM(DebuggerSettings debuggerSettings, DebuggerDispatcher debuggerDispatcher, ProcessFormatterProvider processFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider) {
			AllItems = new ObservableCollection<ProcessVM>();
			SelectedItems = new ObservableCollection<ProcessVM>();
			this.processFormatterProvider = processFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			// We could be in a random thread if IDbgManagerStartListener.OnStart() gets called after the ctor returns
			processContext = debuggerDispatcher.Dispatcher.Invoke(() => {
				var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
				var procCtx = new ProcessContext(debuggerDispatcher.Dispatcher, classificationFormatMap, textElementProvider) {
					SyntaxHighlight = debuggerSettings.SyntaxHighlight,
					Formatter = processFormatterProvider.Create(),
				};
				classificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
				return procCtx;
			}, DispatcherPriority.Send);
		}

		// UI thread
		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) => RefreshThemeFields_UI();

		// random thread
		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => DebuggerSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DebuggerSettings_PropertyChanged_UI(string propertyName) {
			if (propertyName == nameof(DebuggerSettings.UseHexadecimal))
				RefreshHexSettings_UI();
			else if (propertyName == nameof(DebuggerSettings.SyntaxHighlight)) {
				processContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				RefreshThemeFields_UI();
			}
		}

		// UI thread
		void RefreshTitles_UI() {
			if (!processContext.IsVisible)
				return;
			foreach (var vm in AllItems)
				vm.RefreshTitle();
		}

		// UI thread
		void RefreshThemeFields_UI() {
			if (!processContext.IsVisible)
				return;
			foreach (var vm in AllItems)
				vm.RefreshThemeFields();
		}

		// UI thread
		void RecreateFormatter() => processContext.Formatter = processFormatterProvider.Create();

		// UI thread
		void RefreshHexSettings_UI() {
			if (!processContext.IsVisible)
				return;
			RecreateFormatter();
			foreach (var vm in AllItems)
				vm.RefreshHexFields();
		}

		// random thread
		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) {
			dbgManager.ProcessesChanged += DbgManager_ProcessesChanged;
			dbgManager.DelayedIsRunningChanged += DbgManager_DelayedIsRunningChanged;
			dbgManager.IsRunningChanged += DbgManager_IsRunningChanged;
		}

		// random thread
		void UI(Action action) =>
			processContext.Dispatcher.BeginInvoke(DispatcherPriority.Background, action);

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
			if (e.Added) {
				UI(() => {
					foreach (var p in e.Objects)
						AllItems.Add(new ProcessVM(p, processContext, processOrder++));
				});
			}
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
		void RemoveProcessAt_UI(int i) {
			Debug.Assert(0 <= i && i < AllItems.Count);
			var vm = AllItems[i];
			vm.Dispose();
			AllItems.RemoveAt(i);
		}
	}
}

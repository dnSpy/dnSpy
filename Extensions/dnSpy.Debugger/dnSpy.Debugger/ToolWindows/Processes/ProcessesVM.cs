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
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.Processes {
	interface IProcessesVM {
		ProcessVM[] SelectedItems { get; set; }
	}

	[Export(typeof(IProcessesVM))]
	[ExportDbgManagerStartListener]
	sealed class ProcessesVM : ViewModelBase, IProcessesVM, IDbgManagerStartListener {
		public ObservableCollection<ProcessVM> Collection => processesList;
		readonly ObservableCollection<ProcessVM> processesList;

		public ProcessVM[] SelectedItems {
			get => selectedItems;
			set => selectedItems = value ?? Array.Empty<ProcessVM>();
		}
		ProcessVM[] selectedItems;

		public string DetachToolTip => dnSpy_Debugger_Resources.Processes_DetachToolTip;
		public string TerminateToolTip => dnSpy_Debugger_Resources.Processes_TerminateToolTip;
		public string AttachToProcessToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Processes_AttachToProcessToolTip, dnSpy_Debugger_Resources.ShortCutKeyCtrlAltP);

		public ICommand DetachCommand => new RelayCommand(a => DetachProcesses(), a => CanDetachProcesses);
		public ICommand TerminateCommand => new RelayCommand(a => TerminateProcesses(), a => CanTerminateProcesses);
		public ICommand AttachToProcessCommand => new RelayCommand(a => AttachToProcess(), a => CanAttachToProcess);

		readonly DbgManager dbgManager;
		readonly ProcessContext processContext;
		readonly ProcessFormatterProvider processFormatterProvider;
		readonly DebuggerSettings debuggerSettings;

		[ImportingConstructor]
		ProcessesVM(DbgManager dbgManager, DebuggerSettings debuggerSettings, DebuggerDispatcher debuggerDispatcher, ProcessFormatterProvider processFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider) {
			this.dbgManager = dbgManager;
			processesList = new ObservableCollection<ProcessVM>();
			this.processFormatterProvider = processFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			selectedItems = Array.Empty<ProcessVM>();
			// We could be in a random thread if IDbgManagerStartListener.OnStart() gets called after the ctor returns
			processContext = debuggerDispatcher.Dispatcher.Invoke(() => {
				var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
				var procCtx = new ProcessContext(debuggerDispatcher.Dispatcher, classificationFormatMap, textElementProvider) {
					SyntaxHighlight = debuggerSettings.SyntaxHighlight,
					ProcessFormatter = processFormatterProvider.Create(),
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
		void RefreshThemeFields_UI() {
			foreach (var vm in Collection)
				vm.RefreshThemeFields();
		}

		// UI thread
		void RefreshHexSettings_UI() {
			processContext.ProcessFormatter = processFormatterProvider.Create();
			foreach (var vm in Collection)
				vm.RefreshHexFields();
		}

		// random thread
		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) => dbgManager.ProcessesChanged += DbgManager_ProcessesChanged;

		// random thread
		void UI(Action action) =>
			processContext.Dispatcher.BeginInvoke(DispatcherPriority.Background, action);

		// DbgManager thread
		void DbgManager_ProcessesChanged(object sender, DbgCollectionChangedEventArgs<DbgProcess> e) {
			if (e.Added) {
				UI(() => {
					foreach (var p in e.Objects)
						Collection.Add(new ProcessVM(p, processContext));
				});
			}
			else {
				UI(() => {
					var coll = Collection;
					for (int i = coll.Count - 1; i >= 0; i--) {
						if (e.Objects.Contains(coll[i].Process))
							RemoveProcessAt_UI(i);
					}
				});
			}
		}

		// UI thread
		void RemoveProcessAt_UI(int i) {
			Debug.Assert(0 <= i && i < Collection.Count);
			var vm = Collection[i];
			vm.Dispose();
			Collection.RemoveAt(i);
		}

		// UI thread
		bool CanDetachProcesses => SelectedItems.Length != 0;
		// UI thread
		void DetachProcesses() {
			foreach (var vm in SelectedItems)
				vm.Process.Detach();
		}

		// UI thread
		bool CanTerminateProcesses => SelectedItems.Length != 0;
		// UI thread
		void TerminateProcesses() {
			foreach (var vm in SelectedItems)
				vm.Process.Terminate();
		}

		// UI thread
		bool CanAttachToProcess => true;
		// UI thread
		void AttachToProcess() {
			//TODO:
		}
	}
}

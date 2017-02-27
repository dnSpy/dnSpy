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
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Utilities;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.Processes {
	interface IProcessesVM {
		bool IsEnabled { get; set; }
		bool IsVisible { get; set; }

		bool CanCopy { get; }
		void Copy();
		bool CanSelectAll { get; }
		void SelectAll();
		bool CanDetachProcess { get; }
		void DetachProcess();
		bool CanTerminateProcess { get; }
		void TerminateProcess();
		bool CanAttachToProcess { get; }
		void AttachToProcess();
		bool CanToggleDetachWhenDebuggingStopped { get; }
		void ToggleDetachWhenDebuggingStopped();
		bool DetachWhenDebuggingStopped { get; set; }
		bool CanToggleUseHexadecimal { get; }
		void ToggleUseHexadecimal();
		bool UseHexadecimal { get; set; }
	}

	[Export(typeof(IProcessesVM))]
	[ExportDbgManagerStartListener]
	sealed class ProcessesVM : ViewModelBase, IProcessesVM, IDbgManagerStartListener {
		public ObservableCollection<ProcessVM> Collection => processesList;
		readonly ObservableCollection<ProcessVM> processesList;

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

		public string DetachToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Processes_DetachToolTip, null);
		public string TerminateToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Processes_TerminateToolTip, null);
		public string AttachToProcessToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Processes_AttachToProcessToolTip, dnSpy_Debugger_Resources.ShortCutKeyCtrlAltP);

		public ICommand DetachCommand => new RelayCommand(a => DetachProcess(), a => CanDetachProcess);
		public ICommand TerminateCommand => new RelayCommand(a => TerminateProcess(), a => CanTerminateProcess);
		public ICommand AttachToProcessCommand => new RelayCommand(a => AttachToProcess(), a => CanAttachToProcess);

		readonly DbgManager dbgManager;
		readonly ProcessContext processContext;
		readonly ProcessFormatterProvider processFormatterProvider;
		readonly DebuggerSettings debuggerSettings;
		int processOrder;

		[ImportingConstructor]
		ProcessesVM(DbgManager dbgManager, DebuggerSettings debuggerSettings, DebuggerDispatcher debuggerDispatcher, ProcessFormatterProvider processFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider) {
			this.dbgManager = dbgManager;
			processesList = new ObservableCollection<ProcessVM>();
			this.processFormatterProvider = processFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			SelectedItems = new ObservableCollection<ProcessVM>();
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
		void RefreshTitles_UI() {
			if (!processContext.IsVisible)
				return;
			foreach (var vm in Collection)
				vm.RefreshTitle();
		}

		// UI thread
		void RefreshThemeFields_UI() {
			if (!processContext.IsVisible)
				return;
			foreach (var vm in Collection)
				vm.RefreshThemeFields();
		}

		// UI thread
		void RecreateFormatter() => processContext.ProcessFormatter = processFormatterProvider.Create();

		// UI thread
		void RefreshHexSettings_UI() {
			if (!processContext.IsVisible)
				return;
			RecreateFormatter();
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
						Collection.Add(new ProcessVM(p, processContext, processOrder++));
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
		public bool CanCopy => SelectedItems.Count != 0;
		// UI thread
		public void Copy() {
			var output = new StringBuilderTextColorOutput();
			var formatter = processContext.ProcessFormatter;
			foreach (var vm in SelectedItems.OrderBy(a => a.Order)) {
				formatter.WriteImage(output, vm);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteName(output, vm.Process);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteId(output, vm.Process);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteTitle(output, vm);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteState(output, vm.Process);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteDebugging(output, vm.Process);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WritePath(output, vm.Process);
				output.WriteLine();
			}
			var s = output.ToString();
			if (s.Length > 0) {
				try {
					Clipboard.SetText(s);
				}
				catch (ExternalException) { }
			}
		}

		// UI thread
		public bool CanSelectAll => Collection.Count != 0;
		// UI thread
		public void SelectAll() {
			SelectedItems.Clear();
			foreach (var vm in Collection)
				SelectedItems.Add(vm);
		}

		// UI thread
		public bool CanDetachProcess => SelectedItems.Count != 0;
		// UI thread
		public void DetachProcess() {
			foreach (var vm in SelectedItems)
				vm.Process.Detach();
		}

		// UI thread
		public bool CanTerminateProcess => SelectedItems.Count != 0;
		// UI thread
		public void TerminateProcess() {
			foreach (var vm in SelectedItems)
				vm.Process.Terminate();
		}

		// UI thread
		public bool CanAttachToProcess => true;
		// UI thread
		public void AttachToProcess() {
			//TODO:
		}

		// UI thread
		public bool CanToggleDetachWhenDebuggingStopped => SelectedItems.Count != 0;
		// UI thread
		public void ToggleDetachWhenDebuggingStopped() => DetachWhenDebuggingStopped = !DetachWhenDebuggingStopped;
		// UI thread
		public bool DetachWhenDebuggingStopped {
			get => SelectedItems.Count != 0 && !SelectedItems.Any(a => !a.Process.ShouldDetach);
			set {
				foreach (var vm in SelectedItems)
					vm.Process.ShouldDetach = value;
			}
		}

		// UI thread
		public bool CanToggleUseHexadecimal => true;
		// UI thread
		public void ToggleUseHexadecimal() => UseHexadecimal = !UseHexadecimal;
		// UI thread
		public bool UseHexadecimal {
			get => debuggerSettings.UseHexadecimal;
			set => debuggerSettings.UseHexadecimal = value;
		}
	}
}

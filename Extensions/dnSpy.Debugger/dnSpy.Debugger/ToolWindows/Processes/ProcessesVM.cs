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
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.ToolWindows.Search;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.Processes {
	interface IProcessesVM : IGridViewColumnDescsProvider {
		bool IsOpen { get; set; }
		bool IsVisible { get; set; }
		BulkObservableCollection<ProcessVM> AllItems { get; }
		ObservableCollection<ProcessVM> SelectedItems { get; }
		void ResetSearchSettings();
		string GetSearchHelpText();
		IEnumerable<ProcessVM> Sort(IEnumerable<ProcessVM> processes);
	}

	[Export(typeof(IProcessesVM))]
	sealed class ProcessesVM : ViewModelBase, IProcessesVM, ILazyToolWindowVM, IComparer<ProcessVM> {
		public BulkObservableCollection<ProcessVM> AllItems { get; }
		public ObservableCollection<ProcessVM> SelectedItems { get; }
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

		readonly Lazy<DbgManager> dbgManager;
		readonly ProcessContext processContext;
		readonly ProcessFormatterProvider processFormatterProvider;
		readonly DebuggerSettings debuggerSettings;
		readonly LazyToolWindowVMHelper lazyToolWindowVMHelper;
		readonly List<ProcessVM> realAllItems;
		int processOrder;

		[ImportingConstructor]
		ProcessesVM(Lazy<DbgManager> dbgManager, DebuggerSettings debuggerSettings, UIDispatcher uiDispatcher, ProcessFormatterProvider processFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider) {
			uiDispatcher.VerifyAccess();
			realAllItems = new List<ProcessVM>();
			AllItems = new BulkObservableCollection<ProcessVM>();
			SelectedItems = new ObservableCollection<ProcessVM>();
			this.dbgManager = dbgManager;
			this.processFormatterProvider = processFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			lazyToolWindowVMHelper = new DebuggerLazyToolWindowVMHelper(this, uiDispatcher, dbgManager);
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			processContext = new ProcessContext(uiDispatcher, classificationFormatMap, textElementProvider, new SearchMatcher(searchColumnDefinitions)) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlight,
				Formatter = processFormatterProvider.Create(),
			};
			Descs = new GridViewColumnDescs {
				Columns = new GridViewColumnDesc[] {
					new GridViewColumnDesc(ProcessesWindowColumnIds.Icon, string.Empty),
					new GridViewColumnDesc(ProcessesWindowColumnIds.Name, dnSpy_Debugger_Resources.Column_Name),
					new GridViewColumnDesc(ProcessesWindowColumnIds.ID, dnSpy_Debugger_Resources.Column_ID),
					new GridViewColumnDesc(ProcessesWindowColumnIds.Title, dnSpy_Debugger_Resources.Column_Title),
					new GridViewColumnDesc(ProcessesWindowColumnIds.State, dnSpy_Debugger_Resources.Column_State),
					new GridViewColumnDesc(ProcessesWindowColumnIds.Debugging, dnSpy_Debugger_Resources.Column_Debugging),
					new GridViewColumnDesc(ProcessesWindowColumnIds.ProcessArchitecture, dnSpy_Debugger_Resources.Column_ProcessArchitecture),
					new GridViewColumnDesc(ProcessesWindowColumnIds.Path, dnSpy_Debugger_Resources.Column_Path),
				},
			};
			Descs.SortedColumnChanged += (a, b) => SortList();
		}

		// Don't change the order of these instances without also updating input passed to SearchMatcher.IsMatchAll()
		static readonly SearchColumnDefinition[] searchColumnDefinitions = new SearchColumnDefinition[] {
			new SearchColumnDefinition(PredefinedTextClassifierTags.ProcessesWindowName, "n", dnSpy_Debugger_Resources.Column_Name),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ProcessesWindowId, "p", dnSpy_Debugger_Resources.Column_ID),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ProcessesWindowTitle, "t", dnSpy_Debugger_Resources.Column_Title),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ProcessesWindowState, "s", dnSpy_Debugger_Resources.Column_State),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ProcessesWindowDebugging, "d", dnSpy_Debugger_Resources.Column_Debugging),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ProcessesWindowArchitecture, "a", dnSpy_Debugger_Resources.Column_ProcessArchitecture),
			new SearchColumnDefinition(PredefinedTextClassifierTags.ProcessesWindowPath, "f", dnSpy_Debugger_Resources.Column_Path),
		};

		// UI thread
		public string GetSearchHelpText() {
			processContext.UIDispatcher.VerifyAccess();
			return processContext.SearchMatcher.GetHelpText();
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
			ResetSearchSettings();
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
			foreach (var vm in realAllItems)
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
			foreach (var vm in realAllItems)
				vm.RefreshHexFields_UI();
		}

		// random thread
		void UI(Action callback) => processContext.UIDispatcher.UI(callback);

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
					var coll = realAllItems;
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
			foreach (var vm in realAllItems) {
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
		int GetInsertionIndex_UI(ProcessVM vm) {
			Debug.Assert(processContext.UIDispatcher.CheckAccess());
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
			processContext.UIDispatcher.VerifyAccess();
			if (string.IsNullOrWhiteSpace(filterText))
				filterText = string.Empty;
			processContext.SearchMatcher.SetSearchText(filterText);
			SortList(filterText);
		}

		// UI thread
		void SortList() {
			processContext.UIDispatcher.VerifyAccess();
			SortList(filterText);
		}

		// UI thread
		void SortList(string filterText) {
			processContext.UIDispatcher.VerifyAccess();
			var newList = new List<ProcessVM>(GetFilteredItems_UI(filterText));
			newList.Sort(this);
			AllItems.Reset(newList);
			InitializeNothingMatched(filterText);
		}

		// UI thread
		IEnumerable<ProcessVM> IProcessesVM.Sort(IEnumerable<ProcessVM> processes) {
			processContext.UIDispatcher.VerifyAccess();
			var list = new List<ProcessVM>(processes);
			list.Sort(this);
			return list;
		}

		void InitializeNothingMatched() => InitializeNothingMatched(filterText);
		void InitializeNothingMatched(string filterText) =>
			NothingMatched = AllItems.Count == 0 && !string.IsNullOrWhiteSpace(filterText);

		public int Compare(ProcessVM x, ProcessVM y) {
			Debug.Assert(processContext.UIDispatcher.CheckAccess());
			var (desc, dir) = Descs.SortedColumn;

			int id;
			if (desc == null || dir == GridViewSortDirection.Default) {
				id = ProcessesWindowColumnIds.Default_Order;
				dir = GridViewSortDirection.Ascending;
			}
			else
				id = desc.Id;

			int diff;
			switch (id) {
			case ProcessesWindowColumnIds.Default_Order:
				diff = x.Order - y.Order;
				break;

			case ProcessesWindowColumnIds.Icon:
				Debug.Fail("Icon column can't be sorted");
				diff = 0;
				break;

			case ProcessesWindowColumnIds.Name:
				diff = StringComparer.OrdinalIgnoreCase.Compare(x.Process.Name, y.Process.Name);
				break;

			case ProcessesWindowColumnIds.ID:
				diff = x.Process.Id - y.Process.Id;
				break;

			case ProcessesWindowColumnIds.Title:
				diff = StringComparer.OrdinalIgnoreCase.Compare(x.Title, y.Title);
				break;

			case ProcessesWindowColumnIds.State:
				diff = StringComparer.OrdinalIgnoreCase.Compare(GetState_UI(x), GetState_UI(y));
				break;

			case ProcessesWindowColumnIds.Debugging:
				diff = StringComparer.OrdinalIgnoreCase.Compare(GetDebugging_UI(x), GetDebugging_UI(y));
				break;

			case ProcessesWindowColumnIds.ProcessArchitecture:
				diff = StringComparer.OrdinalIgnoreCase.Compare(GetArchitecture_UI(x), GetArchitecture_UI(y));
				break;

			case ProcessesWindowColumnIds.Path:
				diff = StringComparer.OrdinalIgnoreCase.Compare(x.Process.Filename, y.Process.Filename);
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
		IEnumerable<ProcessVM> GetFilteredItems_UI(string filterText) {
			processContext.UIDispatcher.VerifyAccess();
			foreach (var vm in realAllItems) {
				if (IsMatch_UI(vm, filterText))
					yield return vm;
			}
		}

		// UI thread
		bool IsMatch_UI(ProcessVM vm, string filterText) {
			Debug.Assert(processContext.UIDispatcher.CheckAccess());
			// Common case check, we don't need to allocate any strings
			if (filterText == string.Empty)
				return true;
			// The order must match searchColumnDefinitions
			var allStrings = new string[] {
				GetName_UI(vm),
				GetId_UI(vm),
				GetTitle_UI(vm),
				GetState_UI(vm),
				GetDebugging_UI(vm),
				GetArchitecture_UI(vm),
				GetPath_UI(vm),
			};
			sbOutput.Reset();
			return processContext.SearchMatcher.IsMatchAll(allStrings);
		}
		readonly DbgStringBuilderTextWriter sbOutput = new DbgStringBuilderTextWriter();

		// UI thread
		string GetName_UI(ProcessVM vm) {
			Debug.Assert(processContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			processContext.Formatter.WriteName(sbOutput, vm.Process);
			return sbOutput.ToString();
		}

		// UI thread
		string GetId_UI(ProcessVM vm) {
			Debug.Assert(processContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			processContext.Formatter.WriteId(sbOutput, vm.Process);
			return sbOutput.ToString();
		}

		// UI thread
		string GetTitle_UI(ProcessVM vm) {
			Debug.Assert(processContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			processContext.Formatter.WriteTitle(sbOutput, vm);
			return sbOutput.ToString();
		}

		// UI thread
		string GetState_UI(ProcessVM vm) {
			Debug.Assert(processContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			processContext.Formatter.WriteState(sbOutput, vm);
			return sbOutput.ToString();
		}

		// UI thread
		string GetDebugging_UI(ProcessVM vm) {
			Debug.Assert(processContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			processContext.Formatter.WriteDebugging(sbOutput, vm.Process);
			return sbOutput.ToString();
		}

		// UI thread
		string GetArchitecture_UI(ProcessVM vm) {
			Debug.Assert(processContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			processContext.Formatter.WriteArchitecture(sbOutput, vm.Process.Architecture);
			return sbOutput.ToString();
		}

		// UI thread
		string GetPath_UI(ProcessVM vm) {
			Debug.Assert(processContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			processContext.Formatter.WritePath(sbOutput, vm.Process);
			return sbOutput.ToString();
		}

		// UI thread
		void RemoveProcessAt_UI(int i) {
			processContext.UIDispatcher.VerifyAccess();
			Debug.Assert(0 <= i && i < realAllItems.Count);
			var vm = realAllItems[i];
			vm.Dispose();
			realAllItems.RemoveAt(i);
			AllItems.Remove(vm);
		}

		// UI thread
		void RemoveAllProcesses_UI() {
			processContext.UIDispatcher.VerifyAccess();
			AllItems.Reset(Array.Empty<ProcessVM>());
			var coll = realAllItems;
			for (int i = coll.Count - 1; i >= 0; i--)
				RemoveProcessAt_UI(i);
		}

		// UI thread
		public void ResetSearchSettings() {
			processContext.UIDispatcher.VerifyAccess();
			FilterText = string.Empty;
		}
	}
}

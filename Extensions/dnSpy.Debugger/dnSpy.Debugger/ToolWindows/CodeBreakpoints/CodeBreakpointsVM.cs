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
using dnSpy.Contracts.Controls.ToolWindows;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.ToolWindows.Search;
using dnSpy.Debugger.Breakpoints.Code;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.CodeBreakpoints {
	interface ICodeBreakpointsVM {
		bool IsOpen { get; set; }
		bool IsVisible { get; set; }
		BulkObservableCollection<CodeBreakpointVM> AllItems { get; }
		ObservableCollection<CodeBreakpointVM> SelectedItems { get; }
		void ResetSearchSettings();
		string GetSearchHelpText();
	}

	[Export(typeof(ICodeBreakpointsVM))]
	sealed class CodeBreakpointsVM : ViewModelBase, ICodeBreakpointsVM, ILazyToolWindowVM {
		public BulkObservableCollection<CodeBreakpointVM> AllItems { get; }
		public ObservableCollection<CodeBreakpointVM> SelectedItems { get; }

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

		IEditValueProvider LabelsEditValueProvider {
			get {
				codeBreakpointContext.UIDispatcher.VerifyAccess();
				if (labelsEditValueProvider == null)
					labelsEditValueProvider = editValueProviderService.Create(ContentTypes.CodeBreakpointsWindowLabels, Array.Empty<string>());
				return labelsEditValueProvider;
			}
		}
		IEditValueProvider labelsEditValueProvider;

		readonly Lazy<DbgManager> dbgManager;
		readonly CodeBreakpointContext codeBreakpointContext;
		readonly CodeBreakpointFormatterProvider codeBreakpointFormatterProvider;
		readonly DebuggerSettings debuggerSettings;
		readonly CodeBreakpointDisplaySettings codeBreakpointDisplaySettings;
		readonly LazyToolWindowVMHelper lazyToolWindowVMHelper;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		readonly Lazy<DbgBreakpointLocationFormatterService> dbgBreakpointLocationFormatterService;
		readonly EditValueProviderService editValueProviderService;
		readonly Dictionary<DbgCodeBreakpoint, CodeBreakpointVM> bpToVM;
		readonly List<CodeBreakpointVM> realAllItems;
		int codeBreakpointOrder;

		[ImportingConstructor]
		CodeBreakpointsVM(Lazy<DbgManager> dbgManager, DebuggerSettings debuggerSettings, CodeBreakpointDisplaySettings codeBreakpointDisplaySettings, UIDispatcher uiDispatcher, CodeBreakpointFormatterProvider codeBreakpointFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService, Lazy<DbgBreakpointLocationFormatterService> dbgBreakpointLocationFormatterService, BreakpointConditionsFormatter breakpointConditionsFormatter, EditValueProviderService editValueProviderService, DbgCodeBreakpointHitCountService2 dbgCodeBreakpointHitCountService) {
			uiDispatcher.VerifyAccess();
			sbOutput = new StringBuilderTextColorOutput();
			realAllItems = new List<CodeBreakpointVM>();
			AllItems = new BulkObservableCollection<CodeBreakpointVM>();
			SelectedItems = new ObservableCollection<CodeBreakpointVM>();
			bpToVM = new Dictionary<DbgCodeBreakpoint, CodeBreakpointVM>();
			this.dbgManager = dbgManager;
			this.codeBreakpointFormatterProvider = codeBreakpointFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			this.codeBreakpointDisplaySettings = codeBreakpointDisplaySettings;
			lazyToolWindowVMHelper = new LazyToolWindowVMHelper(this, uiDispatcher);
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			this.dbgBreakpointLocationFormatterService = dbgBreakpointLocationFormatterService;
			this.editValueProviderService = editValueProviderService;
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			codeBreakpointContext = new CodeBreakpointContext(uiDispatcher, classificationFormatMap, textElementProvider, breakpointConditionsFormatter, dbgCodeBreakpointHitCountService, new SearchMatcher(searchColumnDefinitions)) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlight,
				Formatter = codeBreakpointFormatterProvider.Create(),
			};
		}
		// Don't change the order of these instances without also updating input passed to SearchMatcher.IsMatchAll()
		static readonly SearchColumnDefinition[] searchColumnDefinitions = new SearchColumnDefinition[] {
			new SearchColumnDefinition(PredefinedTextClassifierTags.CodeBreakpointsWindowName, "n", dnSpy_Debugger_Resources.Column_Name),
			new SearchColumnDefinition(PredefinedTextClassifierTags.CodeBreakpointsWindowLabels, "l", dnSpy_Debugger_Resources.Column_Labels),
			new SearchColumnDefinition(PredefinedTextClassifierTags.CodeBreakpointsWindowCondition, "c", dnSpy_Debugger_Resources.Column_Condition),
			new SearchColumnDefinition(PredefinedTextClassifierTags.CodeBreakpointsWindowHitCount, "h", dnSpy_Debugger_Resources.Column_HitCount),
			new SearchColumnDefinition(PredefinedTextClassifierTags.CodeBreakpointsWindowFilter, "f", dnSpy_Debugger_Resources.Column_Filter),
			new SearchColumnDefinition(PredefinedTextClassifierTags.CodeBreakpointsWindowWhenHit, "w", dnSpy_Debugger_Resources.Column_WhenHit),
			new SearchColumnDefinition(PredefinedTextClassifierTags.CodeBreakpointsWindowModule, "m", dnSpy_Debugger_Resources.Column_Module),
		};

		// DbgManager thread
		void DbgCodeBreakpointHitCountService_HitCountChanged(object sender, DbgHitCountChangedEventArgs e) =>
			UI(() => DbgCodeBreakpointHitCountService_HitCountChanged_UI(e.Breakpoints));

		// UI thread
		void DbgCodeBreakpointHitCountService_HitCountChanged_UI(ReadOnlyCollection<DbgCodeBreakpointAndHitCount> breakpoints) {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			foreach (var info in breakpoints) {
				if (bpToVM.TryGetValue(info.Breakpoint, out var vm))
					vm.OnHitCountChanged_UI();
			}
		}

		// UI thread
		public string GetSearchHelpText() {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			return codeBreakpointContext.SearchMatcher.GetHelpText();
		}

		// random thread
		void DbgThread(Action callback) =>
			dbgManager.Value.DispatcherThread.BeginInvoke(callback);

		// UI thread
		void ILazyToolWindowVM.Show() {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: true);
		}

		// UI thread
		void ILazyToolWindowVM.Hide() {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: false);
		}

		// UI thread
		void InitializeDebugger_UI(bool enable) {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			ResetSearchSettings();
			if (enable) {
				codeBreakpointContext.ClassificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
				codeBreakpointDisplaySettings.PropertyChanged += CodeBreakpointDisplaySettings_PropertyChanged;
				RecreateFormatter_UI();
				codeBreakpointContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
			}
			else {
				codeBreakpointContext.ClassificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged -= DebuggerSettings_PropertyChanged;
				codeBreakpointDisplaySettings.PropertyChanged -= CodeBreakpointDisplaySettings_PropertyChanged;
			}
			DbgThread(() => InitializeDebugger_DbgThread(enable));
		}

		// DbgManager thread
		void InitializeDebugger_DbgThread(bool enable) {
			dbgManager.Value.DispatcherThread.VerifyAccess();
			if (enable) {
				dbgCodeBreakpointsService.Value.BreakpointsChanged += DbgCodeBreakpointsService_BreakpointsChanged;
				dbgCodeBreakpointsService.Value.BreakpointsModified += DbgCodeBreakpointsService_BreakpointsModified;
				dbgCodeBreakpointsService.Value.BoundBreakpointsMessageChanged += DbgCodeBreakpointsService_BoundBreakpointsMessageChanged;
				codeBreakpointContext.DbgCodeBreakpointHitCountService.HitCountChanged += DbgCodeBreakpointHitCountService_HitCountChanged;
				var breakpoints = dbgCodeBreakpointsService.Value.Breakpoints;
				if (breakpoints.Length > 0)
					UI(() => AddItems_UI(breakpoints));
			}
			else {
				dbgCodeBreakpointsService.Value.BreakpointsChanged -= DbgCodeBreakpointsService_BreakpointsChanged;
				dbgCodeBreakpointsService.Value.BreakpointsModified -= DbgCodeBreakpointsService_BreakpointsModified;
				dbgCodeBreakpointsService.Value.BoundBreakpointsMessageChanged -= DbgCodeBreakpointsService_BoundBreakpointsMessageChanged;
				codeBreakpointContext.DbgCodeBreakpointHitCountService.HitCountChanged -= DbgCodeBreakpointHitCountService_HitCountChanged;
				UI(() => RemoveAllCodeBreakpoints_UI());
			}
		}

		// UI thread
		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			RefreshThemeFields_UI();
		}

		// random thread
		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => DebuggerSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DebuggerSettings_PropertyChanged_UI(string propertyName) {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			if (propertyName == nameof(DebuggerSettings.SyntaxHighlight)) {
				codeBreakpointContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				RefreshThemeFields_UI();
			}
		}

		// random thread
		void CodeBreakpointDisplaySettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => CodeBreakpointDisplaySettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void CodeBreakpointDisplaySettings_PropertyChanged_UI(string propertyName) {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			switch (propertyName) {
			case nameof(CodeBreakpointDisplaySettings.ShowTokens):
			case nameof(CodeBreakpointDisplaySettings.ShowModuleNames):
			case nameof(CodeBreakpointDisplaySettings.ShowParameterTypes):
			case nameof(CodeBreakpointDisplaySettings.ShowParameterNames):
			case nameof(CodeBreakpointDisplaySettings.ShowDeclaringTypes):
			case nameof(CodeBreakpointDisplaySettings.ShowReturnTypes):
			case nameof(CodeBreakpointDisplaySettings.ShowNamespaces):
			case nameof(CodeBreakpointDisplaySettings.ShowIntrinsicTypeKeywords):
				RefreshNameColumn_UI();
				break;

			default:
				Debug.Fail($"Unknown property: {propertyName}");
				break;
			}
		}

		// UI thread
		void RefreshThemeFields_UI() {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			foreach (var vm in realAllItems)
				vm.RefreshThemeFields_UI();
		}

		// UI thread
		void RefreshNameColumn_UI() {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			foreach (var vm in realAllItems)
				vm.RefreshNameColumn_UI();
		}

		// UI thread
		void RecreateFormatter_UI() {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			codeBreakpointContext.Formatter = codeBreakpointFormatterProvider.Create();
		}

		// random thread
		void UI(Action callback) => codeBreakpointContext.UIDispatcher.UI(callback);

		// DbgManager thread
		void DbgCodeBreakpointsService_BreakpointsChanged(object sender, DbgCollectionChangedEventArgs<DbgCodeBreakpoint> e) {
			dbgManager.Value.DispatcherThread.VerifyAccess();
			if (e.Added)
				UI(() => AddItems_UI(e.Objects));
			else {
				UI(() => {
					var coll = realAllItems;
					for (int i = coll.Count - 1; i >= 0; i--) {
						if (e.Objects.Contains(coll[i].CodeBreakpoint))
							RemoveCodeBreakpointAt_UI(i);
					}
					InitializeNothingMatched();
				});
			}
		}

		// DbgManager thread
		void DbgCodeBreakpointsService_BreakpointsModified(object sender, DbgBreakpointsModifiedEventArgs e) {
			dbgManager.Value.DispatcherThread.VerifyAccess();
			UI(() => {
				foreach (var info in e.Breakpoints) {
					bool b = bpToVM.TryGetValue(info.Breakpoint, out var vm);
					Debug.Assert(b);
					if (b)
						vm.UpdateSettings_UI(info.Breakpoint.Settings);
				}
			});
		}

		// DbgManager thread
		void DbgCodeBreakpointsService_BoundBreakpointsMessageChanged(object sender, BoundBreakpointsMessageChangedEventArgs e) =>
			UI(() => OnBoundBreakpointsMessageChanged_UI(e.Breakpoints));

		// UI thread
		void OnBoundBreakpointsMessageChanged_UI(ReadOnlyCollection<DbgCodeBreakpoint> breakpoints) {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			foreach (var bp in breakpoints) {
				bool b = bpToVM.TryGetValue(bp, out var vm);
				Debug.Assert(b);
				if (!b)
					continue;
				vm.UpdateImageAndMessage_UI();
			}
		}

		// UI thread
		void AddItems_UI(IList<DbgCodeBreakpoint> codeBreakpoints) {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			foreach (var bp in codeBreakpoints) {
				var vm = new CodeBreakpointVM(bp, dbgBreakpointLocationFormatterService.Value.GetFormatter(bp.Location), codeBreakpointContext, codeBreakpointOrder++, LabelsEditValueProvider);
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
		int GetInsertionIndex_UI(CodeBreakpointVM vm) {
			Debug.Assert(codeBreakpointContext.UIDispatcher.CheckAccess());
			var comparer = CodeBreakpointVMComparer.Instance;
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
		void FilterList_UI(string filterText) {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			if (string.IsNullOrWhiteSpace(filterText))
				filterText = string.Empty;
			codeBreakpointContext.SearchMatcher.SetSearchText(filterText);

			var newList = new List<CodeBreakpointVM>(GetFilteredItems_UI(filterText));
			newList.Sort(CodeBreakpointVMComparer.Instance);
			AllItems.Reset(newList);
			InitializeNothingMatched(filterText);
		}

		void InitializeNothingMatched() => InitializeNothingMatched(filterText);
		void InitializeNothingMatched(string filterText) =>
			NothingMatched = AllItems.Count == 0 && !string.IsNullOrWhiteSpace(filterText);

		sealed class CodeBreakpointVMComparer : IComparer<CodeBreakpointVM> {
			public static readonly IComparer<CodeBreakpointVM> Instance = new CodeBreakpointVMComparer();
			public int Compare(CodeBreakpointVM x, CodeBreakpointVM y) => x.Order - y.Order;
		}

		// UI thread
		IEnumerable<CodeBreakpointVM> GetFilteredItems_UI(string filterText) {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			foreach (var vm in realAllItems) {
				if (IsMatch_UI(vm, filterText))
					yield return vm;
			}
		}

		// UI thread
		bool IsMatch_UI(CodeBreakpointVM vm, string filterText) {
			Debug.Assert(codeBreakpointContext.UIDispatcher.CheckAccess());
			// Common case check, we don't need to allocate any strings
			if (filterText == string.Empty)
				return true;
			// The order must match searchColumnDefinitions
			var allStrings = new string[] {
				GetName_UI(vm),
				GetLabels_UI(vm),
				GetCondition_UI(vm),
				GetHitCount_UI(vm),
				GetFilter_UI(vm),
				GetWhenHit_UI(vm),
				GetModule_UI(vm),
			};
			sbOutput.Reset();
			return codeBreakpointContext.SearchMatcher.IsMatchAll(allStrings);
		}
		readonly StringBuilderTextColorOutput sbOutput;

		// UI thread
		string GetName_UI(CodeBreakpointVM vm) {
			Debug.Assert(codeBreakpointContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			codeBreakpointContext.Formatter.WriteName(sbOutput, vm);
			return sbOutput.ToString();
		}

		// UI thread
		string GetLabels_UI(CodeBreakpointVM vm) {
			Debug.Assert(codeBreakpointContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			codeBreakpointContext.Formatter.WriteLabels(sbOutput, vm);
			return sbOutput.ToString();
		}

		// UI thread
		string GetCondition_UI(CodeBreakpointVM vm) {
			Debug.Assert(codeBreakpointContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			codeBreakpointContext.Formatter.WriteCondition(sbOutput, vm);
			return sbOutput.ToString();
		}

		// UI thread
		string GetHitCount_UI(CodeBreakpointVM vm) {
			Debug.Assert(codeBreakpointContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			codeBreakpointContext.Formatter.WriteHitCount(sbOutput, vm);
			return sbOutput.ToString();
		}

		// UI thread
		string GetFilter_UI(CodeBreakpointVM vm) {
			Debug.Assert(codeBreakpointContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			codeBreakpointContext.Formatter.WriteFilter(sbOutput, vm);
			return sbOutput.ToString();
		}

		// UI thread
		string GetWhenHit_UI(CodeBreakpointVM vm) {
			Debug.Assert(codeBreakpointContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			codeBreakpointContext.Formatter.WriteWhenHit(sbOutput, vm);
			return sbOutput.ToString();
		}

		// UI thread
		string GetModule_UI(CodeBreakpointVM vm) {
			Debug.Assert(codeBreakpointContext.UIDispatcher.CheckAccess());
			sbOutput.Reset();
			codeBreakpointContext.Formatter.WriteModule(sbOutput, vm);
			return sbOutput.ToString();
		}

		// UI thread
		void RemoveCodeBreakpointAt_UI(int i) {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			Debug.Assert(0 <= i && i < realAllItems.Count);
			var vm = realAllItems[i];
			bool b = bpToVM.Remove(vm.CodeBreakpoint);
			Debug.Assert(b);
			vm.Dispose();
			realAllItems.RemoveAt(i);
			AllItems.Remove(vm);
		}

		// UI thread
		void RemoveAllCodeBreakpoints_UI() {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			AllItems.Reset(Array.Empty<CodeBreakpointVM>());
			var coll = realAllItems;
			for (int i = coll.Count - 1; i >= 0; i--)
				RemoveCodeBreakpointAt_UI(i);
			Debug.Assert(bpToVM.Count == 0);
		}

		// UI thread
		public void ResetSearchSettings() {
			codeBreakpointContext.UIDispatcher.VerifyAccess();
			FilterText = string.Empty;
		}
	}
}

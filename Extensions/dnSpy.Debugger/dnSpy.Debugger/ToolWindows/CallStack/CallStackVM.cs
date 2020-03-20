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
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Debugger.Breakpoints.Code;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.UI;
using dnSpy.Debugger.UI.Wpf;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.CallStack {
	interface ICallStackVM : IGridViewColumnDescsProvider {
		bool IsOpen { get; set; }
		bool IsVisible { get; set; }
		ObservableCollection<StackFrameVM> AllItems { get; }
		ObservableCollection<StackFrameVM> SelectedItems { get; }
	}

	[Export(typeof(ICallStackVM))]
	sealed class CallStackVM : ViewModelBase, ICallStackVM, ILazyToolWindowVM {
		public ObservableCollection<StackFrameVM> AllItems { get; }
		public ObservableCollection<StackFrameVM> SelectedItems { get; }
		public GridViewColumnDescs Descs { get; }

		public bool IsOpen {
			get => lazyToolWindowVMHelper.IsOpen;
			set => lazyToolWindowVMHelper.IsOpen = value;
		}

		public bool IsVisible {
			get => lazyToolWindowVMHelper.IsVisible;
			set => lazyToolWindowVMHelper.IsVisible = value;
		}

		readonly Lazy<DbgManager> dbgManager;
		readonly CallStackContext callStackContext;
		readonly Lazy<DbgCallStackService> dbgCallStackService;
		readonly Lazy<DbgLanguageService> dbgLanguageService;
		readonly CallStackDisplaySettings callStackDisplaySettings;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		readonly CallStackFormatterProvider callStackFormatterProvider;
		readonly DebuggerSettings debuggerSettings;
		readonly LazyToolWindowVMHelper lazyToolWindowVMHelper;
		readonly Func<NormalStackFrameVM, BreakpointKind?> getBreakpointKind;
		readonly Dictionary<DbgCodeBreakpoint, HashSet<NormalStackFrameVM>> usedBreakpoints;

		[ImportingConstructor]
		CallStackVM(Lazy<DbgManager> dbgManager, DebuggerSettings debuggerSettings, UIDispatcher uiDispatcher, Lazy<DbgCallStackService> dbgCallStackService, Lazy<DbgLanguageService> dbgLanguageService, CallStackDisplaySettings callStackDisplaySettings, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService, CallStackFormatterProvider callStackFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextBlockContentInfoFactory textBlockContentInfoFactory) {
			uiDispatcher.VerifyAccess();
			AllItems = new ObservableCollection<StackFrameVM>();
			SelectedItems = new ObservableCollection<StackFrameVM>();
			this.dbgManager = dbgManager;
			this.dbgCallStackService = dbgCallStackService;
			this.dbgLanguageService = dbgLanguageService;
			this.callStackDisplaySettings = callStackDisplaySettings;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			getBreakpointKind = GetBreakpointKind_UI;
			usedBreakpoints = new Dictionary<DbgCodeBreakpoint, HashSet<NormalStackFrameVM>>();
			this.callStackFormatterProvider = callStackFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			lazyToolWindowVMHelper = new DebuggerLazyToolWindowVMHelper(this, uiDispatcher, dbgManager);
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			callStackContext = new CallStackContext(uiDispatcher, classificationFormatMap, textBlockContentInfoFactory, callStackFormatterProvider.Create()) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlight,
				StackFrameFormatterOptions = GetStackFrameFormatterOptions(),
				ValueFormatterOptions = GetValueFormatterOptions(),
			};
			Descs = new GridViewColumnDescs {
				Columns = new GridViewColumnDesc[] {
					new GridViewColumnDesc(CallStackWindowColumnIds.Icon, string.Empty) { CanBeSorted = false },
					new GridViewColumnDesc(CallStackWindowColumnIds.Name, dnSpy_Debugger_Resources.Column_Name) { CanBeSorted = false },
				},
			};
			Descs.SortedColumnChanged += (a, b) => throw new InvalidOperationException();
		}

		// random thread
		void DbgThread(Action callback) =>
			dbgManager.Value.Dispatcher.BeginInvoke(callback);

		// UI thread
		void ILazyToolWindowVM.Show() {
			callStackContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: true);
		}

		// UI thread
		void ILazyToolWindowVM.Hide() {
			callStackContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: false);
		}

		// UI thread
		void InitializeDebugger_UI(bool enable) {
			callStackContext.UIDispatcher.VerifyAccess();
			ClearUsedBreakpoints_UI();
			if (enable) {
				callStackDisplaySettings.PropertyChanged += CallStackDisplaySettings_PropertyChanged;
				callStackContext.ClassificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
				callStackContext.UIVersion++;
				RecreateFormatter_UI();
				callStackContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				callStackContext.StackFrameFormatterOptions = GetStackFrameFormatterOptions();
				callStackContext.ValueFormatterOptions = GetValueFormatterOptions();
			}
			else {
				callStackDisplaySettings.PropertyChanged -= CallStackDisplaySettings_PropertyChanged;
				callStackContext.ClassificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged -= DebuggerSettings_PropertyChanged;
			}
			DbgThread(() => InitializeDebugger_DbgThread(enable));
		}

		// DbgManager thread
		void InitializeDebugger_DbgThread(bool enable) {
			dbgManager.Value.Dispatcher.VerifyAccess();
			if (enable) {
				dbgCallStackService.Value.FramesChanged += DbgCallStackService_FramesChanged;
				dbgManager.Value.DelayedIsRunningChanged += DbgManager_DelayedIsRunningChanged;
				dbgCodeBreakpointsService.Value.BreakpointsModified += DbgCodeBreakpointsService_BreakpointsModified;
				dbgCodeBreakpointsService.Value.BreakpointsChanged += DbgCodeBreakpointsService_BreakpointsChanged;
				dbgLanguageService.Value.LanguageChanged += DbgLanguageService_LanguageChanged;

				var framesInfo = dbgCallStackService.Value.Frames;
				var thread = dbgCallStackService.Value.Thread;
				UI(() => UpdateFrames_UI(framesInfo, thread));
			}
			else {
				dbgCallStackService.Value.FramesChanged -= DbgCallStackService_FramesChanged;
				dbgManager.Value.DelayedIsRunningChanged -= DbgManager_DelayedIsRunningChanged;
				dbgCodeBreakpointsService.Value.BreakpointsModified -= DbgCodeBreakpointsService_BreakpointsModified;
				dbgCodeBreakpointsService.Value.BreakpointsChanged -= DbgCodeBreakpointsService_BreakpointsChanged;
				dbgLanguageService.Value.LanguageChanged -= DbgLanguageService_LanguageChanged;

				UI(() => RemoveAllFrames_UI());
			}
		}

		// DbgManager thread
		void DbgLanguageService_LanguageChanged(object? sender, DbgLanguageChangedEventArgs e) => UI(() => RefreshLanguage_UI());

		// UI thread
		void ClassificationFormatMap_ClassificationFormatMappingChanged(object? sender, EventArgs e) {
			callStackContext.UIDispatcher.VerifyAccess();
			callStackContext.UIVersion++;
			RefreshThemeFields_UI();
		}

		// random thread
		void DebuggerSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e) =>
			UI(() => DebuggerSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DebuggerSettings_PropertyChanged_UI(string? propertyName) {
			callStackContext.UIDispatcher.VerifyAccess();
			switch (propertyName) {
			case nameof(DebuggerSettings.UseHexadecimal):
			case nameof(DebuggerSettings.UseDigitSeparators):
			case nameof(DebuggerSettings.PropertyEvalAndFunctionCalls):
			case nameof(DebuggerSettings.UseStringConversionFunction):
				RefreshName_UI();
				break;

			case nameof(DebuggerSettings.SyntaxHighlight):
				callStackContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				RefreshThemeFields_UI();
				break;
			}
		}

		// UI thread
		void RefreshThemeFields_UI() {
			callStackContext.UIDispatcher.VerifyAccess();
			foreach (var vm in AllItems)
				vm.RefreshThemeFields_UI();
		}

		// UI thread
		void RecreateFormatter_UI() {
			callStackContext.UIDispatcher.VerifyAccess();
			callStackContext.Formatter = callStackFormatterProvider.Create();
		}

		// random thread
		void CallStackDisplaySettings_PropertyChanged(object? sender, PropertyChangedEventArgs e) =>
			UI(() => CallStackDisplaySettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void CallStackDisplaySettings_PropertyChanged_UI(string? propertyName) {
			callStackContext.UIDispatcher.VerifyAccess();
			switch (propertyName) {
			case nameof(CallStackDisplaySettings.ShowReturnTypes):
			case nameof(CallStackDisplaySettings.ShowParameterTypes):
			case nameof(CallStackDisplaySettings.ShowParameterNames):
			case nameof(CallStackDisplaySettings.ShowParameterValues):
			case nameof(CallStackDisplaySettings.ShowFunctionOffset):
			case nameof(CallStackDisplaySettings.ShowModuleNames):
			case nameof(CallStackDisplaySettings.ShowDeclaringTypes):
			case nameof(CallStackDisplaySettings.ShowNamespaces):
			case nameof(CallStackDisplaySettings.ShowIntrinsicTypeKeywords):
			case nameof(CallStackDisplaySettings.ShowTokens):
				RefreshName_UI();
				break;

			default:
				Debug.Fail($"Unknown property name: {propertyName}");
				break;
			}
		}

		// random thread
		DbgStackFrameFormatterOptions GetStackFrameFormatterOptions() {
			var options = DbgStackFrameFormatterOptions.None;

			if (callStackDisplaySettings.ShowReturnTypes)			options |= DbgStackFrameFormatterOptions.ReturnTypes;
			if (callStackDisplaySettings.ShowParameterTypes)		options |= DbgStackFrameFormatterOptions.ParameterTypes;
			if (callStackDisplaySettings.ShowParameterNames)		options |= DbgStackFrameFormatterOptions.ParameterNames;
			if (callStackDisplaySettings.ShowParameterValues)		options |= DbgStackFrameFormatterOptions.ParameterValues;
			if (callStackDisplaySettings.ShowFunctionOffset)		options |= DbgStackFrameFormatterOptions.IP;
			if (callStackDisplaySettings.ShowModuleNames)			options |= DbgStackFrameFormatterOptions.ModuleNames;
			if (callStackDisplaySettings.ShowDeclaringTypes)		options |= DbgStackFrameFormatterOptions.DeclaringTypes;
			if (callStackDisplaySettings.ShowNamespaces)			options |= DbgStackFrameFormatterOptions.Namespaces;
			if (callStackDisplaySettings.ShowIntrinsicTypeKeywords)	options |= DbgStackFrameFormatterOptions.IntrinsicTypeKeywords;
			if (callStackDisplaySettings.ShowTokens)				options |= DbgStackFrameFormatterOptions.Tokens;
			if (!debuggerSettings.UseHexadecimal)					options |= DbgStackFrameFormatterOptions.Decimal;
			if (debuggerSettings.UseDigitSeparators)				options |= DbgStackFrameFormatterOptions.DigitSeparators;
			if (debuggerSettings.FullString)						options |= DbgStackFrameFormatterOptions.FullString;

			return options;
		}

		// random thread
		DbgValueFormatterOptions GetValueFormatterOptions() {
			var options = DbgValueFormatterOptions.None;

			if (!debuggerSettings.UseHexadecimal)					options |= DbgValueFormatterOptions.Decimal;
			// We don't enable func-eval since each func-eval will invalidate all stack frames
			//TODO: This can be enabled if we use the interpreter instead of calling real code
			//if (debuggerSettings.PropertyEvalAndFunctionCalls)	options |= DbgValueFormatterOptions.FuncEval;
			if (debuggerSettings.UseStringConversionFunction)		options |= DbgValueFormatterOptions.ToString;
			if (debuggerSettings.UseDigitSeparators)				options |= DbgValueFormatterOptions.DigitSeparators;
			if (debuggerSettings.FullString)						options |= DbgValueFormatterOptions.FullString;
			if (callStackDisplaySettings.ShowNamespaces)			options |= DbgValueFormatterOptions.Namespaces;
			if (callStackDisplaySettings.ShowIntrinsicTypeKeywords)	options |= DbgValueFormatterOptions.IntrinsicTypeKeywords;
			if (callStackDisplaySettings.ShowTokens)				options |= DbgValueFormatterOptions.Tokens;

			return options;
		}

		// UI thread
		void RefreshName_UI() {
			callStackContext.UIDispatcher.VerifyAccess();
			callStackContext.StackFrameFormatterOptions = GetStackFrameFormatterOptions();
			callStackContext.ValueFormatterOptions = GetValueFormatterOptions();
			RecreateFormatter_UI();
			foreach (var vm in AllItems)
				vm.RefreshName_UI();
		}

		// random thread
		void UI(Action callback) => callStackContext.UIDispatcher.UI(callback);

		// DbgManager thread
		void DbgCallStackService_FramesChanged(object? sender, FramesChangedEventArgs e) {
			var framesInfo = dbgCallStackService.Value.Frames;
			var thread = dbgCallStackService.Value.Thread;
			if (e.FramesChanged)
				UI(() => UpdateFrames_UI(framesInfo, thread));
			else if (e.ActiveFrameIndexChanged)
				UI(() => UpdateActiveFrameIndex_UI(framesInfo.ActiveFrameIndex));
		}

		// UI thread
		void UpdateActiveFrameIndex_UI(int activeFrameIndex) {
			callStackContext.UIDispatcher.VerifyAccess();
			foreach (var vm in AllItems)
				vm.IsActive = vm.Index == activeFrameIndex;
		}

		// DbgManager thread
		void DbgManager_DelayedIsRunningChanged(object? sender, EventArgs e) => UI(() => {
			// If all processes are running and the window is hidden, hide it now
			if (!IsVisible)
				lazyToolWindowVMHelper.TryHideWindow();
		});

		// DbgManager thread
		void DbgCodeBreakpointsService_BreakpointsModified(object? sender, DbgBreakpointsModifiedEventArgs e) =>
			UI(() => RefreshBreakpoints_UI(e.Breakpoints.Select(a => a.Breakpoint)));

		// DbgManager thread
		void DbgCodeBreakpointsService_BreakpointsChanged(object? sender, DbgCollectionChangedEventArgs<DbgCodeBreakpoint> e) =>
			UI(() => RefreshBreakpoints_UI(e));

		// DbgManager thread
		void DbgCodeBreakpoint_BoundBreakpointsMessageChanged(object? sender, EventArgs e) =>
			UI(() => RefreshBreakpoints_UI(new[] { (DbgCodeBreakpoint)sender! }));

		// UI thread
		void RefreshBreakpoints_UI(DbgCollectionChangedEventArgs<DbgCodeBreakpoint> e) {
			callStackContext.UIDispatcher.VerifyAccess();
			if (e.Added) {
				foreach (var vm in AllItems)
					vm.RefreshBreakpoint_UI();
			}
			else {
				if (usedBreakpoints.Count != 0) {
					RefreshBreakpoints_UI(e.Objects);
					foreach (var bp in e.Objects)
						usedBreakpoints.Remove(bp);
				}
			}
		}

		// UI thread
		void RefreshBreakpoints_UI(IEnumerable<DbgCodeBreakpoint> breakpoints) {
			callStackContext.UIDispatcher.VerifyAccess();
			if (usedBreakpoints.Count == 0)
				return;
			foreach (var breakpoint in breakpoints) {
				if (breakpoint.IsHidden)
					continue;
				if (usedBreakpoints.TryGetValue(breakpoint, out var hash)) {
					foreach (var vm in hash.ToArray())
						vm.RefreshBreakpoint_UI();
				}
			}
		}

		// UI thread
		void ClearAllItems_UI() {
			callStackContext.UIDispatcher.VerifyAccess();
			foreach (var vm in AllItems)
				vm.Dispose();
			AllItems.Clear();
		}

		// UI thread
		void RefreshLanguage_UI() {
			callStackContext.UIDispatcher.VerifyAccess();
			var language = framesThread is null ? null : dbgLanguageService.Value.GetCurrentLanguage(framesThread.Runtime.RuntimeKindGuid);
			foreach (var vm in AllItems)
				(vm as NormalStackFrameVM)?.SetLanguage_UI(language);
		}

		// UI thread
		void UpdateFrames_UI(DbgCallStackFramesInfo framesInfo, DbgThread? thread) {
			callStackContext.UIDispatcher.VerifyAccess();

			ClearUsedBreakpoints_UI();

			var newFrames = framesInfo.Frames;
			if (newFrames.Count == 0 || framesThread != thread)
				ClearAllItems_UI();
			framesThread = thread;

			bool oldFramesTruncated = AllItems.Count > 0 && AllItems[AllItems.Count - 1] is MessageStackFrameVM;
			int oldVisibleFramesCount = AllItems.Count - (oldFramesTruncated ? 1 : 0);

			int framesToAdd = newFrames.Count - oldVisibleFramesCount;
			const int MAX_FRAMES_DIFF = 50;
			if (Math.Abs(framesToAdd) > MAX_FRAMES_DIFF) {
				oldFramesTruncated = false;
				oldVisibleFramesCount = 0;
				framesToAdd = newFrames.Count;
				ClearAllItems_UI();
			}

			var language = framesThread is null ? null : dbgLanguageService.Value.GetCurrentLanguage(framesThread.Runtime.RuntimeKindGuid);

			int activeFrameIndex = framesInfo.ActiveFrameIndex;
			if (framesToAdd > 0) {
				for (int i = 0; i < framesToAdd; i++) {
					var frame = newFrames[i];
					var vm = new NormalStackFrameVM(language, frame, callStackContext, i, getBreakpointKind);
					vm.IsActive = i == activeFrameIndex;
					AllItems.Insert(i, vm);
				}
			}
			else if (framesToAdd < 0) {
				int frames = framesToAdd;
				while (frames++ < 0) {
					AllItems[0].Dispose();
					AllItems.RemoveAt(0);
				}
			}

			for (int i = framesToAdd >= 0 ? framesToAdd : 0; i < newFrames.Count; i++) {
				var vm = (NormalStackFrameVM)AllItems[i];
				var frame = newFrames[i];

				vm.Index = i;
				vm.IsActive = i == activeFrameIndex;
				vm.SetFrame_UI(language, frame);
			}

			if (oldFramesTruncated == framesInfo.FramesTruncated) {
			}
			else if (oldFramesTruncated && !framesInfo.FramesTruncated) {
				int last = AllItems.Count - 1;
				bool b = last >= 0 && AllItems[last] is MessageStackFrameVM;
				Debug.Assert(b);
				if (b) {
					AllItems[last].Dispose();
					AllItems.RemoveAt(last);
				}
			}
			else if (!oldFramesTruncated && framesInfo.FramesTruncated)
				AllItems.Add(new MessageStackFrameVM(dnSpy_Debugger_Resources.CallStack_MaxFramesExceeded, callStackContext, newFrames.Count));
		}
		DbgThread? framesThread;

		// UI thread
		BreakpointKind? GetBreakpointKind_UI(NormalStackFrameVM vm) {
			callStackContext.UIDispatcher.VerifyAccess();
			if ((vm.Frame.Flags & DbgStackFrameFlags.LocationIsNextStatement) == 0)
				return null;
			var location = vm.Frame.Location;
			var breakpoint = location is null ? null : dbgCodeBreakpointsService.Value.TryGetBreakpoint(location);
			if (breakpoint is null || breakpoint.IsHidden)
				return null;
			if (!usedBreakpoints.TryGetValue(breakpoint, out var hash)) {
				usedBreakpoints.Add(breakpoint, hash = new HashSet<NormalStackFrameVM>());
				breakpoint.BoundBreakpointsMessageChanged += DbgCodeBreakpoint_BoundBreakpointsMessageChanged;
			}
			hash.Add(vm);
			return BreakpointImageUtilities.GetBreakpointKind(breakpoint);
		}

		// UI thread
		void ClearUsedBreakpoints_UI() {
			callStackContext.UIDispatcher.VerifyAccess();
			foreach (var breakpoint in usedBreakpoints.Keys)
				breakpoint.BoundBreakpointsMessageChanged -= DbgCodeBreakpoint_BoundBreakpointsMessageChanged;
			usedBreakpoints.Clear();
		}

		// UI thread
		void RemoveAllFrames_UI() {
			callStackContext.UIDispatcher.VerifyAccess();
			ClearAllItems_UI();
			framesThread = null;
		}
	}
}

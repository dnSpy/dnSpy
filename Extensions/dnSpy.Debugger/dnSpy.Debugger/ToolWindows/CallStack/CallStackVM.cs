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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.Breakpoints.Code;
using dnSpy.Debugger.CallStack;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.CallStack {
	interface ICallStackVM {
		bool IsOpen { get; set; }
		bool IsVisible { get; set; }
		ObservableCollection<StackFrameVM> AllItems { get; }
		ObservableCollection<StackFrameVM> SelectedItems { get; }
	}

	[Export(typeof(ICallStackVM))]
	sealed class CallStackVM : ViewModelBase, ICallStackVM, ILazyToolWindowVM {
		public ObservableCollection<StackFrameVM> AllItems { get; }
		public ObservableCollection<StackFrameVM> SelectedItems { get; }

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
		readonly CallStackDisplaySettings callStackDisplaySettings;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		readonly Lazy<DbgCallStackBreakpointService> dbgCallStackBreakpointService;
		readonly CallStackFormatterProvider callStackFormatterProvider;
		readonly DebuggerSettings debuggerSettings;
		readonly LazyToolWindowVMHelper lazyToolWindowVMHelper;
		readonly Func<NormalStackFrameVM, BreakpointKind?> getBreakpointKind;
		readonly Dictionary<DbgCodeBreakpoint, HashSet<NormalStackFrameVM>> usedBreakpoints;

		[ImportingConstructor]
		CallStackVM(Lazy<DbgManager> dbgManager, DebuggerSettings debuggerSettings, UIDispatcher uiDispatcher, Lazy<DbgCallStackService> dbgCallStackService, CallStackDisplaySettings callStackDisplaySettings, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService, Lazy<DbgCallStackBreakpointService> dbgCallStackBreakpointService, CallStackFormatterProvider callStackFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider) {
			uiDispatcher.VerifyAccess();
			AllItems = new ObservableCollection<StackFrameVM>();
			SelectedItems = new ObservableCollection<StackFrameVM>();
			this.dbgManager = dbgManager;
			this.dbgCallStackService = dbgCallStackService;
			this.callStackDisplaySettings = callStackDisplaySettings;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			this.dbgCallStackBreakpointService = dbgCallStackBreakpointService;
			getBreakpointKind = GetBreakpointKind_UI;
			usedBreakpoints = new Dictionary<DbgCodeBreakpoint, HashSet<NormalStackFrameVM>>();
			this.callStackFormatterProvider = callStackFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			lazyToolWindowVMHelper = new DebuggerLazyToolWindowVMHelper(this, uiDispatcher, dbgManager);
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			callStackContext = new CallStackContext(uiDispatcher, classificationFormatMap, textElementProvider) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlight,
				Formatter = callStackFormatterProvider.Create(),
				StackFrameFormatOptions = GetStackFrameFormatOptions(),
			};
		}

		// random thread
		void DbgThread(Action callback) =>
			dbgManager.Value.DispatcherThread.BeginInvoke(callback);

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
				RecreateFormatter_UI();
				callStackContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				callStackContext.StackFrameFormatOptions = GetStackFrameFormatOptions();
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
			dbgManager.Value.DispatcherThread.VerifyAccess();
			if (enable) {
				dbgCallStackService.Value.FramesChanged += DbgCallStackService_FramesChanged;
				dbgCallStackService.Value.ActiveFrameIndexChanged += DbgCallStackService_ActiveFrameIndexChanged;
				dbgManager.Value.DelayedIsRunningChanged += DbgManager_DelayedIsRunningChanged;
				dbgCodeBreakpointsService.Value.BreakpointsModified += DbgCodeBreakpointsService_BreakpointsModified;
				dbgCodeBreakpointsService.Value.BreakpointsChanged += DbgCodeBreakpointsService_BreakpointsChanged;

				var framesInfo = dbgCallStackService.Value.Frames;
				var thread = dbgCallStackService.Value.Thread;
				UI(() => UpdateFrames_UI(framesInfo, thread));
			}
			else {
				dbgCallStackService.Value.FramesChanged -= DbgCallStackService_FramesChanged;
				dbgCallStackService.Value.ActiveFrameIndexChanged -= DbgCallStackService_ActiveFrameIndexChanged;
				dbgManager.Value.DelayedIsRunningChanged -= DbgManager_DelayedIsRunningChanged;
				dbgCodeBreakpointsService.Value.BreakpointsModified -= DbgCodeBreakpointsService_BreakpointsModified;
				dbgCodeBreakpointsService.Value.BreakpointsChanged -= DbgCodeBreakpointsService_BreakpointsChanged;

				UI(() => RemoveAllFrames_UI());
			}
		}

		// UI thread
		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			callStackContext.UIDispatcher.VerifyAccess();
			RefreshThemeFields_UI();
		}

		// random thread
		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => DebuggerSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DebuggerSettings_PropertyChanged_UI(string propertyName) {
			callStackContext.UIDispatcher.VerifyAccess();
			if (propertyName == nameof(DebuggerSettings.UseHexadecimal))
				RefreshHexFields_UI();
			else if (propertyName == nameof(DebuggerSettings.SyntaxHighlight)) {
				callStackContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				RefreshThemeFields_UI();
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

		// UI thread
		void RefreshHexFields_UI() {
			callStackContext.UIDispatcher.VerifyAccess();
			callStackContext.StackFrameFormatOptions = GetStackFrameFormatOptions();
			RecreateFormatter_UI();
			foreach (var vm in AllItems)
				vm.RefreshHexFields_UI();
		}

		// random thread
		void CallStackDisplaySettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => CallStackDisplaySettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void CallStackDisplaySettings_PropertyChanged_UI(string propertyName) {
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
				callStackContext.StackFrameFormatOptions = GetStackFrameFormatOptions();
				RefreshName_UI();
				break;

			default:
				Debug.Fail($"Unknown property name: {propertyName}");
				break;
			}
		}

		// random thread
		DbgStackFrameFormatOptions GetStackFrameFormatOptions() {
			var options = DbgStackFrameFormatOptions.None;

			if (callStackDisplaySettings.ShowReturnTypes)			options |= DbgStackFrameFormatOptions.ShowReturnTypes;
			if (callStackDisplaySettings.ShowParameterTypes)		options |= DbgStackFrameFormatOptions.ShowParameterTypes;
			if (callStackDisplaySettings.ShowParameterNames)		options |= DbgStackFrameFormatOptions.ShowParameterNames;
			if (callStackDisplaySettings.ShowParameterValues)		options |= DbgStackFrameFormatOptions.ShowParameterValues;
			if (callStackDisplaySettings.ShowFunctionOffset)		options |= DbgStackFrameFormatOptions.ShowFunctionOffset;
			if (callStackDisplaySettings.ShowModuleNames)			options |= DbgStackFrameFormatOptions.ShowModuleNames;
			if (callStackDisplaySettings.ShowDeclaringTypes)		options |= DbgStackFrameFormatOptions.ShowDeclaringTypes;
			if (callStackDisplaySettings.ShowNamespaces)			options |= DbgStackFrameFormatOptions.ShowNamespaces;
			if (callStackDisplaySettings.ShowIntrinsicTypeKeywords)	options |= DbgStackFrameFormatOptions.ShowIntrinsicTypeKeywords;
			if (callStackDisplaySettings.ShowTokens)				options |= DbgStackFrameFormatOptions.ShowTokens;
			if (!debuggerSettings.UseHexadecimal)					options |= DbgStackFrameFormatOptions.UseDecimal;

			return options;
		}

		// UI thread
		void RefreshName_UI() {
			callStackContext.UIDispatcher.VerifyAccess();
			RecreateFormatter_UI();
			foreach (var vm in AllItems)
				vm.RefreshName_UI();
		}

		// random thread
		void UI(Action callback) => callStackContext.UIDispatcher.UI(callback);

		// DbgManager thread
		void DbgCallStackService_FramesChanged(object sender, EventArgs e) {
			var framesInfo = dbgCallStackService.Value.Frames;
			var thread = dbgCallStackService.Value.Thread;
			UI(() => UpdateFrames_UI(framesInfo, thread));
		}

		// DbgManager thread
		void DbgCallStackService_ActiveFrameIndexChanged(object sender, EventArgs e) {
			int activeFrameIndex = dbgCallStackService.Value.ActiveFrameIndex;
			UI(() => UpdateActiveFrameIndex_UI(activeFrameIndex));
		}

		// UI thread
		void UpdateActiveFrameIndex_UI(int activeFrameIndex) {
			callStackContext.UIDispatcher.VerifyAccess();
			foreach (var vm in AllItems)
				vm.IsActive = vm.Index == activeFrameIndex;
		}

		// DbgManager thread
		void DbgManager_DelayedIsRunningChanged(object sender, EventArgs e) => UI(() => {
			// If all processes are running and the window is hidden, hide it now
			if (!IsVisible)
				lazyToolWindowVMHelper.TryHideWindow();
		});

		// DbgManager thread
		void DbgCodeBreakpointsService_BreakpointsModified(object sender, DbgBreakpointsModifiedEventArgs e) =>
			UI(() => RefreshBreakpoints_UI(e.Breakpoints.Select(a => a.Breakpoint)));

		// DbgManager thread
		void DbgCodeBreakpointsService_BreakpointsChanged(object sender, DbgCollectionChangedEventArgs<DbgCodeBreakpoint> e) =>
			UI(() => RefreshBreakpoints_UI(e));

		// DbgManager thread
		void DbgCodeBreakpoint_BoundBreakpointsMessageChanged(object sender, EventArgs e) =>
			UI(() => RefreshBreakpoints_UI(new[] { (DbgCodeBreakpoint)sender }));

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
		void UpdateFrames_UI(DbgCallStackFramesInfo framesInfo, DbgThread thread) {
			callStackContext.UIDispatcher.VerifyAccess();

			ClearUsedBreakpoints_UI();

			var newFrames = framesInfo.Frames;
			if (newFrames.Count == 0 || framesThread != thread)
				AllItems.Clear();
			framesThread = thread;

			bool oldFramesTruncated = AllItems.Count > 0 && AllItems[AllItems.Count - 1] is MessageStackFrameVM;
			int oldVisibleFramesCount = AllItems.Count - (oldFramesTruncated ? 1 : 0);

			int framesToAdd = newFrames.Count - oldVisibleFramesCount;
			const int MAX_FRAMES_DIFF = 50;
			if (Math.Abs(framesToAdd) > MAX_FRAMES_DIFF) {
				oldFramesTruncated = false;
				oldVisibleFramesCount = 0;
				framesToAdd = newFrames.Count;
				AllItems.Clear();
			}

			int activeFrameIndex = framesInfo.ActiveFrameIndex;
			if (framesToAdd > 0) {
				for (int i = 0; i < framesToAdd; i++) {
					var frame = newFrames[i];
					var vm = new NormalStackFrameVM(frame, callStackContext, i, getBreakpointKind);
					vm.IsActive = i == activeFrameIndex;
					AllItems.Insert(i, vm);
				}
			}
			else if (framesToAdd < 0) {
				int frames = framesToAdd;
				while (frames++ < 0)
					AllItems.RemoveAt(0);
			}

			for (int i = framesToAdd >= 0 ? framesToAdd : 0; i < newFrames.Count; i++) {
				var vm = (NormalStackFrameVM)AllItems[i];
				var frame = newFrames[i];

				vm.Index = i;
				vm.IsActive = i == activeFrameIndex;
				vm.SetFrame_UI(frame);
			}

			if (oldFramesTruncated == framesInfo.FramesTruncated) {
			}
			else if (oldFramesTruncated && !framesInfo.FramesTruncated) {
				bool b = AllItems.Count > 0 && AllItems[AllItems.Count - 1] is MessageStackFrameVM;
				Debug.Assert(b);
				if (b)
					AllItems.RemoveAt(AllItems.Count - 1);
			}
			else if (!oldFramesTruncated && framesInfo.FramesTruncated)
				AllItems.Add(new MessageStackFrameVM(dnSpy_Debugger_Resources.CallStack_MaxFramesExceeded, callStackContext, newFrames.Count));
		}
		DbgThread framesThread;

		// UI thread
		BreakpointKind? GetBreakpointKind_UI(NormalStackFrameVM vm) {
			callStackContext.UIDispatcher.VerifyAccess();
			var breakpoint = dbgCallStackBreakpointService.Value.TryGetBreakpoint(vm.Frame.Location);
			if (breakpoint == null || breakpoint.IsHidden)
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
			AllItems.Clear();
			framesThread = null;
		}
	}
}

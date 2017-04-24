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
using dnSpy.Contracts.Debugger;
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
		readonly Lazy<CallStackService> callStackService;
		readonly CallStackDisplaySettings callStackDisplaySettings;
		readonly CallStackFormatterProvider callStackFormatterProvider;
		readonly DebuggerSettings debuggerSettings;
		readonly LazyToolWindowVMHelper lazyToolWindowVMHelper;

		[ImportingConstructor]
		CallStackVM(Lazy<DbgManager> dbgManager, DebuggerSettings debuggerSettings, UIDispatcher uiDispatcher, Lazy<CallStackService> callStackService, CallStackDisplaySettings callStackDisplaySettings, CallStackFormatterProvider callStackFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider) {
			uiDispatcher.VerifyAccess();
			AllItems = new ObservableCollection<StackFrameVM>();
			SelectedItems = new ObservableCollection<StackFrameVM>();
			this.dbgManager = dbgManager;
			this.callStackService = callStackService;
			this.callStackDisplaySettings = callStackDisplaySettings;
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
				callStackService.Value.FramesChanged += CallStackService_FramesChanged;
				callStackService.Value.ActiveFrameIndexChanged += CallStackService_ActiveFrameIndexChanged;
				dbgManager.Value.DelayedIsRunningChanged += DbgManager_DelayedIsRunningChanged;

				var framesInfo = callStackService.Value.Frames;
				var thread = callStackService.Value.Thread;
				UI(() => UpdateFrames_UI(framesInfo, thread));
			}
			else {
				callStackService.Value.FramesChanged -= CallStackService_FramesChanged;
				callStackService.Value.ActiveFrameIndexChanged -= CallStackService_ActiveFrameIndexChanged;
				dbgManager.Value.DelayedIsRunningChanged -= DbgManager_DelayedIsRunningChanged;

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
		void CallStackService_FramesChanged(object sender, EventArgs e) {
			var framesInfo = callStackService.Value.Frames;
			var thread = callStackService.Value.Thread;
			UI(() => UpdateFrames_UI(framesInfo, thread));
		}

		// DbgManager thread
		void CallStackService_ActiveFrameIndexChanged(object sender, EventArgs e) {
			int activeFrameIndex = callStackService.Value.ActiveFrameIndex;
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

		// UI thread
		void UpdateFrames_UI(DbgCallStackFramesInfo framesInfo, DbgThread thread) {
			callStackContext.UIDispatcher.VerifyAccess();

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
					var vm = new NormalStackFrameVM(frame, callStackContext, i);
					vm.IsActive = i == activeFrameIndex;
					vm.BreakpointKind = GetBreakpointKind(frame);
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
				vm.BreakpointKind = GetBreakpointKind(frame);
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

		BreakpointKind? GetBreakpointKind(DbgStackFrame frame) => null;//TODO:

		// UI thread
		void RemoveAllFrames_UI() {
			callStackContext.UIDispatcher.VerifyAccess();
			AllItems.Clear();
			framesThread = null;
		}
	}
}

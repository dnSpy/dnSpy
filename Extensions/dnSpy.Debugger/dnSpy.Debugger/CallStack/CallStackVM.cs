/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dndbg.Engine;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.Properties;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.CallStack {
	interface ICallStackVM {
		bool IsEnabled { get; set; }
		bool IsVisible { get; set; }
	}

	[Export(typeof(ICallStackVM))]
	sealed class CallStackVM : ViewModelBase, ICallStackVM {
		public bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled != value) {
					// Don't call OnPropertyChanged() since it's only used internally by the View
					isEnabled = value;
					InitializeStackFrames();
				}
			}
		}
		bool isEnabled;

		public bool IsVisible {//TODO: Use it
			get { return isVisible; }
			set { isVisible = value; }
		}
		bool isVisible;

		public ObservableCollection<ICallStackFrameVM> Collection => framesList;
		readonly ObservableCollection<ICallStackFrameVM> framesList;

		public object SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnPropertyChanged(nameof(SelectedItem));
				}
			}
		}
		object selectedItem;

		TypePrinterFlags TypePrinterFlags {
			get {
				var flags = TypePrinterFlags.ShowArrayValueSizes;
				if (!debuggerSettings.UseHexadecimal) flags |= TypePrinterFlags.UseDecimal;
				if (callStackSettings.ShowModuleNames) flags |= TypePrinterFlags.ShowModuleNames;
				if (callStackSettings.ShowParameterTypes) flags |= TypePrinterFlags.ShowParameterTypes;
				if (callStackSettings.ShowParameterNames) flags |= TypePrinterFlags.ShowParameterNames;
				if (callStackSettings.ShowParameterValues) flags |= TypePrinterFlags.ShowParameterValues;
				if (callStackSettings.ShowIP) flags |= TypePrinterFlags.ShowIP;
				if (callStackSettings.ShowOwnerTypes) flags |= TypePrinterFlags.ShowOwnerTypes;
				if (callStackSettings.ShowNamespaces) flags |= TypePrinterFlags.ShowNamespaces;
				if (callStackSettings.ShowTypeKeywords) flags |= TypePrinterFlags.ShowTypeKeywords;
				if (callStackSettings.ShowTokens) flags |= TypePrinterFlags.ShowTokens;
				if (callStackSettings.ShowReturnTypes) flags |= TypePrinterFlags.ShowReturnTypes;
				return flags;
			}
		}

		readonly IDebuggerSettings debuggerSettings;
		readonly ICallStackSettings callStackSettings;
		readonly ITheDebugger theDebugger;
		readonly IStackFrameService stackFrameService;
		readonly CallStackFrameContext callStackFrameContext;

		[ImportingConstructor]
		CallStackVM(IDebuggerSettings debuggerSettings, ICallStackSettings callStackSettings, IStackFrameService stackFrameService, ITheDebugger theDebugger, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider) {
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.CallStackWindow);
			this.debuggerSettings = debuggerSettings;
			this.callStackSettings = callStackSettings;
			this.theDebugger = theDebugger;
			this.stackFrameService = stackFrameService;
			this.framesList = new ObservableCollection<ICallStackFrameVM>();
			this.callStackFrameContext = new CallStackFrameContext(classificationFormatMap, textElementProvider) {
				TypePrinterFlags = TypePrinterFlags,
				SyntaxHighlight = debuggerSettings.SyntaxHighlightCallStack,
			};

			stackFrameService.StackFramesUpdated += StackFrameService_StackFramesUpdated;
			stackFrameService.PropertyChanged += StackFrameService_PropertyChanged;
			callStackSettings.PropertyChanged += CallStackSettings_PropertyChanged;
			debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
			theDebugger.ProcessRunning += TheDebugger_ProcessRunning;
			classificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
		}

		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) => RefreshThemeFields();
		void TheDebugger_ProcessRunning(object sender, EventArgs e) => InitializeStackFrames();

		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			switch (e.PropertyName) {
			case nameof(debuggerSettings.UseHexadecimal):
				callStackFrameContext.TypePrinterFlags = TypePrinterFlags;
				RefreshFrameNames();
				break;

			case nameof(debuggerSettings.SyntaxHighlightCallStack):
				callStackFrameContext.SyntaxHighlight = debuggerSettings.SyntaxHighlightCallStack;
				RefreshFrameNames();
				break;

			case nameof(debuggerSettings.PropertyEvalAndFunctionCalls):
			case nameof(debuggerSettings.UseStringConversionFunction):
				RefreshFrameNames();
				break;
			}
		}

		void CallStackSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			switch (e.PropertyName) {
			case nameof(callStackSettings.ShowModuleNames):
			case nameof(callStackSettings.ShowParameterTypes):
			case nameof(callStackSettings.ShowParameterNames):
			case nameof(callStackSettings.ShowParameterValues):
			case nameof(callStackSettings.ShowIP):
			case nameof(callStackSettings.ShowOwnerTypes):
			case nameof(callStackSettings.ShowNamespaces):
			case nameof(callStackSettings.ShowTypeKeywords):
			case nameof(callStackSettings.ShowTokens):
			case nameof(callStackSettings.ShowReturnTypes):
				callStackFrameContext.TypePrinterFlags = TypePrinterFlags;
				RefreshFrameNames();
				break;
			}
		}

		void StackFrameService_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(IStackFrameService.SelectedThread)) {
				framesList.Clear();
				InitializeStackFrames();
			}
			else if (e.PropertyName == nameof(IStackFrameService.SelectedFrameNumber))
				UpdateSelectedFrame(e as VMPropertyChangedEventArgs<int>);
		}

		void StackFrameService_StackFramesUpdated(object sender, StackFramesUpdatedEventArgs e) {
			if (e.Debugger.IsEvaluating)
				return;
			// InitializeStackFrames() is called when the process has been running for a little while. Speeds up stepping.
			if (theDebugger.ProcessState != DebuggerProcessState.Continuing && theDebugger.ProcessState != DebuggerProcessState.Running)
				InitializeStackFrames();
		}

		void InitializeStackFrames() {
			if (!IsEnabled || theDebugger.ProcessState != DebuggerProcessState.Paused) {
				framesList.Clear();
				return;
			}

			var process = theDebugger.Debugger?.Processes?.FirstOrDefault();
			Debug.Assert(process != null);
			if (process == null) {
				framesList.Clear();
				return;
			}

			bool tooManyFrames;
			var newFrames = stackFrameService.GetFrames(out tooManyFrames);

			bool oldHadTooManyFrames = framesList.Count > 0 && framesList[framesList.Count - 1] is MessageCallStackFrameVM;
			int oldVisibleFramesCount = framesList.Count - (oldHadTooManyFrames ? 1 : 0);

			int framesToAdd = newFrames.Count - oldVisibleFramesCount;
			const int MAX_FRAMES_DIFF = 50;
			if (Math.Abs(framesToAdd) > MAX_FRAMES_DIFF) {
				oldHadTooManyFrames = false;
				oldVisibleFramesCount = 0;
				framesToAdd = newFrames.Count;
				framesList.Clear();
			}

			if (framesToAdd > 0) {
				for (int i = 0; i < framesToAdd; i++) {
					var frame = newFrames[i];
					var vm = new CallStackFrameVM(callStackFrameContext, i, frame, process);
					vm.IsCurrentFrame = i == stackFrameService.SelectedFrameNumber;
					vm.IsUserCode = IsUserCode(frame);

					if (framesList.Count == i)
						framesList.Add(vm);
					else
						framesList.Insert(i, vm);
				}
			}
			else if (framesToAdd < 0) {
				int frames = framesToAdd;
				while (frames++ < 0)
					framesList.RemoveAt(0);
			}

			for (int i = framesToAdd >= 0 ? framesToAdd : 0; i < newFrames.Count; i++) {
				var vm = (CallStackFrameVM)framesList[i];
				var frame = newFrames[i];

				vm.Index = i;
				vm.IsCurrentFrame = i == stackFrameService.SelectedFrameNumber;
				vm.IsUserCode = IsUserCode(frame);
				vm.SetFrame(frame, process);
			}

			if (oldHadTooManyFrames == tooManyFrames) {
			}
			else if (oldHadTooManyFrames && !tooManyFrames) {
				bool b = framesList.Count > 0 && framesList[framesList.Count - 1] is MessageCallStackFrameVM;
				Debug.Assert(b);
				if (b)
					framesList.RemoveAt(framesList.Count - 1);
			}
			else if (!oldHadTooManyFrames && tooManyFrames)
				framesList.Add(new MessageCallStackFrameVM(callStackFrameContext, newFrames.Count, dnSpy_Debugger_Resources.CallStack_MaxFramesExceeded));
		}

		bool IsUserCode(CorFrame frame) {
			return true;//TODO:
		}

		void UpdateSelectedFrame(VMPropertyChangedEventArgs<int> e) {
			Debug.Assert(e != null);
			if (e == null)
				return;
			WriteIsCurrentFrame(e.OldValue, false);
			WriteIsCurrentFrame(e.NewValue, true);
		}

		void WriteIsCurrentFrame(int index, bool value) {
			if ((uint)index >= (uint)framesList.Count)
				return;

			var vm = framesList[index] as CallStackFrameVM;
			if (vm != null)
				vm.IsCurrentFrame = value;
		}

		void RefreshThemeFields() {
			foreach (var vm in framesList) {
				var vm2 = vm as CallStackFrameVM;
				if (vm2 != null)
					vm2.RefreshThemeFields();
			}
		}

		void RefreshFrameNames() {
			foreach (var vm in framesList) {
				var vm2 = vm as CallStackFrameVM;
				if (vm2 != null)
					vm2.RefreshName();
			}
		}
	}
}

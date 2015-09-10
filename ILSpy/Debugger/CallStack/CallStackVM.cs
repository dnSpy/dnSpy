/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Diagnostics;
using dndbg.Engine;
using dnSpy.MVVM;

namespace dnSpy.Debugger.CallStack {
	sealed class CallStackVM : ViewModelBase {
		internal ICallStackObjectCreator CallStackObjectCreator {
			get { return callStackObjectCreator; }
			set { callStackObjectCreator = value; }
		}
		ICallStackObjectCreator callStackObjectCreator;

		public ObservableCollection<ICallStackFrameVM> Collection {
			get { return framesList; }
		}
		readonly ObservableCollection<ICallStackFrameVM> framesList;

		internal bool IsEnabled {
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

		public object SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnPropertyChanged("SelectedItem");
				}
			}
		}
		object selectedItem;

		internal TypePrinterFlags TypePrinterFlags {
			get {
				TypePrinterFlags flags = 0;
				if (!DebuggerSettings.Instance.UseHexadecimal) flags |= TypePrinterFlags.UseDecimal;
				if (CallStackSettings.Instance.ShowModuleNames) flags |= TypePrinterFlags.ShowModuleNames;
				if (CallStackSettings.Instance.ShowParameterTypes) flags |= TypePrinterFlags.ShowParameterTypes;
				if (CallStackSettings.Instance.ShowParameterNames) flags |= TypePrinterFlags.ShowParameterNames;
				if (CallStackSettings.Instance.ShowParameterValues) flags |= TypePrinterFlags.ShowParameterValues;
				if (CallStackSettings.Instance.ShowIP) flags |= TypePrinterFlags.ShowIP;
				if (CallStackSettings.Instance.ShowOwnerTypes) flags |= TypePrinterFlags.ShowOwnerTypes;
				if (CallStackSettings.Instance.ShowNamespaces) flags |= TypePrinterFlags.ShowNamespaces;
				if (CallStackSettings.Instance.ShowTypeKeywords) flags |= TypePrinterFlags.ShowTypeKeywords;
				if (CallStackSettings.Instance.ShowTokens) flags |= TypePrinterFlags.ShowTokens;
				if (CallStackSettings.Instance.ShowReturnTypes) flags |= TypePrinterFlags.ShowReturnTypes;
				return flags;
			}
		}

		public CallStackVM() {
			StackFrameManager.Instance.StackFramesUpdated += StackFrameManager_StackFramesUpdated;
			StackFrameManager.Instance.PropertyChanged += StackFrameManager_PropertyChanged;
			this.framesList = new ObservableCollection<ICallStackFrameVM>();
			CallStackSettings.Instance.PropertyChanged += CallStackSettings_PropertyChanged;
			DebuggerSettings.Instance.PropertyChanged += DebuggerSettings_PropertyChanged;
		}

		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			switch (e.PropertyName) {
			case "UseHexadecimal":
				RefreshFrameNames();
				break;
			default:
				break;
			}
		}

		void CallStackSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			switch (e.PropertyName) {
			case "ShowModuleNames":
			case "ShowParameterTypes":
			case "ShowParameterNames":
			case "ShowParameterValues":
			case "ShowIP":
			case "ShowOwnerTypes":
			case "ShowNamespaces":
			case "ShowTypeKeywords":
			case "ShowTokens":
			case "ShowReturnTypes":
				RefreshFrameNames();
				break;
			default:
				break;
			}
		}

		void StackFrameManager_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "SelectedFrame")
				UpdateSelectedFrame(e as VMPropertyChangedEventArgs<int>);
		}

		void StackFrameManager_StackFramesUpdated(object sender, EventArgs e) {
			// InitializeStackFrames() is called by CallStackPaneCreator when the process has been
			// running for a little while. Speeds up stepping.
			if (DebugManager.Instance.ProcessState != DebuggerProcessState.Running)
				InitializeStackFrames();
		}

		internal void InitializeStackFrames() {
			if (!IsEnabled) {
				framesList.Clear();
				return;
			}

			bool tooManyFrames;
			var newFrames = StackFrameManager.Instance.GetFrames(out tooManyFrames);

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
					var vm = new CallStackFrameVM(this, i, frame);
					vm.IsCurrentFrame = i == StackFrameManager.Instance.SelectedFrame;
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
				vm.IsCurrentFrame = i == StackFrameManager.Instance.SelectedFrame;
				vm.IsUserCode = IsUserCode(frame);
				vm.Frame = frame;
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
				framesList.Add(new MessageCallStackFrameVM(this, newFrames.Count, "The maximum number of stack frames supported by dnSpy has been exceeded."));
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

		internal void RefreshThemeFields() {
			foreach (var vm in framesList) {
				var vm2 = vm as CallStackFrameVM;
				if (vm2 != null)
					vm2.RefreshThemeFields();
			}
		}

		internal void RefreshFrameNames() {
			foreach (var vm in framesList) {
				var vm2 = vm as CallStackFrameVM;
				if (vm2 != null)
					vm2.RefreshName();
			}
		}
	}
}

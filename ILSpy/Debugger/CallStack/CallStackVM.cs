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

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using dndbg.Engine;
using dnSpy.MVVM;

namespace dnSpy.Debugger.CallStack {
	sealed class CallStackVM : ViewModelBase {
		public ObservableCollection<ICallStackFrameVM> Collection {
			get { return virtList; }
		}
		readonly ObservableCollection<ICallStackFrameVM> virtList;

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

		public CallStackVM() {
			DebugManager.Instance.OnProcessStateChanged2 += DebugManager_OnProcessStateChanged2;
			StackFrameManager.Instance.PropertyChanged += StackFrameManager_PropertyChanged;
			this.virtList = new ObservableCollection<ICallStackFrameVM>();
		}

		void StackFrameManager_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "SelectedFrame")
				UpdateSelectedFrame(e as VMPropertyChangedEventArgs<int>);
		}

		void DebugManager_OnProcessStateChanged2(object sender, DebuggerEventArgs e) {
			InitializeStackFrames();
		}

		void InitializeStackFrames() {
			virtList.Clear();

			if (!IsEnabled)
				return;
			if (DebugManager.Instance.ProcessState != DebuggerProcessState.Stopped)
				return;

			bool tooManyFrames;
			int frameNo = 0;
			foreach (var frame in StackFrameManager.Instance.GetFrames(out tooManyFrames)) {
				var vm = new CallStackFrameVM(frameNo, frame);
				vm.IsCurrentFrame = frameNo == StackFrameManager.Instance.SelectedFrame;
				vm.IsUserCode = IsUserCode(frame);
				virtList.Add(vm);

				frameNo++;
			}
			if (tooManyFrames)
				virtList.Add(new MessageCallStackFrameVM(frameNo, "The maximum number of stack frames supported by dnSpy has been exceeded."));
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
			if ((uint)index >= (uint)virtList.Count)
				return;

			var vm = virtList[index] as CallStackFrameVM;
			if (vm != null)
				vm.IsCurrentFrame = value;
		}

		internal void RefreshIconFields() {
			foreach (var vm in virtList)
				vm.RefreshIconFields();
		}
	}
}

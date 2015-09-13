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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using dndbg.Engine;
using dnSpy.Debugger.CallStack;
using dnSpy.MVVM;

namespace dnSpy.Debugger.Threads {
	sealed class ThreadsVM : ViewModelBase {
		internal bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled != value) {
					// Don't call OnPropertyChanged() since it's only used internally by the View
					isEnabled = value;
					InitializeThreads();
				}
			}
		}
		bool isEnabled;

		public ObservableCollection<ThreadVM> Collection {
			get { return threadsList; }
		}
		readonly ObservableCollection<ThreadVM> threadsList;

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

		public ThreadsVM() {
			this.threadsList = new ObservableCollection<ThreadVM>();
			StackFrameManager.Instance.StackFramesUpdated += StackFrameManager_StackFramesUpdated;
			StackFrameManager.Instance.PropertyChanged += StackFrameManager_PropertyChanged;
		}

		private void StackFrameManager_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "SelectedThread")
				UpdateSelectedThread();
		}

		private void StackFrameManager_StackFramesUpdated(object sender, System.EventArgs e) {
			// InitializeStackFrames() is called by ThreadsControlCreator when the process has been
			// running for a little while. Speeds up stepping.
			if (DebugManager.Instance.ProcessState != DebuggerProcessState.Running)
				InitializeThreads();
		}

		internal void InitializeThreads() {
			if (!IsEnabled || DebugManager.Instance.ProcessState != DebuggerProcessState.Stopped) {
				Collection.Clear();
				return;
			}

			var debugger = DebugManager.Instance.Debugger;
			var threadsInColl = new HashSet<DnThread>(Collection.Select(a => a.Thread));
			var allThreads = new HashSet<DnThread>(debugger.Processes.SelectMany(p => p.Threads));

			foreach (var thread in allThreads) {
				if (threadsInColl.Contains(thread))
					continue;
				var vm = new ThreadVM(thread);
				Collection.Add(vm);
			}

			for (int i = Collection.Count - 1; i >= 0; i--) {
				if (!allThreads.Contains(Collection[i].Thread))
					Collection.RemoveAt(i);
			}

			foreach (var vm in Collection) {
				vm.IsCurrent = StackFrameManager.Instance.SelectedThread == vm.Thread;
				vm.UpdateFields();
			}
		}

		void UpdateSelectedThread() {
			foreach (var vm in Collection)
				vm.IsCurrent = StackFrameManager.Instance.SelectedThread == vm.Thread;
		}

		internal void RefreshThemeFields() {
			foreach (var vm in Collection)
				vm.RefreshThemeFields();
		}

		internal void RefreshHexFields() {
			foreach (var vm in Collection)
				vm.RefreshHexFields();
		}
	}
}

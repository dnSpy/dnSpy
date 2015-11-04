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
					var dbg = DebugManager.Instance.Debugger;
					if (dbg != null) {
						if (isEnabled)
							InstallDebuggerHooks(dbg);
						else
							UninstallDebuggerHooks(dbg);
					}
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
			DebugManager.Instance.OnProcessStateChanged += DebugManager_OnProcessStateChanged;
			DebuggerSettings.Instance.PropertyChanged += DebuggerSettings_PropertyChanged;
			DebugManager.Instance.ProcessRunning += DebugManager_ProcessRunning;
		}

		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "SyntaxHighlightThreads")
				RefreshThemeFields();
			else if (e.PropertyName == "UseHexadecimal")
				RefreshHexFields();
			else if (e.PropertyName == "PropertyEvalAndFunctionCalls")
				RefreshEvalFields();
		}

		void DebugManager_ProcessRunning(object sender, EventArgs e) {
			InitializeThreads();
		}

		void DebugManager_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			var dbg = (DnDebugger)sender;
			switch (dbg.ProcessState) {
			case DebuggerProcessState.Starting:
				InstallDebuggerHooks(dbg);
				break;

			case DebuggerProcessState.Continuing:
			case DebuggerProcessState.Running:
			case DebuggerProcessState.Stopped:
				break;

			case DebuggerProcessState.Terminated:
				UninstallDebuggerHooks(dbg);
				break;
			}
		}

		void InstallDebuggerHooks(DnDebugger dbg) {
			dbg.OnNameChanged += DnDebugger_OnNameChanged;
		}

		void UninstallDebuggerHooks(DnDebugger dbg) {
			dbg.OnNameChanged -= DnDebugger_OnNameChanged;
		}

		void DnDebugger_OnNameChanged(object sender, NameChangedDebuggerEventArgs e) {
			if (e.Thread != null) {
				foreach (var vm in Collection)
					vm.NameChanged(e.Thread);
			}
		}

		void StackFrameManager_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "SelectedThread")
				UpdateSelectedThread();
		}

		void StackFrameManager_StackFramesUpdated(object sender, StackFramesUpdatedEventArgs e) {
			if (e.Debugger.IsEvaluating)
				return;
			// InitializeThreads() is called when the process has been running for a little while. Speeds up stepping.
			if (DebugManager.Instance.ProcessState != DebuggerProcessState.Continuing && DebugManager.Instance.ProcessState != DebuggerProcessState.Running)
				InitializeThreads();
		}

		void InitializeThreads() {
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

		void RefreshEvalFields() {
			foreach (var vm in Collection)
				vm.RefreshEvalFields();
		}
	}
}

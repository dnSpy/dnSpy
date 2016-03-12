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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using dndbg.Engine;
using dnSpy.Contracts.Images;
using dnSpy.Debugger.CallStack;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.Threads {
	interface IThreadsVM {
		bool IsEnabled { get; set; }
		bool IsVisible { get; set; }
		void RefreshThemeFields();
	}

	[Export, Export(typeof(IThreadsVM)), Export(typeof(ILoadBeforeDebug)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class ThreadsVM : ViewModelBase, IThreadsVM, ILoadBeforeDebug {
		public bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled != value) {
					// Don't call OnPropertyChanged() since it's only used internally by the View
					isEnabled = value;
					InitializeThreads();
					var dbg = theDebugger.Debugger;
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

		public bool IsVisible {//TODO: Use this
			get { return isVisible; }
			set { isVisible = value; }
		}
		bool isVisible;

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

		readonly ITheDebugger theDebugger;
		readonly IStackFrameManager stackFrameManager;
		readonly ThreadContext threadContext;

		[ImportingConstructor]
		ThreadsVM(ITheDebugger theDebugger, IStackFrameManager stackFrameManager, IDebuggerSettings debuggerSettings, IImageManager imageManager) {
			this.theDebugger = theDebugger;
			this.stackFrameManager = stackFrameManager;
			this.threadContext = new ThreadContext(imageManager, theDebugger, debuggerSettings) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlightThreads,
				UseHexadecimal = debuggerSettings.UseHexadecimal,
			};
			this.threadsList = new ObservableCollection<ThreadVM>();
			stackFrameManager.StackFramesUpdated += StackFrameManager_StackFramesUpdated;
			stackFrameManager.PropertyChanged += StackFrameManager_PropertyChanged;
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
			debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
			theDebugger.ProcessRunning += TheDebugger_ProcessRunning;
		}

		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var debuggerSettings = (IDebuggerSettings)sender;
			if (e.PropertyName == "SyntaxHighlightThreads") {
				threadContext.SyntaxHighlight = debuggerSettings.SyntaxHighlightThreads;
				RefreshThemeFields();
			}
			else if (e.PropertyName == "UseHexadecimal") {
				threadContext.UseHexadecimal = debuggerSettings.UseHexadecimal;
				RefreshHexFields();
			}
			else if (e.PropertyName == "PropertyEvalAndFunctionCalls")
				RefreshEvalFields();
		}

		void TheDebugger_ProcessRunning(object sender, EventArgs e) {
			InitializeThreads();
		}

		void TheDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			var dbg = (DnDebugger)sender;
			switch (theDebugger.ProcessState) {
			case DebuggerProcessState.Starting:
				InstallDebuggerHooks(dbg);
				break;

			case DebuggerProcessState.Continuing:
			case DebuggerProcessState.Running:
			case DebuggerProcessState.Paused:
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
			if (e.Debugger.ProcessState != DebuggerProcessState.Continuing && e.Debugger.ProcessState != DebuggerProcessState.Running)
				InitializeThreads();
		}

		void InitializeThreads() {
			if (!IsEnabled || theDebugger.ProcessState != DebuggerProcessState.Paused) {
				Collection.Clear();
				return;
			}

			var debugger = theDebugger.Debugger;
			var threadsInColl = new HashSet<DnThread>(Collection.Select(a => a.Thread));
			var allThreads = new HashSet<DnThread>(debugger.Processes.SelectMany(p => p.Threads));

			foreach (var thread in allThreads) {
				if (threadsInColl.Contains(thread))
					continue;
				var vm = new ThreadVM(thread, threadContext);
				Collection.Add(vm);
			}

			for (int i = Collection.Count - 1; i >= 0; i--) {
				if (!allThreads.Contains(Collection[i].Thread))
					Collection.RemoveAt(i);
			}

			foreach (var vm in Collection) {
				vm.IsCurrent = stackFrameManager.SelectedThread == vm.Thread;
				vm.UpdateFields();
			}
		}

		void UpdateSelectedThread() {
			foreach (var vm in Collection)
				vm.IsCurrent = stackFrameManager.SelectedThread == vm.Thread;
		}

		public void RefreshThemeFields() {
			foreach (var vm in Collection)
				vm.RefreshThemeFields();
		}

		void RefreshHexFields() {
			foreach (var vm in Collection)
				vm.RefreshHexFields();
		}

		void RefreshEvalFields() {
			foreach (var vm in Collection)
				vm.RefreshEvalFields();
		}
	}
}

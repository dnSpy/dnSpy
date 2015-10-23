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
using System.Diagnostics;
using dndbg.Engine;
using dnSpy.Debugger.IMModules;
using dnSpy.MVVM;

namespace dnSpy.Debugger.Breakpoints {
	sealed class BreakpointsVM : ViewModelBase {
		public ObservableCollection<BreakpointVM> Collection {
			get { return breakpointList; }
		}
		readonly ObservableCollection<BreakpointVM> breakpointList;

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

		public BreakpointsVM() {
			this.breakpointList = new ObservableCollection<BreakpointVM>();
			BreakpointSettings.Instance.PropertyChanged += BreakpointSettings_PropertyChanged;
			BreakpointManager.Instance.OnListModified += BreakpointManager_OnListModified;
			DebuggerSettings.Instance.PropertyChanged += DebuggerSettings_PropertyChanged;
			DebugManager.Instance.OnProcessStateChanged += DebugManager_OnProcessStateChanged;
			InMemoryModuleManager.Instance.DynamicModulesLoaded += InMemoryModuleManager_DynamicModulesLoaded;
			foreach (var bp in BreakpointManager.Instance.Breakpoints)
				AddBreakpoint(bp);
		}

		void DebugManager_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			var dbg = (DnDebugger)sender;
			switch (dbg.ProcessState) {
			case DebuggerProcessState.Starting:
				dbg.DebugCallbackEvent += DnDebugger_DebugCallbackEvent;
				break;

			case DebuggerProcessState.Continuing:
			case DebuggerProcessState.Running:
			case DebuggerProcessState.Stopped:
				break;

			case DebuggerProcessState.Terminated:
				dbg.DebugCallbackEvent -= DnDebugger_DebugCallbackEvent;
				break;
			}
		}

		void DnDebugger_DebugCallbackEvent(DnDebugger dbg, DebugCallbackEventArgs e) {
			if (nameErrorCounter != 0 && e.Type == DebugCallbackType.LoadClass) {
				var lcArgs = (LoadClassDebugCallbackEventArgs)e;
				var module = dbg.TryGetModule(lcArgs.CorAppDomain, lcArgs.CorClass);
				Debug.Assert(module != null);
				if (module != null && module.IsDynamic)
					pendingModules.Add(module.SerializedDnModuleWithAssembly);
			}
		}

		void InMemoryModuleManager_DynamicModulesLoaded(object sender, System.EventArgs e) {
			if (nameErrorCounter != 0) {
				foreach (var serMod in pendingModules) {
					foreach (var vm in breakpointList)
						vm.RefreshIfNameError(serMod);
				}
			}
			pendingModules.Clear();
		}

		internal void OnNameErrorChanged(BreakpointVM vm) {
			// Also called by vm.Dispose() when it's already been removed so don't add an Assert() here
			if (vm.NameError)
				nameErrorCounter++;
			else
				nameErrorCounter--;
			Debug.Assert(0 <= nameErrorCounter && nameErrorCounter <= breakpointList.Count);
		}
		int nameErrorCounter;
		readonly HashSet<SerializedDnModuleWithAssembly> pendingModules = new HashSet<SerializedDnModuleWithAssembly>();

		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "SyntaxHighlightBreakpoints")
				RefreshThemeFields();
		}

		void BreakpointSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "ShowTokens")
				RefreshNameField();
		}

		public void Remove(IEnumerable<BreakpointVM> bps) {
			foreach (var bp in bps)
				BreakpointManager.Instance.Remove(bp.Breakpoint);
		}

		void BreakpointManager_OnListModified(object sender, BreakpointListModifiedEventArgs e) {
			if (e.Added)
				AddBreakpoint(e.Breakpoint);
			else
				RemoveBreakpoint(e.Breakpoint);
		}

		void AddBreakpoint(Breakpoint bp) {
			Collection.Add(new BreakpointVM(this, bp));
		}

		void RemoveBreakpoint(Breakpoint bp) {
			for (int i = 0; i < Collection.Count; i++) {
				var vm = Collection[i];
				if (Collection[i].Breakpoint == bp) {
					Collection.RemoveAt(i);
					vm.Dispose();
					return;
				}
			}
			Debug.Fail("Breakpoint got removed but it wasn't in BreakpointsVM's list");
		}

		internal void RefreshThemeFields() {
			foreach (var vm in breakpointList)
				vm.RefreshThemeFields();
		}

		internal void RefreshLanguageFields() {
			RefreshNameField();
		}

		void RefreshNameField() {
			foreach (var vm in breakpointList)
				vm.RefreshNameField();
		}
	}
}

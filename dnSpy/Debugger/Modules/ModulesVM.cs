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
using System.Diagnostics;
using System.Linq;
using dndbg.Engine;
using dnSpy.MVVM;

namespace dnSpy.Debugger.Modules {
	sealed class ModulesVM : ViewModelBase {
		public ObservableCollection<ModuleVM> Collection {
			get { return modulesList; }
		}
		readonly ObservableCollection<ModuleVM> modulesList;

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

		public ModulesVM() {
			this.modulesList = new ObservableCollection<ModuleVM>();
			DebugManager.Instance.OnProcessStateChanged += DebugManager_OnProcessStateChanged;
			DebuggerSettings.Instance.PropertyChanged += DebuggerSettings_PropertyChanged;
			if (DebugManager.Instance.ProcessState != DebuggerProcessState.Terminated)
				InstallDebuggerHooks(DebugManager.Instance.Debugger);
		}

		void DebuggerSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
			if (e.PropertyName == "UseHexadecimal")
				RefreshHexFields();
			else if (e.PropertyName == "SyntaxHighlightModules")
				RefreshThemeFields();
		}

		void InstallDebuggerHooks(DnDebugger dbg) {
			dbg.OnModuleAdded += DnDebugger_OnModuleAdded;
			dbg.OnNameChanged += DnDebugger_OnNameChanged;
			var modules = GetAllModules(dbg).ToArray();
			Array.Sort(modules, (a, b) => a.ModuleOrder.CompareTo(b.ModuleOrder));
			foreach (var module in modules)
				Add(module);
		}

		static IEnumerable<DnModule> GetAllModules(DnDebugger dbg) {
			foreach (var process in dbg.Processes) {
				foreach (var appDomain in process.AppDomains) {
					foreach (var assembly in appDomain.Assemblies) {
						foreach (var module in assembly.Modules)
							yield return module;
					}
				}
			}
		}

		private void DnDebugger_OnNameChanged(object sender, NameChangedDebuggerEventArgs e) {
			if (e.AppDomain != null)
				RefreshAppDomainNames(e.AppDomain);
		}

		void UninstallDebuggerHooks(DnDebugger dbg) {
			dbg.OnModuleAdded -= DnDebugger_OnModuleAdded;
			dbg.OnNameChanged -= DnDebugger_OnNameChanged;
		}

		void DnDebugger_OnModuleAdded(object sender, ModuleDebuggerEventArgs e) {
			if (e.Added)
				Add(e.Module);
			else
				Remove(e.Module);
		}

		void DebugManager_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			var dbg = (DnDebugger)sender;
			switch (DebugManager.Instance.ProcessState) {
			case DebuggerProcessState.Starting:
				Collection.Clear();
				InstallDebuggerHooks(dbg);
				break;

			case DebuggerProcessState.Continuing:
			case DebuggerProcessState.Running:
			case DebuggerProcessState.Stopped:
				break;

			case DebuggerProcessState.Terminated:
				UninstallDebuggerHooks(dbg);
				Collection.Clear();
				break;
			}
		}

		internal void RefreshThemeFields() {
			foreach (var vm in Collection)
				vm.RefreshThemeFields();
		}

		void RefreshHexFields() {
			foreach (var vm in Collection)
				vm.RefreshHexFields();
		}

		void RefreshAppDomainNames(DnAppDomain appDomain) {
			foreach (var vm in Collection)
				vm.RefreshAppDomainNames(appDomain);
		}

		bool VerifyDebugger(DnModule module) {
			if (module == null)
				return false;
			var dbg = DebugManager.Instance.Debugger;
			return module.Debugger == dbg;
		}

		void Add(DnModule module) {
			bool b = VerifyDebugger(module);
			Debug.Assert(b);
			if (!b)
				return;

			Collection.Add(new ModuleVM(module));
		}

		void Remove(DnModule module) {
			bool b = VerifyDebugger(module);
			Debug.Assert(b);
			if (!b)
				return;

			for (int i = Collection.Count - 1; i >= 0; i--) {
				var vm = Collection[i];
				if (vm.Module == module) {
					Collection.RemoveAt(i);
					return;
				}
			}

			Debug.Fail(string.Format("Module wasn't added to list: {0}", module));
		}
	}
}

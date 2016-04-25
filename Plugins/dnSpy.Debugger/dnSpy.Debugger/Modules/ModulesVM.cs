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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dndbg.Engine;
using dnSpy.Contracts.Images;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.Modules {
	interface IModulesVM {
		bool IsEnabled { get; set; }
		bool IsVisible { get; set; }

		void RefreshThemeFields();
	}

	[Export, Export(typeof(IModulesVM)), Export(typeof(ILoadBeforeDebug)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class ModulesVM : ViewModelBase, IModulesVM, ILoadBeforeDebug {
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

		public bool IsEnabled {//TODO: Use
			get { return isEnabled; }
			set { isEnabled = value; }
		}
		bool isEnabled;

		public bool IsVisible {//TODO: Use
			get { return isVisible; }
			set { isVisible = value; }
		}
		bool isVisible;

		readonly ITheDebugger theDebugger;
		readonly ModuleContext moduleContext;

		[ImportingConstructor]
		ModulesVM(ITheDebugger theDebugger, IDebuggerSettings debuggerSettings, IImageManager imageManager) {
			this.theDebugger = theDebugger;
			this.moduleContext = new ModuleContext(imageManager, theDebugger) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlightModules,
				UseHexadecimal = debuggerSettings.UseHexadecimal,
			};
			this.modulesList = new ObservableCollection<ModuleVM>();
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
			debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
			if (theDebugger.ProcessState != DebuggerProcessState.Terminated)
				InstallDebuggerHooks(theDebugger.Debugger);
		}

		void DebuggerSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
			var debuggerSettings = (IDebuggerSettings)sender;
			if (e.PropertyName == "UseHexadecimal") {
				moduleContext.UseHexadecimal = debuggerSettings.UseHexadecimal;
				RefreshHexFields();
			}
			else if (e.PropertyName == "SyntaxHighlightModules") {
				moduleContext.SyntaxHighlight = debuggerSettings.SyntaxHighlightModules;
				RefreshThemeFields();
			}
		}

		void InstallDebuggerHooks(DnDebugger dbg) {
			dbg.OnModuleAdded += DnDebugger_OnModuleAdded;
			dbg.OnNameChanged += DnDebugger_OnNameChanged;
			var modules = GetAllModules(dbg).ToArray();
			Array.Sort(modules, (a, b) => a.UniqueId.CompareTo(b.UniqueId));
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

		void DnDebugger_OnNameChanged(object sender, NameChangedDebuggerEventArgs e) {
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

		void TheDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			var dbg = (DnDebugger)sender;
			switch (theDebugger.ProcessState) {
			case DebuggerProcessState.Starting:
				Collection.Clear();
				InstallDebuggerHooks(dbg);
				break;

			case DebuggerProcessState.Continuing:
			case DebuggerProcessState.Running:
			case DebuggerProcessState.Paused:
				break;

			case DebuggerProcessState.Terminated:
				UninstallDebuggerHooks(dbg);
				Collection.Clear();
				break;
			}
		}

		public void RefreshThemeFields() {
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
			var dbg = theDebugger.Debugger;
			return module.Debugger == dbg;
		}

		void Add(DnModule module) {
			bool b = VerifyDebugger(module);
			Debug.Assert(b);
			if (!b)
				return;

			Collection.Add(new ModuleVM(module, moduleContext));
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

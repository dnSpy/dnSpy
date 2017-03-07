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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.Modules {
	interface IModulesVM {
		bool IsEnabled { get; set; }
		bool IsVisible { get; set; }
		ObservableCollection<ModuleVM> AllItems { get; }
		ObservableCollection<ModuleVM> SelectedItems { get; }
	}

	[Export(typeof(IModulesVM))]
	sealed class ModulesVM : ViewModelBase, IModulesVM {
		public ObservableCollection<ModuleVM> AllItems { get; }
		public ObservableCollection<ModuleVM> SelectedItems { get; }

		public bool IsEnabled {
			get => isEnabled;
			set {
				if (isEnabled == value)
					return;
				isEnabled = value;
				InitializeDebugger_UI(isEnabled);
			}
		}
		bool isEnabled;

		public bool IsVisible { get; set; }

		readonly Lazy<DbgManager> dbgManager;
		readonly ModuleContext moduleContext;
		readonly ModuleFormatterProvider moduleFormatterProvider;
		readonly DebuggerSettings debuggerSettings;
		int moduleOrder;

		[ImportingConstructor]
		ModulesVM(Lazy<DbgManager> dbgManager, DebuggerSettings debuggerSettings, DebuggerDispatcher debuggerDispatcher, ModuleFormatterProvider moduleFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider) {
			debuggerDispatcher.Dispatcher.VerifyAccess();
			AllItems = new ObservableCollection<ModuleVM>();
			SelectedItems = new ObservableCollection<ModuleVM>();
			this.dbgManager = dbgManager;
			this.moduleFormatterProvider = moduleFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			moduleContext = new ModuleContext(debuggerDispatcher.Dispatcher, classificationFormatMap, textElementProvider) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlight,
				Formatter = moduleFormatterProvider.Create(),
			};
		}

		// random thread
		void DbgThread(Action action) =>
			dbgManager.Value.DispatcherThread.BeginInvoke(action);

		// UI thread
		void InitializeDebugger_UI(bool enable) {
			moduleContext.Dispatcher.VerifyAccess();
			if (enable) {
				moduleContext.ClassificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
				RecreateFormatter_UI();
				moduleContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
			}
			else {
				moduleContext.ClassificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged -= DebuggerSettings_PropertyChanged;
			}
			DbgThread(() => InitializeDebugger_DbgThread(enable));
		}

		// DbgManager thread
		void InitializeDebugger_DbgThread(bool enable) {
			dbgManager.Value.DispatcherThread.VerifyAccess();
			if (enable) {
				dbgManager.Value.ProcessesChanged += DbgManager_ProcessesChanged;
				var modules = new List<DbgModule>();
				foreach (var p in dbgManager.Value.Processes) {
					InitializeProcess_DbgThread(p);
					foreach (var r in p.Runtimes) {
						InitializeRuntime_DbgThread(r);
						modules.AddRange(r.Modules);
						foreach (var a in r.AppDomains)
							InitializeAppDomain_DbgThread(a);
					}
				}
				if (modules.Count > 0)
					UI(() => AddItems_UI(modules));
			}
			else {
				dbgManager.Value.ProcessesChanged -= DbgManager_ProcessesChanged;
				foreach (var p in dbgManager.Value.Processes) {
					DeinitializeProcess_DbgThread(p);
					foreach (var r in p.Runtimes) {
						DeinitializeRuntime_DbgThread(r);
						foreach (var a in r.AppDomains)
							DeinitializeAppDomain_DbgThread(a);
					}
				}
				UI(() => RemoveAllModules_UI());
			}
		}

		// DbgManager thread
		void InitializeProcess_DbgThread(DbgProcess process) {
			process.DbgManager.DispatcherThread.VerifyAccess();
			process.RuntimesChanged += DbgProcess_RuntimesChanged;
		}

		// DbgManager thread
		void DeinitializeProcess_DbgThread(DbgProcess process) {
			process.DbgManager.DispatcherThread.VerifyAccess();
			process.RuntimesChanged -= DbgProcess_RuntimesChanged;
		}

		// DbgManager thread
		void InitializeRuntime_DbgThread(DbgRuntime runtime) {
			runtime.Process.DbgManager.DispatcherThread.VerifyAccess();
			runtime.AppDomainsChanged += DbgRuntime_AppDomainsChanged;
			runtime.ModulesChanged += DbgRuntime_ModulesChanged;
		}

		// DbgManager thread
		void DeinitializeRuntime_DbgThread(DbgRuntime runtime) {
			runtime.Process.DbgManager.DispatcherThread.VerifyAccess();
			runtime.AppDomainsChanged -= DbgRuntime_AppDomainsChanged;
			runtime.ModulesChanged -= DbgRuntime_ModulesChanged;
		}

		// DbgManager thread
		void InitializeAppDomain_DbgThread(DbgAppDomain appDomain) {
			appDomain.Process.DbgManager.DispatcherThread.VerifyAccess();
			appDomain.PropertyChanged += DbgAppDomain_PropertyChanged;
		}

		// DbgManager thread
		void DeinitializeAppDomain_DbgThread(DbgAppDomain appDomain) {
			appDomain.Process.DbgManager.DispatcherThread.VerifyAccess();
			appDomain.PropertyChanged -= DbgAppDomain_PropertyChanged;
		}

		// UI thread
		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			moduleContext.Dispatcher.VerifyAccess();
			RefreshThemeFields_UI();
		}

		// random thread
		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => DebuggerSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DebuggerSettings_PropertyChanged_UI(string propertyName) {
			moduleContext.Dispatcher.VerifyAccess();
			if (propertyName == nameof(DebuggerSettings.UseHexadecimal))
				RefreshHexFields_UI();
			else if (propertyName == nameof(DebuggerSettings.SyntaxHighlight)) {
				moduleContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				RefreshThemeFields_UI();
			}
		}

		// UI thread
		void RefreshThemeFields_UI() {
			moduleContext.Dispatcher.VerifyAccess();
			foreach (var vm in AllItems)
				vm.RefreshThemeFields_UI();
		}

		// UI thread
		void RecreateFormatter_UI() {
			moduleContext.Dispatcher.VerifyAccess();
			moduleContext.Formatter = moduleFormatterProvider.Create();
		}

		// UI thread
		void RefreshHexFields_UI() {
			moduleContext.Dispatcher.VerifyAccess();
			RecreateFormatter_UI();
			foreach (var vm in AllItems)
				vm.RefreshHexFields_UI();
		}

		// random thread
		void UI(Action action) =>
			// Use Send so the window is updated as fast as possible when adding new items
			moduleContext.Dispatcher.BeginInvoke(DispatcherPriority.Send, action);

		// DbgManager thread
		void DbgManager_ProcessesChanged(object sender, DbgCollectionChangedEventArgs<DbgProcess> e) {
			if (e.Added) {
				foreach (var p in e.Objects)
					InitializeProcess_DbgThread(p);
			}
			else {
				foreach (var p in e.Objects)
					DeinitializeProcess_DbgThread(p);
				UI(() => {
					var coll = AllItems;
					for (int i = coll.Count - 1; i >= 0; i--) {
						var moduleProcess = coll[i].Module.Process;
						foreach (var p in e.Objects) {
							if (p == moduleProcess) {
								RemoveModuleAt_UI(i);
								break;
							}
						}
					}
				});
			}
		}

		// DbgManager thread
		void DbgProcess_RuntimesChanged(object sender, DbgCollectionChangedEventArgs<DbgRuntime> e) {
			if (e.Added) {
				foreach (var r in e.Objects)
					InitializeRuntime_DbgThread(r);
			}
			else {
				foreach (var r in e.Objects)
					DeinitializeRuntime_DbgThread(r);
				UI(() => {
					var coll = AllItems;
					for (int i = coll.Count - 1; i >= 0; i--) {
						var moduleRuntime = coll[i].Module.Runtime;
						foreach (var r in e.Objects) {
							if (r == moduleRuntime) {
								RemoveModuleAt_UI(i);
								break;
							}
						}
					}
				});
			}
		}

		// DbgManager thread
		void DbgRuntime_AppDomainsChanged(object sender, DbgCollectionChangedEventArgs<DbgAppDomain> e) {
			if (e.Added) {
				foreach (var a in e.Objects)
					InitializeAppDomain_DbgThread(a);
			}
			else {
				foreach (var a in e.Objects)
					DeinitializeAppDomain_DbgThread(a);
				UI(() => {
					var coll = AllItems;
					for (int i = coll.Count - 1; i >= 0; i--) {
						var moduleAppDomain = coll[i].Module.AppDomain;
						if (moduleAppDomain == null)
							continue;
						foreach (var a in e.Objects) {
							if (a == moduleAppDomain) {
								RemoveModuleAt_UI(i);
								break;
							}
						}
					}
				});
			}
		}

		// DbgManager thread
		void DbgRuntime_ModulesChanged(object sender, DbgCollectionChangedEventArgs<DbgModule> e) {
			if (e.Added)
				UI(() => AddItems_UI(e.Objects));
			else {
				UI(() => {
					foreach (var m in e.Objects)
						RemoveModules_UI(m);
				});
			}
		}

		// DbgManager thread
		void DbgAppDomain_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(DbgAppDomain.Name) || e.PropertyName == nameof(DbgAppDomain.Id)) {
				UI(() => {
					var appDomain = (DbgAppDomain)sender;
					foreach (var vm in AllItems)
						vm.RefreshAppDomainNames_UI(appDomain);
				});
			}
		}

		// UI thread
		void AddItems_UI(IList<DbgModule> modules) {
			moduleContext.Dispatcher.VerifyAccess();
			foreach (var m in modules)
				AllItems.Add(new ModuleVM(m, moduleContext, moduleOrder++));
		}

		// UI thread
		void RemoveModuleAt_UI(int i) {
			moduleContext.Dispatcher.VerifyAccess();
			Debug.Assert(0 <= i && i < AllItems.Count);
			var vm = AllItems[i];
			vm.Dispose();
			AllItems.RemoveAt(i);
		}

		// UI thread
		void RemoveModules_UI(DbgModule module) {
			moduleContext.Dispatcher.VerifyAccess();
			var coll = AllItems;
			for (int i = 0; i < coll.Count; i++) {
				if (coll[i].Module == module) {
					RemoveModuleAt_UI(i);
					break;
				}
			}
		}

		// UI thread
		void RemoveAllModules_UI() {
			moduleContext.Dispatcher.VerifyAccess();
			var coll = AllItems;
			for (int i = coll.Count - 1; i >= 0; i--)
				RemoveModuleAt_UI(i);
		}
	}
}

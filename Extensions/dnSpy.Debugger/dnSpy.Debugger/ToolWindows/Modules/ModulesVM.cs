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
	}

	[Export(typeof(IModulesVM))]
	[ExportDbgManagerStartListener]
	sealed class ModulesVM : ViewModelBase, IModulesVM, IDbgManagerStartListener {
		public ObservableCollection<ModuleVM> Collection => modulesList;
		readonly ObservableCollection<ModuleVM> modulesList;

		public object SelectedItem {
			get => selectedItem;
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnPropertyChanged(nameof(SelectedItem));
				}
			}
		}
		object selectedItem;

		readonly DbgManager dbgManager;
		readonly ModuleContext moduleContext;
		readonly ModuleFormatterProvider moduleFormatterProvider;
		readonly DebuggerSettings debuggerSettings;

		[ImportingConstructor]
		ModulesVM(DbgManager dbgManager, DebuggerSettings debuggerSettings, DebuggerDispatcher debuggerDispatcher, ModuleFormatterProvider moduleFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider) {
			this.dbgManager = dbgManager;
			modulesList = new ObservableCollection<ModuleVM>();
			this.moduleFormatterProvider = moduleFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			// We could be in a random thread if IDbgManagerStartListener.OnStart() gets called after the ctor returns
			moduleContext = debuggerDispatcher.Dispatcher.Invoke(() => {
				var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
				var modCtx = new ModuleContext(debuggerDispatcher.Dispatcher, classificationFormatMap, textElementProvider) {
					SyntaxHighlight = debuggerSettings.SyntaxHighlight,
					ModuleFormatter = moduleFormatterProvider.Create(),
				};
				classificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
				return modCtx;
			}, DispatcherPriority.Send);
		}

		// UI thread
		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) => RefreshThemeFields_UI();

		// random thread
		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => DebuggerSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DebuggerSettings_PropertyChanged_UI(string propertyName) {
			if (propertyName == nameof(DebuggerSettings.UseHexadecimal))
				RefreshHexSettings_UI();
			else if (propertyName == nameof(DebuggerSettings.SyntaxHighlight)) {
				moduleContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				RefreshThemeFields_UI();
			}
		}

		// UI thread
		void RefreshThemeFields_UI() {
			foreach (var vm in Collection)
				vm.RefreshThemeFields();
		}

		// UI thread
		void RefreshHexSettings_UI() {
			moduleContext.ModuleFormatter = moduleFormatterProvider.Create();
			foreach (var vm in Collection)
				vm.RefreshHexFields();
		}

		// random thread
		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) => dbgManager.ProcessesChanged += DbgManager_ProcessesChanged;

		// random thread
		void UI(Action action) =>
			moduleContext.Dispatcher.BeginInvoke(DispatcherPriority.Background, action);

		// DbgManager thread
		void DbgManager_ProcessesChanged(object sender, DbgCollectionChangedEventArgs<DbgProcess> e) {
			if (e.Added) {
				foreach (var p in e.Objects)
					p.RuntimesChanged += DbgProcess_RuntimesChanged;
			}
			else {
				foreach (var p in e.Objects)
					p.RuntimesChanged -= DbgProcess_RuntimesChanged;
				UI(() => {
					var coll = Collection;
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

		// UI thread
		void RemoveModuleAt_UI(int i) {
			Debug.Assert(0 <= i && i < Collection.Count);
			var vm = Collection[i];
			vm.Dispose();
			Collection.RemoveAt(i);
		}

		// UI thread
		void RemoveModule_UI(DbgModule m) {
			var coll = Collection;
			for (int i = 0; i < coll.Count; i++) {
				if (coll[i].Module == m) {
					RemoveModuleAt_UI(i);
					break;
				}
			}
		}

		// DbgManager thread
		void DbgProcess_RuntimesChanged(object sender, DbgCollectionChangedEventArgs<DbgRuntime> e) {
			if (e.Added) {
				foreach (var r in e.Objects) {
					r.AppDomainsChanged += DbgRuntime_AppDomainsChanged;
					r.ModulesChanged += DbgRuntime_ModulesChanged;
				}
			}
			else {
				foreach (var r in e.Objects) {
					r.AppDomainsChanged -= DbgRuntime_AppDomainsChanged;
					r.ModulesChanged -= DbgRuntime_ModulesChanged;
				}
				UI(() => {
					var coll = Collection;
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
					a.PropertyChanged += DbgAppDomain_PropertyChanged;
			}
			else {
				foreach (var a in e.Objects)
					a.PropertyChanged -= DbgAppDomain_PropertyChanged;
				UI(() => {
					var coll = Collection;
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
			if (e.Added) {
				UI(() => {
					foreach (var m in e.Objects)
						Collection.Add(new ModuleVM(m, moduleContext));
				});
			}
			else {
				UI(() => {
					foreach (var m in e.Objects)
						RemoveModule_UI(m);
				});
			}
		}

		// DbgManager thread
		void DbgAppDomain_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(DbgAppDomain.Name) || e.PropertyName == nameof(DbgAppDomain.Id)) {
				UI(() => {
					var appDomain = (DbgAppDomain)sender;
					foreach (var vm in Collection)
						vm.RefreshAppDomainNames(appDomain);
				});
			}
		}
	}
}

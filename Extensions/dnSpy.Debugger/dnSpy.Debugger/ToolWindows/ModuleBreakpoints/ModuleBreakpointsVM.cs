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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Modules;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.ToolWindows.Controls;
using dnSpy.Debugger.UI;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.ModuleBreakpoints {
	interface IModuleBreakpointsVM {
		bool IsEnabled { get; set; }
		bool IsVisible { get; set; }
		ObservableCollection<ModuleBreakpointVM> AllItems { get; }
		ObservableCollection<ModuleBreakpointVM> SelectedItems { get; }
	}

	[Export(typeof(IModuleBreakpointsVM))]
	sealed class ModuleBreakpointsVM : ViewModelBase, IModuleBreakpointsVM, ILazyToolWindowVM {
		public ObservableCollection<ModuleBreakpointVM> AllItems { get; }
		public ObservableCollection<ModuleBreakpointVM> SelectedItems { get; }

		public bool IsEnabled {
			get => lazyToolWindowVMHelper.IsEnabled;
			set => lazyToolWindowVMHelper.IsEnabled = value;
		}

		public bool IsVisible {
			get => lazyToolWindowVMHelper.IsVisible;
			set => lazyToolWindowVMHelper.IsVisible = value;
		}

		IEditValueProvider ModuleNameEditValueProvider {
			get {
				moduleBreakpointContext.UIDispatcher.VerifyAccess();
				if (moduleNameEditValueProvider == null)
					moduleNameEditValueProvider = editValueProviderService.Create(ContentTypes.ModuleBreakpointsWindowModuleName, Array.Empty<string>());
				return moduleNameEditValueProvider;
			}
		}
		IEditValueProvider moduleNameEditValueProvider;

		IEditValueProvider OrderEditValueProvider {
			get {
				moduleBreakpointContext.UIDispatcher.VerifyAccess();
				if (orderEditValueProvider == null)
					orderEditValueProvider = editValueProviderService.Create(ContentTypes.ModuleBreakpointsWindowOrder, Array.Empty<string>());
				return orderEditValueProvider;
			}
		}
		IEditValueProvider orderEditValueProvider;

		IEditValueProvider ProcessNameEditValueProvider {
			get {
				moduleBreakpointContext.UIDispatcher.VerifyAccess();
				if (processNameEditValueProvider == null)
					processNameEditValueProvider = editValueProviderService.Create(ContentTypes.ModuleBreakpointsWindowProcessName, Array.Empty<string>());
				return processNameEditValueProvider;
			}
		}
		IEditValueProvider processNameEditValueProvider;

		IEditValueProvider AppDomainNameEditValueProvider {
			get {
				moduleBreakpointContext.UIDispatcher.VerifyAccess();
				if (appDomainNameEditValueProvider == null)
					appDomainNameEditValueProvider = editValueProviderService.Create(ContentTypes.ModuleBreakpointsWindowAppDomainName, Array.Empty<string>());
				return appDomainNameEditValueProvider;
			}
		}
		IEditValueProvider appDomainNameEditValueProvider;

		readonly Lazy<DbgManager> dbgManager;
		readonly ModuleBreakpointContext moduleBreakpointContext;
		readonly ModuleBreakpointFormatterProvider moduleBreakpointFormatterProvider;
		readonly DebuggerSettings debuggerSettings;
		readonly EditValueProviderService editValueProviderService;
		readonly LazyToolWindowVMHelper lazyToolWindowVMHelper;
		readonly Lazy<DbgModuleBreakpointsService> dbgModuleBreakpointsService;
		readonly Dictionary<DbgModuleBreakpoint, ModuleBreakpointVM> bpToVM;
		int moduleBreakpointOrder;

		[ImportingConstructor]
		ModuleBreakpointsVM(Lazy<DbgManager> dbgManager, DebuggerSettings debuggerSettings, UIDispatcher uiDispatcher, ModuleBreakpointFormatterProvider moduleBreakpointFormatterProvider, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider, EditValueProviderService editValueProviderService, Lazy<DbgModuleBreakpointsService> dbgModuleBreakpointsService) {
			uiDispatcher.VerifyAccess();
			AllItems = new ObservableCollection<ModuleBreakpointVM>();
			SelectedItems = new ObservableCollection<ModuleBreakpointVM>();
			bpToVM = new Dictionary<DbgModuleBreakpoint, ModuleBreakpointVM>();
			this.dbgManager = dbgManager;
			this.moduleBreakpointFormatterProvider = moduleBreakpointFormatterProvider;
			this.debuggerSettings = debuggerSettings;
			lazyToolWindowVMHelper = new LazyToolWindowVMHelper(this, uiDispatcher);
			this.editValueProviderService = editValueProviderService;
			this.dbgModuleBreakpointsService = dbgModuleBreakpointsService;
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			moduleBreakpointContext = new ModuleBreakpointContext(uiDispatcher, classificationFormatMap, textElementProvider) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlight,
				Formatter = moduleBreakpointFormatterProvider.Create(),
			};
		}

		// random thread
		void DbgModuleBreakpoint(Action action) =>
			dbgManager.Value.DispatcherThread.BeginInvoke(action);

		// UI thread
		void ILazyToolWindowVM.Show() {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: true);
		}

		// UI thread
		void ILazyToolWindowVM.Hide() {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			InitializeDebugger_UI(enable: false);
		}

		// UI thread
		void InitializeDebugger_UI(bool enable) {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			if (enable) {
				moduleBreakpointContext.ClassificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
				RecreateFormatter_UI();
				moduleBreakpointContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
			}
			else {
				moduleBreakpointContext.ClassificationFormatMap.ClassificationFormatMappingChanged -= ClassificationFormatMap_ClassificationFormatMappingChanged;
				debuggerSettings.PropertyChanged -= DebuggerSettings_PropertyChanged;
			}
			DbgModuleBreakpoint(() => InitializeDebugger_DbgModuleBreakpoint(enable));
		}

		// DbgManager thread
		void InitializeDebugger_DbgModuleBreakpoint(bool enable) {
			dbgManager.Value.DispatcherThread.VerifyAccess();
			if (enable) {
				dbgModuleBreakpointsService.Value.BreakpointsChanged += DbgModuleBreakpointsService_BreakpointsChanged;
				dbgModuleBreakpointsService.Value.BreakpointsModified += DbgModuleBreakpointsService_BreakpointsModified;
				var moduleBreakpoints = dbgModuleBreakpointsService.Value.Breakpoints;
				if (moduleBreakpoints.Length > 0)
					UI(() => AddItems_UI(moduleBreakpoints));
			}
			else {
				dbgModuleBreakpointsService.Value.BreakpointsChanged -= DbgModuleBreakpointsService_BreakpointsChanged;
				dbgModuleBreakpointsService.Value.BreakpointsModified -= DbgModuleBreakpointsService_BreakpointsModified;
				UI(() => RemoveAllModuleBreakpoints_UI());
			}
		}

		// UI thread
		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			RefreshThemeFields_UI();
		}

		// random thread
		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			UI(() => DebuggerSettings_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DebuggerSettings_PropertyChanged_UI(string propertyName) {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			if (propertyName == nameof(DebuggerSettings.SyntaxHighlight)) {
				moduleBreakpointContext.SyntaxHighlight = debuggerSettings.SyntaxHighlight;
				RefreshThemeFields_UI();
			}
		}

		// UI thread
		void RefreshThemeFields_UI() {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			foreach (var vm in AllItems)
				vm.RefreshThemeFields_UI();
		}

		// UI thread
		void RecreateFormatter_UI() {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			moduleBreakpointContext.Formatter = moduleBreakpointFormatterProvider.Create();
		}

		// random thread
		void UI(Action action) => moduleBreakpointContext.UIDispatcher.UI(action);

		// DbgManager thread
		void DbgModuleBreakpointsService_BreakpointsChanged(object sender, DbgCollectionChangedEventArgs<DbgModuleBreakpoint> e) {
			dbgManager.Value.DispatcherThread.VerifyAccess();
			if (e.Added)
				UI(() => AddItems_UI(e.Objects));
			else {
				UI(() => {
					var coll = AllItems;
					for (int i = coll.Count - 1; i >= 0; i--) {
						if (e.Objects.Contains(coll[i].ModuleBreakpoint))
							RemoveModuleBreakpointAt_UI(i);
					}
				});
			}
		}

		// DbgManager thread
		void DbgModuleBreakpointsService_BreakpointsModified(object sender, DbgBreakpointsModifiedEventArgs e) {
			dbgManager.Value.DispatcherThread.VerifyAccess();
			UI(() => {
				foreach (var bp in e.Breakpoints) {
					bool b = bpToVM.TryGetValue(bp, out var vm);
					Debug.Assert(b);
					if (b)
						vm.UpdateSettings_UI(bp.Settings);
				}
			});
		}

		// UI thread
		void AddItems_UI(IList<DbgModuleBreakpoint> moduleBreakpoints) {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			foreach (var bp in moduleBreakpoints) {
				var vm = new ModuleBreakpointVM(bp, moduleBreakpointContext, moduleBreakpointOrder++, ModuleNameEditValueProvider, OrderEditValueProvider, ProcessNameEditValueProvider, AppDomainNameEditValueProvider);
				Debug.Assert(!bpToVM.ContainsKey(bp));
				bpToVM[bp] = vm;
				AllItems.Add(vm);
			}
		}

		// UI thread
		void RemoveModuleBreakpointAt_UI(int i) {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			Debug.Assert(0 <= i && i < AllItems.Count);
			var vm = AllItems[i];
			bool b = bpToVM.Remove(vm.ModuleBreakpoint);
			Debug.Assert(b);
			vm.Dispose();
			AllItems.RemoveAt(i);
		}

		// UI thread
		void RemoveAllModuleBreakpoints_UI() {
			moduleBreakpointContext.UIDispatcher.VerifyAccess();
			var coll = AllItems;
			for (int i = coll.Count - 1; i >= 0; i--)
				RemoveModuleBreakpointAt_UI(i);
			Debug.Assert(bpToVM.Count == 0);
		}
	}
}

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
using System.Diagnostics;
using dndbg.Engine;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.IMModules;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Debugger.Breakpoints {
	interface IBreakpointsVM {
		void Remove(IEnumerable<BreakpointVM> bps);
	}

	//[Export(typeof(IBreakpointsVM)), Export(typeof(ILoadBeforeDebug))]
	sealed class BreakpointsVM : ViewModelBase, IBreakpointsVM, ILoadBeforeDebug {
		public ObservableCollection<BreakpointVM> Collection => breakpointList;
		readonly ObservableCollection<BreakpointVM> breakpointList;

		public object SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnPropertyChanged(nameof(SelectedItem));
				}
			}
		}
		object selectedItem;

		readonly BreakpointContext breakpointContext;
		readonly IBreakpointService breakpointService;
		readonly ITheDebugger theDebugger;

		[ImportingConstructor]
		BreakpointsVM(IDecompilerService decompilerService, IDebuggerSettings debuggerSettings, ITheDebugger theDebugger, IBreakpointService breakpointService, IBreakpointSettings breakpointSettings, Lazy<IModuleLoader> moduleLoader, IInMemoryModuleService inMemoryModuleService, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider) {
			var classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			breakpointContext = new BreakpointContext(moduleLoader, classificationFormatMap, textElementProvider) {
				Decompiler = decompilerService.Decompiler,
				SyntaxHighlight = debuggerSettings.SyntaxHighlightBreakpoints,
				UseHexadecimal = debuggerSettings.UseHexadecimal,
				ShowTokens = breakpointSettings.ShowTokens,
				ShowModuleNames = breakpointSettings.ShowModuleNames,
				ShowParameterTypes = breakpointSettings.ShowParameterTypes,
				ShowParameterNames = breakpointSettings.ShowParameterNames,
				ShowOwnerTypes = breakpointSettings.ShowOwnerTypes,
				ShowReturnTypes = breakpointSettings.ShowReturnTypes,
				ShowNamespaces = breakpointSettings.ShowNamespaces,
				ShowTypeKeywords = breakpointSettings.ShowTypeKeywords,
			};
			this.breakpointService = breakpointService;
			this.theDebugger = theDebugger;
			breakpointList = new ObservableCollection<BreakpointVM>();
			breakpointSettings.PropertyChanged += BreakpointSettings_PropertyChanged;
			breakpointService.BreakpointsAdded += BreakpointService_BreakpointsAdded;
			breakpointService.BreakpointsRemoved += BreakpointService_BreakpointsRemoved;
			debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
			classificationFormatMap.ClassificationFormatMappingChanged += ClassificationFormatMap_ClassificationFormatMappingChanged;
			decompilerService.DecompilerChanged += DecompilerService_DecompilerChanged;
			inMemoryModuleService.DynamicModulesLoaded += InMemoryModuleService_DynamicModulesLoaded;
			foreach (var bp in breakpointService.GetBreakpoints())
				AddBreakpoint(bp);
		}

		void DecompilerService_DecompilerChanged(object sender, EventArgs e) {
			var decompilerService = (IDecompilerService)sender;
			breakpointContext.Decompiler = decompilerService.Decompiler;
			RefreshLanguageFields();
		}

		void ClassificationFormatMap_ClassificationFormatMappingChanged(object sender, EventArgs e) => RefreshThemeFields();

		void TheDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			var dbg = (DnDebugger)sender;
			switch (theDebugger.ProcessState) {
			case DebuggerProcessState.Starting:
				dbg.DebugCallbackEvent += DnDebugger_DebugCallbackEvent;
				break;

			case DebuggerProcessState.Continuing:
			case DebuggerProcessState.Running:
			case DebuggerProcessState.Paused:
				break;

			case DebuggerProcessState.Terminated:
				dbg.DebugCallbackEvent -= DnDebugger_DebugCallbackEvent;
				break;
			}
		}

		void DnDebugger_DebugCallbackEvent(DnDebugger dbg, DebugCallbackEventArgs e) {
			if (nameErrorCounter != 0 && e.Kind == DebugCallbackKind.LoadClass) {
				var lcArgs = (LoadClassDebugCallbackEventArgs)e;
				var module = dbg.TryGetModule(lcArgs.CorAppDomain, lcArgs.CorClass);
				Debug.Assert(module != null);
				if (module != null && module.IsDynamic)
					pendingModules.Add(module.DnModuleId.ToModuleId());
			}
		}

		void InMemoryModuleService_DynamicModulesLoaded(object sender, EventArgs e) {
			if (nameErrorCounter != 0) {
				foreach (var moduleId in pendingModules) {
					foreach (var vm in breakpointList)
						vm.RefreshIfNameError(moduleId);
				}
			}
			pendingModules.Clear();
		}

		internal void OnNameErrorChanged(BreakpointVM vm) {
			// Called by vm.Dispose() when it's already been removed so don't add an Assert() here
			if (vm.NameError)
				nameErrorCounter++;
			else
				nameErrorCounter--;
			Debug.Assert(0 <= nameErrorCounter && nameErrorCounter <= breakpointList.Count);
		}
		int nameErrorCounter;
		readonly HashSet<ModuleId> pendingModules = new HashSet<ModuleId>();

		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var debuggerSettings = (IDebuggerSettings)sender;
			switch (e.PropertyName) {
			case nameof(debuggerSettings.SyntaxHighlightBreakpoints):
				breakpointContext.SyntaxHighlight = debuggerSettings.SyntaxHighlightBreakpoints;
				RefreshThemeFields();
				break;

			case nameof(debuggerSettings.UseHexadecimal):
				breakpointContext.UseHexadecimal = debuggerSettings.UseHexadecimal;
				RefreshThemeFields();
				break;
			}
		}

		void BreakpointSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var breakpointSettings = (IBreakpointSettings)sender;
			switch (e.PropertyName) {
			case nameof(breakpointSettings.ShowTokens):
				breakpointContext.ShowTokens = breakpointSettings.ShowTokens;
				RefreshNameField();
				break;
			case nameof(breakpointSettings.ShowModuleNames):
				breakpointContext.ShowModuleNames = breakpointSettings.ShowModuleNames;
				RefreshNameField();
				break;
			case nameof(breakpointSettings.ShowParameterTypes):
				breakpointContext.ShowParameterTypes = breakpointSettings.ShowParameterTypes;
				RefreshNameField();
				break;
			case nameof(breakpointSettings.ShowParameterNames):
				breakpointContext.ShowParameterNames = breakpointSettings.ShowParameterNames;
				RefreshNameField();
				break;
			case nameof(breakpointSettings.ShowOwnerTypes):
				breakpointContext.ShowOwnerTypes = breakpointSettings.ShowOwnerTypes;
				RefreshNameField();
				break;
			case nameof(breakpointSettings.ShowReturnTypes):
				breakpointContext.ShowReturnTypes = breakpointSettings.ShowReturnTypes;
				RefreshNameField();
				break;
			case nameof(breakpointSettings.ShowNamespaces):
				breakpointContext.ShowNamespaces = breakpointSettings.ShowNamespaces;
				RefreshNameField();
				break;
			case nameof(breakpointSettings.ShowTypeKeywords):
				breakpointContext.ShowTypeKeywords = breakpointSettings.ShowTypeKeywords;
				RefreshNameField();
				break;
			}
		}

		public void Remove(IEnumerable<BreakpointVM> bps) {
			foreach (var bp in bps)
				breakpointService.Remove(bp.Breakpoint);
		}

		void BreakpointService_BreakpointsAdded(object sender, BreakpointsAddedEventArgs e) {
			foreach (var bp in e.Breakpoints)
				AddBreakpoint(bp);
		}

		void BreakpointService_BreakpointsRemoved(object sender, BreakpointsRemovedEventArgs e) {
			foreach (var bp in e.Breakpoints)
				RemoveBreakpoint(bp);
		}

		void AddBreakpoint(Breakpoint bp) => Collection.Add(new BreakpointVM(this, breakpointContext, bp));

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

		void RefreshThemeFields() {
			foreach (var vm in breakpointList)
				vm.RefreshThemeFields();
		}

		void RefreshLanguageFields() => RefreshNameField();

		void RefreshNameField() {
			foreach (var vm in breakpointList)
				vm.RefreshNameField();
		}
	}
}

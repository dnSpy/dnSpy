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
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Themes;
using dnSpy.Debugger.IMModules;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.Breakpoints {
	interface IBreakpointsVM {
		void Remove(IEnumerable<BreakpointVM> bps);
	}

	[Export, Export(typeof(IBreakpointsVM)), Export(typeof(ILoadBeforeDebug)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class BreakpointsVM : ViewModelBase, IBreakpointsVM, ILoadBeforeDebug {
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

		readonly BreakpointContext breakpointContext;
		readonly IBreakpointManager breakpointManager;
		readonly ITheDebugger theDebugger;

		[ImportingConstructor]
		BreakpointsVM(ILanguageManager languageManager, IImageManager imageManager, IThemeManager themeManager, IDebuggerSettings debuggerSettings, ITheDebugger theDebugger, IBreakpointManager breakpointManager, IBreakpointSettings breakpointSettings, Lazy<IModuleLoader> moduleLoader, IInMemoryModuleManager inMemoryModuleManager) {
			this.breakpointContext = new BreakpointContext(imageManager, moduleLoader) {
				Language = languageManager.Language,
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
			this.breakpointManager = breakpointManager;
			this.theDebugger = theDebugger;
			this.breakpointList = new ObservableCollection<BreakpointVM>();
			breakpointSettings.PropertyChanged += BreakpointSettings_PropertyChanged;
			breakpointManager.OnListModified += BreakpointManager_OnListModified;
			debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;
			languageManager.LanguageChanged += LanguageManager_LanguageChanged;
			inMemoryModuleManager.DynamicModulesLoaded += InMemoryModuleManager_DynamicModulesLoaded;
			foreach (var bp in breakpointManager.Breakpoints)
				AddBreakpoint(bp);
		}

		void LanguageManager_LanguageChanged(object sender, EventArgs e) {
			var languageManager = (ILanguageManager)sender;
			breakpointContext.Language = languageManager.Language;
			RefreshLanguageFields();
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			RefreshThemeFields();
		}

		void TheDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			var dbg = (DnDebugger)sender;
			switch (theDebugger.ProcessState) {
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
					pendingModules.Add(module.SerializedDnModule);
			}
		}

		void InMemoryModuleManager_DynamicModulesLoaded(object sender, EventArgs e) {
			if (nameErrorCounter != 0) {
				foreach (var serMod in pendingModules) {
					foreach (var vm in breakpointList)
						vm.RefreshIfNameError(serMod);
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
		readonly HashSet<SerializedDnModule> pendingModules = new HashSet<SerializedDnModule>();

		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var debuggerSettings = (IDebuggerSettings)sender;
			switch (e.PropertyName) {
			case "SyntaxHighlightBreakpoints":
				breakpointContext.SyntaxHighlight = debuggerSettings.SyntaxHighlightBreakpoints;
				RefreshThemeFields();
				break;

			case "UseHexadecimal":
				breakpointContext.UseHexadecimal = debuggerSettings.UseHexadecimal;
				RefreshThemeFields();
				break;
			}
		}

		void BreakpointSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var breakpointSettings = (IBreakpointSettings)sender;
			switch (e.PropertyName) {
			case "ShowTokens":
				breakpointContext.ShowTokens = breakpointSettings.ShowTokens;
				RefreshNameField();
				break;
			case "ShowModuleNames":
				breakpointContext.ShowModuleNames = breakpointSettings.ShowModuleNames;
				RefreshNameField();
				break;
			case "ShowParameterTypes":
				breakpointContext.ShowParameterTypes = breakpointSettings.ShowParameterTypes;
				RefreshNameField();
				break;
			case "ShowParameterNames":
				breakpointContext.ShowParameterNames = breakpointSettings.ShowParameterNames;
				RefreshNameField();
				break;
			case "ShowOwnerTypes":
				breakpointContext.ShowOwnerTypes = breakpointSettings.ShowOwnerTypes;
				RefreshNameField();
				break;
			case "ShowReturnTypes":
				breakpointContext.ShowReturnTypes = breakpointSettings.ShowReturnTypes;
				RefreshNameField();
				break;
			case "ShowNamespaces":
				breakpointContext.ShowNamespaces = breakpointSettings.ShowNamespaces;
				RefreshNameField();
				break;
			case "ShowTypeKeywords":
				breakpointContext.ShowTypeKeywords = breakpointSettings.ShowTypeKeywords;
				RefreshNameField();
				break;
			}
		}

		public void Remove(IEnumerable<BreakpointVM> bps) {
			foreach (var bp in bps)
				breakpointManager.Remove(bp.Breakpoint);
		}

		void BreakpointManager_OnListModified(object sender, BreakpointListModifiedEventArgs e) {
			if (e.Added)
				AddBreakpoint(e.Breakpoint);
			else
				RemoveBreakpoint(e.Breakpoint);
		}

		void AddBreakpoint(Breakpoint bp) {
			Collection.Add(new BreakpointVM(this, breakpointContext, bp));
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

		void RefreshThemeFields() {
			foreach (var vm in breakpointList)
				vm.RefreshThemeFields();
		}

		void RefreshLanguageFields() {
			RefreshNameField();
		}

		void RefreshNameField() {
			foreach (var vm in breakpointList)
				vm.RefreshNameField();
		}
	}
}

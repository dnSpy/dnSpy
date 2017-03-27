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
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.Breakpoints.Code;
using dnSpy.Debugger.Dialogs.CodeBreakpoints;
using dnSpy.Debugger.Text;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.CodeBreakpoints {
	abstract class CodeBreakpointsOperations {
		public abstract bool CanCopy { get; }
		public abstract void Copy();
		public abstract bool CanSelectAll { get; }
		public abstract void SelectAll();
		public abstract bool CanRemoveCodeBreakpoints { get; }
		public abstract void RemoveCodeBreakpoints();
		public abstract bool CanRemoveMatchingCodeBreakpoints { get; }
		public abstract void RemoveMatchingCodeBreakpoints();
		public abstract bool CanToggleEnabled { get; }
		public abstract void ToggleEnabled();
		public abstract bool CanToggleMatchingBreakpoints { get; }
		public abstract void ToggleMatchingBreakpoints();
		public abstract bool CanEnableBreakpoints { get; }
		public abstract void EnableBreakpoints();
		public abstract bool CanDisableBreakpoints { get; }
		public abstract void DisableBreakpoints();
		public abstract bool CanExportSelectedBreakpoints { get; }
		public abstract void ExportSelectedBreakpoints();
		public abstract bool CanExportMatchingBreakpoints { get; }
		public abstract void ExportMatchingBreakpoints();
		public abstract bool CanImportBreakpoints { get; }
		public abstract void ImportBreakpoints();
		public abstract bool CanResetSearchSettings { get; }
		public abstract void ResetSearchSettings();
		public abstract bool CanGoToSourceCode { get; }
		public abstract void GoToSourceCode();
		public abstract bool CanGoToDisassembly { get; }
		public abstract void GoToDisassembly();
		public abstract bool CanEditSettings { get; }
		public abstract void EditSettings();
		public abstract bool ShowTokens { get; set; }
		public abstract bool ShowModuleNames { get; set; }
		public abstract bool ShowParameterTypes { get; set; }
		public abstract bool ShowParameterNames { get; set; }
		public abstract bool ShowDeclaringTypes { get; set; }
		public abstract bool ShowReturnTypes { get; set; }
		public abstract bool ShowNamespaces { get; set; }
		public abstract bool ShowTypeKeywords { get; set; }
	}

	[Export(typeof(CodeBreakpointsOperations))]
	sealed class CodeBreakpointsOperationsImpl : CodeBreakpointsOperations {
		readonly ICodeBreakpointsVM codeBreakpointsVM;
		readonly DebuggerSettings debuggerSettings;
		readonly CodeBreakpointDisplaySettings codeBreakpointDisplaySettings;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		readonly Lazy<DbgEngineCodeBreakpointSerializerService> dbgEngineCodeBreakpointSerializerService;
		readonly Lazy<ISettingsServiceFactory> settingsServiceFactory;
		readonly IPickSaveFilename pickSaveFilename;
		readonly IPickFilename pickFilename;
		readonly IMessageBoxService messageBoxService;
		readonly Lazy<ShowCodeBreakpointSettingsService> showCodeBreakpointSettingsService;

		BulkObservableCollection<CodeBreakpointVM> AllItems => codeBreakpointsVM.AllItems;
		ObservableCollection<CodeBreakpointVM> SelectedItems => codeBreakpointsVM.SelectedItems;
		//TODO: This should be view order
		IEnumerable<CodeBreakpointVM> SortedSelectedItems => SelectedItems.OrderBy(a => a.Order);
		//TODO: This should be view order
		IEnumerable<CodeBreakpointVM> SortedAllItems => AllItems.OrderBy(a => a.Order);

		[ImportingConstructor]
		CodeBreakpointsOperationsImpl(ICodeBreakpointsVM codeBreakpointsVM, DebuggerSettings debuggerSettings, CodeBreakpointDisplaySettings codeBreakpointDisplaySettings, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService, Lazy<DbgEngineCodeBreakpointSerializerService> dbgEngineCodeBreakpointSerializerService, Lazy<ISettingsServiceFactory> settingsServiceFactory, IPickSaveFilename pickSaveFilename, IPickFilename pickFilename, IMessageBoxService messageBoxService, Lazy<ShowCodeBreakpointSettingsService> showCodeBreakpointSettingsService) {
			this.codeBreakpointsVM = codeBreakpointsVM;
			this.debuggerSettings = debuggerSettings;
			this.codeBreakpointDisplaySettings = codeBreakpointDisplaySettings;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			this.dbgEngineCodeBreakpointSerializerService = dbgEngineCodeBreakpointSerializerService;
			this.settingsServiceFactory = settingsServiceFactory;
			this.pickSaveFilename = pickSaveFilename;
			this.pickFilename = pickFilename;
			this.messageBoxService = messageBoxService;
			this.showCodeBreakpointSettingsService = showCodeBreakpointSettingsService;
		}

		public override bool CanCopy => SelectedItems.Count != 0;
		public override void Copy() {
			var output = new StringBuilderTextColorOutput();
			var debugWriter = new DebugOutputWriterImpl(output);
			foreach (var vm in SortedSelectedItems) {
				var formatter = vm.Context.Formatter;
				formatter.WriteName(debugWriter, vm);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteCondition(output, vm);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteHitCount(output, vm);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteFilter(output, vm);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteWhenHit(output, vm);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteModule(debugWriter, vm);
				output.WriteLine();
			}
			var s = output.ToString();
			if (s.Length > 0) {
				try {
					Clipboard.SetText(s);
				}
				catch (ExternalException) { }
			}
		}

		public override bool CanSelectAll => SelectedItems.Count != AllItems.Count;
		public override void SelectAll() {
			SelectedItems.Clear();
			foreach (var vm in AllItems)
				SelectedItems.Add(vm);
		}

		public override bool CanRemoveCodeBreakpoints => SelectedItems.Count > 0;
		public override void RemoveCodeBreakpoints() {
			var bps = SelectedItems.Select(a => a.CodeBreakpoint).ToArray();
			dbgCodeBreakpointsService.Value.Remove(bps);
		}

		public override bool CanRemoveMatchingCodeBreakpoints => AllItems.Count > 0;
		public override void RemoveMatchingCodeBreakpoints() => dbgCodeBreakpointsService.Value.Remove(AllItems.Select(a => a.CodeBreakpoint).ToArray());

		public override bool CanToggleEnabled => SelectedItems.Count > 0;
		public override void ToggleEnabled() => ToggleBreakpoints(SelectedItems);

		public override bool CanToggleMatchingBreakpoints => AllItems.Count > 0;
		public override void ToggleMatchingBreakpoints() => ToggleBreakpoints(AllItems);

		public override bool CanEnableBreakpoints => SelectedItems.Count != 0 && SelectedItems.Any(a => !a.IsEnabled);
		public override void EnableBreakpoints() => EnableDisableBreakpoints(SelectedItems, enable: true);

		public override bool CanDisableBreakpoints => SelectedItems.Count != 0 && SelectedItems.Any(a => a.IsEnabled);
		public override void DisableBreakpoints() => EnableDisableBreakpoints(SelectedItems, enable: false);

		void ToggleBreakpoints(IList<CodeBreakpointVM> breakpoints) {
			// Toggling everything seems to be less useful, it's more likely that you'd want
			// to enable all selected code breakpoints or disable all of them.
			bool allSet = breakpoints.All(a => a.IsEnabled);
			EnableDisableBreakpoints(breakpoints, enable: !allSet);
		}

		void EnableDisableBreakpoints(IList<CodeBreakpointVM> breakpoints, bool enable) {
			var newSettings = new List<DbgCodeBreakpointAndSettings>(breakpoints.Count);
			for (int i = 0; i < breakpoints.Count; i++) {
				var vm = breakpoints[i];
				var settings = vm.CodeBreakpoint.Settings;
				if (settings.IsEnabled == enable)
					continue;
				settings.IsEnabled = enable;
				newSettings.Add(new DbgCodeBreakpointAndSettings(vm.CodeBreakpoint, settings));
			}
			if (newSettings.Count > 0)
				dbgCodeBreakpointsService.Value.Modify(newSettings.ToArray());
		}

		public override bool CanExportSelectedBreakpoints => SelectedItems.Count > 0;
		public override void ExportSelectedBreakpoints() => SaveBreakpoints(SortedSelectedItems);

		public override bool CanExportMatchingBreakpoints => AllItems.Count > 0;
		public override void ExportMatchingBreakpoints() => SaveBreakpoints(SortedAllItems);

		void SaveBreakpoints(IEnumerable<CodeBreakpointVM> vms) {
			if (!vms.Any())
				return;
			var filename = pickSaveFilename.GetFilename(null, "xml", PickFilenameConstants.XmlFilenameFilter);
			if (filename == null)
				return;
			var settingsService = settingsServiceFactory.Value.Create();
			new BreakpointsSerializer(settingsService, dbgEngineCodeBreakpointSerializerService.Value).Save(vms.Select(a => a.CodeBreakpoint));
			try {
				settingsService.Save(filename);
			}
			catch (Exception ex) {
				messageBoxService.Show(ex);
			}
		}

		public override bool CanImportBreakpoints => true;
		public override void ImportBreakpoints() {
			var filename = pickFilename.GetFilename(null, "xml", PickFilenameConstants.XmlFilenameFilter);
			if (!File.Exists(filename))
				return;
			var settingsService = settingsServiceFactory.Value.Create();
			try {
				settingsService.Open(filename);
			}
			catch (Exception ex) {
				messageBoxService.Show(ex);
				return;
			}
			var breakpoints = new BreakpointsSerializer(settingsService, dbgEngineCodeBreakpointSerializerService.Value).Load();
			dbgCodeBreakpointsService.Value.Add(breakpoints);
		}

		public override bool CanResetSearchSettings => true;
		public override void ResetSearchSettings() => codeBreakpointsVM.ResetSearchSettings();

		public override bool CanGoToSourceCode => SelectedItems.Count == 1;
		public override void GoToSourceCode() {
			if (!CanGoToSourceCode)
				return;
			//TODO:
		}

		public override bool CanGoToDisassembly => false;
		public override void GoToDisassembly() {
			if (!CanGoToDisassembly)
				return;
			//TODO:
		}

		public override bool CanEditSettings => SelectedItems.Count == 1;
		public override void EditSettings() {
			if (!CanEditSettings)
				return;
			var bp = SelectedItems[0].CodeBreakpoint;
			var newSettings = showCodeBreakpointSettingsService.Value.Show(bp.Settings);
			if (newSettings == null)
				return;
			bp.Settings = newSettings.Value;
		}

		public override bool ShowTokens {
			get => codeBreakpointDisplaySettings.ShowTokens;
			set => codeBreakpointDisplaySettings.ShowTokens = value;
		}

		public override bool ShowModuleNames {
			get => codeBreakpointDisplaySettings.ShowModuleNames;
			set => codeBreakpointDisplaySettings.ShowModuleNames = value;
		}

		public override bool ShowParameterTypes {
			get => codeBreakpointDisplaySettings.ShowParameterTypes;
			set => codeBreakpointDisplaySettings.ShowParameterTypes = value;
		}

		public override bool ShowParameterNames {
			get => codeBreakpointDisplaySettings.ShowParameterNames;
			set => codeBreakpointDisplaySettings.ShowParameterNames = value;
		}

		public override bool ShowDeclaringTypes {
			get => codeBreakpointDisplaySettings.ShowDeclaringTypes;
			set => codeBreakpointDisplaySettings.ShowDeclaringTypes = value;
		}

		public override bool ShowReturnTypes {
			get => codeBreakpointDisplaySettings.ShowReturnTypes;
			set => codeBreakpointDisplaySettings.ShowReturnTypes = value;
		}

		public override bool ShowNamespaces {
			get => codeBreakpointDisplaySettings.ShowNamespaces;
			set => codeBreakpointDisplaySettings.ShowNamespaces = value;
		}

		public override bool ShowTypeKeywords {
			get => codeBreakpointDisplaySettings.ShowTypeKeywords;
			set => codeBreakpointDisplaySettings.ShowTypeKeywords = value;
		}
	}
}

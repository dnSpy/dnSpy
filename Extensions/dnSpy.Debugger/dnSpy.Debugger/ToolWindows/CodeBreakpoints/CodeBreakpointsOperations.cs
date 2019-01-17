/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Breakpoints.Code.Dialogs;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;
using dnSpy.Debugger.Breakpoints.Code;
using dnSpy.Debugger.Disassembly;
using dnSpy.Debugger.Properties;
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
		public abstract bool IsEditingValues { get; }
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
		public abstract void GoToSourceCode(bool newTab);
		public abstract bool CanGoToDisassembly { get; }
		public abstract void GoToDisassembly();
		public abstract bool CanEditSettings { get; }
		public abstract void EditSettings();
		public abstract bool CanEditLabels { get; }
		public abstract void EditLabels();
		public abstract bool CanResetHitCount { get; }
		public abstract void ResetHitCount();
		public abstract bool ShowTokens { get; set; }
		public abstract bool ShowModuleNames { get; set; }
		public abstract bool ShowParameterTypes { get; set; }
		public abstract bool ShowParameterNames { get; set; }
		public abstract bool ShowDeclaringTypes { get; set; }
		public abstract bool ShowReturnTypes { get; set; }
		public abstract bool ShowNamespaces { get; set; }
		public abstract bool ShowIntrinsicTypeKeywords { get; set; }
	}

	[Export(typeof(CodeBreakpointsOperations))]
	sealed class CodeBreakpointsOperationsImpl : CodeBreakpointsOperations {
		readonly ICodeBreakpointsVM codeBreakpointsVM;
		readonly DbgCodeBreakpointDisplaySettings dbgCodeBreakpointDisplaySettings;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		readonly Lazy<DbgCodeLocationSerializerService> dbgCodeLocationSerializerService;
		readonly Lazy<ISettingsServiceFactory> settingsServiceFactory;
		readonly IPickFilename pickFilename;
		readonly IMessageBoxService messageBoxService;
		readonly Lazy<ShowCodeBreakpointSettingsService> showCodeBreakpointSettingsService;
		readonly Lazy<DbgCodeBreakpointSerializerService> dbgCodeBreakpointSerializerService;
		readonly Lazy<ReferenceNavigatorService> referenceNavigatorService;
		readonly Lazy<DbgCodeBreakpointHitCountService> dbgCodeBreakpointHitCountService;
		readonly Lazy<DbgShowNativeCodeService> dbgShowNativeCodeService;

		BulkObservableCollection<CodeBreakpointVM> AllItems => codeBreakpointsVM.AllItems;
		ObservableCollection<CodeBreakpointVM> SelectedItems => codeBreakpointsVM.SelectedItems;
		IEnumerable<CodeBreakpointVM> SortedSelectedItems => codeBreakpointsVM.Sort(SelectedItems);
		IEnumerable<CodeBreakpointVM> SortedAllItems => codeBreakpointsVM.Sort(AllItems);

		[ImportingConstructor]
		CodeBreakpointsOperationsImpl(ICodeBreakpointsVM codeBreakpointsVM, DbgCodeBreakpointDisplaySettings dbgCodeBreakpointDisplaySettings, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService, Lazy<DbgCodeLocationSerializerService> dbgCodeLocationSerializerService, Lazy<ISettingsServiceFactory> settingsServiceFactory, IPickFilename pickFilename, IMessageBoxService messageBoxService, Lazy<ShowCodeBreakpointSettingsService> showCodeBreakpointSettingsService, Lazy<DbgCodeBreakpointSerializerService> dbgCodeBreakpointSerializerService, Lazy<ReferenceNavigatorService> referenceNavigatorService, Lazy<DbgCodeBreakpointHitCountService> dbgCodeBreakpointHitCountService, Lazy<DbgShowNativeCodeService> dbgShowNativeCodeService) {
			this.codeBreakpointsVM = codeBreakpointsVM;
			this.dbgCodeBreakpointDisplaySettings = dbgCodeBreakpointDisplaySettings;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			this.dbgCodeLocationSerializerService = dbgCodeLocationSerializerService;
			this.settingsServiceFactory = settingsServiceFactory;
			this.pickFilename = pickFilename;
			this.messageBoxService = messageBoxService;
			this.showCodeBreakpointSettingsService = showCodeBreakpointSettingsService;
			this.dbgCodeBreakpointSerializerService = dbgCodeBreakpointSerializerService;
			this.referenceNavigatorService = referenceNavigatorService;
			this.dbgCodeBreakpointHitCountService = dbgCodeBreakpointHitCountService;
			this.dbgShowNativeCodeService = dbgShowNativeCodeService;
		}

		public override bool CanCopy => SelectedItems.Count != 0;
		public override void Copy() {
			var output = new DbgStringBuilderTextWriter();
			foreach (var vm in SortedSelectedItems) {
				var formatter = vm.Context.Formatter;
				bool needTab = false;
				foreach (var column in codeBreakpointsVM.Descs.Columns) {
					if (!column.IsVisible)
						continue;
					if (column.Name == string.Empty)
						continue;

					if (needTab)
						output.Write(DbgTextColor.Text, "\t");
					switch (column.Id) {
					case CodeBreakpointsColumnIds.Name:
						formatter.WriteName(output, vm);
						break;

					case CodeBreakpointsColumnIds.Labels:
						formatter.WriteLabels(output, vm);
						break;

					case CodeBreakpointsColumnIds.Condition:
						formatter.WriteCondition(output, vm);
						break;

					case CodeBreakpointsColumnIds.HitCount:
						formatter.WriteHitCount(output, vm);
						break;

					case CodeBreakpointsColumnIds.Filter:
						formatter.WriteFilter(output, vm);
						break;

					case CodeBreakpointsColumnIds.WhenHit:
						formatter.WriteWhenHit(output, vm);
						break;

					case CodeBreakpointsColumnIds.Module:
						formatter.WriteModule(output, vm);
						break;

					default:
						throw new InvalidOperationException();
					}

					needTab = true;
				}
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

		public override bool IsEditingValues {
			get {
				foreach (var vm in SelectedItems) {
					if (vm.IsEditingValues)
						return true;
				}
				return false;
			}
		}

		public override bool CanToggleEnabled => SelectedItems.Count > 0 && !IsEditingValues;
		public override void ToggleEnabled() => ToggleBreakpoints(SelectedItems);

		public override bool CanToggleMatchingBreakpoints => AllItems.Count > 0;
		public override void ToggleMatchingBreakpoints() => ToggleBreakpoints(AllItems);

		public override bool CanEnableBreakpoints => SelectedItems.Count != 0 && SelectedItems.Any(a => !a.IsEnabled);
		public override void EnableBreakpoints() => EnableDisableBreakpoints(SelectedItems, enable: true);

		public override bool CanDisableBreakpoints => SelectedItems.Count != 0 && SelectedItems.Any(a => a.IsEnabled);
		public override void DisableBreakpoints() => EnableDisableBreakpoints(SelectedItems, enable: false);

		void ToggleBreakpoints(IList<CodeBreakpointVM> breakpoints) {
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
			dbgCodeBreakpointSerializerService.Value.Save(vms.Select(a => a.CodeBreakpoint).ToArray());
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
			dbgCodeBreakpointsService.Value.Add(new BreakpointsSerializer(settingsService, dbgCodeLocationSerializerService.Value).Load());
		}

		public override bool CanResetSearchSettings => true;
		public override void ResetSearchSettings() => codeBreakpointsVM.ResetSearchSettings();

		public override bool CanGoToSourceCode => SelectedItems.Count == 1;
		public override void GoToSourceCode(bool newTab) {
			if (!CanGoToSourceCode)
				return;
			var options = newTab ? new object[] { PredefinedReferenceNavigatorOptions.NewTab } : Array.Empty<object>();
			referenceNavigatorService.Value.GoTo(SelectedItems[0].CodeBreakpoint.Location, options);
		}

		public override bool CanGoToDisassembly {
			get {
				if (SelectedItems.Count != 1)
					return false;
				var boundBps = SelectedItems[0].CodeBreakpoint.BoundBreakpoints;
				return boundBps.Length > 0 && dbgShowNativeCodeService.Value.CanShowNativeCode(boundBps[0]);
			}
		}
		public override void GoToDisassembly() {
			if (!CanGoToDisassembly)
				return;
			var boundBps = SelectedItems[0].CodeBreakpoint.BoundBreakpoints;
			if (boundBps.Length == 0)
				return;
			if (!dbgShowNativeCodeService.Value.ShowNativeCode(boundBps[0]))
				messageBoxService.Show(dnSpy_Debugger_Resources.Error_CouldNotShowDisassembly);
		}

		public override bool CanEditSettings => SelectedItems.Count > 0;
		public override void EditSettings() {
			if (!CanEditSettings)
				return;
			showCodeBreakpointSettingsService.Value.Edit(SelectedItems.Select(a => a.CodeBreakpoint).ToArray());
		}

		public override bool CanEditLabels => (SelectedItems.Count == 1 && !SelectedItems[0].LabelsEditableValue.IsEditingValue) || SelectedItems.Count > 1;
		public override void EditLabels() {
			if (!CanEditLabels)
				return;
			foreach (var vm in codeBreakpointsVM.AllItems)
				vm.ClearEditingValueProperties();
			if (SelectedItems.Count == 1)
				SelectedItems[0].LabelsEditableValue.IsEditingValue = true;
			else {
				var newLabels = messageBoxService.Ask<string>(dnSpy_Debugger_Resources.EditLabelsMsgBoxLabel, SelectedItems[0].GetLabelsString(), dnSpy_Debugger_Resources.EditLabelsTitle);
				if (newLabels != null) {
					var labelsColl = CodeBreakpointVM.CreateLabelsCollection(newLabels);
					dbgCodeBreakpointsService.Value.Modify(SelectedItems.Select(a => {
						var bm = a.CodeBreakpoint;
						var settings = bm.Settings;
						settings.Labels = labelsColl;
						return new DbgCodeBreakpointAndSettings(bm, settings);
					}).ToArray());
				}
			}
		}

		public override bool CanResetHitCount => SelectedItems.Count > 0;
		public override void ResetHitCount() {
			if (!CanResetHitCount)
				return;
			dbgCodeBreakpointHitCountService.Value.Reset(SelectedItems.Select(a => a.CodeBreakpoint).ToArray());
		}

		public override bool ShowTokens {
			get => dbgCodeBreakpointDisplaySettings.ShowTokens;
			set => dbgCodeBreakpointDisplaySettings.ShowTokens = value;
		}

		public override bool ShowModuleNames {
			get => dbgCodeBreakpointDisplaySettings.ShowModuleNames;
			set => dbgCodeBreakpointDisplaySettings.ShowModuleNames = value;
		}

		public override bool ShowParameterTypes {
			get => dbgCodeBreakpointDisplaySettings.ShowParameterTypes;
			set => dbgCodeBreakpointDisplaySettings.ShowParameterTypes = value;
		}

		public override bool ShowParameterNames {
			get => dbgCodeBreakpointDisplaySettings.ShowParameterNames;
			set => dbgCodeBreakpointDisplaySettings.ShowParameterNames = value;
		}

		public override bool ShowDeclaringTypes {
			get => dbgCodeBreakpointDisplaySettings.ShowDeclaringTypes;
			set => dbgCodeBreakpointDisplaySettings.ShowDeclaringTypes = value;
		}

		public override bool ShowReturnTypes {
			get => dbgCodeBreakpointDisplaySettings.ShowReturnTypes;
			set => dbgCodeBreakpointDisplaySettings.ShowReturnTypes = value;
		}

		public override bool ShowNamespaces {
			get => dbgCodeBreakpointDisplaySettings.ShowNamespaces;
			set => dbgCodeBreakpointDisplaySettings.ShowNamespaces = value;
		}

		public override bool ShowIntrinsicTypeKeywords {
			get => dbgCodeBreakpointDisplaySettings.ShowIntrinsicTypeKeywords;
			set => dbgCodeBreakpointDisplaySettings.ShowIntrinsicTypeKeywords = value;
		}
	}
}

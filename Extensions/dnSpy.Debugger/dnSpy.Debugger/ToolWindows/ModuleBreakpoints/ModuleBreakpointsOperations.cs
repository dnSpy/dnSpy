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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Modules;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;
using dnSpy.Debugger.Breakpoints.Modules;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.ModuleBreakpoints {
	abstract class ModuleBreakpointsOperations {
		public abstract bool CanCopy { get; }
		public abstract void Copy();
		public abstract bool CanSelectAll { get; }
		public abstract void SelectAll();
		public abstract bool CanAddModuleBreakpoint { get; }
		public abstract void AddModuleBreakpoint();
		public abstract bool CanRemoveModuleBreakpoints { get; }
		public abstract void RemoveModuleBreakpoints();
		public abstract bool CanRemoveMatchingModuleBreakpoints { get; }
		public abstract void RemoveMatchingModuleBreakpoints();
		public abstract bool CanEditModuleName { get; }
		public abstract void EditModuleName();
		public abstract bool CanEditOrder { get; }
		public abstract void EditOrder();
		public abstract bool CanEditProcessName { get; }
		public abstract void EditProcessName();
		public abstract bool CanEditAppDomainName { get; }
		public abstract void EditAppDomainName();
		public abstract bool CanToggleEnabled { get; }
		public abstract void ToggleEnabled();
		public abstract bool CanToggleMatchingBreakpoints { get; }
		public abstract void ToggleMatchingBreakpoints();
		public abstract bool CanEnableBreakpoints { get; }
		public abstract void EnableBreakpoints();
		public abstract bool CanDisableBreakpoints { get; }
		public abstract void DisableBreakpoints();
		public abstract bool IsEditingValues { get; }
		public abstract bool CanExportSelectedBreakpoints { get; }
		public abstract void ExportSelectedBreakpoints();
		public abstract bool CanExportMatchingBreakpoints { get; }
		public abstract void ExportMatchingBreakpoints();
		public abstract bool CanImportBreakpoints { get; }
		public abstract void ImportBreakpoints();
		public abstract bool CanResetSearchSettings { get; }
		public abstract void ResetSearchSettings();
	}

	[Export(typeof(ModuleBreakpointsOperations))]
	sealed class ModuleBreakpointsOperationsImpl : ModuleBreakpointsOperations {
		readonly IModuleBreakpointsVM moduleBreakpointsVM;
		readonly DebuggerSettings debuggerSettings;
		readonly Lazy<DbgModuleBreakpointsService> dbgModuleBreakpointsService;
		readonly Lazy<ISettingsServiceFactory> settingsServiceFactory;
		readonly IPickSaveFilename pickSaveFilename;
		readonly IPickFilename pickFilename;
		readonly IMessageBoxService messageBoxService;

		BulkObservableCollection<ModuleBreakpointVM> AllItems => moduleBreakpointsVM.AllItems;
		ObservableCollection<ModuleBreakpointVM> SelectedItems => moduleBreakpointsVM.SelectedItems;
		IEnumerable<ModuleBreakpointVM> SortedSelectedItems => moduleBreakpointsVM.Sort(SelectedItems);
		IEnumerable<ModuleBreakpointVM> SortedAllItems => moduleBreakpointsVM.Sort(AllItems);

		[ImportingConstructor]
		ModuleBreakpointsOperationsImpl(IModuleBreakpointsVM moduleBreakpointsVM, DebuggerSettings debuggerSettings, Lazy<DbgModuleBreakpointsService> dbgModuleBreakpointsService, Lazy<ISettingsServiceFactory> settingsServiceFactory, IPickSaveFilename pickSaveFilename, IPickFilename pickFilename, IMessageBoxService messageBoxService) {
			this.moduleBreakpointsVM = moduleBreakpointsVM;
			this.debuggerSettings = debuggerSettings;
			this.dbgModuleBreakpointsService = dbgModuleBreakpointsService;
			this.settingsServiceFactory = settingsServiceFactory;
			this.pickSaveFilename = pickSaveFilename;
			this.pickFilename = pickFilename;
			this.messageBoxService = messageBoxService;
		}

		public override bool CanCopy => SelectedItems.Count != 0;
		public override void Copy() {
			var output = new DbgStringBuilderTextWriter();
			foreach (var vm in SortedSelectedItems) {
				var formatter = vm.Context.Formatter;
				bool needTab = false;
				foreach (var column in moduleBreakpointsVM.Descs.Columns) {
					if (!column.IsVisible)
						continue;

					if (needTab)
						output.Write(DbgTextColor.Text, "\t");
					switch (column.Id) {
					case ModuleBreakpointsWindowColumnIds.IsEnabled:
						formatter.WriteIsEnabled(output, vm.ModuleBreakpoint);
						break;

					case ModuleBreakpointsWindowColumnIds.Name:
						formatter.WriteModuleName(output, vm.ModuleBreakpoint);
						break;

					case ModuleBreakpointsWindowColumnIds.DynamicModule:
						formatter.WriteDynamic(output, vm.ModuleBreakpoint);
						break;

					case ModuleBreakpointsWindowColumnIds.InMemoryModule:
						formatter.WriteInMemory(output, vm.ModuleBreakpoint);
						break;

					case ModuleBreakpointsWindowColumnIds.LoadModule:
						formatter.WriteLoadModule(output, vm.ModuleBreakpoint);
						break;

					case ModuleBreakpointsWindowColumnIds.Order:
						formatter.WriteOrder(output, vm.ModuleBreakpoint);
						break;

					case ModuleBreakpointsWindowColumnIds.Process:
						formatter.WriteProcessName(output, vm.ModuleBreakpoint);
						break;

					case ModuleBreakpointsWindowColumnIds.AppDomain:
						formatter.WriteAppDomainName(output, vm.ModuleBreakpoint);
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

		public override bool CanAddModuleBreakpoint => true;
		public override void AddModuleBreakpoint() {
			var settings = new DbgModuleBreakpointSettings {
				IsEnabled = true,
				IsLoaded = true,
				ModuleName = "*mymodule*",
			};
			dbgModuleBreakpointsService.Value.Add(settings);
		}

		public override bool CanRemoveModuleBreakpoints => SelectedItems.Count > 0;
		public override void RemoveModuleBreakpoints() {
			var bps = SelectedItems.Select(a => a.ModuleBreakpoint).ToArray();
			dbgModuleBreakpointsService.Value.Remove(bps);
		}

		public override bool CanRemoveMatchingModuleBreakpoints => AllItems.Count > 0;
		public override void RemoveMatchingModuleBreakpoints() => dbgModuleBreakpointsService.Value.Remove(AllItems.Select(a => a.ModuleBreakpoint).ToArray());

		public override bool CanEditModuleName => SelectedItems.Count == 1 && !SelectedItems[0].ModuleNameEditableValue.IsEditingValue;
		public override void EditModuleName() {
			if (!CanEditModuleName)
				return;
			SelectedItems[0].ClearEditingValueProperties();
			SelectedItems[0].ModuleNameEditableValue.IsEditingValue = true;
		}

		public override bool CanEditOrder => SelectedItems.Count == 1 && !SelectedItems[0].OrderEditableValue.IsEditingValue;
		public override void EditOrder() {
			if (!CanEditOrder)
				return;
			SelectedItems[0].ClearEditingValueProperties();
			SelectedItems[0].OrderEditableValue.IsEditingValue = true;
		}

		public override bool CanEditProcessName => SelectedItems.Count == 1 && !SelectedItems[0].ProcessNameEditableValue.IsEditingValue;
		public override void EditProcessName() {
			if (!CanEditProcessName)
				return;
			SelectedItems[0].ClearEditingValueProperties();
			SelectedItems[0].ProcessNameEditableValue.IsEditingValue = true;
		}

		public override bool CanEditAppDomainName => SelectedItems.Count == 1 && !SelectedItems[0].AppDomainNameEditableValue.IsEditingValue;
		public override void EditAppDomainName() {
			if (!CanEditAppDomainName)
				return;
			SelectedItems[0].ClearEditingValueProperties();
			SelectedItems[0].AppDomainNameEditableValue.IsEditingValue = true;
		}

		public override bool CanToggleEnabled => SelectedItems.Count > 0 && !IsEditingValues;
		public override void ToggleEnabled() => ToggleBreakpoints(SelectedItems);

		public override bool CanToggleMatchingBreakpoints => AllItems.Count > 0;
		public override void ToggleMatchingBreakpoints() => ToggleBreakpoints(AllItems);

		public override bool CanEnableBreakpoints => SelectedItems.Count != 0 && SelectedItems.Any(a => !a.IsEnabled);
		public override void EnableBreakpoints() => EnableDisableBreakpoints(SelectedItems, enable: true);

		public override bool CanDisableBreakpoints => SelectedItems.Count != 0 && SelectedItems.Any(a => a.IsEnabled);
		public override void DisableBreakpoints() => EnableDisableBreakpoints(SelectedItems, enable: false);

		void ToggleBreakpoints(IList<ModuleBreakpointVM> breakpoints) {
			bool allSet = breakpoints.All(a => a.IsEnabled);
			EnableDisableBreakpoints(breakpoints, enable: !allSet);
		}

		void EnableDisableBreakpoints(IList<ModuleBreakpointVM> breakpoints, bool enable) {
			var newSettings = new List<DbgModuleBreakpointAndSettings>(breakpoints.Count);
			for (int i = 0; i < breakpoints.Count; i++) {
				var vm = breakpoints[i];
				var settings = vm.ModuleBreakpoint.Settings;
				if (settings.IsEnabled == enable)
					continue;
				settings.IsEnabled = enable;
				newSettings.Add(new DbgModuleBreakpointAndSettings(vm.ModuleBreakpoint, settings));
			}
			if (newSettings.Count > 0)
				dbgModuleBreakpointsService.Value.Modify(newSettings.ToArray());
		}

		public override bool IsEditingValues {
			get {
				foreach (var vm in SelectedItems) {
					if (vm.IsEditingValues)
						return true;
				}
				return false;
			}
		}

		public override bool CanExportSelectedBreakpoints => SelectedItems.Count > 0;
		public override void ExportSelectedBreakpoints() => SaveBreakpoints(SortedSelectedItems);

		public override bool CanExportMatchingBreakpoints => AllItems.Count > 0;
		public override void ExportMatchingBreakpoints() => SaveBreakpoints(SortedAllItems);

		void SaveBreakpoints(IEnumerable<ModuleBreakpointVM> vms) {
			if (!vms.Any())
				return;
			var filename = pickSaveFilename.GetFilename(null, "xml", PickFilenameConstants.XmlFilenameFilter);
			if (filename is null)
				return;
			var settingsService = settingsServiceFactory.Value.Create();
			new BreakpointsSerializer(settingsService).Save(vms.Select(a => a.ModuleBreakpoint).ToArray());
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
			Debug2.Assert(filename is not null);
			var settingsService = settingsServiceFactory.Value.Create();
			try {
				settingsService.Open(filename);
			}
			catch (Exception ex) {
				messageBoxService.Show(ex);
				return;
			}
			var breakpoints = new BreakpointsSerializer(settingsService).Load();
			dbgModuleBreakpointsService.Value.Add(breakpoints);
		}

		public override bool CanResetSearchSettings => true;
		public override void ResetSearchSettings() => moduleBreakpointsVM.ResetSearchSettings();
	}
}

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
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Modules;
using dnSpy.Contracts.Text;

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
		public abstract bool CanRemoveAllModuleBreakpoints { get; }
		public abstract void RemoveAllModuleBreakpoints();
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
		public abstract bool CanEnableBreakpoints { get; }
		public abstract void EnableBreakpoints();
		public abstract bool CanDisableBreakpoints { get; }
		public abstract void DisableBreakpoints();
	}

	[Export(typeof(ModuleBreakpointsOperations))]
	sealed class ModuleBreakpointsOperationsImpl : ModuleBreakpointsOperations {
		readonly IModuleBreakpointsVM moduleBreakpointsVM;
		readonly DebuggerSettings debuggerSettings;
		readonly Lazy<DbgModuleBreakpointsService> dbgModuleBreakpointsService;

		ObservableCollection<ModuleBreakpointVM> AllItems => moduleBreakpointsVM.AllItems;
		ObservableCollection<ModuleBreakpointVM> SelectedItems => moduleBreakpointsVM.SelectedItems;
		//TODO: This should be view order
		IEnumerable<ModuleBreakpointVM> SortedSelectedItems => SelectedItems.OrderBy(a => a.Order);

		[ImportingConstructor]
		ModuleBreakpointsOperationsImpl(IModuleBreakpointsVM moduleBreakpointsVM, DebuggerSettings debuggerSettings, Lazy<DbgModuleBreakpointsService> dbgModuleBreakpointsService) {
			this.moduleBreakpointsVM = moduleBreakpointsVM;
			this.debuggerSettings = debuggerSettings;
			this.dbgModuleBreakpointsService = dbgModuleBreakpointsService;
		}

		public override bool CanCopy => SelectedItems.Count != 0;
		public override void Copy() {
			var output = new StringBuilderTextColorOutput();
			foreach (var vm in SortedSelectedItems) {
				var formatter = vm.Context.Formatter;
				formatter.WriteIsEnabled(output, vm.ModuleBreakpoint);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteId(output, vm.ModuleBreakpoint);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteModuleName(output, vm.ModuleBreakpoint);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteDynamic(output, vm.ModuleBreakpoint);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteInMemory(output, vm.ModuleBreakpoint);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteOrder(output, vm.ModuleBreakpoint);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteProcessName(output, vm.ModuleBreakpoint);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteAppDomainName(output, vm.ModuleBreakpoint);
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
				ModuleName = "*mymodule*",
			};
			dbgModuleBreakpointsService.Value.Add(settings);
		}

		public override bool CanRemoveModuleBreakpoints => SelectedItems.Count > 0;
		public override void RemoveModuleBreakpoints() {
			var bps = SelectedItems.Select(a => a.ModuleBreakpoint).ToArray();
			dbgModuleBreakpointsService.Value.Remove(bps);
		}

		public override bool CanRemoveAllModuleBreakpoints => true;
		public override void RemoveAllModuleBreakpoints() => dbgModuleBreakpointsService.Value.Clear();

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

		public override bool CanToggleEnabled => SelectedItems.Count > 0;
		public override void ToggleEnabled() {
			// Toggling everything seems to be less useful, it's more likely that you'd want
			// to enable all selected module breakpoints or disable all of them.
			bool allSet = SelectedItems.All(a => a.IsEnabled);
			EnableDisableBreakpoints(enable: !allSet);
		}

		public override bool CanEnableBreakpoints => SelectedItems.Count != 0 && SelectedItems.Any(a => !a.IsEnabled);
		public override void EnableBreakpoints() => EnableDisableBreakpoints(enable: true);

		public override bool CanDisableBreakpoints => SelectedItems.Count != 0 && SelectedItems.Any(a => a.IsEnabled);
		public override void DisableBreakpoints() => EnableDisableBreakpoints(enable: false);

		void EnableDisableBreakpoints(bool enable) {
			var newSettings = new List<DbgModuleBreakpointAndSettings>(SelectedItems.Count);
			for (int i = 0; i < SelectedItems.Count; i++) {
				var vm = SelectedItems[i];
				var settings = vm.ModuleBreakpoint.Settings;
				if (settings.IsEnabled == enable)
					continue;
				settings.IsEnabled = enable;
				newSettings.Add(new DbgModuleBreakpointAndSettings(vm.ModuleBreakpoint, settings));
			}
			if (newSettings.Count > 0)
				dbgModuleBreakpointsService.Value.Modify(newSettings.ToArray());
		}
	}
}

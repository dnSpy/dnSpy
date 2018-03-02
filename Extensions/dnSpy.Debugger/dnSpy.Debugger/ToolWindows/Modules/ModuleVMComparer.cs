using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.ToolWindows.Modules;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.Modules {
	sealed class ModuleVMComparer : FormatterObjectVMComparer<ModuleVM> {
		public static readonly ModuleVMComparer Instance = new ModuleVMComparer(null, ListSortDirection.Ascending);

		public ModuleVMComparer(string vmPropertyName, ListSortDirection direction) : base(vmPropertyName, direction) { }

		protected override int doCompare(ModuleVM x, ModuleVM y) {
			if (String.IsNullOrEmpty(this.VMPropertyName)) {
				return x.Order - y.Order;
			}

			if (this.Tag == PredefinedTextClassifierTags.ModulesWindowName) {
				return String.Compare(x.Module.Name, y.Module.Name);
			}
			else if (this.Tag == PredefinedTextClassifierTags.ModulesWindowPath) {
				return String.Compare(x.Module.Filename, y.Module.Filename);
			}
			else if (this.Tag == PredefinedTextClassifierTags.ModulesWindowOptimized) {
				return Comparer<bool?>.Default.Compare(x.Module.IsOptimized, y.Module.IsOptimized);
			}
			else if (this.Tag == PredefinedTextClassifierTags.ModulesWindowDynamic) {
				return Comparer<bool?>.Default.Compare(x.Module.IsDynamic, y.Module.IsDynamic);
			}
			else if (this.Tag == PredefinedTextClassifierTags.ModulesWindowInMemory) {
				return Comparer<bool?>.Default.Compare(x.Module.IsInMemory, y.Module.IsInMemory);
			}
			else if (this.Tag == PredefinedTextClassifierTags.ModulesWindowOrder) {
				return Comparer<int>.Default.Compare(x.Module.Order, y.Module.Order);
			}
			else if (this.Tag == PredefinedTextClassifierTags.ModulesWindowVersion) {
				return String.Compare(x.Module.Version, y.Module.Version);
			}
			else if (this.Tag == PredefinedTextClassifierTags.ModulesWindowTimestamp) {
				return Comparer<DateTime?>.Default.Compare(x.Module.Timestamp, y.Module.Timestamp);
			}
			else if (this.Tag == PredefinedTextClassifierTags.ModulesWindowAddress) {
				return Comparer<ulong>.Default.Compare(x.Module.Address, y.Module.Address);
			}
			else if (this.Tag == PredefinedTextClassifierTags.ModulesWindowProcess) {
				return Comparer<ulong>.Default.Compare(x.Module.Process.Id, y.Module.Process.Id);
			}
			else if (this.Tag == PredefinedTextClassifierTags.ModulesWindowAppDomain) {
				return Comparer<int?>.Default.Compare(x.Module.AppDomain?.Id, y.Module.AppDomain?.Id);
			}
			else {
				Debug.Fail($"Unknown module property: {this.Tag}");
			}

			return 0;
		}
	}
}

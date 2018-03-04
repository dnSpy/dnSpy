/*
    Copyright (C) 2014-2018 eichhorn@posteo.de

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
using System.ComponentModel;
using System.Diagnostics;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.Modules {
	sealed class ModuleVMComparer : FormatterObjectVMComparer<ModuleVM> {
		public static readonly ModuleVMComparer Instance = new ModuleVMComparer(null, ListSortDirection.Ascending);

		public ModuleVMComparer(string vmPropertyName, ListSortDirection direction) : base(vmPropertyName, direction) { }

		protected override int CompareCore(ModuleVM x, ModuleVM y) {
			if (String.IsNullOrEmpty(VMPropertyName))
				return x.Order - y.Order;

			if (Tag == PredefinedTextClassifierTags.ModulesWindowName)
				return StringComparer.CurrentCulture.Compare(x.Module.Name, y.Module.Name);
			else if (Tag == PredefinedTextClassifierTags.ModulesWindowPath)
				return StringComparer.CurrentCulture.Compare(x.Module.Filename, y.Module.Filename);
			else if (Tag == PredefinedTextClassifierTags.ModulesWindowOptimized)
				return Comparer<bool?>.Default.Compare(x.Module.IsOptimized, y.Module.IsOptimized);
			else if (Tag == PredefinedTextClassifierTags.ModulesWindowDynamic)
				return Comparer<bool?>.Default.Compare(x.Module.IsDynamic, y.Module.IsDynamic);
			else if (Tag == PredefinedTextClassifierTags.ModulesWindowInMemory)
				return Comparer<bool?>.Default.Compare(x.Module.IsInMemory, y.Module.IsInMemory);
			else if (Tag == PredefinedTextClassifierTags.ModulesWindowOrder)
				return Comparer<int>.Default.Compare(x.Module.Order, y.Module.Order);
			else if (Tag == PredefinedTextClassifierTags.ModulesWindowVersion)
				return StringComparer.CurrentCulture.Compare(x.Module.Version, y.Module.Version);
			else if (Tag == PredefinedTextClassifierTags.ModulesWindowTimestamp)
				return Comparer<DateTime?>.Default.Compare(x.Module.Timestamp, y.Module.Timestamp);
			else if (Tag == PredefinedTextClassifierTags.ModulesWindowAddress)
				return Comparer<ulong>.Default.Compare(x.Module.Address, y.Module.Address);
			else if (Tag == PredefinedTextClassifierTags.ModulesWindowProcess)
				return Comparer<ulong>.Default.Compare(x.Module.Process.Id, y.Module.Process.Id);
			else if (Tag == PredefinedTextClassifierTags.ModulesWindowAppDomain)
				return Comparer<int?>.Default.Compare(x.Module.AppDomain?.Id, y.Module.AppDomain?.Id);
			else
				Debug.Fail($"Unknown module property: {Tag}");

			return 0;
		}
	}
}

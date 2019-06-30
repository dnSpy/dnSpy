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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger.Breakpoints.Modules;
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Debugger.ToolWindows.ModuleBreakpoints {
	[Export(typeof(ModuleBreakpointFormatterProvider))]
	sealed class ModuleBreakpointFormatterProvider {
		public ModuleBreakpointFormatter Create() =>
			ModuleBreakpointFormatter.Create_DONT_USE();
	}

	sealed class ModuleBreakpointFormatter {
		ModuleBreakpointFormatter() { }

		internal static ModuleBreakpointFormatter Create_DONT_USE() => new ModuleBreakpointFormatter();

		void WriteInt32Decimal(IDbgTextWriter output, int? value) {
			if (value is null)
				return;
			output.Write(DbgTextColor.Number, value.Value.ToString());
		}

		void WriteBoolean(IDbgTextWriter output, bool? value) {
			if (value is null)
				return;
			output.WriteYesNo(value.Value);
		}

		internal void WriteIsEnabled(IDbgTextWriter output, DbgModuleBreakpoint bp) => WriteBoolean(output, bp.IsEnabled);
		internal void WriteModuleName(IDbgTextWriter output, DbgModuleBreakpoint bp) => output.Write(DbgTextColor.String, bp.ModuleName ?? string.Empty);
		internal void WriteDynamic(IDbgTextWriter output, DbgModuleBreakpoint bp) => WriteBoolean(output, bp.IsDynamic);
		internal void WriteInMemory(IDbgTextWriter output, DbgModuleBreakpoint bp) => WriteBoolean(output, bp.IsInMemory);
		internal void WriteLoadModule(IDbgTextWriter output, DbgModuleBreakpoint bp) => WriteBoolean(output, bp.IsLoaded);
		internal void WriteOrder(IDbgTextWriter output, DbgModuleBreakpoint bp) => WriteInt32Decimal(output, bp.Order);
		internal void WriteProcessName(IDbgTextWriter output, DbgModuleBreakpoint bp) => output.Write(DbgTextColor.String, bp.ProcessName ?? string.Empty);
		internal void WriteAppDomainName(IDbgTextWriter output, DbgModuleBreakpoint bp) => output.Write(DbgTextColor.String, bp.AppDomainName ?? string.Empty);
	}
}

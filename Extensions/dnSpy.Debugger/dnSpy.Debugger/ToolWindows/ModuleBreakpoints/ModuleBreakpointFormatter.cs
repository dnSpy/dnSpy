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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger.Breakpoints.Modules;
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.ToolWindows.ModuleBreakpoints {
	[Export(typeof(ModuleBreakpointFormatterProvider))]
	sealed class ModuleBreakpointFormatterProvider {
		public ModuleBreakpointFormatter Create() =>
			ModuleBreakpointFormatter.Create_DONT_USE();
	}

	sealed class ModuleBreakpointFormatter {
		ModuleBreakpointFormatter() { }

		internal static ModuleBreakpointFormatter Create_DONT_USE() => new ModuleBreakpointFormatter();

		void WriteInt32Decimal(ITextColorWriter output, int? value) {
			if (value == null)
				return;
			output.Write(BoxedTextColor.Number, value.Value.ToString());
		}

		void WriteBoolean(ITextColorWriter output, bool? value) {
			if (value == null)
				return;
			output.WriteYesNo(value.Value);
		}

		internal void WriteIsEnabled(ITextColorWriter output, DbgModuleBreakpoint bp) => WriteBoolean(output, bp.IsEnabled);
		internal void WriteModuleName(ITextColorWriter output, DbgModuleBreakpoint bp) => output.Write(BoxedTextColor.String, bp.ModuleName ?? string.Empty);
		internal void WriteDynamic(ITextColorWriter output, DbgModuleBreakpoint bp) => WriteBoolean(output, bp.IsDynamic);
		internal void WriteInMemory(ITextColorWriter output, DbgModuleBreakpoint bp) => WriteBoolean(output, bp.IsInMemory);
		internal void WriteOrder(ITextColorWriter output, DbgModuleBreakpoint bp) => WriteInt32Decimal(output, bp.Order);
		internal void WriteAppDomainName(ITextColorWriter output, DbgModuleBreakpoint bp) => output.Write(BoxedTextColor.String, bp.AppDomainName ?? string.Empty);
		internal void WriteProcessName(ITextColorWriter output, DbgModuleBreakpoint bp) => output.Write(BoxedTextColor.String, bp.ProcessName ?? string.Empty);
	}
}

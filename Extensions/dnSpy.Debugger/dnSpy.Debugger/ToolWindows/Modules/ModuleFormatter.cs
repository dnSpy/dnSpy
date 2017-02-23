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
using System.Globalization;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.Modules {
	[Export(typeof(ModuleFormatterProvider))]
	sealed class ModuleFormatterProvider {
		readonly DebuggerSettings debuggerSettings;

		[ImportingConstructor]
		ModuleFormatterProvider(DebuggerSettings debuggerSettings) => this.debuggerSettings = debuggerSettings;

		public ModuleFormatter Create() =>
			ModuleFormatter.Create_DONT_USE(debuggerSettings.UseHexadecimal);
	}

	sealed class ModuleFormatter {
		readonly bool useHex;

		ModuleFormatter(bool useHex) => this.useHex = useHex;

		internal static ModuleFormatter Create_DONT_USE(bool useHex) => new ModuleFormatter(useHex);

		void WriteFilename(ITextColorWriter output, DbgModule module, string filename) {
			if (module.IsDynamic || module.IsInMemory)
				filename = FormatterUtils.FilterName(filename, 300);
			output.WriteFilename(filename);
		}

		public void WriteName(ITextColorWriter output, DbgModule module) => WriteFilename(output, module, PathUtils.GetFilename(module.Name));
		public void WritePath(ITextColorWriter output, DbgModule module) => WriteFilename(output, module, module.Filename);
		public void WriteOptimized(ITextColorWriter output, DbgModule module) => output.WriteYesNoError(module.IsOptimized);
		public void WriteDynamic(ITextColorWriter output, DbgModule module) => output.WriteYesNo(module.IsDynamic);
		public void WriteInMemory(ITextColorWriter output, DbgModule module) => output.WriteYesNo(module.IsInMemory);
		public void WriteProcess(ITextColorWriter output, DbgModule module) => output.Write(module.Process, useHex);
		public void WriteAppDomain(ITextColorWriter output, DbgModule module) => output.Write(module.AppDomain);

		// Order is always in decimal (same as VS)
		public void WriteOrder(ITextColorWriter output, DbgModule module) => output.Write(BoxedTextColor.Number, module.Order.ToString());

		public void WriteVersion(ITextColorWriter output, DbgModule module) {
			var versionString = module.Version;
			if (versionString != null)
				output.Write(BoxedTextColor.Text, versionString);
		}

		public void WriteTimestamp(ITextColorWriter output, DbgModule module) {
			var date = module.Timestamp;
			if (date != null) {
				var dateString = date.Value.ToString(CultureInfo.CurrentUICulture.DateTimeFormat);
				output.Write(BoxedTextColor.Text, dateString);
			}
		}

		public void WriteAddress(ITextColorWriter output, DbgModule module) {
			ulong addr = module.Address;
			ulong endAddr = addr + module.Size;
			if (!module.HasAddress)
				output.Write(BoxedTextColor.Text, dnSpy_Debugger_Resources.Module_NoAddress);
			else {
				WriteAddress(output, module, addr);
				output.Write(BoxedTextColor.Operator, "-");
				WriteAddress(output, module, endAddr);
			}
		}

		void WriteAddress(ITextColorWriter output, DbgModule module, ulong addr) {
			// Addresses are always in hex
			if (module.Process.Bitness == 32)
				output.Write(BoxedTextColor.Number, string.Format("{0:X8}", addr));
			else
				output.Write(BoxedTextColor.Number, string.Format("{0:X16}", addr));
		}
	}
}

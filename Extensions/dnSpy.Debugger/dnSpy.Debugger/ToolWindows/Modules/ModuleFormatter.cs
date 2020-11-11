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
using System.Globalization;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Debugger.Text.DnSpy;
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

		void WriteFilename(IDbgTextWriter output, DbgModule module, string filename) {
			if (module.IsDynamic || module.IsInMemory)
				filename = FormatterUtils.FilterName(filename, 300);
			new DbgTextColorWriter(output).WriteFilename(filename);
		}

		public void WriteName(IDbgTextWriter output, DbgModule module) => WriteFilename(output, module, module.Name);
		public void WritePath(IDbgTextWriter output, DbgModule module) => WriteFilename(output, module, module.Filename);
		public void WriteOptimized(IDbgTextWriter output, DbgModule module) => output.WriteYesNoOrNA(module.IsOptimized);
		public void WriteDynamic(IDbgTextWriter output, DbgModule module) => output.WriteYesNo(module.IsDynamic);
		public void WriteInMemory(IDbgTextWriter output, DbgModule module) => output.WriteYesNo(module.IsInMemory);
		public void WriteProcess(IDbgTextWriter output, DbgModule module) => output.Write(module.Process, useHex);
		public void WriteAppDomain(IDbgTextWriter output, DbgModule module) => output.Write(module.AppDomain);

		// Order is always in decimal (same as VS)
		public void WriteOrder(IDbgTextWriter output, DbgModule module) => output.Write(DbgTextColor.Number, module.Order.ToString());

		public void WriteVersion(IDbgTextWriter output, DbgModule module) {
			var versionString = module.Version;
			if (versionString is not null) {
				const int MAX_VER_LEN = 100;
				if (versionString.Length <= MAX_VER_LEN)
					output.Write(DbgTextColor.Text, versionString);
				else
					output.Write(DbgTextColor.Text, versionString.Substring(0, MAX_VER_LEN) + "[...]");
			}
		}

		public void WriteTimestamp(IDbgTextWriter output, DbgModule module) {
			var date = module.Timestamp;
			if (date is not null) {
				var dateString = date.Value.ToLocalTime().ToString(CultureInfo.CurrentUICulture.DateTimeFormat);
				output.Write(DbgTextColor.Text, dateString);
			}
			else
				output.Write(DbgTextColor.Error, dnSpy_Debugger_Resources.UnknownValue);
		}

		public void WriteAddress(IDbgTextWriter output, DbgModule module) {
			ulong addr = module.Address;
			ulong endAddr = addr + module.Size;
			if (!module.HasAddress)
				output.Write(DbgTextColor.Text, dnSpy_Debugger_Resources.Module_NoAddress);
			else {
				WriteAddress(output, module, addr);
				output.Write(DbgTextColor.Operator, "-");
				WriteAddress(output, module, endAddr);
			}
		}

		void WriteAddress(IDbgTextWriter output, DbgModule module, ulong addr) {
			// Addresses are always in hex
			if (module.Process.Bitness == 32)
				output.Write(DbgTextColor.Number, addr.ToString("X8"));
			else
				output.Write(DbgTextColor.Number, addr.ToString("X16"));
		}
	}
}

/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.Globalization;
using dndbg.Engine;
using dnSpy.Contracts.Highlighting;
using dnSpy.Debugger.Properties;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Highlighting;

namespace dnSpy.Debugger.Modules {
	sealed class ModulePrinter {
		readonly ISyntaxHighlightOutput output;
		readonly bool useHex;
		readonly DnDebugger dbg;

		public ModulePrinter(ISyntaxHighlightOutput output, bool useHex, DnDebugger dbg) {
			this.output = output;
			this.useHex = useHex;
			this.dbg = dbg;
		}

		void WriteFilename(ModuleVM vm, string filename) {
			if (vm.Module.IsDynamic || vm.Module.IsInMemory)
				filename = DebugOutputUtils.FilterName(filename, 300);
			output.WriteFilename(filename);
		}

		public void WriteName(ModuleVM vm) {
			WriteFilename(vm, DebugOutputUtils.GetFilename(vm.Module.Name));
		}

		public void WritePath(ModuleVM vm) {
			WriteFilename(vm, vm.Module.Name);
		}

		public void WriteOptimized(ModuleVM vm) {
			output.WriteYesNo(vm.IsOptimized);
		}

		public void WriteDynamic(ModuleVM vm) {
			output.WriteYesNo(vm.Module.IsDynamic);
		}

		public void WriteInMemory(ModuleVM vm) {
			output.WriteYesNo(vm.Module.IsInMemory);
		}

		public void WriteOrder(ModuleVM vm) {
			output.Write(string.Format("{0}", vm.Module.UniqueId), TextTokenKind.Number);
		}

		public void WriteVersion(ModuleVM vm) {
			if (vm.Version != null)
				output.Write(vm.Version);
		}

		public void WriteTimestamp(ModuleVM vm) {
			var ts = vm.Timestamp;
			if (ts != null) {
				var date = Epoch.AddSeconds(ts.Value);
				var dateString = date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat);
				output.Write(dateString, TextTokenKind.Text);
			}
		}
		static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public void WriteAddress(ModuleVM vm) {
			ulong addr = vm.Module.Address;
			ulong endAddr = addr + vm.Module.Size;
			if (addr == 0)
				output.Write(dnSpy_Debugger_Resources.Module_NoAddress, TextTokenKind.Text);
			else {
				WriteAddress(addr);
				output.Write("-", TextTokenKind.Operator);
				WriteAddress(endAddr);
			}
		}

		void WriteAddress(ulong addr) {
			if (IntPtr.Size == 4)
				output.Write(string.Format("{0:X8}", addr), TextTokenKind.Number);
			else
				output.Write(string.Format("{0:X16}", addr), TextTokenKind.Number);
		}

		public void WriteProcess(ModuleVM vm) {
			output.Write(vm.Module.Process, useHex);
		}

		public void WriteAppDomain(ModuleVM vm) {
			output.Write(vm.Module.AppDomain.CorAppDomain, dbg);
		}
	}
}

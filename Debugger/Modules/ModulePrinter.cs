/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;

namespace dnSpy.Debugger.Modules {
	sealed class ModulePrinter {
		readonly ITextOutput output;
		readonly bool useHex;

		public ModulePrinter(ITextOutput output, bool useHex) {
			this.output = output;
			this.useHex = useHex;
		}

		void WriteFilename(ModuleVM vm, string filename) {
			if (vm.Module.IsDynamic || vm.Module.IsInMemory)
				filename = DebugOutputUtils.FilterName(filename, 300);
			output.WriteFilename_OLD(filename);
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
			output.Write(string.Format("{0}", vm.Module.ModuleOrder), TextTokenType.Number);
		}

		public void WriteVersion(ModuleVM vm) {
			if (vm.Version != null)
				output.Write_OLD(vm.Version);
		}

		public void WriteTimestamp(ModuleVM vm) {
			var ts = vm.Timestamp;
			if (ts != null) {
				var date = Epoch.AddSeconds(ts.Value);
				var dateString = date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat);
				output.Write(dateString, TextTokenType.Text);
			}
		}
		static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public void WriteAddress(ModuleVM vm) {
			ulong addr = vm.Module.Address;
			ulong endAddr = addr + vm.Module.Size;
			if (addr == 0)
				output.Write("<no address>", TextTokenType.Text);
			else {
				WriteAddress(addr);
				output.Write("-", TextTokenType.Operator);
				WriteAddress(endAddr);
			}
		}

		void WriteAddress(ulong addr) {
			if (IntPtr.Size == 4)
				output.Write(string.Format("{0:X8}", addr), TextTokenType.Number);
			else
				output.Write(string.Format("{0:X16}", addr), TextTokenType.Number);
		}

		public void WriteProcess(ModuleVM vm) {
			output.Write(vm.Module.Process, useHex);
		}

		public void WriteAppDomain(ModuleVM vm) {
			output.Write(vm.Module.AppDomain.CorAppDomain);
		}
	}
}

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

using System.Diagnostics;
using dndbg.Engine;
using dnlib.PE;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Dialogs_OLD {
	sealed class ProcessPrinter {
		readonly ITextColorWriter output;
		readonly bool useHex;

		public ProcessPrinter(ITextColorWriter output, bool useHex) {
			this.output = output;
			this.useHex = useHex;
		}

		void WriteFilename(ProcessVM vm, string filename) => output.WriteFilename(filename);
		public void WriteFilename(ProcessVM vm) => WriteFilename(vm, DebugOutputUtils.GetFilename(vm.FullPath));
		public void WriteFullPath(ProcessVM vm) => WriteFilename(vm, vm.FullPath);
		public void WriteCLRVersion(ProcessVM vm) => output.Write(BoxedTextColor.Number, vm.CLRVersion);
		public void WriteType(ProcessVM vm) => output.Write(BoxedTextColor.EnumField, TypeToString(vm.CLRTypeInfo.CLRType));
		public void WriteMachine(ProcessVM vm) => output.Write(BoxedTextColor.InstanceMethod, ToString(vm.Machine));
		public void WriteTitle(ProcessVM vm) => output.Write(BoxedTextColor.String, vm.Title);

		public void WritePID(ProcessVM vm) {
			if (useHex)
				output.Write(BoxedTextColor.Number, string.Format("0x{0:X8}", vm.PID));
			else
				output.Write(BoxedTextColor.Number, string.Format("{0}", vm.PID));
		}

		static string TypeToString(CLRType type) {
			switch (type) {
			case CLRType.Desktop:	return dnSpy_Debugger_Resources.Process_CLR_Desktop;
			case CLRType.CoreCLR:	return dnSpy_Debugger_Resources.Process_CLR_CoreCLR;
			default:
				Debug.Fail("Unknown CLR type");
				return "???";
			}
		}

		static string ToString(Machine machine) {
			switch (machine) {
			case Machine.I386:		return "x86";
			case Machine.AMD64:		return "x64";
			default:				return machine.ToString();
			}
		}
	}
}

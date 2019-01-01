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
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Debugger.Text.DnSpy;
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.Dialogs.AttachToProcess {
	[Export(typeof(ProgramFormatterProvider))]
	sealed class ProgramFormatterProvider {
		public ProgramFormatter Create() => ProgramFormatter.Create_DONT_USE();
	}

	sealed class ProgramFormatter {
		ProgramFormatter() { }
		internal static ProgramFormatter Create_DONT_USE() => new ProgramFormatter();

		public void WriteProcess(IDbgTextWriter output, ProgramVM vm) => new DbgTextColorWriter(output).WriteFilename(vm.Name);
		public void WritePid(IDbgTextWriter output, ProgramVM vm) => output.Write(DbgTextColor.Number, vm.Id.ToString());
		public void WriteTitle(IDbgTextWriter output, ProgramVM vm) => output.Write(DbgTextColor.String, vm.Title);
		public void WriteType(IDbgTextWriter output, ProgramVM vm) => output.Write(DbgTextColor.Text, vm.RuntimeName);
		public void WriteMachine(IDbgTextWriter output, ProgramVM vm) => output.Write(DbgTextColor.Text, vm.Architecture);
		public void WritePath(IDbgTextWriter output, ProgramVM vm) => new DbgTextColorWriter(output).WriteFilename(vm.Filename);
		public void WriteCommandLine(IDbgTextWriter output, ProgramVM vm) => output.Write(DbgTextColor.Text, vm.CommandLine);
	}
}

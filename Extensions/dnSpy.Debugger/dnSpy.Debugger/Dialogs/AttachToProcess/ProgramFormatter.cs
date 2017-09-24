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
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.Dialogs.AttachToProcess {
	[Export(typeof(ProgramFormatterProvider))]
	sealed class ProgramFormatterProvider {
		public ProgramFormatter Create() => ProgramFormatter.Create_DONT_USE();
	}

	sealed class ProgramFormatter {
		ProgramFormatter() { }
		internal static ProgramFormatter Create_DONT_USE() => new ProgramFormatter();

		public void WriteProcess(ITextColorWriter output, ProgramVM vm) => output.WriteFilename(vm.Name);
		public void WritePid(ITextColorWriter output, ProgramVM vm) => output.Write(BoxedTextColor.Number, vm.Id.ToString());
		public void WriteTitle(ITextColorWriter output, ProgramVM vm) => output.Write(BoxedTextColor.String, vm.Title);
		public void WriteType(ITextColorWriter output, ProgramVM vm) => output.Write(BoxedTextColor.Text, vm.RuntimeName);
		public void WriteMachine(ITextColorWriter output, ProgramVM vm) => output.Write(BoxedTextColor.InstanceMethod, vm.Architecture);
		public void WritePath(ITextColorWriter output, ProgramVM vm) => output.WriteFilename(vm.Filename);
		public void WriteCommandLine(ITextColorWriter output, ProgramVM vm) => output.Write(BoxedTextColor.Text, vm.CommandLine);
	}
}

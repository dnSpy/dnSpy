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

using System;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.Processes {
	[Export(typeof(ProcessFormatterProvider))]
	sealed class ProcessFormatterProvider {
		readonly DebuggerSettings debuggerSettings;

		[ImportingConstructor]
		ProcessFormatterProvider(DebuggerSettings debuggerSettings) => this.debuggerSettings = debuggerSettings;

		public ProcessFormatter Create() =>
			ProcessFormatter.Create_DONT_USE(debuggerSettings.UseHexadecimal);
	}

	sealed class ProcessFormatter {
		readonly bool useHex;

		ProcessFormatter(bool useHex) => this.useHex = useHex;

		internal static ProcessFormatter Create_DONT_USE(bool useHex) => new ProcessFormatter(useHex);

		public void WriteImage(ITextColorWriter output, ProcessVM vm) {
			if (vm.IsSelectedProcess)
				output.Write(BoxedTextColor.Text, ">");
		}

		public void WriteName(ITextColorWriter output, DbgProcess process) => output.WriteFilename(PathUtils.GetFilename(process.Filename));
		public void WriteTitle(ITextColorWriter output, ProcessVM vm) => output.Write(BoxedTextColor.String, vm.Title);
		public void WriteState(ITextColorWriter output, DbgProcess process) => output.Write(BoxedTextColor.EnumField, GetStateText(process.State));
		public void WriteDebugging(ITextColorWriter output, DbgProcess process) => output.Write(BoxedTextColor.Text, process.Debugging);
		public void WritePath(ITextColorWriter output, DbgProcess process) => output.WriteFilename(process.Filename);

		public void WriteId(ITextColorWriter output, DbgProcess process) {
			if (useHex) {
				// Lower case like in VS
				output.Write(BoxedTextColor.Number, "0x" + process.Id.ToString("x8"));
			}
			else
				output.Write(BoxedTextColor.Number, process.Id.ToString());
		}

		static string GetStateText(DbgProcessState state) {
			switch (state) {
			case DbgProcessState.Running:	return dnSpy_Debugger_Resources.Process_Running;
			case DbgProcessState.Paused:	return dnSpy_Debugger_Resources.Process_Paused;
			case DbgProcessState.Terminated:return string.Empty;// The user will never see this string so there's no need to show it, let alone localize it
			default: throw new ArgumentOutOfRangeException(nameof(state));
			}
		}
	}
}

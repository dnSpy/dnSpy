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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Debugger.ToolWindows.CallStack {
	[Export(typeof(CallStackFormatterProvider))]
	sealed class CallStackFormatterProvider {
		readonly DebuggerSettings debuggerSettings;

		[ImportingConstructor]
		CallStackFormatterProvider(DebuggerSettings debuggerSettings) => this.debuggerSettings = debuggerSettings;

		public CallStackFormatter Create() =>
			CallStackFormatter.Create_DONT_USE(debuggerSettings.UseHexadecimal);
	}

	sealed class CallStackFormatter {
		readonly bool useHex;

		CallStackFormatter(bool useHex) => this.useHex = useHex;

		internal static CallStackFormatter Create_DONT_USE(bool useHex) => new CallStackFormatter(useHex);

		public void WriteImage(IDbgTextWriter output, StackFrameVM vm) {
			if (vm.IsActive)
				output.Write(DbgTextColor.Text, ">");
			else
				output.Write(DbgTextColor.Text, " ");
		}

		public void WriteName(IDbgTextWriter output, StackFrameVM vm) => vm.CachedOutput.WriteTo(output);
	}
}

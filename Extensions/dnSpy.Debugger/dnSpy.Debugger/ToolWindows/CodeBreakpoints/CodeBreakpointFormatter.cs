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

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Debugger.ToolWindows.CodeBreakpoints {
	[Export(typeof(CodeBreakpointFormatterProvider))]
	sealed class CodeBreakpointFormatterProvider {
		public CodeBreakpointFormatter Create() =>
			CodeBreakpointFormatter.Create_DONT_USE();
	}

	sealed class CodeBreakpointFormatter {
		CodeBreakpointFormatter() { }

		internal static CodeBreakpointFormatter Create_DONT_USE() => new CodeBreakpointFormatter();

		public const char LabelsSeparatorChar = ',';
		static readonly string LabelsSeparatorString = LabelsSeparatorChar.ToString();

		internal void WriteLabels(IDbgTextWriter output, CodeBreakpointVM vm) {
			bool needSep = false;
			foreach (var label in vm.CodeBreakpoint.Labels ?? emptyLabels) {
				if (needSep) {
					output.Write(DbgTextColor.Text, LabelsSeparatorString);
					output.Write(DbgTextColor.Text, " ");
				}
				needSep = true;
				output.Write(DbgTextColor.Text, label);
			}
		}
		static readonly ReadOnlyCollection<string> emptyLabels = new ReadOnlyCollection<string>(Array.Empty<string>());

		internal void WriteName(IDbgTextWriter output, CodeBreakpointVM vm) => vm.BreakpointLocationFormatter.WriteName(output, vm.Context.BreakpointLocationFormatterOptions);
		internal void WriteCondition(IDbgTextWriter output, CodeBreakpointVM vm) => vm.Context.BreakpointConditionsFormatter.Write(output, vm.CodeBreakpoint.Condition);
		internal void WriteHitCount(IDbgTextWriter output, CodeBreakpointVM vm) => vm.Context.BreakpointConditionsFormatter.Write(output, vm.CodeBreakpoint.HitCount, vm.Context.DbgCodeBreakpointHitCountService.GetHitCount(vm.CodeBreakpoint));
		internal void WriteFilter(IDbgTextWriter output, CodeBreakpointVM vm) => vm.Context.BreakpointConditionsFormatter.Write(output, vm.CodeBreakpoint.Filter);
		internal void WriteWhenHit(IDbgTextWriter output, CodeBreakpointVM vm) => vm.Context.BreakpointConditionsFormatter.Write(output, vm.CodeBreakpoint.Trace);
		internal void WriteModule(IDbgTextWriter output, CodeBreakpointVM vm) => vm.BreakpointLocationFormatter.WriteModule(output);
	}
}

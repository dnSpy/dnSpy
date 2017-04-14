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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Text;

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

		internal void WriteLabels(ITextColorWriter output, CodeBreakpointVM vm) {
			bool needSep = false;
			foreach (var label in vm.CodeBreakpoint.Labels ?? emptyLabels) {
				if (needSep) {
					output.Write(BoxedTextColor.Text, LabelsSeparatorString);
					output.WriteSpace();
				}
				needSep = true;
				output.Write(BoxedTextColor.Text, label);
			}
		}
		static readonly ReadOnlyCollection<string> emptyLabels = new ReadOnlyCollection<string>(Array.Empty<string>());

		internal void WriteName(ITextColorWriter output, CodeBreakpointVM vm) => vm.BreakpointLocationFormatter.WriteName(output);
		internal void WriteCondition(ITextColorWriter output, CodeBreakpointVM vm) => vm.Context.BreakpointConditionsFormatter.Write(output, vm.CodeBreakpoint.Condition);
		internal void WriteHitCount(ITextColorWriter output, CodeBreakpointVM vm) => vm.Context.BreakpointConditionsFormatter.Write(output, vm.CodeBreakpoint.HitCount);
		internal void WriteFilter(ITextColorWriter output, CodeBreakpointVM vm) => vm.Context.BreakpointConditionsFormatter.Write(output, vm.CodeBreakpoint.Filter);
		internal void WriteWhenHit(ITextColorWriter output, CodeBreakpointVM vm) => vm.Context.BreakpointConditionsFormatter.Write(output, vm.CodeBreakpoint.Trace);
		internal void WriteModule(ITextColorWriter output, CodeBreakpointVM vm) => vm.BreakpointLocationFormatter.WriteModule(output);
	}
}

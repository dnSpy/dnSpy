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
using System.Collections.Generic;
using System.Linq;
using dnSpy.Contracts.Text;
using dnSpy.Scripting.Roslyn.Properties;

namespace dnSpy.Scripting.Roslyn.Common {
	sealed class HelpCommand : IScriptCommand {
		const int LEFT_COL_LEN = 20;

		static readonly (string shortcut, string help)[] keyboardShortcuts = new (string, string)[] {
			(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyEnter, dnSpy_Scripting_Roslyn_Resources.HelpEnter),
			(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyCtrlEnter, dnSpy_Scripting_Roslyn_Resources.HelpCtrlEnter),
			(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyShiftEnter, dnSpy_Scripting_Roslyn_Resources.HelpShiftEnter),
			(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyEscape, dnSpy_Scripting_Roslyn_Resources.HelpEscape),
			(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyAltUp, dnSpy_Scripting_Roslyn_Resources.HelpAltUp),
			(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyAltDown, dnSpy_Scripting_Roslyn_Resources.HelpAltDown),
			(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyCtrlAltUp, dnSpy_Scripting_Roslyn_Resources.HelpCtrlAltUp),
			(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyCtrlAltDown, dnSpy_Scripting_Roslyn_Resources.HelpCtrlAltDown),
			(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyCtrlA, dnSpy_Scripting_Roslyn_Resources.HelpCtrlA),
		};
		static readonly (string directive, string help)[] scriptDirectives = new (string, string)[] {
			("#r", dnSpy_Scripting_Roslyn_Resources.HelpScriptDirective_r),
			("#load", dnSpy_Scripting_Roslyn_Resources.HelpScriptDirective_load),
		};

		public IEnumerable<string> Names {
			get { yield return "help"; }
		}

		public string ShortDescription => dnSpy_Scripting_Roslyn_Resources.HelpHelpDescription;

		public void Execute(ScriptControlVM vm, string[] args) {
			vm.ReplEditor.OutputPrintLine(dnSpy_Scripting_Roslyn_Resources.HelpKeyboardShortcuts, BoxedTextColor.ReplOutputText);
			Print(vm, keyboardShortcuts, BoxedTextColor.PreprocessorKeyword, BoxedTextColor.ReplOutputText);
			vm.ReplEditor.OutputPrintLine(dnSpy_Scripting_Roslyn_Resources.HelpReplCommands, BoxedTextColor.ReplOutputText);
			PrintCommands(vm, BoxedTextColor.PreprocessorKeyword, BoxedTextColor.ReplOutputText);
			vm.ReplEditor.OutputPrintLine(dnSpy_Scripting_Roslyn_Resources.HelpScriptDirectives, BoxedTextColor.ReplOutputText);
			Print(vm, scriptDirectives, BoxedTextColor.PreprocessorKeyword, BoxedTextColor.ReplOutputText);
		}

		void Print(ScriptControlVM vm, IEnumerable<(string cmd, string help)> descs, object color1, object color2) {
			foreach (var t in descs) {
				vm.ReplEditor.OutputPrint("  ", BoxedTextColor.ReplOutputText);
				vm.ReplEditor.OutputPrint(t.cmd, color1);
				int len = LEFT_COL_LEN - t.cmd.Length;
				if (len > 0)
					vm.ReplEditor.OutputPrint(new string(' ', len), BoxedTextColor.ReplOutputText);
				vm.ReplEditor.OutputPrint(" ", BoxedTextColor.ReplOutputText);
				vm.ReplEditor.OutputPrint(t.help, color2);
				vm.ReplEditor.OutputPrintLine(string.Empty, BoxedTextColor.ReplOutputText);
			}
		}

		void PrintCommands(ScriptControlVM vm, object color1, object color2) {
			const string CMDS_SEP = ", ";
			var hash = new HashSet<IScriptCommand>(vm.ScriptCommands);
			var cmds = hash.Select(a => Tuple.Create(a.Names.Select(b => ScriptControlVM.CMD_PREFIX + b).ToArray(), a.ShortDescription))
						.OrderBy(a => a.Item1[0], StringComparer.OrdinalIgnoreCase);
			foreach (var t in cmds) {
				vm.ReplEditor.OutputPrint("  ", BoxedTextColor.ReplOutputText);
				int cmdsLen = t.Item1.Sum(a => a.Length) + CMDS_SEP.Length * (t.Item1.Length - 1);
				for (int i = 0; i < t.Item1.Length; i++) {
					if (i > 0)
						vm.ReplEditor.OutputPrint(", ", BoxedTextColor.ReplOutputText);
					vm.ReplEditor.OutputPrint(t.Item1[i], color1);
				}
				int len = LEFT_COL_LEN - cmdsLen;
				if (len > 0)
					vm.ReplEditor.OutputPrint(new string(' ', len), BoxedTextColor.ReplOutputText);
				vm.ReplEditor.OutputPrint(" ", BoxedTextColor.ReplOutputText);
				vm.ReplEditor.OutputPrint(t.Item2, color2);
				vm.ReplEditor.OutputPrintLine(string.Empty, BoxedTextColor.ReplOutputText);
			}
		}
	}
}

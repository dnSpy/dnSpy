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
using System.Collections.Generic;
using System.Linq;
using dnSpy.Scripting.Roslyn.Properties;

namespace dnSpy.Scripting.Roslyn.Common {
	sealed class HelpCommand : IScriptCommand {
		const string NAMES_SEP = ", ";

		static readonly Tuple<string, string>[] keyboardShortcuts = new Tuple<string, string>[] {
			Tuple.Create(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyEnter, dnSpy_Scripting_Roslyn_Resources.HelpEnter),
			Tuple.Create(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyCtrlEnter, dnSpy_Scripting_Roslyn_Resources.HelpCtrlEnter),
			Tuple.Create(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyShiftEnter, dnSpy_Scripting_Roslyn_Resources.HelpShiftEnter),
			Tuple.Create(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyEscape, dnSpy_Scripting_Roslyn_Resources.HelpEscape),
			Tuple.Create(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyAltUp, dnSpy_Scripting_Roslyn_Resources.HelpAltUp),
			Tuple.Create(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyAltDown, dnSpy_Scripting_Roslyn_Resources.HelpAltDown),
			Tuple.Create(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyCtrlAltUp, dnSpy_Scripting_Roslyn_Resources.HelpCtrlAltUp),
			Tuple.Create(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyCtrlAltDown, dnSpy_Scripting_Roslyn_Resources.HelpCtrlAltDown),
			Tuple.Create(dnSpy_Scripting_Roslyn_Resources.ShortCutKeyCtrlA, dnSpy_Scripting_Roslyn_Resources.HelpCtrlA),
		};
		static readonly Tuple<string, string>[] scriptDirectives = new Tuple<string, string>[] {
			Tuple.Create("#r", dnSpy_Scripting_Roslyn_Resources.HelpScriptDirective_r),
			Tuple.Create("#load", dnSpy_Scripting_Roslyn_Resources.HelpScriptDirective_load),
		};

		public IEnumerable<string> Names {
			get { yield return "help"; }
		}

		public string ShortDescription => dnSpy_Scripting_Roslyn_Resources.HelpHelpDescription;

		public void Execute(ScriptControlVM vm, string[] args) {
			vm.ReplEditor.OutputPrintLine(dnSpy_Scripting_Roslyn_Resources.HelpKeyboardShortcuts);
			Print(vm, keyboardShortcuts);
			vm.ReplEditor.OutputPrintLine(dnSpy_Scripting_Roslyn_Resources.HelpReplCommands);
			Print(vm, GetCommands(vm));
			vm.ReplEditor.OutputPrintLine(dnSpy_Scripting_Roslyn_Resources.HelpScriptDirectives);
			Print(vm, scriptDirectives);
		}

		void Print(ScriptControlVM vm, IEnumerable<Tuple<string, string>> descs) {
			foreach (var t in descs)
				vm.ReplEditor.OutputPrintLine(string.Format("  {0,-20} {1}", t.Item1, t.Item2));
		}

		IEnumerable<Tuple<string, string>> GetCommands(ScriptControlVM vm) {
			var hash = new HashSet<IScriptCommand>(vm.ScriptCommands);
			return hash.Select(a => Tuple.Create(string.Join(NAMES_SEP, a.Names.Select(b => ScriptControlVM.CMD_PREFIX + b).ToArray()), a.ShortDescription))
						.OrderBy(a => a.Item1, StringComparer.OrdinalIgnoreCase);
		}
	}
}

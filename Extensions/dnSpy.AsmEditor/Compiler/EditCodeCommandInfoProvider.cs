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

using System.Collections.Generic;
using System.Windows.Input;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.AsmEditor.Compiler {
	[ExportCommandInfoProvider(CommandConstants.CMDINFO_ORDER_EDITCODE)]
	sealed class EditCodeCommandInfoProvider : ICommandInfoProvider {
		public IEnumerable<CommandShortcut> GetCommandShortcuts(object target) {
			var textView = target as ITextView;
			if (textView?.Roles.Contains(PredefinedDnSpyTextViewRoles.CodeEditor) != true)
				yield break;

			yield return CommandShortcut.Control(Key.P, EditCodeIds.Compile.ToCommandInfo());
		}
	}
}

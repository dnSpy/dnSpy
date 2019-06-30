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

using System.Collections.Generic;
using System.Windows.Input;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Debugger.ToolWindows.TextView {
	[ExportCommandInfoProvider(CommandInfoProviderOrder.TextEditor - 4000)]
	sealed class DocumentViewerCommandInfoProvider : ICommandInfoProvider {
		public IEnumerable<CommandShortcut> GetCommandShortcuts(object target) {
			var textView = target as ITextView;
			if (textView?.Roles.Contains(PredefinedDsTextViewRoles.DocumentViewer) != true)
				yield break;

			yield return CommandShortcut.Create(KeyInput.CtrlAlt(Key.V), KeyInput.Create(Key.A), DebuggerToolWindowIds.ShowAutos.ToCommandInfo());

			yield return CommandShortcut.Create(KeyInput.CtrlAlt(Key.W), KeyInput.Create(Key.D1), DebuggerToolWindowIds.ShowWatch1.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.CtrlAlt(Key.W), KeyInput.Create(Key.D2), DebuggerToolWindowIds.ShowWatch2.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.CtrlAlt(Key.W), KeyInput.Create(Key.D3), DebuggerToolWindowIds.ShowWatch3.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.CtrlAlt(Key.W), KeyInput.Create(Key.D4), DebuggerToolWindowIds.ShowWatch4.ToCommandInfo());
		}
	}
}

/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Windows.Input;
using ICSharpCode.ILSpy;

namespace dnSpy.AsmEditor {
	sealed class TextEditorCommandProxy : ICommand {
		readonly IContextMenuEntry2 cmd;

		public TextEditorCommandProxy(IContextMenuEntry2 cmd) {
			this.cmd = cmd;
		}

		public event EventHandler CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		static ContextMenuEntryContext CreateContext() {
			return ContextMenuEntryContext.Create(MainWindow.Instance.ActiveTextView, true);
		}

		public bool CanExecute(object parameter) {
			var ctx = CreateContext();
			return cmd.IsVisible(ctx) && cmd.IsEnabled(ctx);
		}

		public void Execute(object parameter) {
			cmd.Execute(CreateContext());
		}
	}
}

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

namespace dnSpy.MVVM {
	abstract class ContextMenuEntryCommandProxy : ICommand {
		readonly IContextMenuEntry cmd;

		protected ContextMenuEntryCommandProxy(IContextMenuEntry cmd) {
			this.cmd = cmd;
		}

		protected abstract ContextMenuEntryContext CreateContext();

		public event EventHandler CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public bool CanExecute(object parameter) {
			var ctx = CreateContext();
			return ctx != null && cmd.IsVisible(ctx) && cmd.IsEnabled(ctx);
		}

		public void Execute(object parameter) {
			var ctx = CreateContext();
			if (ctx != null)
				cmd.Execute(ctx);
		}
	}
}

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

using System.Windows.Input;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.ToolBars;

namespace dnSpy.Shared.UI.ToolBars {
	public abstract class ToolBarButtonCommand : ToolBarButtonBase, ICommandHolder {
		public ICommand Command {
			get { return realCommand; }
		}
		readonly ICommand realCommand;

		protected ToolBarButtonCommand(ICommand realCommand) {
			this.realCommand = realCommand;
		}

		public override void Execute(IToolBarItemContext context) {
			realCommand.Execute(context);
		}

		public override bool IsEnabled(IToolBarItemContext context) {
			return realCommand.CanExecute(context);
		}
	}
}

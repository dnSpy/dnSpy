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

using System.Diagnostics;
using System.Windows.Input;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Menus;

namespace dnSpy.Shared.UI.Menus {
	public abstract class MenuItemCommand : MenuItemBase, ICommandHolder {
		public ICommand Command {
			get { return realCommand; }
		}
		readonly ICommand realCommand;

		protected MenuItemCommand(ICommand realCommand) {
			this.realCommand = realCommand;
		}

		public override void Execute(IMenuItemContext context) {
			realCommand.Execute(context);
		}

		public override bool IsEnabled(IMenuItemContext context) {
			return realCommand.CanExecute(context);
		}
	}

	public abstract class MenuItemCommand<TContext> : MenuItemBase<TContext>, ICommandHolder where TContext : class {
		public ICommand Command {
			get { return realCommand; }
		}
		readonly ICommand realCommand;

		protected MenuItemCommand(ICommand realCommand) {
			this.realCommand = realCommand;
		}

		public sealed override void Execute(TContext context) {
			Debug.Fail("MenuItemCommand.Execute() got called");
			realCommand.Execute(context);
		}

		public override bool IsEnabled(TContext context) {
			return realCommand.CanExecute(context);
		}
	}
}

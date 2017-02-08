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

using System.Windows.Input;
using dnSpy.Contracts.Command;

namespace dnSpy.Contracts.ToolBars {
	/// <summary>
	/// Toolbar button base class
	/// </summary>
	public abstract class ToolBarButtonCommand : ToolBarButtonBase, ICommandHolder {
		/// <summary>
		/// Gets the real command
		/// </summary>
		public ICommand Command { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="realCommand">Real command that gets executed the button is pressed</param>
		protected ToolBarButtonCommand(ICommand realCommand) {
			Command = realCommand;
		}

		/// <inheritdoc/>
		public override void Execute(IToolBarItemContext context) => Command.Execute(context);
		/// <inheritdoc/>
		public override bool IsEnabled(IToolBarItemContext context) => Command.CanExecute(context);
	}
}

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

using System;
using System.Windows.Input;

namespace dnSpy.Contracts.Command {
	/// <summary>
	/// Implements <see cref="ICommand"/> by using a <see cref="ICommandTarget"/> instance
	/// </summary>
	public sealed class CommandTargetCommand : ICommand {
		readonly ICommandTarget commandTarget;
		readonly Guid group;
		readonly int cmdId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="commandTarget">Command target</param>
		/// <param name="cmdId">Command ID</param>
		public CommandTargetCommand(ICommandTarget commandTarget, StandardIds cmdId)
			: this(commandTarget, CommandConstants.StandardGroup, (int)cmdId) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="commandTarget">Command target</param>
		/// <param name="cmdId">Command ID</param>
		public CommandTargetCommand(ICommandTarget commandTarget, TextEditorIds cmdId)
			: this(commandTarget, CommandConstants.TextEditorGroup, (int)cmdId) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="commandTarget">Command target</param>
		/// <param name="group">Command group, eg. <see cref="CommandConstants.StandardGroup"/></param>
		/// <param name="cmdId">Command ID</param>
		public CommandTargetCommand(ICommandTarget commandTarget, Guid group, int cmdId) {
			this.commandTarget = commandTarget ?? throw new ArgumentNullException(nameof(commandTarget));
			this.group = group;
			this.cmdId = cmdId;
		}

		event EventHandler ICommand.CanExecuteChanged {
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		bool ICommand.CanExecute(object parameter) => commandTarget.CanExecute(group, cmdId) == CommandTargetStatus.Handled;
		void ICommand.Execute(object parameter) => commandTarget.Execute(group, cmdId);
	}
}

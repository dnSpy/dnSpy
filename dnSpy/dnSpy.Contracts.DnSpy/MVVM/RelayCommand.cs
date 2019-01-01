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

namespace dnSpy.Contracts.MVVM {
	/// <summary>
	/// Implements the <see cref="ICommand"/> interface
	/// </summary>
	public sealed class RelayCommand : ICommand {
		readonly Action<object> exec;
		readonly Predicate<object> canExec;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="exec">Called when the command gets executed</param>
		/// <param name="canExec">Gets called to check whether <paramref name="exec"/> can execute,
		/// may be null</param>
		public RelayCommand(Action<object> exec, Predicate<object> canExec = null) {
			this.exec = exec ?? throw new ArgumentNullException(nameof(exec));
			this.canExec = canExec;
		}

		bool ICommand.CanExecute(object parameter) => canExec == null ? true : canExec(parameter);

		event EventHandler ICommand.CanExecuteChanged {
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		void ICommand.Execute(object parameter) => exec(parameter);
	}
}

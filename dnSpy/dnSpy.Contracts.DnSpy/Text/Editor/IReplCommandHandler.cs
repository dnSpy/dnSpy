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

using System.Threading;
using System.Threading.Tasks;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Gets notified by a <see cref="IReplEditor"/> instance
	/// </summary>
	interface IReplCommandHandler {
		/// <summary>
		/// Called by <see cref="IReplEditor"/> after enter has been pressed. Returns true if
		/// <paramref name="input"/> is a command. If false is returned, the user can enter more
		/// text.
		/// </summary>
		/// <param name="input">Current user input</param>
		/// <returns></returns>
		bool IsCommand(string input);

		/// <summary>
		/// Called after <see cref="IsCommand(string)"/> has returned true
		/// </summary>
		/// <param name="input">User input</param>
		void ExecuteCommand(string input);

		/// <summary>
		/// Called when a new command can be entered by the user
		/// </summary>
		void OnNewCommand();

		/// <summary>
		/// Called when the command gets modified by the user. Can be used to colorize the output.
		/// </summary>
		/// <param name="command">Current command</param>
		/// <param name="cancellationToken">Cancellation token</param>
		Task OnCommandUpdatedAsync(IReplCommandInput command, CancellationToken cancellationToken);
	}
}

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

namespace dnSpy.Contracts.Command {
	/// <summary>
	/// REPL command IDs
	/// </summary>
	public enum ReplIds {
		/// <summary>
		/// Copies only the code, but not the prompts or script output
		/// </summary>
		CopyCode,

		/// <summary>
		/// Submits current user input
		/// </summary>
		Submit,

		/// <summary>
		/// Adds a new line without submitting the current input
		/// </summary>
		NewLineDontSubmit,

		/// <summary>
		/// Clears the user input
		/// </summary>
		ClearInput,

		/// <summary>
		/// Clears the screen
		/// </summary>
		ClearScreen,

		/// <summary>
		/// Resets the REPL editor (but not the owner, eg. C# scripting state)
		/// </summary>
		Reset,

		/// <summary>
		/// Selects the previous command
		/// </summary>
		SelectPreviousCommand,

		/// <summary>
		/// Selects the next command
		/// </summary>
		SelectNextCommand,

		/// <summary>
		/// Selects the previous command matching the current input text
		/// </summary>
		SelectSameTextPreviousCommand,

		/// <summary>
		/// Selects the next command matching the current input text
		/// </summary>
		SelectSameTextNextCommand,
	}
}

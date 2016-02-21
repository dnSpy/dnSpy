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

namespace dnSpy.Contracts.TextEditor {
	/// <summary>
	/// A REPL (Read, Eval, Print, Loop) editor
	/// </summary>
	public interface IReplEditor {
		/// <summary>
		/// true if <see cref="Clear"/> can be called
		/// </summary>
		bool CanClear { get; }

		/// <summary>
		/// Clears the screen
		/// </summary>
		void Clear();

		/// <summary>
		/// true if <see cref="SelectPreviousCommand"/> can be called
		/// </summary>
		bool CanSelectPreviousCommand { get; }

		/// <summary>
		/// Selects the previous command
		/// </summary>
		void SelectPreviousCommand();

		/// <summary>
		/// true if <see cref="SelectNextCommand"/> can be called
		/// </summary>
		bool CanSelectNextCommand { get; }

		/// <summary>
		/// Selects the next command
		/// </summary>
		void SelectNextCommand();

		/// <summary>
		/// Adds script output. This method can be called from any thread
		/// </summary>
		/// <param name="text">Text</param>
		void OutputPrint(string text);

		/// <summary>
		/// Adds script output and a new line. This method can be called from any thread
		/// </summary>
		/// <param name="text">Text</param>
		void OutputPrintLine(string text);

		/// <summary>
		/// Gets notified by this instance
		/// </summary>
		IReplCommandHandler CommandHandler { get; set; }

		/// <summary>
		/// Called by <see cref="CommandHandler"/> after the command has finished executing
		/// </summary>
		void OnCommandExecuted();

		/// <summary>
		/// Resets the state to original executing state, but doesn't reset history or clears the screen
		/// </summary>
		void Reset();
	}
}

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

using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace dnSpy.Contracts.Text.Operations {
	/// <summary>
	/// <see cref="IReplEditor"/> operations
	/// </summary>
	interface IReplEditorOperations : IEditorOperations3 {
		/// <summary>
		/// Gets the REPL editor
		/// </summary>
		IReplEditor ReplEditor { get; }

		/// <summary>
		/// true if <see cref="CopyCode"/> can be called
		/// </summary>
		bool CanCopyCode { get; }

		/// <summary>
		/// Copies only the code, but not the prompts or script output
		/// </summary>
		void CopyCode();

		/// <summary>
		/// Submits current input
		/// </summary>
		/// <returns></returns>
		bool Submit();

		/// <summary>
		/// Adds a new line without submitting the current input
		/// </summary>
		/// <returns></returns>
		bool InsertNewLineDontSubmit();

		/// <summary>
		/// Clears user input
		/// </summary>
		void ClearInput();

		/// <summary>
		/// Clears the screen
		/// </summary>
		void ClearScreen();

		/// <summary>
		/// Resets the REPL editor
		/// </summary>
		void Reset();

		/// <summary>
		/// Selects the previous command
		/// </summary>
		void SelectPreviousCommand();

		/// <summary>
		/// Selects the next command
		/// </summary>
		void SelectNextCommand();

		/// <summary>
		/// true if <see cref="SelectPreviousCommand"/> can be called
		/// </summary>
		bool CanSelectPreviousCommand { get; }

		/// <summary>
		/// true if <see cref="SelectNextCommand"/> can be called
		/// </summary>
		bool CanSelectNextCommand { get; }

		/// <summary>
		/// Selects the previous command matching the current input text
		/// </summary>
		void SelectSameTextPreviousCommand();

		/// <summary>
		/// Selects the next command matching the current input text
		/// </summary>
		void SelectSameTextNextCommand();

		/// <summary>
		/// Adds user input
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="clearSearchText">true to clear search text</param>
		void AddUserInput(string text, bool clearSearchText = true);

		/// <summary>
		/// Adds user input
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="text">Text</param>
		/// <param name="clearSearchText">true to clear search text</param>
		void AddUserInput(Span span, string text, bool clearSearchText = true);
	}
}

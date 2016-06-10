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

using System;
using System.Collections.Generic;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Text.Editor.Operations;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// A REPL (Read, Eval, Print, Loop) editor
	/// </summary>
	public interface IReplEditor : IUIObjectProvider2, IDisposable {
		/// <summary>
		/// true if <see cref="ClearScreen"/> can be called
		/// </summary>
		bool CanClearScreen { get; }

		/// <summary>
		/// Clears the screen
		/// </summary>
		void ClearScreen();

		/// <summary>
		/// true if <see cref="CopyCode"/> can be called
		/// </summary>
		bool CanCopyCode { get; }

		/// <summary>
		/// Copies the selected code
		/// </summary>
		void CopyCode();

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
		/// <param name="color">Color</param>
		/// <param name="startOnNewLine">true to print the text on a new line</param>
		void OutputPrint(string text, object color, bool startOnNewLine = false);

		/// <summary>
		/// Adds script output. This method can be called from any thread
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="color">Color</param>
		/// <param name="startOnNewLine">true to print the text on a new line</param>
		void OutputPrint(string text, OutputColor color, bool startOnNewLine = false);

		/// <summary>
		/// Adds script output and a new line. This method can be called from any thread
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="color">Color</param>
		/// <param name="startOnNewLine">true to print the text on a new line</param>
		void OutputPrintLine(string text, object color, bool startOnNewLine = false);

		/// <summary>
		/// Adds script output and a new line. This method can be called from any thread
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="color">Color</param>
		/// <param name="startOnNewLine">true to print the text on a new line</param>
		void OutputPrintLine(string text, OutputColor color, bool startOnNewLine = false);

		/// <summary>
		/// Adds script output. This method can be called from any thread
		/// </summary>
		/// <param name="text">Text</param>
		void OutputPrint(IEnumerable<ColorAndText> text);

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

		/// <summary>
		/// Gets the REPL editor operations
		/// </summary>
		IReplEditorOperations ReplEditorOperations { get; }

		/// <summary>
		/// Gets the command target
		/// </summary>
		ICommandTargetCollection CommandTarget { get; }

		/// <summary>
		/// Gets the text view
		/// </summary>
		ITextView TextView { get; }
	}
}

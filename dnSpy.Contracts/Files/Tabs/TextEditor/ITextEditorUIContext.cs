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

using System;
using dnlib.DotNet;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Decompiler;

namespace dnSpy.Contracts.Files.Tabs.TextEditor {
	/// <summary>
	/// Text editor UI context
	/// </summary>
	public interface ITextEditorUIContext : IFileTabUIContext {
		/// <summary>
		/// Sets document to <paramref name="output"/>, which should be an <c>AvalonEditTextOutput</c>
		/// instance
		/// </summary>
		/// <param name="output">New document</param>
		/// <param name="newHighlighting">Highlighting to use or null</param>
		void SetOutput(ITextOutput output, IHighlightingDefinition newHighlighting);

		/// <summary>
		/// Shows a cancel button. Can be used when decompiling in another thread
		/// </summary>
		/// <param name="onCancel">Called if the user clicks the cancel button</param>
		/// <param name="msg">Message to show to the user</param>
		void ShowCancelButton(Action onCancel, string msg);

		/// <summary>
		/// Hides the cancel button shown by <see cref="ShowCancelButton(Action, string)"/>
		/// </summary>
		void HideCancelButton();

		/// <summary>
		/// Moves the caret to a reference, this can be a <see cref="CodeReferenceSegment"/>,
		/// or a <see cref="IMemberDef"/>. Anything else isn't currently supported.
		/// </summary>
		/// <param name="ref">Reference</param>
		void MoveCaretTo(object @ref);

		/// <summary>
		/// true if there's selected text
		/// </summary>
		bool HasSelectedText { get; }
	}
}

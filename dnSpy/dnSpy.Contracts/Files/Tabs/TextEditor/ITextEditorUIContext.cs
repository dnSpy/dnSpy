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
using dnlib.DotNet;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.Shared;
using ICSharpCode.AvalonEdit.Highlighting;

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
		/// <param name="highlighting">Highlighting to use or null</param>
		/// <param name="contentType">Content type or null</param>
		void SetOutput(ITextOutput output, IHighlightingDefinition highlighting, IContentType contentType);

		/// <summary>
		/// Adds data that is cleared each time <see cref="SetOutput(ITextOutput, IHighlightingDefinition, IContentType)"/>
		/// gets called.
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="data">Value</param>
		void AddOutputData(object key, object data);

		/// <summary>
		/// Returns data added by <see cref="AddOutputData(object, object)"/> or null if not found
		/// </summary>
		/// <param name="key">Key</param>
		/// <returns></returns>
		object GetOutputData(object key);

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
		/// Moves the caret to a reference, this can be a <see cref="CodeReference"/>,
		/// or a <see cref="IMemberDef"/>. Anything else isn't currently supported.
		/// </summary>
		/// <param name="ref">Reference</param>
		void MoveCaretTo(object @ref);

		/// <summary>
		/// true if there's selected text
		/// </summary>
		bool HasSelectedText { get; }

		/// <summary>
		/// Gets the current location
		/// </summary>
		TextEditorLocation Location { get; }

		/// <summary>
		/// Scrolls to a line and column
		/// </summary>
		/// <param name="line">Line, 0-based</param>
		/// <param name="column">Column, 0-based</param>
		void ScrollAndMoveCaretTo(int line, int column);

		/// <summary>
		/// Gets the selected reference or null
		/// </summary>
		object SelectedReference { get; }

		/// <summary>
		/// Gets the selected reference or null
		/// </summary>
		CodeReference SelectedCodeReference { get; }

		/// <summary>
		/// Returns all selected <see cref="CodeReference"/>s
		/// </summary>
		/// <returns></returns>
		IEnumerable<CodeReference> GetSelectedCodeReferences();

		/// <summary>
		/// Gets the references in the document
		/// </summary>
		IEnumerable<object> References { get; }

		/// <summary>
		/// Gets all code references starting from a certain location
		/// </summary>
		/// <param name="line">Line, 0-based</param>
		/// <param name="column">Column, 0-based</param>
		/// <returns></returns>
		IEnumerable<Tuple<CodeReference, TextEditorLocation>> GetCodeReferences(int line, int column);

		/// <summary>
		/// Saves current location relative to some reference in the code. Return value can be
		/// passed to <see cref="RestoreReferencePosition(object)"/>
		/// </summary>
		/// <returns></returns>
		object SaveReferencePosition();

		/// <summary>
		/// Restores location saved by <see cref="SaveReferencePosition()"/>
		/// </summary>
		/// <param name="obj">Saved position</param>
		/// <returns></returns>
		bool RestoreReferencePosition(object obj);
	}
}

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
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Files.Tabs.TextEditor {
	/// <summary>
	/// Text editor UI context
	/// </summary>
	public interface ITextEditorUIContext : IFileTabUIContext {
		/// <summary>
		/// Sets document to <paramref name="result"/>
		/// </summary>
		/// <param name="result">New document data</param>
		/// <param name="contentType">Content type or null</param>
		void SetOutput(DnSpyTextOutputResult result, IContentType contentType);

		/// <summary>
		/// Adds data that is cleared each time <see cref="SetOutput(DnSpyTextOutputResult, IContentType)"/>
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
		/// <param name="message">Message to show to the user or null</param>
		void ShowCancelButton(Action onCancel, string message);

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
		/// Gets the text view host. Don't write to the text buffer directly, use
		/// <see cref="SetOutput(DnSpyTextOutputResult, IContentType)"/> to write new text.
		/// </summary>
		IDnSpyWpfTextViewHost TextViewHost { get; }

		/// <summary>
		/// Gets the text view. Don't write to the text buffer directly, use
		/// <see cref="SetOutput(DnSpyTextOutputResult, IContentType)"/> to write new text.
		/// </summary>
		IDnSpyTextView TextView { get; }

		/// <summary>
		/// Gets the caret
		/// </summary>
		ITextCaret Caret { get; }

		/// <summary>
		/// Gets the selection
		/// </summary>
		ITextSelection Selection { get; }

		/// <summary>
		/// Gets the latest output (set by <see cref="SetOutput(DnSpyTextOutputResult, IContentType)"/>)
		/// </summary>
		DnSpyTextOutputResult OutputResult { get; }

		/// <summary>
		/// Gets the current caret position
		/// </summary>
		TextEditorLocation Location { get; }

		/// <summary>
		/// Scrolls to a line and column
		/// </summary>
		/// <param name="line">Line, 0-based</param>
		/// <param name="column">Column, 0-based</param>
		void ScrollAndMoveCaretTo(int line, int column);

		/// <summary>
		/// Gets the reference at the caret or null if none
		/// </summary>
		SpanData<ReferenceInfo>? SelectedReferenceInfo { get; }

		/// <summary>
		/// Gets all references intersecting with the selection
		/// </summary>
		/// <returns></returns>
		IEnumerable<SpanData<ReferenceInfo>> GetSelectedCodeReferences();

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

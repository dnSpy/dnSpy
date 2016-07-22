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
using System.Windows;
using dnlib.DotNet;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Files.Tabs.DocViewer {
	/// <summary>
	/// Document viewer
	/// </summary>
	public interface IDocumentViewer : IFileTabUIContext {
		/// <summary>
		/// Gets the document viewer control
		/// </summary>
		new FrameworkElement UIObject { get; }

		/// <summary>
		/// Sets new content
		/// </summary>
		/// <param name="content">New content</param>
		/// <param name="contentType">Content type or null</param>
		void SetContent(DocumentViewerContent content, IContentType contentType);

		/// <summary>
		/// Adds data that is removed each time <see cref="SetContent(DocumentViewerContent, IContentType)"/>
		/// gets called with new content.
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="data">Value</param>
		void AddContentData(object key, object data);

		/// <summary>
		/// Returns data added by <see cref="AddContentData(object, object)"/> or null if not found
		/// </summary>
		/// <param name="key">Key</param>
		/// <returns></returns>
		object GetContentData(object key);

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
		/// Moves the caret to a reference, this can be a <see cref="TextReference"/>,
		/// or a <see cref="IMemberDef"/>. Anything else isn't currently supported.
		/// </summary>
		/// <param name="ref">Reference</param>
		void MoveCaretTo(object @ref);

		/// <summary>
		/// Gets the text view host. Don't write to the text buffer directly, use
		/// <see cref="SetContent(DocumentViewerContent, IContentType)"/> to write new text.
		/// </summary>
		IDnSpyWpfTextViewHost TextViewHost { get; }

		/// <summary>
		/// Gets the text view. Don't write to the text buffer directly, use
		/// <see cref="SetContent(DocumentViewerContent, IContentType)"/> to write new text.
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
		/// Gets the current content (set by <see cref="SetContent(DocumentViewerContent, IContentType)"/>)
		/// </summary>
		DocumentViewerContent Content { get; }

		/// <summary>
		/// Gets the current caret position
		/// </summary>
		TextEditorLocation CaretLocation { get; }

		/// <summary>
		/// Scrolls to a line and column
		/// </summary>
		/// <param name="line">Line, 0-based</param>
		/// <param name="column">Column, 0-based</param>
		void ScrollAndMoveCaretTo(int line, int column);

		/// <summary>
		/// Scrolls to a line and column
		/// </summary>
		/// <param name="location">Location</param>
		void ScrollAndMoveCaretTo(TextEditorLocation location);

		/// <summary>
		/// Gets the reference at the caret or null if none
		/// </summary>
		SpanData<ReferenceInfo>? SelectedReferenceInfo { get; }

		/// <summary>
		/// Gets all references intersecting with the selection
		/// </summary>
		/// <returns></returns>
		IEnumerable<SpanData<ReferenceInfo>> GetSelectedTextReferences();

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

		/// <summary>
		/// Raised when this instance gets new content (its <see cref="SetContent(DocumentViewerContent, IContentType)"/>
		/// method was called). It's only raised if the new content is different from the current
		/// content. I.e., calling it twice in a row with the same content won't raise this event
		/// the second time. This event is raised before <see cref="IDocumentViewerService.NewContent"/>
		/// </summary>
		event EventHandler<DocumentViewerNewContentEventArgs> NewContent;

		/// <summary>
		/// Raised when this instance has been closed. This event is raised before
		/// <see cref="IDocumentViewerService.Removed"/>
		/// </summary>
		event EventHandler<DocumentViewerRemovedEventArgs> Removed;
	}
}

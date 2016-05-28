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

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Text buffer
	/// </summary>
	public interface ITextBuffer : IPropertyOwner {
		/// <summary>
		/// Gets/sets the content type
		/// </summary>
		IContentType ContentType { get; }

		/// <summary>
		/// Current content of this buffer
		/// </summary>
		ITextSnapshot CurrentSnapshot { get; }

		/// <summary>
		/// Raised when <see cref="ContentType"/> has changed
		/// </summary>
		event EventHandler<ContentTypeChangedEventArgs> ContentTypeChanged;

		/// <summary>
		/// Raised when the text has changed. It is raised before <see cref="Changed"/>
		/// and <see cref="ChangedLowPriority"/>
		/// </summary>
		event EventHandler<TextContentChangedEventArgs> ChangedHighPriority;

		/// <summary>
		/// Raised when the text has changed. It is raised after <see cref="ChangedHighPriority"/>
		/// and before <see cref="ChangedLowPriority"/>
		/// </summary>
		event EventHandler<TextContentChangedEventArgs> Changed;

		/// <summary>
		/// Raised when the text has changed. It is raised after <see cref="ChangedHighPriority"/>
		/// and <see cref="Changed"/>
		/// </summary>
		event EventHandler<TextContentChangedEventArgs> ChangedLowPriority;

		/// <summary>
		/// Raised when the buffer is about to change
		/// </summary>
		event EventHandler<TextContentChangingEventArgs> Changing;

		/// <summary>
		/// Raised after an edit operation has been applied or canceled
		/// </summary>
		event EventHandler PostChanged;

		/// <summary>
		/// Takes ownership of this text buffer for the current thread. Can only be called once.
		/// </summary>
		void TakeThreadOwnership();

		/// <summary>
		/// Changes <see cref="ContentType"/>
		/// </summary>
		/// <param name="newContentType">New content type</param>
		/// <param name="editTag">Edit tag or null</param>
		void ChangeContentType(IContentType newContentType, object editTag);

		/// <summary>
		/// true if an edit operation is in progress
		/// </summary>
		bool EditInProgress { get; }

		/// <summary>
		/// Returns true if the current thread can edit the text buffer
		/// </summary>
		/// <returns></returns>
		bool CheckEditAccess();

		/// <summary>
		/// Creates a text edit object
		/// </summary>
		/// <returns></returns>
		ITextEdit CreateEdit();

		/// <summary>
		/// Creates a text edit object
		/// </summary>
		/// <param name="editTag">Edit tag</param>
		/// <returns></returns>
		ITextEdit CreateEdit(object editTag);

		/// <summary>
		/// Deletes characters from the buffer. Returns the new <see cref="ITextSnapshot"/> instance
		/// </summary>
		/// <param name="deleteSpan">Characters to remove</param>
		/// <returns></returns>
		ITextSnapshot Delete(Span deleteSpan);

		/// <summary>
		/// Inserts text at <paramref name="position"/>. Returns the new <see cref="ITextSnapshot"/> instance
		/// </summary>
		/// <param name="position">Position in the buffer</param>
		/// <param name="text">Text to insert</param>
		/// <returns></returns>
		ITextSnapshot Insert(int position, string text);

		/// <summary>
		/// Replaces characters with a string. Returns the new <see cref="ITextSnapshot"/> instance
		/// </summary>
		/// <param name="replaceSpan">Characters to remove</param>
		/// <param name="replaceWith">New string</param>
		/// <returns></returns>
		ITextSnapshot Replace(Span replaceSpan, string replaceWith);
	}
}

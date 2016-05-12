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
	public interface ITextBuffer {
		/// <summary>
		/// Gets/sets the content type
		/// </summary>
		IContentType ContentType { get; set; }

		/// <summary>
		/// Current content of this buffer
		/// </summary>
		ITextSnapshot CurrentSnapshot { get; }

		/// <summary>
		/// Raised when <see cref="ContentType"/> has been changed
		/// </summary>
		event EventHandler<ContentTypeChangedEventArgs> ContentTypeChanged;

		/// <summary>
		/// Raised when the text has been changed
		/// </summary>
		event EventHandler<TextContentChangedEventArgs> Changed;

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

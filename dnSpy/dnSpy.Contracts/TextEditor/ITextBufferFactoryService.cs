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
using System.IO;

namespace dnSpy.Contracts.TextEditor {
	/// <summary>
	/// <see cref="ITextBuffer"/> factory service
	/// </summary>
	public interface ITextBufferFactoryService {
		/// <summary>
		/// Inert content type (<see cref="ContentTypes.INERT"/>)
		/// </summary>
		IContentType InertContentType { get; }

		/// <summary>
		/// Plain text content type (<see cref="ContentTypes.PLAIN_TEXT"/>)
		/// </summary>
		IContentType PlaintextContentType { get; }

		/// <summary>
		/// Text content type (<see cref="ContentTypes.TEXT"/>)
		/// </summary>
		IContentType TextContentType { get; }

		/// <summary>
		/// Raised when a new <see cref="ITextBuffer"/> has been created
		/// </summary>
		event EventHandler<TextBufferCreatedEventArgs> TextBufferCreated;

		/// <summary>
		/// Creates a new empty <see cref="ITextBuffer"/> instance with content type <see cref="ContentTypes.TEXT"/>
		/// </summary>
		/// <returns></returns>
		ITextBuffer CreateTextBuffer();

		/// <summary>
		/// Creates a new empty <see cref="ITextBuffer"/> instance
		/// </summary>
		/// <param name="contentType">Content type</param>
		/// <returns></returns>
		ITextBuffer CreateTextBuffer(IContentType contentType);

		/// <summary>
		/// Creates a new empty <see cref="ITextBuffer"/> instance
		/// </summary>
		/// <param name="contentType">Content type</param>
		/// <returns></returns>
		ITextBuffer CreateTextBuffer(Guid contentType);

		/// <summary>
		/// Creates a new <see cref="ITextBuffer"/> instance with the specified text
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="contentType">Content type</param>
		/// <returns></returns>
		ITextBuffer CreateTextBuffer(string text, IContentType contentType);

		/// <summary>
		/// Creates a new <see cref="ITextBuffer"/> instance with the specified text
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="contentType">Content type</param>
		/// <returns></returns>
		ITextBuffer CreateTextBuffer(string text, Guid contentType);

		/// <summary>
		/// Creates a new <see cref="ITextBuffer"/> instance initialized to the contents of <paramref name="reader"/>
		/// </summary>
		/// <param name="reader">New buffer data</param>
		/// <param name="contentType">Content type</param>
		/// <returns></returns>
		ITextBuffer CreateTextBuffer(TextReader reader, IContentType contentType);

		/// <summary>
		/// Creates a new <see cref="ITextBuffer"/> instance initialized to the contents of <paramref name="reader"/>
		/// </summary>
		/// <param name="reader">New buffer data</param>
		/// <param name="contentType">Content type</param>
		/// <returns></returns>
		ITextBuffer CreateTextBuffer(TextReader reader, Guid contentType);
	}
}

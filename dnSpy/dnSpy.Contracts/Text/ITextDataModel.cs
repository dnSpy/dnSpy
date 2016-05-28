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
	/// Text data model
	/// </summary>
	public interface ITextDataModel {
		/// <summary>
		/// Gets the content type (it's usually <see cref="DocumentBuffer"/>'s content type)
		/// </summary>
		IContentType ContentType { get; }

		/// <summary>
		/// Text buffer shown in the text editor and may be identical to <see cref="DocumentBuffer"/>
		/// </summary>
		ITextBuffer DataBuffer { get; }

		/// <summary>
		/// Text buffer source document and may be identical to <see cref="DataBuffer"/>
		/// </summary>
		ITextBuffer DocumentBuffer { get; }

		/// <summary>
		/// Raised when <see cref="ContentType"/> has changed
		/// </summary>
		event EventHandler<TextDataModelContentTypeChangedEventArgs> ContentTypeChanged;
	}
}

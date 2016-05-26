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
	/// Content type changed event args
	/// </summary>
	public sealed class ContentTypeChangedEventArgs : TextSnapshotChangedEventArgs {
		/// <summary>
		/// Original content type
		/// </summary>
		public IContentType BeforeContentType { get; }

		/// <summary>
		/// New content type
		/// </summary>
		public IContentType AfterContentType { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="beforeSnapshot">Original snapshot</param>
		/// <param name="afterSnapshot">New snapshot</param>
		/// <param name="beforeContentType">Original content type</param>
		/// <param name="afterContentType">New content type</param>
		/// <param name="editTag">Edit tag or null</param>
		public ContentTypeChangedEventArgs(ITextSnapshot beforeSnapshot, ITextSnapshot afterSnapshot, IContentType beforeContentType, IContentType afterContentType, object editTag)
			: base(beforeSnapshot, afterSnapshot, editTag) {
			if (beforeContentType == null)
				throw new ArgumentNullException(nameof(beforeContentType));
			if (afterContentType == null)
				throw new ArgumentNullException(nameof(afterContentType));
			BeforeContentType = beforeContentType;
			AfterContentType = afterContentType;
		}
	}
}

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

namespace dnSpy.Contracts.Text.Tagging {
	/// <summary>
	/// Implements <see cref="ITagSpan{T}"/>
	/// </summary>
	/// <typeparam name="T">The type, which must be a subclass of <see cref="ITag"/></typeparam>
	public sealed class TagSpan<T> : ITagSpan<T> where T : ITag {
		/// <summary>
		/// Gets the snapshot span
		/// </summary>
		public SnapshotSpan Span { get; }

		/// <summary>
		/// Gets the tag
		/// </summary>
		public T Tag { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="tag">Tag</param>
		public TagSpan(SnapshotSpan span, T tag) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			if (tag == null)
				throw new ArgumentNullException(nameof(tag));
			Span = span;
			Tag = tag;
		}
	}
}

/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Documents.Tabs;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Bookmarks.TextEditor {
	/// <summary>
	/// Creates bookmark locations in text views
	/// </summary>
	public abstract class TextViewBookmarkLocationProvider {
		/// <summary>
		/// Creates a new <see cref="BookmarkLocation"/> instance whose text view span is >= <paramref name="position"/>
		/// </summary>
		/// <param name="tab">Tab</param>
		/// <param name="textView">Text view</param>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract TextViewBookmarkLocationResult? CreateLocation(IDocumentTab tab, ITextView textView, VirtualSnapshotPoint position);
	}

	/// <summary>
	/// Text view location
	/// </summary>
	public readonly struct TextViewBookmarkLocationResult {
		/// <summary>
		/// Gets the bookmark location
		/// </summary>
		public BookmarkLocation Location { get; }

		/// <summary>
		/// Gets the span
		/// </summary>
		public VirtualSnapshotSpan Span { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="location">Location</param>
		/// <param name="span">Text view span</param>
		public TextViewBookmarkLocationResult(BookmarkLocation location, SnapshotSpan span) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			Location = location ?? throw new ArgumentNullException(nameof(location));
			Span = new VirtualSnapshotSpan(span);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="location">Location</param>
		/// <param name="span">Text view span</param>
		public TextViewBookmarkLocationResult(BookmarkLocation location, VirtualSnapshotSpan span) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			Location = location ?? throw new ArgumentNullException(nameof(location));
			Span = span;
		}
	}
}

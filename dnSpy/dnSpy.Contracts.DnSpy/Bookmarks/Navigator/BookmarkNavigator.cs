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

namespace dnSpy.Contracts.Bookmarks.Navigator {
	/// <summary>
	/// Selects the next or previous bookmark
	/// </summary>
	public abstract class BookmarkNavigator {
		/// <summary>
		/// Current active bookmark. It's null if there are no bookmarks or no bookmarks are visible in the UI
		/// </summary>
		public abstract Bookmark ActiveBookmark { get; set; }

		/// <summary>
		/// Raised when <see cref="ActiveBookmark"/> is changed
		/// </summary>
		public abstract event EventHandler ActiveBookmarkChanged;

		/// <summary>
		/// true if <see cref="SelectPreviousBookmark"/> can be called
		/// </summary>
		public abstract bool CanSelectPreviousBookmark { get; }

		/// <summary>
		/// Select the previous bookmark
		/// </summary>
		public abstract void SelectPreviousBookmark();

		/// <summary>
		/// true if <see cref="SelectNextBookmark"/> can be called
		/// </summary>
		public abstract bool CanSelectNextBookmark { get; }

		/// <summary>
		/// Select the next bookmark
		/// </summary>
		public abstract void SelectNextBookmark();

		/// <summary>
		/// true if <see cref="SelectPreviousBookmarkInDocument"/> can be called
		/// </summary>
		public abstract bool CanSelectPreviousBookmarkInDocument { get; }

		/// <summary>
		/// Select the previous bookmark in the document
		/// </summary>
		public abstract void SelectPreviousBookmarkInDocument();

		/// <summary>
		/// true if <see cref="SelectNextBookmarkInDocument"/> can be called
		/// </summary>
		public abstract bool CanSelectNextBookmarkInDocument { get; }

		/// <summary>
		/// Select the next bookmark in the document
		/// </summary>
		public abstract void SelectNextBookmarkInDocument();

		/// <summary>
		/// true if <see cref="SelectPreviousBookmarkWithSameLabel"/> can be called
		/// </summary>
		public abstract bool CanSelectPreviousBookmarkWithSameLabel { get; }

		/// <summary>
		/// Select the previous bookmark with the same label
		/// </summary>
		public abstract void SelectPreviousBookmarkWithSameLabel();

		/// <summary>
		/// true if <see cref="SelectNextBookmarkWithSameLabel"/> can be called
		/// </summary>
		public abstract bool CanSelectNextBookmarkWithSameLabel { get; }

		/// <summary>
		/// Select the next bookmark with the same label
		/// </summary>
		public abstract void SelectNextBookmarkWithSameLabel();
	}
}

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

namespace dnSpy.Contracts.Command {
	/// <summary>
	/// Bookmark IDs
	/// </summary>
	public enum BookmarkIds {
		/// <summary>
		/// Shows bookmarks window
		/// </summary>
		ShowBookmarkWindow,

		/// <summary>
		/// Removes all bookmarks in the document
		/// </summary>
		ClearAllBookmarksInDocument,

		/// <summary>
		/// Removes all bookmarks
		/// </summary>
		ClearBookmarks,

		/// <summary>
		/// Enables or disables all bookmarks
		/// </summary>
		EnableAllBookmarks,

		/// <summary>
		/// Enables or disables a bookmark
		/// </summary>
		EnableBookmark,

		/// <summary>
		/// Toggles (adds or removes) a bookmark
		/// </summary>
		ToggleBookmark,

		/// <summary>
		/// Goes to the next bookmark
		/// </summary>
		NextBookmark,

		/// <summary>
		/// Goes to the previous bookmark
		/// </summary>
		PreviousBookmark,

		/// <summary>
		/// Goes to the next bookmark in the document
		/// </summary>
		NextBookmarkInDocument,

		/// <summary>
		/// Goes to the previous bookmark in the document
		/// </summary>
		PreviousBookmarkInDocument,

		/// <summary>
		/// Goes to the next bookmark with the same label
		/// </summary>
		NextBookmarkWithSameLabel,

		/// <summary>
		/// Goes to the previous bookmark with the same label
		/// </summary>
		PreviousBookmarkWithSameLabel,
	}
}

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
using System.Collections.Generic;
using dnSpy.Contracts.Bookmarks;

namespace dnSpy.Bookmarks.Navigator {
	abstract class ViewBookmarkProvider {
		/// <summary>
		/// Gets all bookmarks in view order
		/// </summary>
		public abstract IList<Bookmark> BookmarksViewOrder { get; }

		/// <summary>
		/// Raised when <see cref="BookmarksViewOrder"/> is changed
		/// </summary>
		public abstract event EventHandler BookmarksViewOrderChanged;

		/// <summary>
		/// Gets the default bookmark that gets selected when the active bookmark gets removed
		/// </summary>
		public abstract Bookmark DefaultBookmark { get; }

		/// <summary>
		/// Called when there's a new active bookmark
		/// </summary>
		public abstract void SetActiveBookmark(Bookmark bookmark);
	}
}

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

using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Bookmarks {
	/// <summary>
	/// Bookmark
	/// </summary>
	public abstract class Bookmark : BMObject {
		/// <summary>
		/// Gets the unique bookmark id
		/// </summary>
		public abstract int Id { get; }

		/// <summary>
		/// Gets/sets the current settings
		/// </summary>
		public abstract BookmarkSettings Settings { get; set; }

		/// <summary>
		/// true if the bookmark is enabled
		/// </summary>
		public abstract bool IsEnabled { get; set; }

		/// <summary>
		/// Name of the bookmark
		/// </summary>
		public abstract string Name { get; set; }

		/// <summary>
		/// Labels
		/// </summary>
		public abstract ReadOnlyCollection<string> Labels { get; set; }

		/// <summary>
		/// Gets the bookmark location
		/// </summary>
		public abstract BookmarkLocation Location { get; }

		/// <summary>
		/// Removes the bookmark
		/// </summary>
		public abstract void Remove();
	}
}

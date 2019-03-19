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
using System.Collections.ObjectModel;
using System.Linq;

namespace dnSpy.Contracts.Bookmarks {
	/// <summary>
	/// Bookmarks service
	/// </summary>
	public abstract class BookmarksService {
		/// <summary>
		/// Modifies a bookmark
		/// </summary>
		/// <param name="bookmark">Bookmark</param>
		/// <param name="settings">New settings</param>
		public void Modify(Bookmark bookmark, BookmarkSettings settings) =>
			Modify(new[] { new BookmarkAndSettings(bookmark, settings) });

		/// <summary>
		/// Modifies bookmarks
		/// </summary>
		/// <param name="settings">New settings</param>
		public abstract void Modify(BookmarkAndSettings[] settings);

		/// <summary>
		/// Raised when bookmarks are modified
		/// </summary>
		public abstract event EventHandler<BookmarksModifiedEventArgs> BookmarksModified;

		/// <summary>
		/// Gets all bookmarks
		/// </summary>
		public abstract Bookmark[] Bookmarks { get; }

		/// <summary>
		/// Raised when <see cref="Bookmarks"/> is changed
		/// </summary>
		public abstract event EventHandler<CollectionChangedEventArgs<Bookmark>> BookmarksChanged;

		/// <summary>
		/// Adds a bookmark. If the bookmark already exists, null is returned.
		/// </summary>
		/// <param name="bookmark">Bookmark info</param>
		/// <returns></returns>
		public Bookmark Add(BookmarkInfo bookmark) => Add(new[] { bookmark }).FirstOrDefault();

		/// <summary>
		/// Adds bookmarks. Duplicate bookmarks are ignored.
		/// </summary>
		/// <param name="bookmarks">Bookmarks</param>
		/// <returns></returns>
		public abstract Bookmark[] Add(BookmarkInfo[] bookmarks);

		/// <summary>
		/// Removes a bookmark
		/// </summary>
		/// <param name="bookmark">Bookmark to remove</param>
		public void Remove(Bookmark bookmark) => Remove(new[] { bookmark ?? throw new ArgumentNullException(nameof(bookmark)) });

		/// <summary>
		/// Removes bookmarks
		/// </summary>
		/// <param name="bookmarks">Bookmarks to remove</param>
		public abstract void Remove(Bookmark[] bookmarks);

		/// <summary>
		/// Removes all bookmarks
		/// </summary>
		public abstract void Clear();

		/// <summary>
		/// Closes <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">Object to close</param>
		public void Close(BMObject obj) => Close(new[] { obj ?? throw new ArgumentNullException(nameof(obj)) });

		/// <summary>
		/// Closes <paramref name="objs"/>
		/// </summary>
		/// <param name="objs">Objects to close</param>
		public abstract void Close(BMObject[] objs);
	}

	/// <summary>
	/// Bookmark and old settings
	/// </summary>
	public readonly struct BookmarkAndOldSettings {
		/// <summary>
		/// Gets the bookmark
		/// </summary>
		public Bookmark Bookmark { get; }

		/// <summary>
		/// Gets the old settings
		/// </summary>
		public BookmarkSettings OldSettings { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bookmark">Bookmark</param>
		/// <param name="oldSettings">Old settings</param>
		public BookmarkAndOldSettings(Bookmark bookmark, BookmarkSettings oldSettings) {
			Bookmark = bookmark ?? throw new ArgumentNullException(nameof(bookmark));
			OldSettings = oldSettings;
		}
	}

	/// <summary>
	/// Bookmarks modified event args
	/// </summary>
	public readonly struct BookmarksModifiedEventArgs {
		/// <summary>
		/// Gets the bookmarks
		/// </summary>
		public ReadOnlyCollection<BookmarkAndOldSettings> Bookmarks { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bookmarks">Bookmarks and old settings</param>
		public BookmarksModifiedEventArgs(ReadOnlyCollection<BookmarkAndOldSettings> bookmarks) =>
			Bookmarks = bookmarks ?? throw new ArgumentNullException(nameof(bookmarks));
	}

	/// <summary>
	/// Bookmark and settings
	/// </summary>
	public readonly struct BookmarkAndSettings {
		/// <summary>
		/// Gets the bookmark
		/// </summary>
		public Bookmark Bookmark { get; }

		/// <summary>
		/// Gets the new settings
		/// </summary>
		public BookmarkSettings Settings { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bookmark">Bookmark</param>
		/// <param name="settings">New settings</param>
		public BookmarkAndSettings(Bookmark bookmark, BookmarkSettings settings) {
			Bookmark = bookmark ?? throw new ArgumentNullException(nameof(bookmark));
			Settings = settings;
		}
	}

	/// <summary>
	/// Info needed to add a bookmark
	/// </summary>
	public readonly struct BookmarkInfo {
		/// <summary>
		/// Bookmark location
		/// </summary>
		public BookmarkLocation Location { get; }

		/// <summary>
		/// Bookmark settings
		/// </summary>
		public BookmarkSettings Settings { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="location">Bookmark location</param>
		/// <param name="settings">Bookmark settings</param>
		public BookmarkInfo(BookmarkLocation location, BookmarkSettings settings) {
			Location = location ?? throw new ArgumentNullException(nameof(location));
			Settings = settings;
		}
	}

	/// <summary>
	/// Export an instance to get created when <see cref="BookmarksService"/> gets created
	/// </summary>
	public interface IBookmarksServiceListener {
		/// <summary>
		/// Called once by <see cref="BookmarksService"/>
		/// </summary>
		/// <param name="bookmarksService">Bookmarks service</param>
		void Initialize(BookmarksService bookmarksService);
	}
}

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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Bookmarks;
using dnSpy.UI;

namespace dnSpy.Bookmarks.Impl {
	[Export(typeof(BookmarksService))]
	sealed class BookmarksServiceImpl : BookmarksService {
		readonly object lockObj;
		readonly HashSet<BookmarkImpl> bookmarks;
		readonly Dictionary<BookmarkLocation, BookmarkImpl> locationToBookmark;
		readonly UIDispatcher uiDispatcher;
		int bookmarkId;

		internal UIDispatcher Dispatcher => uiDispatcher;

		[ImportingConstructor]
		BookmarksServiceImpl(UIDispatcher uiDispatcher, [ImportMany] IEnumerable<Lazy<IBookmarksServiceListener>> bookmarksServiceListener) {
			lockObj = new object();
			bookmarks = new HashSet<BookmarkImpl>();
			locationToBookmark = new Dictionary<BookmarkLocation, BookmarkImpl>();
			this.uiDispatcher = uiDispatcher;
			bookmarkId = 0;

			foreach (var lz in bookmarksServiceListener)
				lz.Value.Initialize(this);
		}

		void BMThread(Action callback) => uiDispatcher.UI(callback);

		public override void Modify(BookmarkAndSettings[] settings) {
			if (settings is null)
				throw new ArgumentNullException(nameof(settings));
			BMThread(() => ModifyCore(settings));
		}

		void ModifyCore(BookmarkAndSettings[] settings) {
			uiDispatcher.VerifyAccess();
			var bms = new List<BookmarkAndOldSettings>(settings.Length);
			lock (lockObj) {
				foreach (var info in settings) {
					var bmImpl = info.Bookmark as BookmarkImpl;
					Debug2.Assert(!(bmImpl is null));
					if (bmImpl is null)
						continue;
					Debug.Assert(bookmarks.Contains(bmImpl));
					if (!bookmarks.Contains(bmImpl))
						continue;
					var currentSettings = bmImpl.Settings;
					if (currentSettings == info.Settings)
						continue;
					bms.Add(new BookmarkAndOldSettings(bmImpl, currentSettings));
					bmImpl.WriteSettings_BMThread(info.Settings);
				}
			}
			if (bms.Count > 0)
				BookmarksModified?.Invoke(this, new BookmarksModifiedEventArgs(new ReadOnlyCollection<BookmarkAndOldSettings>(bms)));
		}

		public override event EventHandler<BookmarksModifiedEventArgs>? BookmarksModified;

		public override event EventHandler<CollectionChangedEventArgs<Bookmark>>? BookmarksChanged;
		public override Bookmark[] Bookmarks {
			get {
				lock (lockObj)
					return bookmarks.ToArray();
			}
		}

		public override Bookmark[] Add(BookmarkInfo[] bookmarks) {
			if (bookmarks is null)
				throw new ArgumentNullException(nameof(bookmarks));
			var bmImpls = new List<BookmarkImpl>(bookmarks.Length);
			List<BMObject>? objsToClose = null;
			lock (lockObj) {
				for (int i = 0; i < bookmarks.Length; i++) {
					var location = bookmarks[i].Location;
					if (locationToBookmark.ContainsKey(location)) {
						if (objsToClose is null)
							objsToClose = new List<BMObject>();
						objsToClose.Add(location);
					}
					else {
						var bm = new BookmarkImpl(this, bookmarkId++, location, bookmarks[i].Settings);
						bmImpls.Add(bm);
					}
				}
				BMThread(() => AddCore(bmImpls, objsToClose));
			}
			return bmImpls.ToArray();
		}

		void AddCore(List<BookmarkImpl> bookmarks, List<BMObject>? objsToClose) {
			uiDispatcher.VerifyAccess();
			var added = new List<Bookmark>(bookmarks.Count);
			lock (lockObj) {
				foreach (var bm in bookmarks) {
					Debug.Assert(!this.bookmarks.Contains(bm));
					if (this.bookmarks.Contains(bm))
						continue;
					if (locationToBookmark.ContainsKey(bm.Location)) {
						if (objsToClose is null)
							objsToClose = new List<BMObject>();
						objsToClose.Add(bm);
					}
					else {
						added.Add(bm);
						this.bookmarks.Add(bm);
						locationToBookmark.Add(bm.Location, bm);
					}
				}
			}
			if (!(objsToClose is null)) {
				foreach (var obj in objsToClose)
					obj.Close();
			}
			if (added.Count > 0)
				BookmarksChanged?.Invoke(this, new CollectionChangedEventArgs<Bookmark>(added, added: true));
		}

		public override void Remove(Bookmark[] bookmarks) {
			if (bookmarks is null)
				throw new ArgumentNullException(nameof(bookmarks));
			BMThread(() => RemoveCore(bookmarks));
		}

		void RemoveCore(Bookmark[] bookmarks) {
			uiDispatcher.VerifyAccess();
			var removed = new List<Bookmark>(bookmarks.Length);
			lock (lockObj) {
				foreach (var bm in bookmarks) {
					var bmImpl = bm as BookmarkImpl;
					Debug2.Assert(!(bmImpl is null));
					if (bmImpl is null)
						continue;
					if (!this.bookmarks.Contains(bmImpl))
						continue;
					removed.Add(bmImpl);
					this.bookmarks.Remove(bmImpl);
					bool b = locationToBookmark.Remove(bmImpl.Location);
					Debug.Assert(b);
				}
			}
			if (removed.Count > 0) {
				BookmarksChanged?.Invoke(this, new CollectionChangedEventArgs<Bookmark>(removed, added: false));
				foreach (var bm in removed)
					bm.Close();
			}
		}

		public override void Clear() => BMThread(() => ClearCore());
		void ClearCore() {
			uiDispatcher.VerifyAccess();
			Bookmark[] removed;
			lock (lockObj) {
				removed = bookmarks.ToArray();
				bookmarks.Clear();
				locationToBookmark.Clear();
			}
			if (removed.Length > 0) {
				BookmarksChanged?.Invoke(this, new CollectionChangedEventArgs<Bookmark>(removed, added: false));
				foreach (var bm in removed)
					bm.Close();
			}
		}

		public override void Close(BMObject[] objs) {
			if (objs is null)
				throw new ArgumentNullException(nameof(objs));
			BMThread(() => Close_BMThread(objs));
		}

		void Close_BMThread(BMObject[] objs) {
			uiDispatcher.VerifyAccess();
			foreach (var obj in objs)
				obj.Close();
		}
	}
}

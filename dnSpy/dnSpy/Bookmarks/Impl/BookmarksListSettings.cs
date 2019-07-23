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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Settings;
using dnSpy.UI;

namespace dnSpy.Bookmarks.Impl {
	[Export(typeof(IBookmarksServiceListener))]
	sealed class BookmarksListSettingsListener : IBookmarksServiceListener {
		readonly UIDispatcher uiDispatcher;
		readonly ISettingsService settingsService;
		readonly BookmarkLocationSerializerService bookmarkLocationSerializerService;

		[ImportingConstructor]
		BookmarksListSettingsListener(UIDispatcher uiDispatcher, ISettingsService settingsService, BookmarkLocationSerializerService bookmarkLocationSerializerService) {
			this.uiDispatcher = uiDispatcher;
			this.settingsService = settingsService;
			this.bookmarkLocationSerializerService = bookmarkLocationSerializerService;
		}

		void IBookmarksServiceListener.Initialize(BookmarksService bookmarksService) =>
			new BookmarksListSettings(uiDispatcher, settingsService, bookmarkLocationSerializerService, bookmarksService);
	}

	sealed class BookmarksListSettings {
		readonly UIDispatcher uiDispatcher;
		readonly ISettingsService settingsService;
		readonly BookmarkLocationSerializerService bookmarkLocationSerializerService;
		readonly BookmarksService bookmarksService;

		public BookmarksListSettings(UIDispatcher uiDispatcher, ISettingsService settingsService, BookmarkLocationSerializerService bookmarkLocationSerializerService, BookmarksService bookmarksService) {
			this.uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
			this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
			this.bookmarkLocationSerializerService = bookmarkLocationSerializerService ?? throw new ArgumentNullException(nameof(bookmarkLocationSerializerService));
			this.bookmarksService = bookmarksService ?? throw new ArgumentNullException(nameof(bookmarksService));
			bookmarksService.BookmarksChanged += BookmarksService_BookmarksChanged;
			bookmarksService.BookmarksModified += BookmarksService_BookmarksModified;
			uiDispatcher.UI(() => Load());
		}

		void Load() {
			uiDispatcher.VerifyAccess();
			ignoreSave = true;
			bookmarksService.Add(new BookmarksSerializer(settingsService, bookmarkLocationSerializerService).Load());
			uiDispatcher.UI(() => ignoreSave = false);
		}

		void BookmarksService_BookmarksChanged(object? sender, CollectionChangedEventArgs<Bookmark> e) => Save();
		void BookmarksService_BookmarksModified(object? sender, BookmarksModifiedEventArgs e) => Save();

		void Save() {
			uiDispatcher.VerifyAccess();
			if (ignoreSave)
				return;
			new BookmarksSerializer(settingsService, bookmarkLocationSerializerService).Save(bookmarksService.Bookmarks);
		}
		bool ignoreSave;
	}
}

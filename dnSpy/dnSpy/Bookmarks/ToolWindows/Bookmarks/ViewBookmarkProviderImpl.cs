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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Bookmarks.Navigator;
using dnSpy.Contracts.Bookmarks;
using dnSpy.UI;

namespace dnSpy.Bookmarks.ToolWindows.Bookmarks {
	[Export(typeof(ViewBookmarkProvider))]
	sealed class ViewBookmarkProviderImpl : ViewBookmarkProvider {
		public override event EventHandler? BookmarksViewOrderChanged;
		public override IList<Bookmark> BookmarksViewOrder => allBookmarks;
		public override Bookmark? DefaultBookmark => allBookmarks.FirstOrDefault();

		readonly UIDispatcher uiDispatcher;
		readonly BookmarksService bookmarksService;
		readonly Lazy<IBookmarksVM> bookmarksVM;
		Bookmark? activeBookmark;
		Bookmark[] allBookmarks;

		[ImportingConstructor]
		ViewBookmarkProviderImpl(UIDispatcher uiDispatcher, BookmarksService bookmarksService, Lazy<IBookmarksVM> bookmarksVM) {
			this.uiDispatcher = uiDispatcher;
			this.bookmarksService = bookmarksService;
			this.bookmarksVM = bookmarksVM;
			allBookmarks = Array.Empty<Bookmark>();
			UI(() => Initialize_UI());
		}

		void UI(Action callback) => uiDispatcher.UI(callback);

		void Initialize_UI() {
			uiDispatcher.VerifyAccess();
			bookmarksVM.Value.OnShowChanged += BookmarksVM_OnShowChanged;
			bookmarksVM.Value.AllItemsFiltered += BookmarksVM_AllItemsFiltered;
			bookmarksService.BookmarksChanged += BookmarksService_BookmarksChanged;
			BookmarksVM_OnShowChanged();
		}

		void BookmarksVM_OnShowChanged(object? sender, EventArgs e) => BookmarksVM_OnShowChanged();
		void BookmarksVM_OnShowChanged() => UpdateBookmarks_UI();
		void BookmarksVM_AllItemsFiltered(object? sender, EventArgs e) => UpdateBookmarks_UI();

		void BookmarksService_BookmarksChanged(object? sender, CollectionChangedEventArgs<Bookmark> e) =>
			// Add an extra UI() so it's guaranteed to be called after BookmarksVM's handler
			UI(() => UI(() => BookmarksService_BookmarksChanged_UI(e)));
		void BookmarksService_BookmarksChanged_UI(CollectionChangedEventArgs<Bookmark> e) => UpdateBookmarks_UI();

		void UpdateBookmarks_UI() {
			uiDispatcher.VerifyAccess();
			if (bookmarksVM.Value.IsOpen) {
				var bookmarks = bookmarksVM.Value.Sort(bookmarksVM.Value.AllItems).Select(a => a.Bookmark).ToArray();
				UpdateBookmarks_UI(bookmarks);
			}
			else {
				// There's no view, so there's no view order
				var bookmarks = bookmarksService.Bookmarks.OrderBy(a => a.Id).ToArray();
				UpdateBookmarks_UI(bookmarks);
			}
			UpdateActiveBookmark_UI();
		}

		void UpdateBookmarks_UI(Bookmark[] bookmarks) {
			uiDispatcher.VerifyAccess();
			allBookmarks = bookmarks ?? throw new ArgumentNullException(nameof(bookmarks));
			BookmarksViewOrderChanged?.Invoke(this, EventArgs.Empty);
		}

		public override void SetActiveBookmark(Bookmark? bookmark) {
			uiDispatcher.VerifyAccess();
			if (activeBookmark == bookmark)
				return;
			activeBookmark = bookmark;
			UpdateActiveBookmark_UI();
		}

		void UpdateActiveBookmark_UI() {
			foreach (var vm in bookmarksVM.Value.AllItems)
				vm.IsActive = vm.Bookmark == activeBookmark;
		}
	}
}

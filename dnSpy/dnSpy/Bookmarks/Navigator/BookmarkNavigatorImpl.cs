/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Bookmarks.Navigator;
using dnSpy.UI;

namespace dnSpy.Bookmarks.Navigator {
	[Export(typeof(BookmarkNavigator))]
	sealed class BookmarkNavigatorImpl : BookmarkNavigator {
		public override Bookmark ActiveBookmark {
			get => activeBookmark;
			set {
				uiDispatcher.VerifyAccess();
				if (activeBookmark == value)
					return;
				var newBookmark = value;
				if (!viewBookmarkProvider.BookmarksViewOrder.Contains(newBookmark))
					newBookmark = viewBookmarkProvider.DefaultBookmark;
				activeBookmark = newBookmark;
				viewBookmarkProvider.SetActiveBookmark(activeBookmark);
				ActiveBookmarkChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		Bookmark activeBookmark;

		public override event EventHandler ActiveBookmarkChanged;

		readonly UIDispatcher uiDispatcher;
		readonly ViewBookmarkProvider viewBookmarkProvider;

		[ImportingConstructor]
		BookmarkNavigatorImpl(UIDispatcher uiDispatcher, ViewBookmarkProvider viewBookmarkProvider) {
			this.uiDispatcher = uiDispatcher;
			this.viewBookmarkProvider = viewBookmarkProvider;
			activeBookmark = viewBookmarkProvider.DefaultBookmark;
			viewBookmarkProvider.BookmarksViewOrderChanged += ViewBookmarkProvider_BookmarksViewOrderChanged;
			UI(() => viewBookmarkProvider.SetActiveBookmark(activeBookmark));
		}

		void UI(Action action) => uiDispatcher.UI(action);

		void ViewBookmarkProvider_BookmarksViewOrderChanged(object sender, EventArgs e) => UI(() => ViewBookmarkProvider_BookmarksViewOrderChanged_UI());
		void ViewBookmarkProvider_BookmarksViewOrderChanged_UI() {
			uiDispatcher.VerifyAccess();
			if (!viewBookmarkProvider.BookmarksViewOrder.Contains(activeBookmark))
				ActiveBookmark = viewBookmarkProvider.DefaultBookmark;
		}

		public override bool CanSelectPreviousBookmark => true;
		public override void SelectPreviousBookmark() {
			uiDispatcher.VerifyAccess();
			//TODO:
		}

		public override bool CanSelectNextBookmark => true;
		public override void SelectNextBookmark() {
			uiDispatcher.VerifyAccess();
			//TODO:
		}

		public override bool CanSelectPreviousBookmarkInDocument => true;
		public override void SelectPreviousBookmarkInDocument() {
			uiDispatcher.VerifyAccess();
			//TODO:
		}

		public override bool CanSelectNextBookmarkInDocument => true;
		public override void SelectNextBookmarkInDocument() {
			uiDispatcher.VerifyAccess();
			//TODO:
		}

		public override bool CanSelectPreviousBookmarkWithSameLabel => true;
		public override void SelectPreviousBookmarkWithSameLabel() {
			uiDispatcher.VerifyAccess();
			//TODO:
		}

		public override bool CanSelectNextBookmarkWithSameLabel => true;
		public override void SelectNextBookmarkWithSameLabel() {
			uiDispatcher.VerifyAccess();
			//TODO:
		}
	}
}

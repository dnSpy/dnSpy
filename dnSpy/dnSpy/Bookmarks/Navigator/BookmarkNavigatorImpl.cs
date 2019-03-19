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
using dnSpy.Contracts.Bookmarks.Navigator;
using dnSpy.Contracts.Documents;
using dnSpy.UI;

namespace dnSpy.Bookmarks.Navigator {
	abstract class BookmarkNavigator2 : BookmarkNavigator {
		public abstract void SetActiveBookmarkNoCheck(Bookmark bookmark);
	}

	[Export(typeof(BookmarkNavigator))]
	[Export(typeof(BookmarkNavigator2))]
	sealed class BookmarkNavigatorImpl : BookmarkNavigator2 {
		public override Bookmark ActiveBookmark {
			get => activeBookmark;
			set => SetActiveBookmark(value, verifyBookmark: true);
		}
		Bookmark activeBookmark;

		public override event EventHandler ActiveBookmarkChanged;

		readonly UIDispatcher uiDispatcher;
		readonly ViewBookmarkProvider viewBookmarkProvider;
		readonly Lazy<ReferenceNavigatorService> referenceNavigatorService;
		readonly Lazy<BookmarkDocumentProvider, IBookmarkDocumentProviderMetadata>[] bookmarkDocumentProviders;
		ReadOnlyCollection<string> currentLabels;

		[ImportingConstructor]
		BookmarkNavigatorImpl(UIDispatcher uiDispatcher, ViewBookmarkProvider viewBookmarkProvider, Lazy<ReferenceNavigatorService> referenceNavigatorService, [ImportMany] IEnumerable<Lazy<BookmarkDocumentProvider, IBookmarkDocumentProviderMetadata>> bookmarkDocumentProviders) {
			this.uiDispatcher = uiDispatcher;
			this.viewBookmarkProvider = viewBookmarkProvider;
			this.referenceNavigatorService = referenceNavigatorService;
			this.bookmarkDocumentProviders = bookmarkDocumentProviders.OrderBy(a => a.Metadata.Order).ToArray();
			activeBookmark = viewBookmarkProvider.DefaultBookmark;
			viewBookmarkProvider.BookmarksViewOrderChanged += ViewBookmarkProvider_BookmarksViewOrderChanged;
			UI(() => viewBookmarkProvider.SetActiveBookmark(activeBookmark));
		}

		void UI(Action callback) => uiDispatcher.UI(callback);

		void ViewBookmarkProvider_BookmarksViewOrderChanged(object sender, EventArgs e) => UI(() => ViewBookmarkProvider_BookmarksViewOrderChanged_UI());
		void ViewBookmarkProvider_BookmarksViewOrderChanged_UI() {
			uiDispatcher.VerifyAccess();
			if (!viewBookmarkProvider.BookmarksViewOrder.Contains(activeBookmark))
				ActiveBookmark = viewBookmarkProvider.DefaultBookmark;
		}

		public override bool CanSelectPreviousBookmark => true;
		public override void SelectPreviousBookmark() => SelectAndGoTo(GetNextBookmark(-1));

		public override bool CanSelectNextBookmark => true;
		public override void SelectNextBookmark() => SelectAndGoTo(GetNextBookmark(1));

		public override bool CanSelectPreviousBookmarkInDocument => true;
		public override void SelectPreviousBookmarkInDocument() => SelectAndGoTo(GetNextBookmarkInDocument(-1));

		public override bool CanSelectNextBookmarkInDocument => true;
		public override void SelectNextBookmarkInDocument() => SelectAndGoTo(GetNextBookmarkInDocument(1));

		public override bool CanSelectPreviousBookmarkWithSameLabel => true;
		public override void SelectPreviousBookmarkWithSameLabel() => SelectAndGoTo(GetNextBookmarkWithSameLabel(-1), keepLabels: true);

		public override bool CanSelectNextBookmarkWithSameLabel => true;
		public override void SelectNextBookmarkWithSameLabel() => SelectAndGoTo(GetNextBookmarkWithSameLabel(1), keepLabels: true);

		Bookmark GetNextBookmark(int increment) {
			uiDispatcher.VerifyAccess();
			foreach (var bm in GetBookmarks(increment))
				return bm;
			return null;
		}

		Bookmark GetNextBookmarkInDocument(int increment) {
			uiDispatcher.VerifyAccess();
			var currentDocument = GetDocument(activeBookmark);
			foreach (var bm in GetBookmarks(increment)) {
				if (currentDocument == null)
					return bm;
				var doc = GetDocument(bm);
				if (currentDocument.Equals(doc))
					return bm;
			}
			return null;
		}

		BookmarkDocument GetDocument(Bookmark bookmark) {
			uiDispatcher.VerifyAccess();
			if (bookmark == null)
				return null;
			foreach (var lz in bookmarkDocumentProviders) {
				var doc = lz.Value.GetDocument(bookmark);
				if (doc != null)
					return doc;
			}
			return null;
		}

		Bookmark GetNextBookmarkWithSameLabel(int increment) {
			uiDispatcher.VerifyAccess();
			if (currentLabels == null)
				currentLabels = activeBookmark?.Labels ?? emptyLabels;
			foreach (var bm in GetBookmarks(increment)) {
				if (SameLabel(currentLabels, bm.Labels))
					return bm;
			}
			return null;
		}
		static readonly ReadOnlyCollection<string> emptyLabels = new ReadOnlyCollection<string>(Array.Empty<string>());

		static bool SameLabel(ReadOnlyCollection<string> validLabels, ReadOnlyCollection<string> labels) {
			if (labels == null)
				labels = emptyLabels;
			if (validLabels.Count == 0)
				return labels.Count == 0;
			foreach (var label in labels) {
				if (validLabels.Contains(label))
					return true;
			}
			return false;
		}

		IEnumerable<Bookmark> GetBookmarks(int increment) {
			uiDispatcher.VerifyAccess();
			Debug.Assert(increment == 1 || increment == -1);
			var bookmarks = viewBookmarkProvider.BookmarksViewOrder;
			int currentIndex = bookmarks.IndexOf(activeBookmark);
			// If this is true, there are no visible bookmarks in the UI or there are no bookmarks
			if (currentIndex < 0)
				yield break;
			currentIndex += increment;
			for (int i = 0; i < bookmarks.Count; i++, currentIndex += increment) {
				var bm = bookmarks[(currentIndex + bookmarks.Count) % bookmarks.Count];
				if (!bm.IsEnabled)
					continue;
				yield return bm;
			}
		}

		void SelectAndGoTo(Bookmark bookmark, bool keepLabels = false) {
			uiDispatcher.VerifyAccess();
			if (bookmark == null)
				return;
			var currentLabelsTmp = currentLabels;
			ActiveBookmark = bookmark;
			if (keepLabels)
				currentLabels = currentLabelsTmp;
			referenceNavigatorService.Value.GoTo(bookmark.Location);
		}

		public override void SetActiveBookmarkNoCheck(Bookmark bookmark) => SetActiveBookmark(bookmark, verifyBookmark: false);

		void SetActiveBookmark(Bookmark bookmark, bool verifyBookmark) {
			uiDispatcher.VerifyAccess();
			currentLabels = null;
			if (bookmark == null || (verifyBookmark && !viewBookmarkProvider.BookmarksViewOrder.Contains(bookmark)))
				bookmark = viewBookmarkProvider.DefaultBookmark;
			if (activeBookmark == bookmark)
				return;
			activeBookmark = bookmark;
			viewBookmarkProvider.SetActiveBookmark(activeBookmark);
			ActiveBookmarkChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}

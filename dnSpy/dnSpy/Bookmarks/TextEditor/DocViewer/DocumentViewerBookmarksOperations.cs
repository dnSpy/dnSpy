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
using dnSpy.Contracts.Bookmarks.Navigator;
using dnSpy.Contracts.ToolWindows.App;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Bookmarks.TextEditor.DocViewer {
	abstract class DocumentViewerBookmarksOperationsProvider {
		public abstract DocumentViewerBookmarksOperations Create(ITextView textView);
	}

	[Export(typeof(DocumentViewerBookmarksOperationsProvider))]
	sealed class DocumentViewerBookmarksOperationsProviderImpl : DocumentViewerBookmarksOperationsProvider {
		readonly IDsToolWindowService toolWindowService;
		readonly TextViewBookmarkService textViewBookmarkService;
		readonly Lazy<BookmarksService> bookmarksService;
		readonly Lazy<BookmarkNavigator> bookmarkNavigator;

		[ImportingConstructor]
		DocumentViewerBookmarksOperationsProviderImpl(IDsToolWindowService toolWindowService, TextViewBookmarkService textViewBookmarkService, Lazy<BookmarksService> bookmarksService, Lazy<BookmarkNavigator> bookmarkNavigator) {
			this.toolWindowService = toolWindowService;
			this.textViewBookmarkService = textViewBookmarkService;
			this.bookmarksService = bookmarksService;
			this.bookmarkNavigator = bookmarkNavigator;
		}

		public override DocumentViewerBookmarksOperations Create(ITextView textView) {
			if (textView is null)
				throw new ArgumentNullException(nameof(textView));
			return textView.Properties.GetOrCreateSingletonProperty(() => new DocumentViewerBookmarksOperationsImpl(textView, toolWindowService, textViewBookmarkService, bookmarksService, bookmarkNavigator));
		}
	}

	abstract class DocumentViewerBookmarksOperations {
		public abstract void ShowBookmarkWindow();
		public abstract void ClearAllBookmarksInDocument();
		public abstract void ClearBookmarks();
		public abstract void EnableAllBookmarks();
		public abstract void EnableBookmark();
		public abstract void ToggleBookmark();
		public abstract void SelectNextBookmark();
		public abstract void SelectPreviousBookmark();
		public abstract void SelectNextBookmarkInDocument();
		public abstract void SelectPreviousBookmarkInDocument();
		public abstract void SelectNextBookmarkWithSameLabel();
		public abstract void SelectPreviousBookmarkWithSameLabel();
	}

	sealed class DocumentViewerBookmarksOperationsImpl : DocumentViewerBookmarksOperations {
		readonly ITextView textView;
		readonly IDsToolWindowService toolWindowService;
		readonly TextViewBookmarkService textViewBookmarkService;
		readonly Lazy<BookmarksService> bookmarksService;
		readonly Lazy<BookmarkNavigator> bookmarkNavigator;

		public DocumentViewerBookmarksOperationsImpl(ITextView textView, IDsToolWindowService toolWindowService, TextViewBookmarkService textViewBookmarkService, Lazy<BookmarksService> bookmarksService, Lazy<BookmarkNavigator> bookmarkNavigator) {
			this.textView = textView ?? throw new ArgumentNullException(nameof(textView));
			this.toolWindowService = toolWindowService ?? throw new ArgumentNullException(nameof(toolWindowService));
			this.textViewBookmarkService = textViewBookmarkService ?? throw new ArgumentNullException(nameof(textViewBookmarkService));
			this.bookmarksService = bookmarksService ?? throw new ArgumentNullException(nameof(bookmarksService));
			this.bookmarkNavigator = bookmarkNavigator ?? throw new ArgumentNullException(nameof(bookmarkNavigator));
		}

		public override void ShowBookmarkWindow() => toolWindowService.Show(ToolWindows.Bookmarks.BookmarksToolWindowContent.THE_GUID);
		public override void ClearAllBookmarksInDocument() => textViewBookmarkService.ClearAllBookmarksInDocument();
		public override void ClearBookmarks() => bookmarksService.Value.Clear();
		public override void EnableAllBookmarks() => textViewBookmarkService.EnableAllBookmarks();
		public override void EnableBookmark() => textViewBookmarkService.ToggleEnableBookmark(textView);
		public override void ToggleBookmark() => textViewBookmarkService.ToggleCreateBookmark(textView);
		public override void SelectNextBookmark() => bookmarkNavigator.Value.SelectNextBookmark();
		public override void SelectPreviousBookmark() => bookmarkNavigator.Value.SelectPreviousBookmark();
		public override void SelectNextBookmarkInDocument() => bookmarkNavigator.Value.SelectNextBookmarkInDocument();
		public override void SelectPreviousBookmarkInDocument() => bookmarkNavigator.Value.SelectPreviousBookmarkInDocument();
		public override void SelectNextBookmarkWithSameLabel() => bookmarkNavigator.Value.SelectNextBookmarkWithSameLabel();
		public override void SelectPreviousBookmarkWithSameLabel() => bookmarkNavigator.Value.SelectPreviousBookmarkWithSameLabel();
	}
}

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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
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

		[ImportingConstructor]
		DocumentViewerBookmarksOperationsProviderImpl(IDsToolWindowService toolWindowService, TextViewBookmarkService textViewBookmarkService, Lazy<BookmarksService> bookmarksService) {
			this.toolWindowService = toolWindowService;
			this.textViewBookmarkService = textViewBookmarkService;
			this.bookmarksService = bookmarksService;
		}

		public override DocumentViewerBookmarksOperations Create(ITextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			return textView.Properties.GetOrCreateSingletonProperty(() => new DocumentViewerBookmarksOperationsImpl(textView, toolWindowService, textViewBookmarkService, bookmarksService));
		}
	}

	abstract class DocumentViewerBookmarksOperations {
		public abstract void ShowBookmarkWindow();
		public abstract void ClearAllBookmarksInDocument();
		public abstract void ClearBookmarks();
		public abstract void EnableAllBookmarks();
		public abstract void EnableBookmark();
		public abstract void ToggleBookmark();
		public abstract void NextBookmark();
		public abstract void PreviousBookmark();
		public abstract void NextBookmarkInDocument();
		public abstract void PreviousBookmarkInDocument();
		public abstract void NextBookmarkWithSameLabel();
		public abstract void PreviousBookmarkWithSameLabel();
	}

	sealed class DocumentViewerBookmarksOperationsImpl : DocumentViewerBookmarksOperations {
		readonly ITextView textView;
		readonly IDsToolWindowService toolWindowService;
		readonly TextViewBookmarkService textViewBookmarkService;
		readonly Lazy<BookmarksService> bookmarksService;

		public DocumentViewerBookmarksOperationsImpl(ITextView textView, IDsToolWindowService toolWindowService, TextViewBookmarkService textViewBookmarkService, Lazy<BookmarksService> bookmarksService) {
			this.textView = textView ?? throw new ArgumentNullException(nameof(textView));
			this.toolWindowService = toolWindowService ?? throw new ArgumentNullException(nameof(toolWindowService));
			this.textViewBookmarkService = textViewBookmarkService ?? throw new ArgumentNullException(nameof(textViewBookmarkService));
			this.bookmarksService = bookmarksService ?? throw new ArgumentNullException(nameof(bookmarksService));
		}

		public override void ShowBookmarkWindow() => toolWindowService.Show(ToolWindows.Bookmarks.BookmarksToolWindowContent.THE_GUID);

		public override void ClearAllBookmarksInDocument() {
			//TODO:
		}

		public override void ClearBookmarks() => bookmarksService.Value.Clear();

		public override void EnableAllBookmarks() {
			var bookmarks = bookmarksService.Value.Bookmarks;
			bool enable = !bookmarks.All(a => a.IsEnabled);

			var newSettings = new List<BookmarkAndSettings>(bookmarks.Length);
			for (int i = 0; i < bookmarks.Length; i++) {
				var bm = bookmarks[i];
				var settings = bm.Settings;
				if (settings.IsEnabled == enable)
					continue;
				settings.IsEnabled = enable;
				newSettings.Add(new BookmarkAndSettings(bm, settings));
			}
			if (newSettings.Count > 0)
				bookmarksService.Value.Modify(newSettings.ToArray());
		}

		public override void EnableBookmark() => textViewBookmarkService.ToggleEnableBookmark(textView);
		public override void ToggleBookmark() => textViewBookmarkService.ToggleCreateBookmark(textView);

		public override void NextBookmark() {
			//TODO:
		}

		public override void PreviousBookmark() {
			//TODO:
		}

		public override void NextBookmarkInDocument() {
			//TODO:
		}

		public override void PreviousBookmarkInDocument() {
			//TODO:
		}

		public override void NextBookmarkWithSameLabel() {
			//TODO:
		}

		public override void PreviousBookmarkWithSameLabel() {
			//TODO:
		}
	}
}

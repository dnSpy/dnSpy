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
using dnSpy.Bookmarks.TextEditor;
using dnSpy.Contracts.Bookmarks.Navigator;

namespace dnSpy.Bookmarks.Commands {
	abstract class MainMenuOperations {
		public abstract bool CanToggleBookmark { get; }
		public abstract void ToggleBookmark();
		public abstract EnableAllBookmarksKind GetEnableAllBookmarksKind();
		public abstract bool CanEnableAllBookmarks { get; }
		public abstract void EnableAllBookmarks();
		public abstract bool CanEnableBookmark { get; }
		public abstract void EnableBookmark();
		public abstract bool CanClearBookmarks { get; }
		public abstract void ClearBookmarks();
		public abstract bool CanClearAllBookmarksInDocument { get; }
		public abstract void ClearAllBookmarksInDocument();
		public abstract bool CanSelectPreviousBookmark { get; }
		public abstract void SelectPreviousBookmark();
		public abstract bool CanSelectNextBookmark { get; }
		public abstract void SelectNextBookmark();
		public abstract bool CanSelectPreviousBookmarkWithSameLabel { get; }
		public abstract void SelectPreviousBookmarkWithSameLabel();
		public abstract bool CanSelectNextBookmarkWithSameLabel { get; }
		public abstract void SelectNextBookmarkWithSameLabel();
		public abstract bool CanSelectPreviousBookmarkInDocument { get; }
		public abstract void SelectPreviousBookmarkInDocument();
		public abstract bool CanSelectNextBookmarkInDocument { get; }
		public abstract void SelectNextBookmarkInDocument();
	}

	[Export(typeof(MainMenuOperations))]
	sealed class MainMenuOperationsImpl : MainMenuOperations {
		readonly Lazy<TextViewBookmarkService> textViewBookmarkService;
		readonly Lazy<BookmarkNavigator> bookmarkNavigator;

		[ImportingConstructor]
		MainMenuOperationsImpl(Lazy<TextViewBookmarkService> textViewBookmarkService, Lazy<BookmarkNavigator> bookmarkNavigator) {
			this.textViewBookmarkService = textViewBookmarkService;
			this.bookmarkNavigator = bookmarkNavigator;
		}

		public override bool CanToggleBookmark => textViewBookmarkService.Value.CanToggleCreateBookmark;
		public override void ToggleBookmark() => textViewBookmarkService.Value.ToggleCreateBookmark();

		public override EnableAllBookmarksKind GetEnableAllBookmarksKind() => textViewBookmarkService.Value.GetEnableAllBookmarksKind();
		public override bool CanEnableAllBookmarks => textViewBookmarkService.Value.CanEnableAllBookmarks;
		public override void EnableAllBookmarks() => textViewBookmarkService.Value.EnableAllBookmarks();

		public override bool CanEnableBookmark => textViewBookmarkService.Value.CanToggleEnableBookmark;
		public override void EnableBookmark() => textViewBookmarkService.Value.ToggleEnableBookmark();

		public override bool CanClearBookmarks => textViewBookmarkService.Value.CanClearBookmarks;
		public override void ClearBookmarks() => textViewBookmarkService.Value.ClearBookmarks();

		public override bool CanClearAllBookmarksInDocument => textViewBookmarkService.Value.CanClearAllBookmarksInDocument;
		public override void ClearAllBookmarksInDocument() => textViewBookmarkService.Value.ClearAllBookmarksInDocument();

		public override bool CanSelectPreviousBookmark => bookmarkNavigator.Value.CanSelectPreviousBookmark;
		public override void SelectPreviousBookmark() => bookmarkNavigator.Value.SelectPreviousBookmark();

		public override bool CanSelectNextBookmark => bookmarkNavigator.Value.CanSelectNextBookmark;
		public override void SelectNextBookmark() => bookmarkNavigator.Value.SelectNextBookmark();

		public override bool CanSelectPreviousBookmarkWithSameLabel => bookmarkNavigator.Value.CanSelectPreviousBookmarkWithSameLabel;
		public override void SelectPreviousBookmarkWithSameLabel() => bookmarkNavigator.Value.SelectPreviousBookmarkWithSameLabel();

		public override bool CanSelectNextBookmarkWithSameLabel => bookmarkNavigator.Value.CanSelectNextBookmarkWithSameLabel;
		public override void SelectNextBookmarkWithSameLabel() => bookmarkNavigator.Value.SelectNextBookmarkWithSameLabel();

		public override bool CanSelectPreviousBookmarkInDocument => bookmarkNavigator.Value.CanSelectPreviousBookmarkInDocument;
		public override void SelectPreviousBookmarkInDocument() => bookmarkNavigator.Value.SelectPreviousBookmarkInDocument();

		public override bool CanSelectNextBookmarkInDocument => bookmarkNavigator.Value.CanSelectNextBookmarkInDocument;
		public override void SelectNextBookmarkInDocument() => bookmarkNavigator.Value.SelectNextBookmarkInDocument();
	}
}

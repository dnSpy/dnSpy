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
using dnSpy.Bookmarks.TextEditor;

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
		public abstract bool CanGoToPreviousBookmark { get; }
		public abstract void GoToPreviousBookmark();
		public abstract bool CanGoToNextBookmark { get; }
		public abstract void GoToNextBookmark();
		public abstract bool CanGoToPreviousBookmarkWithSameLabel { get; }
		public abstract void GoToPreviousBookmarkWithSameLabel();
		public abstract bool CanGoToNextBookmarkWithSameLabel { get; }
		public abstract void GoToNextBookmarkWithSameLabel();
		public abstract bool CanGoToPreviousBookmarkInDocument { get; }
		public abstract void GoToPreviousBookmarkInDocument();
		public abstract bool CanGoToNextBookmarkInDocument { get; }
		public abstract void GoToNextBookmarkInDocument();
	}

	[Export(typeof(MainMenuOperations))]
	sealed class MainMenuOperationsImpl : MainMenuOperations {
		readonly Lazy<TextViewBookmarkService> textViewBookmarkService;

		[ImportingConstructor]
		MainMenuOperationsImpl(Lazy<TextViewBookmarkService> textViewBookmarkService) =>
			this.textViewBookmarkService = textViewBookmarkService;

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

		public override bool CanGoToPreviousBookmark => true;//TODO:
		public override void GoToPreviousBookmark() {
			//TODO:
		}

		public override bool CanGoToNextBookmark => true;//TODO:
		public override void GoToNextBookmark() {
			//TODO:
		}

		public override bool CanGoToPreviousBookmarkWithSameLabel => true;//TODO:
		public override void GoToPreviousBookmarkWithSameLabel() {
			//TODO:
		}

		public override bool CanGoToNextBookmarkWithSameLabel => true;//TODO:
		public override void GoToNextBookmarkWithSameLabel() {
			//TODO:
		}

		public override bool CanGoToPreviousBookmarkInDocument => true;//TODO:
		public override void GoToPreviousBookmarkInDocument() {
			//TODO:
		}

		public override bool CanGoToNextBookmarkInDocument => true;//TODO:
		public override void GoToNextBookmarkInDocument() {
			//TODO:
		}
	}
}

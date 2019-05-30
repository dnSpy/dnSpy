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
using System.Diagnostics;
using dnSpy.Contracts.Command;

namespace dnSpy.Bookmarks.TextEditor.DocViewer {
	sealed class DocumentViewerCommandTargetFilter : ICommandTargetFilter {
		readonly DocumentViewerBookmarksOperations documentViewerBookmarksOperations;

		public DocumentViewerCommandTargetFilter(DocumentViewerBookmarksOperations documentViewerBookmarksOperations) => this.documentViewerBookmarksOperations = documentViewerBookmarksOperations;

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (group == CommandConstants.BookmarkGroup) {
				switch ((BookmarkIds)cmdId) {
				case BookmarkIds.ShowBookmarkWindow:
				case BookmarkIds.ClearAllBookmarksInDocument:
				case BookmarkIds.ClearBookmarks:
				case BookmarkIds.EnableAllBookmarks:
				case BookmarkIds.EnableBookmark:
				case BookmarkIds.ToggleBookmark:
				case BookmarkIds.NextBookmark:
				case BookmarkIds.PreviousBookmark:
				case BookmarkIds.NextBookmarkInDocument:
				case BookmarkIds.PreviousBookmarkInDocument:
				case BookmarkIds.NextBookmarkWithSameLabel:
				case BookmarkIds.PreviousBookmarkWithSameLabel:
					return CommandTargetStatus.Handled;

				default:
					Debug.Fail($"Unknown {nameof(BookmarkIds)} id: {(BookmarkIds)cmdId}");
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object? args = null) {
			object? result = null;
			return Execute(group, cmdId, args, ref result);
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object? args, ref object? result) {
			if (group == CommandConstants.BookmarkGroup) {
				switch ((BookmarkIds)cmdId) {
				case BookmarkIds.ShowBookmarkWindow:
					documentViewerBookmarksOperations.ShowBookmarkWindow();
					return CommandTargetStatus.Handled;

				case BookmarkIds.ClearAllBookmarksInDocument:
					documentViewerBookmarksOperations.ClearAllBookmarksInDocument();
					return CommandTargetStatus.Handled;

				case BookmarkIds.ClearBookmarks:
					documentViewerBookmarksOperations.ClearBookmarks();
					return CommandTargetStatus.Handled;

				case BookmarkIds.EnableAllBookmarks:
					documentViewerBookmarksOperations.EnableAllBookmarks();
					return CommandTargetStatus.Handled;

				case BookmarkIds.EnableBookmark:
					documentViewerBookmarksOperations.EnableBookmark();
					return CommandTargetStatus.Handled;

				case BookmarkIds.ToggleBookmark:
					documentViewerBookmarksOperations.ToggleBookmark();
					return CommandTargetStatus.Handled;

				case BookmarkIds.NextBookmark:
					documentViewerBookmarksOperations.SelectNextBookmark();
					return CommandTargetStatus.Handled;

				case BookmarkIds.PreviousBookmark:
					documentViewerBookmarksOperations.SelectPreviousBookmark();
					return CommandTargetStatus.Handled;

				case BookmarkIds.NextBookmarkInDocument:
					documentViewerBookmarksOperations.SelectNextBookmarkInDocument();
					return CommandTargetStatus.Handled;

				case BookmarkIds.PreviousBookmarkInDocument:
					documentViewerBookmarksOperations.SelectPreviousBookmarkInDocument();
					return CommandTargetStatus.Handled;

				case BookmarkIds.NextBookmarkWithSameLabel:
					documentViewerBookmarksOperations.SelectNextBookmarkWithSameLabel();
					return CommandTargetStatus.Handled;

				case BookmarkIds.PreviousBookmarkWithSameLabel:
					documentViewerBookmarksOperations.SelectPreviousBookmarkWithSameLabel();
					return CommandTargetStatus.Handled;

				default:
					Debug.Fail($"Unknown {nameof(BookmarkIds)} id: {(BookmarkIds)cmdId}");
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}
}

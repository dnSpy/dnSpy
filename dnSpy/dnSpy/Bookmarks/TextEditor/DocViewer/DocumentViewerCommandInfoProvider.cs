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

using System.Collections.Generic;
using System.Windows.Input;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Bookmarks.TextEditor.DocViewer {
	[ExportCommandInfoProvider(CommandInfoProviderOrder.Bookmarks)]
	sealed class DocumentViewerCommandInfoProvider : ICommandInfoProvider {
		public IEnumerable<CommandShortcut> GetCommandShortcuts(object target) {
			var textView = target as ITextView;
			if (textView?.Roles.Contains(PredefinedDsTextViewRoles.DocumentViewer) != true)
				yield break;

			yield return CommandShortcut.Create(KeyInput.Control(Key.K), KeyInput.Control(Key.W), BookmarkIds.ShowBookmarkWindow.ToCommandInfo());

			//yield return CommandShortcut.Create(Key.XXXXXXXXXXXX, BookmarkIds.ClearAllBookmarksInDocument.ToCommandInfo());

			yield return CommandShortcut.Create(KeyInput.Control(Key.K), KeyInput.Control(Key.L), BookmarkIds.ClearBookmarks.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.B), KeyInput.Control(Key.C), BookmarkIds.ClearBookmarks.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.B), KeyInput.Create(Key.C), BookmarkIds.ClearBookmarks.ToCommandInfo());
			yield return CommandShortcut.CtrlShift(Key.F2, BookmarkIds.ClearBookmarks.ToCommandInfo());

			//yield return CommandShortcut.Create(Key.XXXXXXXXXXXX, BookmarkIds.EnableAllBookmarks.ToCommandInfo());

			yield return CommandShortcut.Create(KeyInput.Control(Key.B), KeyInput.Control(Key.E), BookmarkIds.EnableBookmark.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.B), KeyInput.Create(Key.E), BookmarkIds.EnableBookmark.ToCommandInfo());

			yield return CommandShortcut.Create(KeyInput.Control(Key.K), KeyInput.Control(Key.K), BookmarkIds.ToggleBookmark.ToCommandInfo());
			yield return CommandShortcut.Control(Key.F2, BookmarkIds.ToggleBookmark.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.B), KeyInput.Control(Key.T), BookmarkIds.ToggleBookmark.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.B), KeyInput.Create(Key.T), BookmarkIds.ToggleBookmark.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.K), KeyInput.Control(Key.T), BookmarkIds.ToggleBookmark.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.K), KeyInput.Create(Key.T), BookmarkIds.ToggleBookmark.ToCommandInfo());

			yield return CommandShortcut.Create(Key.F2, BookmarkIds.NextBookmark.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.K), KeyInput.Control(Key.N), BookmarkIds.NextBookmark.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.K), KeyInput.Create(Key.N), BookmarkIds.NextBookmark.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.B), KeyInput.Control(Key.N), BookmarkIds.NextBookmark.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.B), KeyInput.Create(Key.N), BookmarkIds.NextBookmark.ToCommandInfo());

			yield return CommandShortcut.Shift(Key.F2, BookmarkIds.PreviousBookmark.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.K), KeyInput.Control(Key.P), BookmarkIds.PreviousBookmark.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.K), KeyInput.Create(Key.P), BookmarkIds.PreviousBookmark.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.B), KeyInput.Control(Key.P), BookmarkIds.PreviousBookmark.ToCommandInfo());
			yield return CommandShortcut.Create(KeyInput.Control(Key.B), KeyInput.Create(Key.P), BookmarkIds.PreviousBookmark.ToCommandInfo());

			//yield return CommandShortcut.Create(Key.XXXXXXXXXXXX, BookmarkIds.NextBookmarkInDocument.ToCommandInfo());
			//yield return CommandShortcut.Create(Key.XXXXXXXXXXXX, BookmarkIds.PreviousBookmarkInDocument.ToCommandInfo());

			//yield return CommandShortcut.Create(Key.XXXXXXXXXXXX, BookmarkIds.NextBookmarkWithSameLabel.ToCommandInfo());
			//yield return CommandShortcut.Create(Key.XXXXXXXXXXXX, BookmarkIds.PreviousBookmarkWithSameLabel.ToCommandInfo());
		}
	}
}

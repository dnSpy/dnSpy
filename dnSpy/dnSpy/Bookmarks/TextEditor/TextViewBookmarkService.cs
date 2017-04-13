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
using dnSpy.Contracts.Bookmarks.Navigator;
using dnSpy.Contracts.Bookmarks.TextEditor;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Bookmarks.TextEditor {
	abstract class TextViewBookmarkService {
		public abstract void ToggleCreateBookmark(ITextView textView);
		public abstract bool CanToggleCreateBookmark { get; }
		public abstract void ToggleCreateBookmark();
		public abstract ToggleCreateBookmarkKind GetToggleCreateBookmarkKind();
		public abstract bool CanToggleEnableBookmark { get; }
		public abstract void ToggleEnableBookmark(ITextView textView);
		public abstract void ToggleEnableBookmark();
		public abstract ToggleEnableBookmarkKind GetToggleEnableBookmarkKind();
		public abstract bool CanClearBookmarks { get; }
		public abstract void ClearBookmarks();
		public abstract EnableAllBookmarksKind GetEnableAllBookmarksKind();
		public abstract bool CanEnableAllBookmarks { get; }
		public abstract void EnableAllBookmarks();
		public abstract bool CanClearAllBookmarksInDocument { get; }
		public abstract void ClearAllBookmarksInDocument();
	}

	enum EnableAllBookmarksKind {
		None,
		Enable,
		Disable,
	}

	enum ToggleCreateBookmarkKind {
		None,
		Add,
		Delete,
	}

	enum ToggleEnableBookmarkKind {
		None,
		Enable,
		Disable,
	}

	[Export(typeof(TextViewBookmarkService))]
	sealed class TextViewBookmarkServiceImpl : TextViewBookmarkService {
		readonly Lazy<IDocumentTabService> documentTabService;
		readonly Lazy<BookmarksService> bookmarksService;
		readonly Lazy<BookmarkNavigator> bookmarkNavigator;
		readonly Lazy<TextViewBookmarkLocationProvider>[] textViewBookmarkLocationProviders;

		[ImportingConstructor]
		TextViewBookmarkServiceImpl(Lazy<IDocumentTabService> documentTabService, Lazy<BookmarksService> bookmarksService, Lazy<BookmarkNavigator> bookmarkNavigator, [ImportMany] IEnumerable<Lazy<TextViewBookmarkLocationProvider>> textViewBookmarkLocationProviders) {
			this.documentTabService = documentTabService;
			this.bookmarksService = bookmarksService;
			this.bookmarkNavigator = bookmarkNavigator;
			this.textViewBookmarkLocationProviders = textViewBookmarkLocationProviders.ToArray();
		}

		ITextView GetTextView() => GetTextView(documentTabService.Value.ActiveTab);
		ITextView GetTextView(IDocumentTab tab) => (tab?.UIContext as IDocumentViewer)?.TextView;

		TextViewBookmarkLocationResult? GetLocation(IDocumentTab tab, VirtualSnapshotPoint? position) {
			var textView = GetTextView(tab);
			if (textView == null)
				return null;
			var pos = position ?? textView.Caret.Position.VirtualBufferPosition;
			if (pos.Position.Snapshot != textView.TextSnapshot)
				throw new ArgumentException();
			TextViewBookmarkLocationResult? res = null;
			foreach (var lz in textViewBookmarkLocationProviders) {
				var result = lz.Value.CreateLocation(tab, textView, pos);
				if (result?.Location == null || result.Value.Span.Snapshot != textView.TextSnapshot)
					continue;
				if (res == null || result.Value.Span.Start < res.Value.Span.Start)
					res = result;
			}
			return res;
		}

		Bookmark[] GetBookmarks(TextViewBookmarkLocationResult locations) {
			var list = new List<Bookmark>();
			foreach (var bm in bookmarksService.Value.Bookmarks) {
				var loc = locations.Location;
				if (bm.Location.Equals(loc)) {
					list.Add(bm);
					break;
				}
			}
			return list.ToArray();
		}

		Bookmark[] GetBookmarks(IDocumentTab tab, VirtualSnapshotPoint? position) {
			if (GetLocation(tab, position) is TextViewBookmarkLocationResult locRes)
				return GetBookmarks(locRes);
			return Array.Empty<Bookmark>();
		}

		IDocumentTab GetTab(ITextView textView) {
			foreach (var g in documentTabService.Value.TabGroupService.TabGroups) {
				foreach (var t in g.TabContents) {
					var tab = t as IDocumentTab;
					if (GetTextView(tab) == textView)
						return tab;
				}
			}
			return null;
		}

		public override void ToggleCreateBookmark(ITextView textView) =>
			ToggleCreateBookmark(GetToggleCreateBookmarkInfo(GetTab(textView), textView.Caret.Position.VirtualBufferPosition));

		public override bool CanToggleCreateBookmark => GetToggleCreateBookmarkInfo(documentTabService.Value.ActiveTab, null).kind != ToggleCreateBookmarkKind.None;
		public override void ToggleCreateBookmark() => ToggleCreateBookmark(GetToggleCreateBookmarkInfo(documentTabService.Value.ActiveTab, null));
		public override ToggleCreateBookmarkKind GetToggleCreateBookmarkKind() => GetToggleCreateBookmarkInfo(documentTabService.Value.ActiveTab, null).kind;

		(ToggleCreateBookmarkKind kind, Bookmark[] bookmarks, BookmarkLocation location) GetToggleCreateBookmarkInfo(IDocumentTab tab, VirtualSnapshotPoint? position) {
			var locRes = GetLocation(tab, position);
			var bms = locRes == null ? Array.Empty<Bookmark>() : GetBookmarks(locRes.Value);
			if (bms.Length != 0)
				return (ToggleCreateBookmarkKind.Delete, bms, null);
			else {
				if (locRes == null || locRes.Value.Location == null)
					return (ToggleCreateBookmarkKind.None, Array.Empty<Bookmark>(), null);
				return (ToggleCreateBookmarkKind.Add, Array.Empty<Bookmark>(), locRes.Value.Location);
			}
		}

		void ToggleCreateBookmark((ToggleCreateBookmarkKind kind, Bookmark[] bookmarks, BookmarkLocation location) info) {
			switch (info.kind) {
			case ToggleCreateBookmarkKind.Add:
				var bookmark = bookmarksService.Value.Add(new Contracts.Bookmarks.BookmarkInfo(info.location, new BookmarkSettings() { IsEnabled = true }));
				if (bookmark != null)
					bookmarkNavigator.Value.ActiveBookmark = bookmark;
				break;

			case ToggleCreateBookmarkKind.Delete:
				bookmarksService.Value.Remove(info.bookmarks);
				break;

			case ToggleCreateBookmarkKind.None:
			default:
				return;
			}
		}

		public override bool CanToggleEnableBookmark => GetToggleEnableBookmarkInfo(documentTabService.Value.ActiveTab, null).kind != ToggleEnableBookmarkKind.None;
		public override void ToggleEnableBookmark(ITextView textView) => ToggleEnableBookmark(GetTab(textView), textView.Caret.Position.VirtualBufferPosition);
		public override void ToggleEnableBookmark() => ToggleEnableBookmark(documentTabService.Value.ActiveTab, null);
		void ToggleEnableBookmark(IDocumentTab tab, VirtualSnapshotPoint? position) {
			var info = GetToggleEnableBookmarkInfo(tab, position);
			bool newIsEnabled;
			switch (info.kind) {
			case ToggleEnableBookmarkKind.Enable:
				newIsEnabled = true;
				break;

			case ToggleEnableBookmarkKind.Disable:
				newIsEnabled = false;
				break;

			case ToggleEnableBookmarkKind.None:
			default:
				return;
			}
			bookmarksService.Value.Modify(info.bookmarks.Select(a => {
				var newSettings = a.Settings;
				newSettings.IsEnabled = newIsEnabled;
				return new BookmarkAndSettings(a, newSettings);
			}).ToArray());
		}

		public override ToggleEnableBookmarkKind GetToggleEnableBookmarkKind() => GetToggleEnableBookmarkInfo(documentTabService.Value.ActiveTab, null).kind;

		(ToggleEnableBookmarkKind kind, Bookmark[] bookmarks) GetToggleEnableBookmarkInfo(IDocumentTab tab, VirtualSnapshotPoint? position) {
			var bms = GetBookmarks(tab, position);
			if (bms.Length == 0)
				return (ToggleEnableBookmarkKind.None, Array.Empty<Bookmark>());
			bool newIsEnabled = !bms.All(a => a.IsEnabled);
			var kind = newIsEnabled ? ToggleEnableBookmarkKind.Enable : ToggleEnableBookmarkKind.Disable;
			return (kind, bms);
		}

		public override bool CanClearBookmarks => bookmarksService.Value.Bookmarks.Length > 0;
		public override void ClearBookmarks() => bookmarksService.Value.Clear();

		EnableAllBookmarksKind GetEnableAllBookmarksKind(Bookmark[] bookmarks) {
			if (bookmarks.Length == 0)
				return EnableAllBookmarksKind.None;
			if (bookmarks.All(a => a.IsEnabled))
				return EnableAllBookmarksKind.Disable;
			return EnableAllBookmarksKind.Enable;
		}
		public override EnableAllBookmarksKind GetEnableAllBookmarksKind() => GetEnableAllBookmarksKind(bookmarksService.Value.Bookmarks);
		public override bool CanEnableAllBookmarks => GetEnableAllBookmarksKind() != EnableAllBookmarksKind.None;
		public override void EnableAllBookmarks() {
			var bookmarks = bookmarksService.Value.Bookmarks;
			var kind = GetEnableAllBookmarksKind(bookmarks);
			if (kind == EnableAllBookmarksKind.None)
				return;
			bool enable = kind == EnableAllBookmarksKind.Enable;

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

		public override bool CanClearAllBookmarksInDocument => true;//TODO:
		public override void ClearAllBookmarksInDocument() {
			//TODO:
		}
	}
}

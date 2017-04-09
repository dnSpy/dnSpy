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
using dnSpy.Contracts.Bookmarks.TextEditor;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Bookmarks.TextEditor {
	abstract class TextViewBookmarkService {
		public abstract void ToggleCreateBookmark(ITextView textView, VirtualSnapshotPoint position);
		public abstract bool CanToggleCreateBookmark { get; }
		public abstract void ToggleCreateBookmark();
		public abstract ToggleCreateBookmarkKind GetToggleCreateBookmarkKind();
		public abstract bool CanToggleEnableBookmark { get; }
		public abstract void ToggleEnableBookmark();
		public abstract ToggleEnableBookmarkKind GetToggleEnableBookmarkKind();
	}

	enum ToggleCreateBookmarkKind {
		None,
		Add,
		Delete,
		Enable,
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
		readonly Lazy<TextViewBookmarkLocationProvider>[] textViewBookmarkLocationProviders;

		[ImportingConstructor]
		TextViewBookmarkServiceImpl(Lazy<IDocumentTabService> documentTabService, Lazy<BookmarksService> bookmarksService, [ImportMany] IEnumerable<Lazy<TextViewBookmarkLocationProvider>> textViewBookmarkLocationProviders) {
			this.documentTabService = documentTabService;
			this.bookmarksService = bookmarksService;
			this.textViewBookmarkLocationProviders = textViewBookmarkLocationProviders.ToArray();
		}

		ITextView GetTextView() => GetTextView(documentTabService.Value.ActiveTab);
		ITextView GetTextView(IDocumentTab tab) => (tab?.UIContext as IDocumentViewer)?.TextView;

		TextViewBookmarkLocationResult? GetLocations(IDocumentTab tab, VirtualSnapshotPoint? position) {
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

		Bookmark[] GetBookmarks() {
			if (GetLocations(documentTabService.Value.ActiveTab, null) is TextViewBookmarkLocationResult locRes)
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

		public override void ToggleCreateBookmark(ITextView textView, VirtualSnapshotPoint position) =>
			ToggleCreateBookmark(GetToggleCreateBookmarkInfo(GetTab(textView), position));

		public override bool CanToggleCreateBookmark => GetToggleCreateBookmarkInfo(documentTabService.Value.ActiveTab, null).kind != ToggleCreateBookmarkKind.None;
		public override void ToggleCreateBookmark() => ToggleCreateBookmark(GetToggleCreateBookmarkInfo(documentTabService.Value.ActiveTab, null));
		public override ToggleCreateBookmarkKind GetToggleCreateBookmarkKind() => GetToggleCreateBookmarkInfo(documentTabService.Value.ActiveTab, null).kind;

		(ToggleCreateBookmarkKind kind, Bookmark[] bookmarks, BookmarkLocation location) GetToggleCreateBookmarkInfo(IDocumentTab tab, VirtualSnapshotPoint? position) {
			var locRes = GetLocations(tab, position);
			var bms = locRes == null ? Array.Empty<Bookmark>() : GetBookmarks(locRes.Value);
			if (bms.Length != 0) {
				if (bms.All(a => a.IsEnabled))
					return (ToggleCreateBookmarkKind.Delete, bms, null);
				return (ToggleCreateBookmarkKind.Enable, bms, null);
			}
			else {
				if (locRes == null || locRes.Value.Location == null)
					return (ToggleCreateBookmarkKind.None, Array.Empty<Bookmark>(), null);
				return (ToggleCreateBookmarkKind.Add, Array.Empty<Bookmark>(), locRes.Value.Location);
			}
		}

		void ToggleCreateBookmark((ToggleCreateBookmarkKind kind, Bookmark[] bookmarks, BookmarkLocation location) info) {
			switch (info.kind) {
			case ToggleCreateBookmarkKind.Add:
				bookmarksService.Value.Add(new Contracts.Bookmarks.BookmarkInfo(info.location, new BookmarkSettings() { IsEnabled = true }));
				break;

			case ToggleCreateBookmarkKind.Delete:
				bookmarksService.Value.Remove(info.bookmarks);
				break;

			case ToggleCreateBookmarkKind.Enable:
				bookmarksService.Value.Modify(info.bookmarks.Select(a => {
					var newSettings = a.Settings;
					newSettings.IsEnabled = true;
					return new BookmarkAndSettings(a, newSettings);
				}).ToArray());
				break;

			case ToggleCreateBookmarkKind.None:
			default:
				return;
			}
		}

		public override bool CanToggleEnableBookmark => GetToggleEnableBookmarkInfo().kind != ToggleEnableBookmarkKind.None;
		public override void ToggleEnableBookmark() {
			var info = GetToggleEnableBookmarkInfo();
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

		public override ToggleEnableBookmarkKind GetToggleEnableBookmarkKind() => GetToggleEnableBookmarkInfo().kind;

		(ToggleEnableBookmarkKind kind, Bookmark[] bookmarks) GetToggleEnableBookmarkInfo() {
			var bms = GetBookmarks();
			if (bms.Length == 0)
				return (ToggleEnableBookmarkKind.None, Array.Empty<Bookmark>());
			bool newIsEnabled = !bms.All(a => a.IsEnabled);
			var kind = newIsEnabled ? ToggleEnableBookmarkKind.Enable : ToggleEnableBookmarkKind.Disable;
			return (kind, bms);
		}
	}
}

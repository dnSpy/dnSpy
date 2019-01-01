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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Bookmarks.Navigator;
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Bookmarks.Navigator;
using dnSpy.Contracts.Bookmarks.TextEditor;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.UI;
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
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<IDocumentTabService> documentTabService;
		readonly Lazy<BookmarksService> bookmarksService;
		readonly Lazy<BookmarkNavigator2> bookmarkNavigator;
		readonly Lazy<TextViewBookmarkLocationProvider>[] textViewBookmarkLocationProviders;
		readonly Lazy<BookmarkDocumentProvider, IBookmarkDocumentProviderMetadata>[] bookmarkDocumentProviders;

		[ImportingConstructor]
		TextViewBookmarkServiceImpl(UIDispatcher uiDispatcher, Lazy<IDocumentTabService> documentTabService, Lazy<BookmarksService> bookmarksService, Lazy<BookmarkNavigator2> bookmarkNavigator, [ImportMany] IEnumerable<Lazy<TextViewBookmarkLocationProvider>> textViewBookmarkLocationProviders, [ImportMany] IEnumerable<Lazy<BookmarkDocumentProvider, IBookmarkDocumentProviderMetadata>> bookmarkDocumentProviders) {
			this.uiDispatcher = uiDispatcher;
			this.documentTabService = documentTabService;
			this.bookmarksService = bookmarksService;
			this.bookmarkNavigator = bookmarkNavigator;
			this.textViewBookmarkLocationProviders = textViewBookmarkLocationProviders.ToArray();
			this.bookmarkDocumentProviders = bookmarkDocumentProviders.OrderBy(a => a.Metadata.Order).ToArray();
		}

		ITextView GetTextView() => GetTextView(documentTabService.Value.ActiveTab);
		ITextView GetTextView(IDocumentTab tab) => (tab?.UIContext as IDocumentViewer)?.TextView;

		readonly struct LocationsResult : IDisposable {
			public readonly TextViewBookmarkLocationResult? locRes;
			readonly Lazy<BookmarksService> bookmarksService;
			readonly List<BookmarkLocation> allLocations;

			public LocationsResult(Lazy<BookmarksService> bookmarksService, TextViewBookmarkLocationResult? locRes, List<BookmarkLocation> allLocations) {
				this.bookmarksService = bookmarksService;
				this.locRes = locRes;
				this.allLocations = allLocations;
			}

			public void Dispose() {
				if (allLocations.Count > 0)
					bookmarksService.Value.Close(allLocations.ToArray());
			}

			public BookmarkLocation TakeOwnership(BookmarkLocation location) {
				bool b = allLocations.Remove(location);
				Debug.Assert(b);
				return location;
			}
		}

		LocationsResult GetLocation(IDocumentTab tab, VirtualSnapshotPoint? position) {
			var allLocations = new List<BookmarkLocation>();
			var textView = GetTextView(tab);
			if (textView == null)
				return new LocationsResult(bookmarksService, null, allLocations);
			var pos = position ?? textView.Caret.Position.VirtualBufferPosition;
			if (pos.Position.Snapshot != textView.TextSnapshot)
				throw new ArgumentException();
			TextViewBookmarkLocationResult? res = null;
			foreach (var lz in textViewBookmarkLocationProviders) {
				var result = lz.Value.CreateLocation(tab, textView, pos);
				if (result?.Location == null)
					continue;
				allLocations.Add(result.Value.Location);
				if (result.Value.Span.Snapshot != textView.TextSnapshot)
					continue;
				if (res == null || result.Value.Span.Start < res.Value.Span.Start)
					res = result;
			}
			return new LocationsResult(bookmarksService, res, allLocations);
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
			using (var info = GetLocation(tab, position)) {
				if (info.locRes is TextViewBookmarkLocationResult locRes)
					return GetBookmarks(locRes);
				return Array.Empty<Bookmark>();
			}
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

		public override bool CanToggleCreateBookmark => GetToggleCreateBookmarkKind() != ToggleCreateBookmarkKind.None;
		public override void ToggleCreateBookmark() => ToggleCreateBookmark(GetToggleCreateBookmarkInfo(documentTabService.Value.ActiveTab, null));

		public override ToggleCreateBookmarkKind GetToggleCreateBookmarkKind() {
			using (var info = GetToggleCreateBookmarkInfo(documentTabService.Value.ActiveTab, null))
				return info.kind;
		}

		struct ToggleCreateBreakpointInfoResult : IDisposable {
			readonly Lazy<BookmarksService> bookmarksService;
			public readonly ToggleCreateBookmarkKind kind;
			public readonly Bookmark[] bookmarks;
			public BookmarkLocation location;
			public ToggleCreateBreakpointInfoResult(Lazy<BookmarksService> bookmarksService, ToggleCreateBookmarkKind kind, Bookmark[] bookmarks, BookmarkLocation location) {
				this.bookmarksService = bookmarksService;
				this.kind = kind;
				this.bookmarks = bookmarks;
				this.location = location;
			}

			public void Dispose() {
				if (location != null)
					bookmarksService.Value.Close(location);
			}

			public BookmarkLocation TakeOwnershipOfLocation() {
				var res = location;
				location = null;
				return res;
			}
		}

		ToggleCreateBreakpointInfoResult GetToggleCreateBookmarkInfo(IDocumentTab tab, VirtualSnapshotPoint? position) {
			using (var info = GetLocation(tab, position)) {
				var locRes = info.locRes;
				var bms = locRes == null ? Array.Empty<Bookmark>() : GetBookmarks(locRes.Value);
				if (bms.Length != 0)
					return new ToggleCreateBreakpointInfoResult(bookmarksService, ToggleCreateBookmarkKind.Delete, bms, null);
				else {
					if (locRes == null || locRes.Value.Location == null)
						return new ToggleCreateBreakpointInfoResult(bookmarksService, ToggleCreateBookmarkKind.None, Array.Empty<Bookmark>(), null);
					return new ToggleCreateBreakpointInfoResult(bookmarksService, ToggleCreateBookmarkKind.Add, Array.Empty<Bookmark>(), info.TakeOwnership(locRes.Value.Location));
				}
			}
		}

		void ToggleCreateBookmark(ToggleCreateBreakpointInfoResult info) {
			try {
				switch (info.kind) {
				case ToggleCreateBookmarkKind.Add:
					var bookmark = bookmarksService.Value.Add(new Contracts.Bookmarks.BookmarkInfo(info.TakeOwnershipOfLocation(), new BookmarkSettings() { IsEnabled = true }));
					if (bookmark != null)
						bookmarkNavigator.Value.SetActiveBookmarkNoCheck(bookmark);
					break;

				case ToggleCreateBookmarkKind.Delete:
					bookmarksService.Value.Remove(info.bookmarks);
					break;

				case ToggleCreateBookmarkKind.None:
				default:
					return;
				}
			}
			finally {
				// Don't use a using statement since the compiler will make a copy of it and this
				// copy will always dispose the bookmark location.
				info.Dispose();
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

		public override bool CanClearAllBookmarksInDocument => true;
		public override void ClearAllBookmarksInDocument() {
			var currentDoc = GetDocument(bookmarkNavigator.Value.ActiveBookmark);
			if (currentDoc == null)
				return;
			bookmarksService.Value.Remove(bookmarksService.Value.Bookmarks.Where(a => currentDoc.Equals(GetDocument(a))).ToArray());
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
	}
}

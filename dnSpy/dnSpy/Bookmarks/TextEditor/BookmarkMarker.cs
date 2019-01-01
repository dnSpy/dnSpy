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
using System.Linq;
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Text.Editor;
using dnSpy.UI;

namespace dnSpy.Bookmarks.TextEditor {
	[ExportDocumentViewerListener]
	sealed class BookmarkMarkerDocumentViewerListener : IDocumentViewerListener {
		[ImportingConstructor]
		BookmarkMarkerDocumentViewerListener(BookmarksService bookmarksService) {
			// Nothing, we just need to make sure that BookmarksService gets imported and constructed
		}

		void IDocumentViewerListener.OnEvent(DocumentViewerEventArgs e) {
			// Nothing, the ctor does all the work
		}
	}

	[Export(typeof(IBookmarksServiceListener))]
	sealed class BookmarkMarker : IBookmarksServiceListener {
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<IGlyphTextMarkerService> glyphTextMarkerService;
		readonly BookmarkGlyphTextMarkerLocationProviderService bookmarkGlyphTextMarkerLocationProviderService;
		readonly BookmarkGlyphTextMarkerHandler bookmarkGlyphTextMarkerHandler;
		BookmarkInfo[] bookmarkInfos;

		[ImportingConstructor]
		BookmarkMarker(UIDispatcher uiDispatcher, Lazy<IGlyphTextMarkerService> glyphTextMarkerService, BookmarkGlyphTextMarkerLocationProviderService bookmarkGlyphTextMarkerLocationProviderService, BookmarkGlyphTextMarkerHandler bookmarkGlyphTextMarkerHandler) {
			this.uiDispatcher = uiDispatcher;
			this.glyphTextMarkerService = glyphTextMarkerService;
			this.bookmarkGlyphTextMarkerLocationProviderService = bookmarkGlyphTextMarkerLocationProviderService;
			this.bookmarkGlyphTextMarkerHandler = bookmarkGlyphTextMarkerHandler;
			UI(() => Initialize_UI());
		}

		void UI(Action callback) => uiDispatcher.UI(callback);

		void Initialize_UI() {
			uiDispatcher.VerifyAccess();
			bookmarkInfos = new BookmarkInfo[(int)BookmarkKind.Last] {
				new BookmarkInfo(BookmarkKind.BookmarkDisabled, null, null, null, GlyphTextMarkerServiceZIndexes.DisabledBookmark),
				new BookmarkInfo(BookmarkKind.BookmarkEnabled, null, null, null, GlyphTextMarkerServiceZIndexes.Bookmark),
			};
		}

		void IBookmarksServiceListener.Initialize(BookmarksService bookmarksService) {
			bookmarksService.BookmarksChanged += BookmarksService_BookmarksChanged;
			bookmarksService.BookmarksModified += BookmarksService_BookmarksModified;
		}

		sealed class BookmarkData {
			public GlyphTextMarkerLocationInfo Location { get; }
			public IGlyphTextMarker Marker { get; set; }
			public BookmarkInfo Info { get; set; }
			public BookmarkData(GlyphTextMarkerLocationInfo location) => Location = location ?? throw new ArgumentNullException(nameof(location));
		}

		void BookmarksService_BookmarksChanged(object sender, CollectionChangedEventArgs<Bookmark> e) {
			if (e.Added)
				UI(() => OnBookmarksAdded_UI(e));
			else {
				var list = new List<(Bookmark bookmark, BookmarkData data)>(e.Objects.Count);
				foreach (var bm in e.Objects) {
					if (!bm.TryGetData(out BookmarkData data))
						continue;
					list.Add((bm, data));
				}
				if (list.Count > 0)
					UI(() => OnBookmarksRemoved_UI(list));
			}
		}

		void OnBookmarksAdded_UI(CollectionChangedEventArgs<Bookmark> e) {
			uiDispatcher.VerifyAccess();
			if (!e.Added)
				throw new InvalidOperationException();
			foreach (var bm in e.Objects) {
				var location = bookmarkGlyphTextMarkerLocationProviderService.GetLocation(bm);
				if (location != null) {
					bm.GetOrCreateData(() => new BookmarkData(location));
					UpdateMarker(bm);
					continue;
				}
			}
		}

		void OnBookmarksRemoved_UI(List<(Bookmark bookmark, BookmarkData data)> list) {
			uiDispatcher.VerifyAccess();
			glyphTextMarkerService.Value.Remove(list.Select(a => a.data.Marker).Where(a => a != null));
		}

		void BookmarksService_BookmarksModified(object sender, BookmarksModifiedEventArgs e) =>
			UI(() => OnBookmarksModified_UI(e.Bookmarks.Select(a => a.Bookmark).ToArray()));

		void OnBookmarksModified_UI(IList<Bookmark> bookmarks) {
			uiDispatcher.VerifyAccess();
			var bms = new List<Bookmark>(bookmarks.Count);
			var removedMarkers = new List<IGlyphTextMarker>(bookmarks.Count);
			for (int i = 0; i < bookmarks.Count; i++) {
				var bm = bookmarks[i];
				if (!bm.TryGetData(out BookmarkData data))
					continue;
				bms.Add(bm);
				if (data.Marker == null)
					continue;
				if (data.Info == bookmarkInfos[(int)BookmarkImageUtilities.GetBookmarkKind(bm)])
					continue;
				removedMarkers.Add(data.Marker);
				data.Marker = null;
			}
			glyphTextMarkerService.Value.Remove(removedMarkers);
			foreach (var bm in bms)
				UpdateMarker(bm);
		}

		void UpdateMarker(Bookmark bm) {
			if (!bm.TryGetData(out BookmarkData data))
				return;

			var info = bookmarkInfos[(int)BookmarkImageUtilities.GetBookmarkKind(bm)];
			if (data.Info == info && data.Marker != null)
				return;
			data.Info = info;
			if (data.Marker != null)
				glyphTextMarkerService.Value.Remove(data.Marker);

			data.Marker = glyphTextMarkerService.Value.AddMarker(data.Location, info.ImageReference, info.MarkerTypeName, info.SelectedMarkerTypeName, info.ClassificationType, info.ZIndex, bm, bookmarkGlyphTextMarkerHandler, null);
		}
	}
}

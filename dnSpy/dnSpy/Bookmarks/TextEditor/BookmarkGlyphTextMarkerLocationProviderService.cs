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
using dnSpy.Contracts.Bookmarks.TextEditor;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Bookmarks.TextEditor {
	abstract class BookmarkGlyphTextMarkerLocationProviderService {
		public abstract GlyphTextMarkerLocationInfo GetLocation(Bookmark bookmark);
	}

	[Export(typeof(BookmarkGlyphTextMarkerLocationProviderService))]
	sealed class BookmarkGlyphTextMarkerLocationProviderServiceImpl : BookmarkGlyphTextMarkerLocationProviderService {
		readonly Lazy<BookmarkGlyphTextMarkerLocationProvider, IBookmarkGlyphTextMarkerLocationProviderMetadata>[] bookmarkGlyphTextMarkerLocationProviders;

		[ImportingConstructor]
		BookmarkGlyphTextMarkerLocationProviderServiceImpl([ImportMany] IEnumerable<Lazy<BookmarkGlyphTextMarkerLocationProvider, IBookmarkGlyphTextMarkerLocationProviderMetadata>> bookmarkGlyphTextMarkerLocationProviders) =>
			this.bookmarkGlyphTextMarkerLocationProviders = bookmarkGlyphTextMarkerLocationProviders.OrderBy(a => a.Metadata.Order).ToArray();

		public override GlyphTextMarkerLocationInfo GetLocation(Bookmark bookmark) {
			if (bookmark == null)
				throw new ArgumentNullException(nameof(bookmark));
			foreach (var lz in bookmarkGlyphTextMarkerLocationProviders) {
				var loc = lz.Value.GetLocation(bookmark);
				if (loc != null)
					return loc;
			}
			return null;
		}
	}
}

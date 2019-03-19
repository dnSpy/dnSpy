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

using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Bookmarks.DotNet;
using dnSpy.Contracts.Bookmarks.TextEditor;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Bookmarks.DotNet.TextEditor {
	[ExportBookmarkGlyphTextMarkerLocationProvider]
	sealed class BookmarkGlyphTextMarkerLocationProviderImpl : BookmarkGlyphTextMarkerLocationProvider {
		public override GlyphTextMarkerLocationInfo GetLocation(Bookmark bookmark) {
			if (bookmark.Location is DotNetMethodBodyBookmarkLocation bodyLoc)
				return new DotNetMethodBodyGlyphTextMarkerLocationInfo(bodyLoc.Module, bodyLoc.Token, bodyLoc.Offset);
			if (bookmark.Location is DotNetTokenBookmarkLocation tokenLoc)
				return new DotNetTokenGlyphTextMarkerLocationInfo(tokenLoc.Module, tokenLoc.Token);
			return null;
		}
	}
}

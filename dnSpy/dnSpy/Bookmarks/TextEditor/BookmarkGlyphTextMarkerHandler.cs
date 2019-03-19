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
using System.ComponentModel.Composition;
using System.Windows;
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Bookmarks.TextEditor {
	abstract class BookmarkGlyphTextMarkerHandler : IGlyphTextMarkerHandler {
		public abstract IGlyphTextMarkerHandlerMouseProcessor MouseProcessor { get; }
		public abstract IEnumerable<GuidObject> GetContextMenuObjects(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, Point marginRelativePoint);
		public abstract GlyphTextMarkerToolTip GetToolTipContent(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker);
		public abstract FrameworkElement GetPopupContent(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker);
	}

	[Export(typeof(BookmarkGlyphTextMarkerHandler))]
	sealed class BookmarkGlyphTextMarkerHandlerImpl : BookmarkGlyphTextMarkerHandler {
		public override IGlyphTextMarkerHandlerMouseProcessor MouseProcessor => null;
		public override FrameworkElement GetPopupContent(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker) => null;
		public override GlyphTextMarkerToolTip GetToolTipContent(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker) => null;

		public override IEnumerable<GuidObject> GetContextMenuObjects(IGlyphTextMarkerHandlerContext context, IGlyphTextMarker marker, Point marginRelativePoint) {
			var bm = (Bookmark)marker.Tag;
			yield return new GuidObject(MenuConstants.GUIDOBJ_BOOKMARK_GUID, bm);
		}
	}
}

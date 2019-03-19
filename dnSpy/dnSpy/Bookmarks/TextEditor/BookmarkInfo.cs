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

using dnSpy.Contracts.Images;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Bookmarks.TextEditor {
	sealed class BookmarkInfo {
		public BookmarkKind Kind { get; }
		public ImageReference? ImageReference { get; }
		public string MarkerTypeName { get; }
		public string SelectedMarkerTypeName { get; }
		public IClassificationType ClassificationType { get; }
		public int ZIndex { get; }

		public BookmarkInfo(BookmarkKind kind, string markerTypeName, string selectedMarkerTypeName, IClassificationType classificationType, int zIndex) {
			Kind = kind;
			ImageReference = BookmarkImageUtilities.GetImage(kind);
			MarkerTypeName = markerTypeName;
			SelectedMarkerTypeName = selectedMarkerTypeName;
			ClassificationType = classificationType;
			ZIndex = zIndex;
		}
	}
}

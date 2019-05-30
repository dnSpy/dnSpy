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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Contracts.Bookmarks.TextEditor {
	/// <summary>
	/// Creates <see cref="GlyphTextMarkerLocationInfo"/>s. Use <see cref="ExportBookmarkGlyphTextMarkerLocationProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class BookmarkGlyphTextMarkerLocationProvider {
		/// <summary>
		/// Gets the location of the bookmark or null
		/// </summary>
		/// <param name="bookmark">Bookmark</param>
		/// <returns></returns>
		public abstract GlyphTextMarkerLocationInfo? GetLocation(Bookmark bookmark);
	}

	/// <summary>Metadata</summary>
	public interface IBookmarkGlyphTextMarkerLocationProviderMetadata {
		/// <summary>See <see cref="ExportBookmarkGlyphTextMarkerLocationProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="BookmarkGlyphTextMarkerLocationProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportBookmarkGlyphTextMarkerLocationProviderAttribute : ExportAttribute, IBookmarkGlyphTextMarkerLocationProviderMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="order">Order</param>
		public ExportBookmarkGlyphTextMarkerLocationProviderAttribute(double order = double.MaxValue)
			: base(typeof(BookmarkGlyphTextMarkerLocationProvider)) => Order = order;

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}
}

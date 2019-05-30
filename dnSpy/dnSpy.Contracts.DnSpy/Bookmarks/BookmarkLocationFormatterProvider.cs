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

namespace dnSpy.Contracts.Bookmarks {
	/// <summary>
	/// Creates <see cref="BookmarkLocation"/> formatters. Use <see cref="ExportBookmarkLocationFormatterProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class BookmarkLocationFormatterProvider {
		/// <summary>
		/// Returns a formatter or null
		/// </summary>
		/// <param name="location">Bookmark location</param>
		/// <returns></returns>
		public abstract BookmarkLocationFormatter? Create(BookmarkLocation location);
	}

	/// <summary>Metadata</summary>
	public interface IBookmarkLocationFormatterProviderMetadata {
		/// <summary>See <see cref="ExportBookmarkLocationFormatterProviderAttribute.Types"/></summary>
		string[] Types { get; }
	}

	/// <summary>
	/// Exports a <see cref="BookmarkLocationFormatterProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportBookmarkLocationFormatterProviderAttribute : ExportAttribute, IBookmarkLocationFormatterProviderMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type (compared against <see cref="BookmarkLocation.Type"/>), see <see cref="PredefinedBookmarkLocationTypes"/></param>
		public ExportBookmarkLocationFormatterProviderAttribute(string type)
			: base(typeof(BookmarkLocationFormatterProvider)) => Types = new[] { type ?? throw new ArgumentNullException(nameof(type)) };

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="types">Types (compared against <see cref="BookmarkLocation.Type"/>), see <see cref="PredefinedBookmarkLocationTypes"/></param>
		public ExportBookmarkLocationFormatterProviderAttribute(string[] types)
			: base(typeof(BookmarkLocationFormatterProvider)) => Types = types ?? throw new ArgumentNullException(nameof(types));

		/// <summary>
		/// Types (compared against <see cref="BookmarkLocation.Type"/>), see <see cref="PredefinedBookmarkLocationTypes"/>
		/// </summary>
		public string[] Types { get; }
	}
}

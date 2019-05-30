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
using dnSpy.Contracts.Bookmarks;

namespace dnSpy.Bookmarks.DotNet {
	[ExportBookmarkLocationFormatterProvider(new[] { PredefinedBookmarkLocationTypes.DotNetBody, PredefinedBookmarkLocationTypes.DotNetToken })]
	sealed class BookmarkLocationFormatterProviderImpl : BookmarkLocationFormatterProvider {
		readonly Lazy<BookmarkFormatterService> bookmarkFormatterService;

		[ImportingConstructor]
		BookmarkLocationFormatterProviderImpl(Lazy<BookmarkFormatterService> bookmarkFormatterService) =>
			this.bookmarkFormatterService = bookmarkFormatterService;

		public override BookmarkLocationFormatter? Create(BookmarkLocation location) {
			switch (location) {
			case DotNetMethodBodyBookmarkLocationImpl loc:
				var formatter = loc.Formatter;
				if (!(formatter is null))
					return formatter;
				formatter = bookmarkFormatterService.Value.Create(loc);
				loc.Formatter = formatter;
				return formatter;

			case DotNetTokenBookmarkLocationImpl loc:
				formatter = loc.Formatter;
				if (!(formatter is null))
					return formatter;
				formatter = bookmarkFormatterService.Value.Create(loc);
				loc.Formatter = formatter;
				return formatter;
			}

			return null;
		}
	}
}

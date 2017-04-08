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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Bookmarks.DotNet;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Bookmarks.DotNet {
	abstract class DotNetBookmarkFactory2 : DotNetBookmarkFactory {
		public abstract DotNetMethodBodyBookmarkLocation CreateMethodBodyLocation(ModuleId module, uint token, uint offset);
		public abstract DotNetTokenBookmarkLocation CreateTokenLocation(ModuleId module, uint token);
	}

	[Export(typeof(DotNetBookmarkFactory))]
	[Export(typeof(DotNetBookmarkFactory2))]
	sealed class DotNetBookmarkFactoryImpl : DotNetBookmarkFactory2 {
		readonly Lazy<BookmarksService> bookmarksService;
		readonly BookmarkFormatterService bookmarkFormatterService;

		[ImportingConstructor]
		DotNetBookmarkFactoryImpl(Lazy<BookmarksService> bookmarksService, BookmarkFormatterService bookmarkFormatterService) {
			this.bookmarksService = bookmarksService;
			this.bookmarkFormatterService = bookmarkFormatterService;
		}

		public override Bookmark[] Create(DotNetMethodBodyBookmarkInfo[] bookmarks) =>
			bookmarksService.Value.Add(bookmarks.Select(a => new BookmarkInfo(CreateMethodBodyLocation(a.Module, a.Token, a.Offset), a.Settings)).ToArray());

		public override Bookmark[] Create(DotNetTokenBookmarkInfo[] bookmarks) =>
			bookmarksService.Value.Add(bookmarks.Select(a => new BookmarkInfo(CreateTokenLocation(a.Module, a.Token), a.Settings)).ToArray());

		public override DotNetMethodBodyBookmarkLocation CreateMethodBodyLocation(ModuleId module, uint token, uint offset) {
			var bodyLoc = new DotNetMethodBodyBookmarkLocationImpl(module, token, offset);
			var formatter = bookmarkFormatterService.Create(bodyLoc);
			bodyLoc.Formatter = formatter;
			return bodyLoc;
		}

		public override DotNetTokenBookmarkLocation CreateTokenLocation(ModuleId module, uint token) {
			var tokenLoc = new DotNetTokenBookmarkLocationImpl(module, token);
			var formatter = bookmarkFormatterService.Create(tokenLoc);
			tokenLoc.Formatter = formatter;
			return tokenLoc;
		}
	}
}

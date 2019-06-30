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
using System.Linq;
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Bookmarks.DotNet;

namespace dnSpy.Bookmarks.DotNet {
	[Export(typeof(DotNetBookmarkFactory))]
	sealed class DotNetBookmarkFactoryImpl : DotNetBookmarkFactory {
		readonly Lazy<BookmarksService> bookmarksService;
		readonly DotNetBookmarkLocationFactory dotNetBookmarkLocationFactory;

		[ImportingConstructor]
		DotNetBookmarkFactoryImpl(Lazy<BookmarksService> bookmarksService, DotNetBookmarkLocationFactory dotNetBookmarkLocationFactory) {
			this.bookmarksService = bookmarksService;
			this.dotNetBookmarkLocationFactory = dotNetBookmarkLocationFactory;
		}

		public override Bookmark[] Create(DotNetMethodBodyBookmarkInfo[] bookmarks) =>
			bookmarksService.Value.Add(bookmarks.Select(a => new BookmarkInfo(dotNetBookmarkLocationFactory.CreateMethodBodyLocation(a.Module, a.Token, a.Offset), a.Settings)).ToArray());

		public override Bookmark[] Create(DotNetTokenBookmarkInfo[] bookmarks) =>
			bookmarksService.Value.Add(bookmarks.Select(a => new BookmarkInfo(dotNetBookmarkLocationFactory.CreateTokenLocation(a.Module, a.Token), a.Settings)).ToArray());
	}
}

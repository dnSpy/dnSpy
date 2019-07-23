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
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Metadata;
using dnSpy.UI;

namespace dnSpy.Bookmarks.DotNet {
	abstract class BookmarkFormatterService {
		public abstract DotNetBookmarkLocationFormatter Create(DotNetMethodBodyBookmarkLocationImpl location);
		public abstract DotNetBookmarkLocationFormatter Create(DotNetTokenBookmarkLocationImpl location);
	}

	[Export(typeof(BookmarkFormatterService))]
	sealed class BookmarkFormatterServiceImpl : BookmarkFormatterService {
		readonly Lazy<IDecompilerService> decompilerService;
		readonly Lazy<BookmarksService> bookmarksService;
		readonly Lazy<DbgMetadataService> dbgMetadataService;

		internal IDecompiler MethodDecompiler => decompilerService.Value.Decompiler;

		[ImportingConstructor]
		BookmarkFormatterServiceImpl(UIDispatcher uiDispatcher, Lazy<IDecompilerService> decompilerService, Lazy<BookmarksService> bookmarksService, Lazy<DbgMetadataService> dbgMetadataService) {
			this.decompilerService = decompilerService;
			this.bookmarksService = bookmarksService;
			this.dbgMetadataService = dbgMetadataService;
			uiDispatcher.UI(() => decompilerService.Value.DecompilerChanged += DecompilerService_DecompilerChanged);
		}

		void DecompilerService_DecompilerChanged(object? sender, EventArgs e) {
			foreach (var bm in bookmarksService.Value.Bookmarks) {
				if (bm.Location is IDotNetBookmarkLocation location)
					location.Formatter?.RefreshLocation();
			}
		}

		public override DotNetBookmarkLocationFormatter Create(DotNetMethodBodyBookmarkLocationImpl location) =>
			new DotNetMethodBodyBookmarkLocationFormatterImpl(this, location);

		public override DotNetBookmarkLocationFormatter Create(DotNetTokenBookmarkLocationImpl location) =>
			new DotNetTokenBookmarkLocationFormatterImpl(this, location);

		internal TDef? GetDefinition<TDef>(ModuleId module, uint token) where TDef : class {
			var md = dbgMetadataService.Value.TryGetMetadata(module, DbgLoadModuleOptions.AutoLoaded);
			return md?.ResolveToken(token) as TDef;
		}
	}
}

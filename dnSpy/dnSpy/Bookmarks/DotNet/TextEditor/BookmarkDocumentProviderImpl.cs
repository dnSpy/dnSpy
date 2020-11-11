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
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Bookmarks.DotNet;
using dnSpy.Contracts.Bookmarks.Navigator;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Bookmarks.DotNet.TextEditor {
	[ExportBookmarkDocumentProvider]
	sealed class BookmarkDocumentProviderImpl : BookmarkDocumentProvider {
		readonly Lazy<IDocumentTabService> documentTabService;
		readonly Lazy<IModuleIdProvider> moduleIdProvider;

		[ImportingConstructor]
		BookmarkDocumentProviderImpl(Lazy<IDocumentTabService> documentTabService, Lazy<IModuleIdProvider> moduleIdProvider) {
			this.documentTabService = documentTabService;
			this.moduleIdProvider = moduleIdProvider;
		}

		public override BookmarkDocument? GetDocument(Bookmark bookmark) {
			switch (bookmark.Location) {
			case DotNetMethodBodyBookmarkLocation bodyLoc:
				return GetDocument(bodyLoc);

			case DotNetTokenBookmarkLocation tokenLoc:
				return GetDocument(tokenLoc);
			}

			return null;
		}

		sealed class BookmarkDocumentImpl : BookmarkDocument {
			readonly IDocumentTab tab;
			public BookmarkDocumentImpl(IDocumentTab tab) => this.tab = tab ?? throw new ArgumentNullException(nameof(tab));
			public override bool Equals(object? obj) => obj is BookmarkDocumentImpl other && tab == other.tab;
			public override int GetHashCode() => tab.GetHashCode();
		}

		BookmarkDocument? GetDocument(DotNetMethodBodyBookmarkLocation bodyLoc) {
			var tab = documentTabService.Value.ActiveTab;
			var documentViewer = tab?.TryGetDocumentViewer();
			if (documentViewer is null)
				return null;
			Debug2.Assert(tab is not null);
			var methodDebugService = documentViewer.GetMethodDebugService();
			var info = methodDebugService.TryGetMethodDebugInfo(new ModuleTokenId(bodyLoc.Module, bodyLoc.Token));
			if (info is null)
				return null;
			if (info.GetSourceStatementByCodeOffset(bodyLoc.Offset) is null)
				return null;
			return new BookmarkDocumentImpl(tab);
		}

		BookmarkDocument? GetDocument(DotNetTokenBookmarkLocation tokenLoc) {
			var tab = documentTabService.Value.ActiveTab;
			var documentViewer = tab?.TryGetDocumentViewer();
			if (documentViewer is null)
				return null;
			Debug2.Assert(tab is not null);

			foreach (var info in documentViewer.ReferenceCollection) {
				if (!info.Data.IsDefinition)
					continue;
				var def = info.Data.Reference as IMemberDef;
				if (def is null || def.MDToken.Raw != tokenLoc.Token)
					continue;
				if (moduleIdProvider.Value.Create(def.Module) != tokenLoc.Module)
					continue;

				return new BookmarkDocumentImpl(tab);
			}
			return null;
		}
	}
}

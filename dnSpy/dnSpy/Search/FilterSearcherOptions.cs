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
using System.Threading;
using System.Windows.Threading;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Search;

namespace dnSpy.Search {
	sealed class FilterSearcherOptions {
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
		public Dispatcher Dispatcher { get; set; }
		public IDocumentTreeView DocumentTreeView { get; set; }
		public IDotNetImageService DotNetImageService { get; set; }
		public IDocumentTreeNodeFilter Filter { get; set; }
		public ISearchComparer SearchComparer { get; set; }
		public Action<SearchResult> OnMatch { get; set; }
		public SearchResultContext Context { get; set; }
		public CancellationToken CancellationToken { get; set; }
		public bool SearchDecompiledData { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
	}
}

/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Documents.Tabs {
	/// <summary>
	/// Caches decompiled code
	/// </summary>
	interface IDecompilationCache {
		/// <summary>
		/// Looks up cached output
		/// </summary>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="nodes">Nodes</param>
		/// <param name="contentType">Content type</param>
		/// <returns></returns>
		DocumentViewerContent Lookup(IDecompiler decompiler, DocumentTreeNodeData[] nodes, out IContentType contentType);

		/// <summary>
		/// Cache decompiled output
		/// </summary>
		/// <param name="decompiler">Decompiler</param>
		/// <param name="nodes">Nodes</param>
		/// <param name="content">Content</param>
		/// <param name="contentType">Content type</param>
		void Cache(IDecompiler decompiler, DocumentTreeNodeData[] nodes, DocumentViewerContent content, IContentType contentType);

		/// <summary>
		/// Clear the cache
		/// </summary>
		void ClearAll();

		/// <summary>
		/// Clear everything referencing <paramref name="modules"/>
		/// </summary>
		/// <param name="modules">Module</param>
		void Clear(HashSet<IDsDocument> modules);
	}
}

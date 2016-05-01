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
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TextEditor;
using dnSpy.Shared.Decompiler;
using ICSharpCode.AvalonEdit.Highlighting;

namespace dnSpy.Files.Tabs {
	/// <summary>
	/// Caches decompiled code
	/// </summary>
	interface IDecompilationCache {
		/// <summary>
		/// Looks up cached output
		/// </summary>
		/// <param name="language">Language</param>
		/// <param name="nodes">Nodes</param>
		/// <param name="highlighting">Highlighting</param>
		/// <param name="contentType">Content type</param>
		/// <returns></returns>
		AvalonEditTextOutput Lookup(ILanguage language, IFileTreeNodeData[] nodes, out IHighlightingDefinition highlighting, out IContentType contentType);

		/// <summary>
		/// Cache decompiled output
		/// </summary>
		/// <param name="language">Language</param>
		/// <param name="nodes">Nodes</param>
		/// <param name="textOutput">Output</param>
		/// <param name="highlighting">Highlighting</param>
		/// <param name="contentType">Content type</param>
		void Cache(ILanguage language, IFileTreeNodeData[] nodes, AvalonEditTextOutput textOutput, IHighlightingDefinition highlighting, IContentType contentType);

		/// <summary>
		/// Clear the cache
		/// </summary>
		void ClearAll();

		/// <summary>
		/// Clear everything referencing <paramref name="modules"/>
		/// </summary>
		/// <param name="modules">Module</param>
		void Clear(HashSet<IDnSpyFile> modules);
	}
}

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

using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Analyzer.TreeNodes {
	interface IAnalyzerTreeNodeData : ITreeNodeData {
		/// <summary>
		/// Gets the context. Initialized by the owner.
		/// </summary>
		IAnalyzerTreeNodeDataContext Context { get; set; }

		/// <summary>
		/// ToString()
		/// </summary>
		/// <param name="decompiler">Decompiler</param>
		/// <returns></returns>
		string ToString(IDecompiler decompiler);

		/// <summary>
		/// Called when documents have been added/removed from the documents list
		/// </summary>
		/// <param name="removedAssemblies">Removed documents</param>
		/// <param name="addedAssemblies">Added documents</param>
		/// <returns></returns>
		bool HandleAssemblyListChanged(IDsDocument[] removedAssemblies, IDsDocument[] addedAssemblies);

		/// <summary>
		/// Called when documents have been modified
		/// </summary>
		/// <param name="documents">Documents</param>
		/// <returns></returns>
		bool HandleModelUpdated(IDsDocument[] documents);
	}
}

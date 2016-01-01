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

using dnSpy.Contracts.Files;
using dnSpy.Contracts.Languages;
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
		/// <param name="language">Language</param>
		/// <returns></returns>
		string ToString(ILanguage language);

		/// <summary>
		/// Called when files have been added/removed from the files list
		/// </summary>
		/// <param name="removedAssemblies">Removed files</param>
		/// <param name="addedAssemblies">Added files</param>
		/// <returns></returns>
		bool HandleAssemblyListChanged(IDnSpyFile[] removedAssemblies, IDnSpyFile[] addedAssemblies);

		/// <summary>
		/// Called when a file has been modified
		/// </summary>
		/// <param name="file">Files</param>
		/// <returns></returns>
		bool HandleModelUpdated(IDnSpyFile[] files);
	}
}

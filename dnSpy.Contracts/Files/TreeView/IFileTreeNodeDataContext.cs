/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using dnSpy.Contracts.Languages;

namespace dnSpy.Contracts.Files.TreeView {
	/// <summary>
	/// <see cref="IFileTreeNodeData"/> context
	/// </summary>
	public interface IFileTreeNodeDataContext {
		/// <summary>
		/// Owner <see cref="IFileTreeView"/>
		/// </summary>
		IFileTreeView FileTreeView { get; }

		/// <summary>
		/// Default language
		/// </summary>
		ILanguage Language { get; }

		/// <summary>
		/// true if it should be syntax highlighted
		/// </summary>
		bool SyntaxHighlight { get; }

		/// <summary>
		/// true if single clicks expand children
		/// </summary>
		bool SingleClickExpandsChildren { get; }

		/// <summary>
		/// Show assembly version
		/// </summary>
		bool ShowAssemblyVersion { get; }

		/// <summary>
		/// Show assembly public key token
		/// </summary>
		bool ShowAssemblyPublicKeyToken { get; }

		/// <summary>
		/// Show MD token
		/// </summary>
		bool ShowToken { get; }

		/// <summary>
		/// true to use the new optimized renderer. It doesn't support all unicode chars or word wrapping
		/// </summary>
		bool UseNewRenderer { get; }
	}
}

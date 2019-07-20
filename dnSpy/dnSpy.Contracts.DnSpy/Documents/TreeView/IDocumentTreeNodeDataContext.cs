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

using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.TreeView.Text;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// <see cref="DocumentTreeNodeData"/> context
	/// </summary>
	public interface IDocumentTreeNodeDataContext {
		/// <summary>
		/// Owner <see cref="IDocumentTreeView"/>
		/// </summary>
		IDocumentTreeView DocumentTreeView { get; }

		/// <summary>
		/// Default language
		/// </summary>
		IDecompiler Decompiler { get; }

		/// <summary>
		/// Gets the <see cref="IResourceNodeFactory"/> instance
		/// </summary>
		IResourceNodeFactory ResourceNodeFactory { get; }

		/// <summary>
		/// Gets the filter
		/// </summary>
		IDocumentTreeNodeFilter Filter { get; }

		/// <summary>
		/// Gets the treeview node text element provider
		/// </summary>
		ITreeViewNodeTextElementProvider TreeViewNodeTextElementProvider { get; }

		/// <summary>
		/// Filter version, gets incremented each time <see cref="Filter"/> gets updated
		/// </summary>
		int FilterVersion { get; }

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
		/// true to deserialize resources
		/// </summary>
		bool DeserializeResources { get; }

		/// <summary>
		/// true if drag and drop is allowed
		/// </summary>
		bool CanDragAndDrop { get; }
	}
}

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

using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// A node in the document treeview
	/// </summary>
	public interface IDocumentTreeNodeData : ITreeNodeData {
		/// <summary>
		/// Gets the node path name
		/// </summary>
		NodePathName NodePathName { get; }

		/// <summary>
		/// Gets the context. Should only be set by the owner <see cref="IDocumentTreeView"/>
		/// </summary>
		IDocumentTreeNodeDataContext Context { get; set; }

		/// <summary>
		/// ToString()
		/// </summary>
		/// <param name="decompiler">Decompiler</param>
		/// <returns></returns>
		string ToString(IDecompiler decompiler);

		/// <summary>
		/// Called when <see cref="IDocumentTreeNodeDataContext.Filter"/> has changed
		/// </summary>
		void Refilter();

		/// <summary>
		/// The class (<see cref="DocumentTreeNodeData"/>) should call <see cref="Refilter()"/> when updating
		/// this value.
		/// </summary>
		int FilterVersion { get; set; }

		/// <summary>
		/// Gets the <see cref="FilterType"/> to filter this instance
		/// </summary>
		/// <param name="filter">Filter to call</param>
		/// <returns></returns>
		FilterType GetFilterType(IDocumentTreeNodeFilter filter);
	}

	/// <summary>
	/// Extension methods
	/// </summary>
	public static class DocumentTreeNodeDataExtensionMethods {
		/// <summary>
		/// Gets the <see cref="IAssemblyDocumentNode"/> owner or null if none was found
		/// </summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public static IAssemblyDocumentNode GetAssemblyNode(this ITreeNodeData self) => self.GetAncestorOrSelf<IAssemblyDocumentNode>();

		/// <summary>
		/// Gets the <see cref="IModuleDocumentNode"/> owner or null if none was found
		/// </summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public static IModuleDocumentNode GetModuleNode(this ITreeNodeData self) => self.GetAncestorOrSelf<IModuleDocumentNode>();

		/// <summary>
		/// Gets the first <see cref="IDsDocumentNode"/> owner or null if none was found
		/// </summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public static IDsDocumentNode GetDocumentNode(this ITreeNodeData self) => self.GetAncestorOrSelf<IDsDocumentNode>();

		/// <summary>
		/// Gets the <see cref="IDsDocumentNode"/> top node or null if none was found
		/// </summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public static IDsDocumentNode GetTopNode(this ITreeNodeData self) {
			var root = self == null ? null : self.TreeNode.TreeView.Root;
			while (self != null) {
				var found = self as IDsDocumentNode;
				if (found != null) {
					var p = found.TreeNode.Parent;
					if (p == null || p == root)
						return found;
				}
				var parent = self.TreeNode.Parent;
				if (parent == null)
					break;
				self = parent.Data;
			}
			return null;
		}

		/// <summary>
		/// Gets the <see cref="ModuleDef"/> instance or null
		/// </summary>
		/// <param name="self">This</param>
		/// <returns></returns>
		public static ModuleDef GetModule(this ITreeNodeData self) {
			var node = self.GetDocumentNode();
			return node == null ? null : node.Document.ModuleDef;
		}
	}
}

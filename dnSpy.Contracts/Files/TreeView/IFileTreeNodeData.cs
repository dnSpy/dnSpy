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

using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Files.TreeView {
	/// <summary>
	/// A node in the file tree view
	/// </summary>
	public interface IFileTreeNodeData : ITreeNodeData {
		/// <summary>
		/// Gets the node path name
		/// </summary>
		NodePathName NodePathName { get; }

		/// <summary>
		/// Gets the context. Should only be set by the owner <see cref="IFileTreeView"/>
		/// </summary>
		IFileTreeNodeDataContext Context { get; set; }

		/// <summary>
		/// ToString()
		/// </summary>
		/// <param name="language">Language</param>
		/// <returns></returns>
		string ToString(ILanguage language);
	}

	/// <summary>
	/// Extension methods
	/// </summary>
	public static class FileTreeNodeDataExtensionMethods {
		/// <summary>
		/// Gets the <see cref="IAssemblyFileNode"/> owner or null if none was found
		/// </summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public static IAssemblyFileNode GetAssemblyNode(this ITreeNodeData self) {
			return self.GetAncestorOrSelf<IAssemblyFileNode>();
		}

		/// <summary>
		/// Gets the <see cref="IModuleFileNode"/> owner or null if none was found
		/// </summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public static IModuleFileNode GetModuleNode(this ITreeNodeData self) {
			return self.GetAncestorOrSelf<IModuleFileNode>();
		}

		/// <summary>
		/// Gets the <see cref="IDnSpyFileNode"/> top node or null if none was found
		/// </summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public static IDnSpyFileNode GetTopNode(this ITreeNodeData self) {
			var root = self == null ? null : self.TreeNode.TreeView.Root;
			while (self != null) {
				var found = self as IDnSpyFileNode;
				if (found != null) {
					var p = found.TreeNode.Parent;
					Debug.Assert(p != null);
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
			var node = self.GetModuleNode();
			return node == null ? null : node.DnSpyFile.ModuleDef;
		}
	}
}

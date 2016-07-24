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

using System.Linq;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.AsmEditor.Hex.Nodes {
	[ExportFileTreeNodeDataFinder]
	sealed class HexFileTreeNodeDataFinder : IFileTreeNodeDataFinder {
		public IFileTreeNodeData FindNode(IFileTreeView fileTreeView, object @ref) => FindNode(fileTreeView, @ref as TokenReference);

		internal static MetaDataTableRecordNode FindNode(IFileTreeView fileTreeView, TokenReference tokRef) {
			if (tokRef == null)
				return null;

			var modNode = fileTreeView.FindNode(tokRef.ModuleDef);
			if (modNode == null)
				return null;
			modNode.TreeNode.EnsureChildrenLoaded();
			var peNode = (PENode)modNode.TreeNode.DataChildren.FirstOrDefault(a => a is PENode);
			return peNode?.FindTokenNode(tokRef.Token);
		}
	}
}

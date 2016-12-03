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
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.AsmEditor.Commands {
	/// <summary>
	/// Creates a <see cref="NamespaceNode"/> if it doesn't exist. Caches the node to make sure
	/// the same <see cref="NamespaceNode"/> is used all the time.
	/// </summary>
	sealed class NamespaceNodeCreator {
		readonly ModuleDocumentNode modNode;
		readonly NamespaceNode nsNode;
		readonly bool nsNodeCreated;

		public NamespaceNode NamespaceNode => nsNode;

		public IEnumerable<DocumentTreeNodeData> OriginalNodes {
			get {
				yield return modNode;
				if (!nsNodeCreated)
					yield return nsNode;
			}
		}

		public NamespaceNodeCreator(string ns, ModuleDocumentNode modNode) {
			this.modNode = modNode;
			nsNode = modNode.FindNode(ns);
			if (nsNode == null) {
				nsNode = modNode.Create(ns);
				nsNodeCreated = true;
			}
		}

		/// <summary>
		/// Add the <see cref="NamespaceNode"/> if it doesn't exist
		/// </summary>
		public void Add() {
			if (nsNodeCreated)
				modNode.TreeNode.AddChild(nsNode.TreeNode);
		}

		/// <summary>
		/// Undo what <see cref="Add()"/> did
		/// </summary>
		public void Remove() {
			if (nsNodeCreated)
				modNode.TreeNode.Children.Remove(nsNode.TreeNode);
		}
	}
}

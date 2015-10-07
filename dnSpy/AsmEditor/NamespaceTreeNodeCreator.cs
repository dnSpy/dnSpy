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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor {
	/// <summary>
	/// Creates a <see cref="NamespaceTreeNode"/> if it doesn't exist. Caches the node to make sure
	/// the same <see cref="NamespaceTreeNode"/> is used all the time.
	/// </summary>
	sealed class NamespaceTreeNodeCreator {
		readonly AssemblyTreeNode asmNode;
		readonly NamespaceTreeNode nsNode;
		readonly bool nsNodeCreated;

		public NamespaceTreeNode NamespaceTreeNode {
			get { return nsNode; }
		}

		public IEnumerable<ILSpyTreeNode> OriginalNodes {
			get {
				yield return asmNode;
				if (!nsNodeCreated)
					yield return nsNode;
			}
		}

		public NamespaceTreeNodeCreator(string ns, AssemblyTreeNode asmNode) {
			Debug.Assert(asmNode.IsModule);
			if (!asmNode.IsModule)
				throw new InvalidOperationException();

			this.asmNode = asmNode;
			this.nsNode = asmNode.FindNamespaceNode(ns);
			if (this.nsNode == null) {
				this.nsNode = new NamespaceTreeNode(ns);
				this.nsNodeCreated = true;
			}
		}

		/// <summary>
		/// Add the <see cref="NamespaceTreeNode"/> if it doesn't exist
		/// </summary>
		public void Add() {
			if (nsNodeCreated) {
				asmNode.AddToChildren(nsNode);
				nsNode.OnReadded();
			}
		}

		/// <summary>
		/// Undo what <see cref="Add()"/> did
		/// </summary>
		public void Remove() {
			if (nsNodeCreated) {
				nsNode.OnBeforeRemoved();
				asmNode.Children.Remove(nsNode);
			}
		}
	}
}

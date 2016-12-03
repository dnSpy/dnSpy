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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.AsmEditor.Commands {
	/// <summary>
	/// Creates nested type nodes, see also <see cref="TypeNodeCreator"/>
	/// </summary>
	sealed class NestedTypeNodeCreator {
		readonly TypeNode ownerTypeNode;
		readonly TypeNode nestedTypeNode;

		public IEnumerable<DocumentTreeNodeData> OriginalNodes {
			get {
				yield return ownerTypeNode;
				if (nestedTypeNode != null)
					yield return nestedTypeNode;
			}
		}

		public NestedTypeNodeCreator(ModuleDocumentNode modNode, TypeNode ownerTypeNode, TypeDef nestedType) {
			if (modNode == null)
				throw new ArgumentNullException(nameof(modNode));
			if (nestedType == null)
				throw new ArgumentNullException(nameof(nestedType));
			if (nestedType.Module != null)
				throw new ArgumentException();
			this.ownerTypeNode = ownerTypeNode;
			nestedTypeNode = modNode.Context.DocumentTreeView.CreateNested(nestedType);
		}

		public void Add() {
			ownerTypeNode.TreeNode.EnsureChildrenLoaded();
			ownerTypeNode.TypeDef.NestedTypes.Add(nestedTypeNode.TypeDef);
			ownerTypeNode.TreeNode.AddChild(nestedTypeNode.TreeNode);
		}

		public void Remove() {
			bool b = ownerTypeNode.TreeNode.Children.Remove(nestedTypeNode.TreeNode) &&
					ownerTypeNode.TypeDef.NestedTypes.Remove(nestedTypeNode.TypeDef);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}
	}
}

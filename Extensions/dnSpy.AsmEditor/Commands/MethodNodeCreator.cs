/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
	sealed class MethodNodeCreator {
		public IEnumerable<DocumentTreeNodeData> OriginalNodes {
			get { yield return ownerNode; }
		}

		readonly TypeNode ownerNode;
		readonly MethodNode methodNode;

		public MethodNodeCreator(ModuleDocumentNode modNode, TypeNode ownerNode, MethodDef method) {
			this.ownerNode = ownerNode;
			methodNode = modNode.Context.DocumentTreeView.Create(method);
		}

		public void Add() {
			ownerNode.TreeNode.EnsureChildrenLoaded();
			ownerNode.TypeDef.Methods.Add(methodNode.MethodDef);
			ownerNode.TreeNode.AddChild(methodNode.TreeNode);
		}

		public void Remove() {
			bool b = ownerNode.TreeNode.Children.Remove(methodNode.TreeNode) &&
					ownerNode.TypeDef.Methods.Remove(methodNode.MethodDef);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}
	}
}

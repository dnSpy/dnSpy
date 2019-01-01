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

using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.TreeView;

namespace dnSpy.AsmEditor.Commands {
	sealed class DeletedMethodUpdater {
		public IEnumerable<DocumentTreeNodeData> OriginalNodes {
			get { yield return ownerNode; }
		}

		readonly TreeNodeData parentNode;
		readonly MethodNode ownerNode;
		readonly TypeDef ownerType;
		readonly MethodDef method;
		int methodIndex;

		public DeletedMethodUpdater(ModuleDocumentNode modNode, MethodDef originalMethod) {
			ownerNode = modNode.Context.DocumentTreeView.FindNode(originalMethod);
			if (ownerNode == null)
				throw new InvalidOperationException();
			parentNode = ownerNode.TreeNode.Parent.Data;
			ownerType = originalMethod.DeclaringType;
			method = originalMethod;
		}

		public void Add() {
			if (!parentNode.TreeNode.Children.Remove(ownerNode.TreeNode))
				throw new InvalidOperationException();
			methodIndex = ownerType.Methods.IndexOf(method);
			ownerType.Methods.RemoveAt(methodIndex);
		}

		public void Remove() {
			ownerType.Methods.Insert(methodIndex, method);
			parentNode.TreeNode.AddChild(ownerNode.TreeNode);
		}
	}
}

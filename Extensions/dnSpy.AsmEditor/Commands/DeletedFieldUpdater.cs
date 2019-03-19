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
	sealed class DeletedFieldUpdater {
		public IEnumerable<DocumentTreeNodeData> OriginalNodes {
			get { yield return ownerNode; }
		}

		readonly TreeNodeData parentNode;
		readonly FieldNode ownerNode;
		readonly TypeDef ownerType;
		readonly FieldDef field;
		int fieldIndex;

		public DeletedFieldUpdater(ModuleDocumentNode modNode, FieldDef originalField) {
			ownerNode = modNode.Context.DocumentTreeView.FindNode(originalField);
			if (ownerNode == null)
				throw new InvalidOperationException();
			parentNode = ownerNode.TreeNode.Parent.Data;
			ownerType = originalField.DeclaringType;
			field = originalField;
		}

		public void Add() {
			if (!parentNode.TreeNode.Children.Remove(ownerNode.TreeNode))
				throw new InvalidOperationException();
			fieldIndex = ownerType.Fields.IndexOf(field);
			ownerType.Fields.RemoveAt(fieldIndex);
		}

		public void Remove() {
			ownerType.Fields.Insert(fieldIndex, field);
			parentNode.TreeNode.AddChild(ownerNode.TreeNode);
		}
	}
}

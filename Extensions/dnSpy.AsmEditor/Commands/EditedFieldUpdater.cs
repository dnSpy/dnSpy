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
using dnlib.DotNet;
using dnSpy.AsmEditor.Field;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.AsmEditor.Commands {
	sealed class EditedFieldUpdater {
		public IEnumerable<DocumentTreeNodeData> OriginalNodes {
			get { yield return ownerNode; }
		}

		readonly FieldNode ownerNode;
		readonly FieldDef field;
		readonly FieldDefOptions originalFieldDefOptions;
		readonly FieldDefOptions newFieldDefOptions;

		public EditedFieldUpdater(ModuleDocumentNode modNode, FieldDef originalField, FieldDefOptions fieldDefOptions) {
			this.ownerNode = modNode.Context.DocumentTreeView.FindNode(originalField);
			if (ownerNode == null)
				throw new InvalidOperationException();
			this.field = originalField;
			this.originalFieldDefOptions = new FieldDefOptions(originalField);
			this.newFieldDefOptions = fieldDefOptions;
		}

		public void Add() => newFieldDefOptions.CopyTo(field);
		public void Remove() => originalFieldDefOptions.CopyTo(field);
	}
}

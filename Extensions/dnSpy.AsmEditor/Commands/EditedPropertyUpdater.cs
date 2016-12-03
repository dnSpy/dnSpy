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
using dnSpy.AsmEditor.Property;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.AsmEditor.Commands {
	sealed class EditedPropertyUpdater {
		public IEnumerable<DocumentTreeNodeData> OriginalNodes {
			get { yield return ownerNode; }
		}

		readonly PropertyNode ownerNode;
		readonly PropertyDef property;
		readonly PropertyDefOptions originalPropertyDefOptions;
		readonly PropertyDefOptions newPropertyDefOptions;

		public EditedPropertyUpdater(ModuleDocumentNode modNode, PropertyDef originalProperty, PropertyDefOptions propertyDefOptions) {
			ownerNode = modNode.Context.DocumentTreeView.FindNode(originalProperty);
			if (ownerNode == null)
				throw new InvalidOperationException();
			property = originalProperty;
			originalPropertyDefOptions = new PropertyDefOptions(originalProperty);
			newPropertyDefOptions = propertyDefOptions;
		}

		public void Add() => newPropertyDefOptions.CopyTo(property);
		public void Remove() => originalPropertyDefOptions.CopyTo(property);
	}
}

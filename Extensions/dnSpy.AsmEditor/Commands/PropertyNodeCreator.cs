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
	sealed class PropertyNodeCreator {
		public IEnumerable<IDocumentTreeNodeData> OriginalNodes {
			get { yield return ownerNode; }
		}

		readonly ITypeNode ownerNode;
		readonly IPropertyNode propNode;

		public PropertyNodeCreator(IModuleDocumentNode modNode, ITypeNode ownerNode, PropertyDef property) {
			this.ownerNode = ownerNode;
			this.propNode = modNode.Context.DocumentTreeView.Create(property);
		}

		IEnumerable<MethodDef> GetMethods() {
			foreach (var m in propNode.PropertyDef.GetMethods)
				yield return m;
			foreach (var m in propNode.PropertyDef.SetMethods)
				yield return m;
			foreach (var m in propNode.PropertyDef.OtherMethods)
				yield return m;
		}

		public void Add() {
			ownerNode.TreeNode.EnsureChildrenLoaded();
			ownerNode.TypeDef.Properties.Add(propNode.PropertyDef);
			ownerNode.TypeDef.Methods.AddRange(GetMethods());
			ownerNode.TreeNode.AddChild(propNode.TreeNode);
		}

		public void Remove() {
			bool b = ownerNode.TreeNode.Children.Remove(propNode.TreeNode);
			if (b) {
				foreach (var m in GetMethods())
					b = b && ownerNode.TypeDef.Methods.Remove(m);
			}
			b = b && ownerNode.TypeDef.Properties.Remove(propNode.PropertyDef);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}
	}
}

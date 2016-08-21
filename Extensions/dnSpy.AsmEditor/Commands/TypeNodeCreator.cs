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
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.AsmEditor.Commands {
	/// <summary>
	/// Creates non-nested type nodes, see also <see cref="NestedTypeNodeCreator"/>
	/// </summary>
	sealed class TypeNodeCreator {
		readonly NamespaceNodeCreator nsNodeCreator;
		readonly IList<TypeDef> ownerList;
		readonly ITypeNode typeNode;

		public IEnumerable<IFileTreeNodeData> OriginalNodes {
			get {
				foreach (var n in nsNodeCreator.OriginalNodes)
					yield return n;
				if (typeNode != null)
					yield return typeNode;
			}
		}

		public TypeNodeCreator(IModuleFileNode modNode, TypeDef type) {
			if (modNode == null)
				throw new ArgumentNullException(nameof(modNode));
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			// Can't be a nested type, and can't be part of a module yet
			if (type.DeclaringType != null || type.Module != null)
				throw new ArgumentException();
			this.ownerList = modNode.DnSpyFile.ModuleDef.Types;
			this.nsNodeCreator = new NamespaceNodeCreator(type.Namespace, modNode);
			this.typeNode = modNode.Context.FileTreeView.Create(type);
		}

		public void Add() {
			nsNodeCreator.Add();
			nsNodeCreator.NamespaceNode.TreeNode.EnsureChildrenLoaded();
			ownerList.Add(typeNode.TypeDef);
			nsNodeCreator.NamespaceNode.TreeNode.AddChild(typeNode.TreeNode);
		}

		public void Remove() {
			bool b = nsNodeCreator.NamespaceNode.TreeNode.Children.Remove(typeNode.TreeNode) &&
					ownerList.Remove(typeNode.TypeDef);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
			nsNodeCreator.Remove();
		}
	}
}

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
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.AsmEditor.Commands {
	/// <summary>
	/// Creates non-nested type nodes, see also <see cref="NestedTypeNodeCreator"/>
	/// </summary>
	sealed class TypeNodeCreator {
		readonly NamespaceNodeCreator nsNodeCreator;
		readonly IList<TypeDef> ownerList;
		readonly TypeNode[] typeNodes;

		public IEnumerable<DocumentTreeNodeData> OriginalNodes {
			get {
				foreach (var n in nsNodeCreator.OriginalNodes)
					yield return n;
				foreach (var t in typeNodes)
					yield return t;
			}
		}

		public TypeNodeCreator(ModuleDocumentNode modNode, List<TypeDef> types) {
			if (modNode == null)
				throw new ArgumentNullException(nameof(modNode));
			if (types == null)
				throw new ArgumentNullException(nameof(types));
			if (types.Count == 0)
				throw new ArgumentException();

			var ns = (types[0].Namespace ?? UTF8String.Empty).String;
			foreach (var t in types) {
				var tns = (t.Namespace ?? UTF8String.Empty).String;
				if (tns != ns)
					throw new ArgumentException();
				// Can't be a nested type, and can't be part of a module yet
				if (t.DeclaringType != null || t.Module != null)
					throw new ArgumentException();
			}
			ownerList = modNode.Document.ModuleDef.Types;
			nsNodeCreator = new NamespaceNodeCreator(ns, modNode);
			typeNodes = types.Select(a => modNode.Context.DocumentTreeView.Create(a)).ToArray();
		}

		public void Add() {
			nsNodeCreator.Add();
			nsNodeCreator.NamespaceNode.TreeNode.EnsureChildrenLoaded();
			for (int i = 0; i < typeNodes.Length; i++) {
				var typeNode = typeNodes[i];
				ownerList.Add(typeNode.TypeDef);
				nsNodeCreator.NamespaceNode.TreeNode.AddChild(typeNode.TreeNode);
			}
		}

		public void Remove() {
			for (int i = typeNodes.Length - 1; i >= 0; i--) {
				var typeNode = typeNodes[i];
				bool b = nsNodeCreator.NamespaceNode.TreeNode.Children.Remove(typeNode.TreeNode) &&
						ownerList.Remove(typeNode.TypeDef);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
			}
			nsNodeCreator.Remove();
		}
	}
}

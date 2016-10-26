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
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.AsmEditor.Commands {
	sealed class RootDocumentNodeCreator {
		readonly IDocumentTreeView documentTreeView;
		DsDocumentNode documentNode;
		bool restoreIndex;
		int origIndex = -1;

		public DsDocumentNode DocumentNode => documentNode;
		public static RootDocumentNodeCreator CreateAssembly(IDocumentTreeView documentTreeView, IDsDotNetDocument asm) =>
			new RootDocumentNodeCreator(documentTreeView, documentTreeView.CreateAssembly(asm), false);
		public static RootDocumentNodeCreator CreateModule(IDocumentTreeView documentTreeView, IDsDotNetDocument asm) =>
			new RootDocumentNodeCreator(documentTreeView, documentTreeView.CreateModule(asm), false);

		public RootDocumentNodeCreator(IDocumentTreeView documentTreeView, DsDocumentNode asmNode)
			: this(documentTreeView, asmNode, true) {
		}

		RootDocumentNodeCreator(IDocumentTreeView documentTreeView, DsDocumentNode fileNode, bool restoreIndex) {
			this.documentTreeView = documentTreeView;
			this.documentNode = fileNode;
			this.restoreIndex = restoreIndex;
		}

		public void Add() {
			Debug.Assert(documentNode.TreeNode.Parent == null);
			if (documentNode.TreeNode.Parent != null)
				throw new InvalidOperationException();
			Debug.Assert(!restoreIndex || origIndex >= 0);

			documentTreeView.AddNode(documentNode, origIndex);

			bool b = documentNode.Document.ModuleDef == null ||
				(documentTreeView.FindNode(
				documentNode.Document.AssemblyDef != null ?
				(object)documentNode.Document.AssemblyDef :
				documentNode.Document.ModuleDef) == documentNode);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		public void Remove() {
			Debug.Assert(documentNode.TreeNode.Parent != null);
			if (documentNode.TreeNode.Parent == null)
				throw new InvalidOperationException();

			if (restoreIndex && origIndex == -1) {
				origIndex = documentTreeView.TreeView.Root.DataChildren.ToList().IndexOf(documentNode);
				Debug.Assert(origIndex >= 0);
				if (origIndex < 0)
					throw new InvalidOperationException();
			}

			documentNode.Context.DocumentTreeView.Remove(new[] { documentNode });
		}
	}
}

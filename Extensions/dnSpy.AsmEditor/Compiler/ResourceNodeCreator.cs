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
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.AsmEditor.Resources;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.AsmEditor.Compiler {
	sealed class ResourceNodeCreator {
		readonly ModuleDef module;
		readonly ResourcesFolderNode rsrcListNode;
		readonly NodeAndResource[] nodes;

		public ResourceNodeCreator(ResourcesFolderNode rsrcListNode, NodeAndResource[] nodes) {
			module = rsrcListNode.GetModule()!;
			Debug2.Assert(module is not null);
			this.rsrcListNode = rsrcListNode;
			this.nodes = nodes;
		}

		public void Add() {
			for (int i = 0; i < nodes.Length; i++) {
				var info = nodes[i];
				module.Resources.Add(info.Resource);
				rsrcListNode.TreeNode.AddChild(info.Node.TreeNode);
			}
		}

		public void Remove() {
			for (int i = nodes.Length - 1; i >= 0; i--) {
				var info = nodes[i];
				bool b = rsrcListNode.TreeNode.Children.Remove(info.Node.TreeNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				b = module.Resources.Remove(info.Resource);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
			}
		}
	}
}

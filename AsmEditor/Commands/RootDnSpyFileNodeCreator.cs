/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.AsmEditor.Commands {
	sealed class RootDnSpyFileNodeCreator {
		readonly IFileTreeView fileTreeView;
		IDnSpyFileNode fileNode;
		bool restoreIndex;
		int origIndex = -1;

		public IDnSpyFileNode DnSpyFileNode {
			get { return fileNode; }
		}

		public static RootDnSpyFileNodeCreator CreateAssembly(IFileTreeView fileTreeView, IDnSpyDotNetFile asm) {
			return new RootDnSpyFileNodeCreator(fileTreeView, fileTreeView.CreateAssembly(asm), false);
		}

		public static RootDnSpyFileNodeCreator CreateModule(IFileTreeView fileTreeView, IDnSpyDotNetFile asm) {
			return new RootDnSpyFileNodeCreator(fileTreeView, fileTreeView.CreateModule(asm), false);
		}

		public RootDnSpyFileNodeCreator(IFileTreeView fileTreeView, IDnSpyFileNode asmNode)
			: this(fileTreeView, asmNode, true) {
		}

		RootDnSpyFileNodeCreator(IFileTreeView fileTreeView, IDnSpyFileNode fileNode, bool restoreIndex) {
			this.fileTreeView = fileTreeView;
			this.fileNode = fileNode;
			this.restoreIndex = restoreIndex;
		}

		public void Add() {
			Debug.Assert(fileNode.TreeNode.Parent == null);
			if (fileNode.TreeNode.Parent != null)
				throw new InvalidOperationException();
			Debug.Assert(!restoreIndex || origIndex >= 0);

			fileTreeView.AddNode(fileNode, origIndex);

			bool b = fileNode.DnSpyFile.ModuleDef == null ||
				(fileTreeView.FindNode(
				fileNode.DnSpyFile.AssemblyDef != null ?
				(object)fileNode.DnSpyFile.AssemblyDef :
				fileNode.DnSpyFile.ModuleDef) == fileNode);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		public void Remove() {
			Debug.Assert(fileNode.TreeNode.Parent != null);
			if (fileNode.TreeNode.Parent == null)
				throw new InvalidOperationException();

			if (restoreIndex && origIndex == -1) {
				origIndex = fileTreeView.TreeView.Root.DataChildren.ToList().IndexOf(fileNode);
				Debug.Assert(origIndex >= 0);
				if (origIndex < 0)
					throw new InvalidOperationException();
			}

			fileNode.Context.FileTreeView.Remove(new[] { fileNode });
		}
	}
}

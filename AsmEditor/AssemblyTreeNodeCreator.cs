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
using dnSpy.Contracts.Files;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor {
	/// <summary>
	/// Creates and caches an <see cref="AssemblyTreeNode"/> so the same one is always used.
	/// </summary>
	sealed class AssemblyTreeNodeCreator : IDisposable {
		AssemblyTreeNode asmNode;
		bool restoreIndex;
		int origIndex = -1;

		public AssemblyTreeNode AssemblyTreeNode {
			get { return asmNode; }
		}

		public AssemblyTreeNodeCreator(IDnSpyFile asm)
			: this(asm, null, false) {
		}

		public AssemblyTreeNodeCreator(AssemblyTreeNode asmNode)
			: this(asmNode.DnSpyFile, asmNode, true) {
		}

		AssemblyTreeNodeCreator(IDnSpyFile asm, AssemblyTreeNode asmNode, bool restoreIndex) {
			this.asmNode = asmNode ?? new AssemblyTreeNode(asm);
			this.restoreIndex = restoreIndex;
			MainWindow.Instance.DnSpyFileListTreeNode.RegisterCached(asm, this.asmNode);
		}

		public void Add() {
			Debug.Assert(asmNode.Parent == null);
			if (asmNode.Parent != null)
				throw new InvalidOperationException();
			Debug.Assert(!restoreIndex || origIndex >= 0);

			MainWindow.Instance.DnSpyFileList.ForceAddFileToList(asmNode.DnSpyFile, true, false, origIndex, false);

			bool b = asmNode.DnSpyFile.ModuleDef == null ||
				(MainWindow.Instance.FindTreeNode(
				asmNode.DnSpyFile.AssemblyDef != null ?
				(object)asmNode.DnSpyFile.AssemblyDef :
				asmNode.DnSpyFile.ModuleDef) == asmNode);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		public void Remove() {
			Debug.Assert(asmNode.Parent != null);
			if (asmNode.Parent == null)
				throw new InvalidOperationException();

			if (restoreIndex && origIndex == -1) {
				origIndex = MainWindow.Instance.DnSpyFileListTreeNode.Children.IndexOf(asmNode);
				Debug.Assert(origIndex >= 0);
				if (origIndex < 0)
					throw new InvalidOperationException();
			}

			asmNode.Delete(false);
		}

		public void Dispose() {
			if (asmNode != null)
				MainWindow.Instance.DnSpyFileListTreeNode.UnregisterCached(asmNode.DnSpyFile);
			asmNode = null;
		}
	}
}

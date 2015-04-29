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
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.AsmEditor
{
	/// <summary>
	/// Creates and caches an <see cref="AssemblyTreeNode"/> so the same one is always used.
	/// </summary>
	sealed class AssemblyTreeNodeCreator : IDisposable
	{
		AssemblyTreeNode asmNode;
		bool restoreIndex;
		int origIndex = -1;

		public AssemblyTreeNode AssemblyTreeNode {
			get { return asmNode; }
		}

		public AssemblyTreeNodeCreator(LoadedAssembly asm)
			: this(asm, null, false)
		{
		}

		public AssemblyTreeNodeCreator(AssemblyTreeNode asmNode)
			: this(asmNode.LoadedAssembly, asmNode, true)
		{
		}

		AssemblyTreeNodeCreator(LoadedAssembly asm, AssemblyTreeNode asmNode, bool restoreIndex)
		{
			this.asmNode = asmNode ?? new AssemblyTreeNode(asm);
			this.restoreIndex = restoreIndex;
			MainWindow.Instance.AssemblyListTreeNode.RegisterCached(asm, this.asmNode);
		}

		public void Add()
		{
			Debug.Assert(asmNode.Parent == null);
			if (asmNode.Parent != null)
				throw new InvalidOperationException();
			Debug.Assert(!restoreIndex || origIndex >= 0);

			MainWindow.Instance.CurrentAssemblyList.ForceAddAssemblyToList(asmNode.LoadedAssembly, true, false, origIndex);

			bool b = asmNode.LoadedAssembly.ModuleDefinition == null ||
				(MainWindow.Instance.FindTreeNode(
				asmNode.LoadedAssembly.AssemblyDefinition != null ?
				(object)asmNode.LoadedAssembly.AssemblyDefinition :
				asmNode.LoadedAssembly.ModuleDefinition) == asmNode);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		public void Remove()
		{
			Debug.Assert(asmNode.Parent != null);
			if (asmNode.Parent == null)
				throw new InvalidOperationException();

			if (restoreIndex && origIndex == -1) {
				origIndex = MainWindow.Instance.AssemblyListTreeNode.Children.IndexOf(asmNode);
				Debug.Assert(origIndex >= 0);
				if (origIndex < 0)
					throw new InvalidOperationException();
			}

			asmNode.Delete();
		}

		public void Dispose()
		{
			if (asmNode != null)
				MainWindow.Instance.AssemblyListTreeNode.UnregisterCached(asmNode.LoadedAssembly);
			asmNode = null;
		}
	}
}

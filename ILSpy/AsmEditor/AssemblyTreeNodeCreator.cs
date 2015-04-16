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
	/// Creates <see cref="AssemblyTreeNode"/> and makes sure to re-use it if it's deleted and then
	/// re-added.
	/// </summary>
	sealed class AssemblyTreeNodeCreator : IDisposable
	{
		AssemblyTreeNode asmNode;

		public AssemblyTreeNode AssemblyTreeNode
		{
			get { return asmNode; }
		}

		public AssemblyTreeNodeCreator(LoadedAssembly asm)
			: this(asm, null)
		{
		}

		public AssemblyTreeNodeCreator(AssemblyTreeNode asmNode)
			: this(asmNode.LoadedAssembly, asmNode)
		{
		}

		AssemblyTreeNodeCreator(LoadedAssembly asm, AssemblyTreeNode asmNode)
		{
			this.asmNode = asmNode ?? new AssemblyTreeNode(asm);
			MainWindow.Instance.AssemblyListTreeNode.RegisterCached(asm, this.asmNode);
		}

		public void Add()
		{
			Debug.Assert(asmNode.Parent == null);
			if (asmNode.Parent != null)
				throw new InvalidOperationException();

			MainWindow.Instance.CurrentAssemblyList.ForceAddAssemblyToList(asmNode.LoadedAssembly, true, false);

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

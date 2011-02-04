// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Represents a list of assemblies.
	/// </summary>
	public sealed class AssemblyListTreeNode : SharpTreeNode
	{
		public override bool CanDelete(SharpTreeNode[] nodes)
		{
			return nodes.All(n => n is AssemblyTreeNode);
		}
		
		public override void Delete(SharpTreeNode[] nodes)
		{
			DeleteCore(nodes);
		}
		
		public override void DeleteCore(SharpTreeNode[] nodes)
		{
			foreach (SharpTreeNode node in nodes)
				this.Children.Remove(node);
		}
		
		public override DropEffect CanDrop(System.Windows.IDataObject data, DropEffect requestedEffect)
		{
			return requestedEffect;
		}
	}
}

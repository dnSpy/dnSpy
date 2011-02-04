// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Module reference in ReferenceFolderTreeNode.
	/// </summary>
	public class ModuleReferenceTreeNode : SharpTreeNode
	{
		ModuleReference r;
		
		public ModuleReferenceTreeNode(ModuleReference r)
		{
			if (r == null)
				throw new ArgumentNullException("r");
			this.r = r;
		}
		
		public override object Text {
			get { return r.Name; }
		}
	}
}

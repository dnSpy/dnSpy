// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Node within assembly reference list.
	/// </summary>
	public class AssemblyReferenceTreeNode : SharpTreeNode
	{
		readonly AssemblyNameReference r;
		
		public AssemblyReferenceTreeNode(AssemblyNameReference r)
		{
			if (r == null)
				throw new ArgumentNullException("r");
			this.r = r;
		}
		
		public override object Text {
			get { return r.Name; }
		}
		
		public override object Icon {
			get { return Images.Assembly; }
		}
		
		// TODO: allow drilling down into references used by this reference
	}
}

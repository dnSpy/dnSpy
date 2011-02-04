// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.ObjectModel;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy
{
	sealed class NamespaceTreeNode : SharpTreeNode
	{
		string name;
		
		public string Name {
			get { return name; }
		}
		
		public NamespaceTreeNode(string name)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			this.name = name.Length == 0 ? "-" : name;
		}
		
		public override object Text {
			get { return name; }
		}
		
		public override object Icon {
			get { return Images.Namespace; }
		}
	}
}

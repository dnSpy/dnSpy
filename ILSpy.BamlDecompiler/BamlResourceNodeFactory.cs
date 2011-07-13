// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

using ICSharpCode.ILSpy.TreeNodes;

namespace ILSpy.BamlDecompiler
{
	[Export(typeof(IResourceNodeFactory))]
	public sealed class BamlResourceNodeFactory : IResourceNodeFactory
	{
		public ILSpyTreeNode CreateNode(Mono.Cecil.Resource resource)
		{
			return null;
		}
		
		public ILSpyTreeNode CreateNode(string key, object data)
		{
			if (key.EndsWith(".baml", StringComparison.OrdinalIgnoreCase) && data is Stream)
				return new BamlResourceEntryNode(key, (Stream)data);
			else
				return null;
		}
	}
}

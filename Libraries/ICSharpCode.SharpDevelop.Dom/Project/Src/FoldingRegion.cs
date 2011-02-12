// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom
{
	public sealed class FoldingRegion : Immutable
	{
		string  name;
		DomRegion region;
		
		public string Name {
			get {
				return name;
			}
		}
		
		public DomRegion Region {
			get {
				return region;
			}
		}
		
		public FoldingRegion(string name, DomRegion region)
		{
			this.name = name;
			this.region = region;
		}
	}
}

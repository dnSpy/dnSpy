// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.PatternMatching
{
	public class Backreference : Pattern
	{
		readonly string referencedGroupName;
		
		public Backreference(string referencedGroupName)
		{
			if (referencedGroupName == null)
				throw new ArgumentNullException("referencedGroupName");
			this.referencedGroupName = referencedGroupName;
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			return match[referencedGroupName].Last().Match(other) != null;
		}
	}
}

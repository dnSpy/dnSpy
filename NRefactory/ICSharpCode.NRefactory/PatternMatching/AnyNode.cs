// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.PatternMatching
{
	/// <summary>
	/// Matches any node.
	/// </summary>
	/// <remarks>Does not match null nodes.</remarks>
	public class AnyNode : Pattern
	{
		readonly string groupName;
		
		public string GroupName {
			get { return groupName; }
		}
		
		public AnyNode(string groupName = null)
		{
			this.groupName = groupName;
		}
		
		public override bool DoMatch(INode other, Match match)
		{
			match.Add(this.groupName, other);
			return other != null && !other.IsNull;
		}
		
		public override S AcceptVisitor<T, S>(IPatternAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitAnyNode(this, data);
		}
	}
}

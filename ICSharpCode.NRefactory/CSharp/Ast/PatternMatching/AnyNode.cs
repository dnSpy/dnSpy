// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.CSharp.PatternMatching
{
	/// <summary>
	/// Matches any node.
	/// </summary>
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
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			match.Add(this.groupName, other);
			return other != null && !other.IsNull;
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return ((IPatternAstVisitor<T, S>)visitor).VisitAnyNode(this, data);
		}
	}
}

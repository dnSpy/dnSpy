// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.PatternMatching
{
	/// <summary>
	/// Represents a named node within a pattern.
	/// </summary>
	public class NamedNode : Pattern
	{
		readonly string groupName;
		readonly INode childNode;
		
		public string GroupName {
			get { return groupName; }
		}
		
		public INode ChildNode {
			get { return childNode; }
		}
		
		public NamedNode(string groupName, INode childNode)
		{
			if (childNode == null)
				throw new ArgumentNullException("childNode");
			this.groupName = groupName;
			this.childNode = childNode;
		}
		
		public override bool DoMatch(INode other, Match match)
		{
			match.Add(this.groupName, other);
			return childNode.DoMatch(other, match);
		}
		
		public override S AcceptVisitor<T, S>(IPatternAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitNamedNode(this, data);
		}
	}
}

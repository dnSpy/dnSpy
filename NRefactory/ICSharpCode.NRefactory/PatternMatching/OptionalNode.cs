// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ICSharpCode.NRefactory.PatternMatching
{
	public class OptionalNode : Pattern
	{
		readonly INode childNode;
		
		public INode ChildNode {
			get { return childNode; }
		}
		
		public OptionalNode(INode childNode)
		{
			if (childNode == null)
				throw new ArgumentNullException("childNode");
			this.childNode = childNode;
		}
		
		public OptionalNode(string groupName, INode childNode) : this(new NamedNode(groupName, childNode))
		{
		}
		
		public override bool DoMatchCollection(Role role, INode pos, Match match, BacktrackingInfo backtrackingInfo)
		{
			backtrackingInfo.backtrackingStack.Push(new PossibleMatch(pos, match.CheckPoint()));
			return childNode.DoMatch(pos, match);
		}
		
		public override bool DoMatch(INode other, Match match)
		{
			if (other == null || other.IsNull)
				return true;
			else
				return childNode.DoMatch(other, match);
		}
		
		public override S AcceptVisitor<T, S>(IPatternAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitOptionalNode(this, data);
		}
	}
}

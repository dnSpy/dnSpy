// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ICSharpCode.NRefactory.CSharp.PatternMatching
{
	public class OptionalNode : Pattern
	{
		public static readonly Role<AstNode> ElementRole = new Role<AstNode>("Element", AstNode.Null);
		
		public OptionalNode(AstNode childNode)
		{
			AddChild(childNode, ElementRole);
		}
		
		public OptionalNode(string groupName, AstNode childNode) : this(new NamedNode(groupName, childNode))
		{
		}
		
		internal override bool DoMatchCollection(Role role, AstNode pos, Match match, Stack<Pattern.PossibleMatch> backtrackingStack)
		{
			backtrackingStack.Push(new PossibleMatch(pos, match.CheckPoint()));
			return GetChildByRole(ElementRole).DoMatch(pos, match);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			if (other == null || other.IsNull)
				return true;
			else
				return GetChildByRole(ElementRole).DoMatch(other, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return ((IPatternAstVisitor<T, S>)visitor).VisitOptionalNode(this, data);
		}
	}
}

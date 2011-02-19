// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.CSharp.PatternMatching
{
	/// <summary>
	/// Represents a named node within a pattern.
	/// </summary>
	public class NamedNode : Pattern
	{
		public static readonly Role<AstNode> ElementRole = new Role<AstNode>("Element", AstNode.Null);
		
		readonly string groupName;
		
		public NamedNode(string groupName, AstNode childNode)
		{
			this.groupName = groupName;
			AddChild(childNode, ElementRole);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			match.Add(this.groupName, other);
			return GetChildByRole(ElementRole).DoMatch(other, match);
		}
	}
}

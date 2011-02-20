// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.CSharp.PatternMatching
{
	/// <summary>
	/// Matches one of several alternatives.
	/// </summary>
	public class Choice : Pattern
	{
		public static readonly Role<AstNode> AlternativeRole = new Role<AstNode>("Alternative", AstNode.Null);
		
		public Choice(params AstNode[] alternatives)
		{
			foreach (AstNode node in alternatives)
				AddChild(node, AlternativeRole);
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			var checkPoint = match.CheckPoint();
			foreach (AstNode alt in GetChildrenByRole(AlternativeRole)) {
				if (alt.DoMatch(other, match))
					return true;
				else
					match.RestoreCheckPoint(checkPoint);
			}
			return false;
		}
	}
}

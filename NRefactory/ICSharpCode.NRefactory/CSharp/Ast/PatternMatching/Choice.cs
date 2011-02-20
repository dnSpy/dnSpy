// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections;

namespace ICSharpCode.NRefactory.CSharp.PatternMatching
{
	/// <summary>
	/// Matches one of several alternatives.
	/// </summary>
	public class Choice : Pattern, IEnumerable
	{
		public static readonly Role<AstNode> AlternativeRole = new Role<AstNode>("Alternative", AstNode.Null);
		
		public void Add(string name, AstNode alternative)
		{
			AddChild(new NamedNode(name, alternative), AlternativeRole);
		}
		
		public void Add(AstNode alternative)
		{
			AddChild(alternative, AlternativeRole);
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
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetChildrenByRole(AlternativeRole).GetEnumerator();
		}
	}
}

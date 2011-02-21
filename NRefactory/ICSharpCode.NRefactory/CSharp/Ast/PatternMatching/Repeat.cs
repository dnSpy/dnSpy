// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ICSharpCode.NRefactory.CSharp.PatternMatching
{
	/// <summary>
	/// Represents an optional node.
	/// </summary>
	public class Repeat : Pattern
	{
		public static readonly Role<AstNode> ElementRole = new Role<AstNode>("Element", AstNode.Null);
		public int MinCount;
		public int MaxCount = int.MaxValue;
		
		public Repeat(AstNode childNode)
		{
			AddChild(childNode, ElementRole);
		}
		
		internal override bool DoMatchCollection(Role role, AstNode pos, Match match, Stack<Pattern.PossibleMatch> backtrackingStack)
		{
			Debug.Assert(pos == null || pos.Role == role);
			int matchCount = 0;
			if (this.MinCount <= 0)
				backtrackingStack.Push(new PossibleMatch(pos, match.CheckPoint()));
			AstNode element = GetChildByRole(ElementRole);
			while (matchCount < this.MaxCount && pos != null && element.DoMatch(pos, match)) {
				matchCount++;
				do {
					pos = pos.NextSibling;
				} while (pos != null && pos.Role != role);
				if (matchCount >= this.MinCount)
					backtrackingStack.Push(new PossibleMatch(pos, match.CheckPoint()));
			}
			return false; // never do a normal (single-element) match; always make the caller look at the results on the back-tracking stack.
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			if (other == null || other.IsNull)
				return this.MinCount <= 0;
			else
				return this.MaxCount >= 1 && GetChildByRole(ElementRole).DoMatch(other, match);
		}
	}
}

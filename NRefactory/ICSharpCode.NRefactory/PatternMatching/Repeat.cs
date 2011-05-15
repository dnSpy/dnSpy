// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ICSharpCode.NRefactory.PatternMatching
{
	/// <summary>
	/// Represents an optional node.
	/// </summary>
	public class Repeat : Pattern
	{
		readonly INode childNode;
		
		public int MinCount { get; set; }
		public int MaxCount { get; set; }
		
		public INode ChildNode {
			get { return childNode; }
		}
		
		public Repeat(INode childNode)
		{
			if (childNode == null)
				throw new ArgumentNullException("childNode");
			this.childNode = childNode;
			this.MinCount = 0;
			this.MaxCount = int.MaxValue;
		}
		
		public override bool DoMatchCollection(Role role, INode pos, Match match, BacktrackingInfo backtrackingInfo)
		{
			var backtrackingStack = backtrackingInfo.backtrackingStack;
			Debug.Assert(pos == null || pos.Role == role);
			int matchCount = 0;
			if (this.MinCount <= 0)
				backtrackingStack.Push(new PossibleMatch(pos, match.CheckPoint()));
			while (matchCount < this.MaxCount && pos != null && childNode.DoMatch(pos, match)) {
				matchCount++;
				do {
					pos = pos.NextSibling;
				} while (pos != null && pos.Role != role);
				if (matchCount >= this.MinCount)
					backtrackingStack.Push(new PossibleMatch(pos, match.CheckPoint()));
			}
			return false; // never do a normal (single-element) match; always make the caller look at the results on the back-tracking stack.
		}
		
		public override bool DoMatch(INode other, Match match)
		{
			if (other == null || other.IsNull)
				return this.MinCount <= 0;
			else
				return this.MaxCount >= 1 && childNode.DoMatch(other, match);
		}
		
		public override S AcceptVisitor<T, S>(IPatternAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitRepeat(this, data);
		}
	}
}

// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.PatternMatching
{
	/// <summary>
	/// Matches one of several alternatives.
	/// </summary>
	public class Choice : Pattern, IEnumerable<INode>
	{
		readonly List<INode> alternatives = new List<INode>();
		
		public void Add(string name, INode alternative)
		{
			if (alternative == null)
				throw new ArgumentNullException("alternative");
			alternatives.Add(new NamedNode(name, alternative));
		}
		
		public void Add(INode alternative)
		{
			if (alternative == null)
				throw new ArgumentNullException("alternative");
			alternatives.Add(alternative);
		}
		
		public override bool DoMatch(INode other, Match match)
		{
			var checkPoint = match.CheckPoint();
			foreach (INode alt in alternatives) {
				if (alt.DoMatch(other, match))
					return true;
				else
					match.RestoreCheckPoint(checkPoint);
			}
			return false;
		}
		
		IEnumerator<INode> IEnumerable<INode>.GetEnumerator()
		{
			return alternatives.GetEnumerator();
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return alternatives.GetEnumerator();
		}
		
		public override S AcceptVisitor<T, S>(IPatternAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitChoice(this, data);
		}
	}
}

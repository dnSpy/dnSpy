// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
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
		
		protected internal override bool DoMatchCollection(Role role, ref AstNode other, Match match)
		{
			Debug.Assert(other != null && other.Role == role);
			int matchCount = 0;
			var lastValidCheckpoint = match.CheckPoint();
			AstNode element = GetChildByRole(ElementRole);
			AstNode pos = other;
			while (pos != null && element.DoMatch(pos, match)) {
				matchCount++;
				lastValidCheckpoint = match.CheckPoint();
				do {
					pos = pos.NextSibling;
				} while (pos != null && pos.Role != role);
				// set 'other' (=pointer in collection) to the next node after the valid match
				other = pos;
			}
			match.RestoreCheckPoint(lastValidCheckpoint); // restote old checkpoint after failed match
			return matchCount >= MinCount && matchCount <= MaxCount;
		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			if (other == null || other.IsNull)
				return this.MinCount <= 0;
			else
				return GetChildByRole(ElementRole).DoMatch(other, match);
		}
	}
}

// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ICSharpCode.NRefactory.PatternMatching
{
	/// <summary>
	/// Base class for all patterns.
	/// </summary>
	public abstract class Pattern : INode
	{
		internal struct PossibleMatch
		{
			public readonly INode NextOther; // next node after the last matched node
			public readonly int Checkpoint; // checkpoint
			
			public PossibleMatch(INode nextOther, int checkpoint)
			{
				this.NextOther = nextOther;
				this.Checkpoint = checkpoint;
			}
		}
		
		bool INode.IsNull {
			get { return false; }
		}
		
		Role INode.Role {
			get { return null; }
		}
		
		INode INode.NextSibling {
			get { return null; }
		}
		
		INode INode.FirstChild {
			get { return null; }
		}
		
		public abstract bool DoMatch(INode other, Match match);
		
		public virtual bool DoMatchCollection(Role role, INode pos, Match match, BacktrackingInfo backtrackingInfo)
		{
			return DoMatch (pos, match);
		}
		
		public abstract S AcceptVisitor<T, S> (IPatternAstVisitor<T, S> visitor, T data);
		
		// Make debugging easier by giving Patterns a ToString() implementation
		public override string ToString()
		{
			// TODO: what if this pattern contains a VB-AST?
			// either remove ToString() here, or add some magic to figure out the correct output visitor
			StringWriter w = new StringWriter();
			AcceptVisitor(new CSharp.OutputVisitor(w, new CSharp.CSharpFormattingOptions()), null);
			return w.ToString();
		}
		
		public static bool DoMatchCollection(Role role, INode firstPatternChild, INode firstOtherChild, Match match)
		{
			BacktrackingInfo backtrackingInfo = new BacktrackingInfo();
			Stack<INode> patternStack = new Stack<INode>();
			Stack<PossibleMatch> stack = backtrackingInfo.backtrackingStack;
			patternStack.Push(firstPatternChild);
			stack.Push(new PossibleMatch(firstOtherChild, match.CheckPoint()));
			while (stack.Count > 0) {
				INode cur1 = patternStack.Pop();
				INode cur2 = stack.Peek().NextOther;
				match.RestoreCheckPoint(stack.Pop().Checkpoint);
				bool success = true;
				while (cur1 != null && success) {
					while (cur1 != null && cur1.Role != role)
						cur1 = cur1.NextSibling;
					while (cur2 != null && cur2.Role != role)
						cur2 = cur2.NextSibling;
					if (cur1 == null)
						break;
					
					Debug.Assert(stack.Count == patternStack.Count);
					success = cur1.DoMatchCollection(role, cur2, match, backtrackingInfo);
					Debug.Assert(stack.Count >= patternStack.Count);
					while (stack.Count > patternStack.Count)
						patternStack.Push(cur1.NextSibling);
					
					cur1 = cur1.NextSibling;
					if (cur2 != null)
						cur2 = cur2.NextSibling;
				}
				while (cur2 != null && cur2.Role != role)
					cur2 = cur2.NextSibling;
				if (success && cur2 == null)
					return true;
			}
			return false;
		}
	}
}

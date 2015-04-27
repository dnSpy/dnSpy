// Copyright (c) 2011-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

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
		/// <summary>
		/// Gets the string that matches any string.
		/// </summary>
		public static readonly string AnyString = "$any$";
		
		public static bool MatchString(string pattern, string text)
		{
			return pattern == AnyString || pattern == text;
		}
		
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

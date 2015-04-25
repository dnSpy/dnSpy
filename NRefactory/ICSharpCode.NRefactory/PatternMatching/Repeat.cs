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
	}
}

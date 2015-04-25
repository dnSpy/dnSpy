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
	}
}

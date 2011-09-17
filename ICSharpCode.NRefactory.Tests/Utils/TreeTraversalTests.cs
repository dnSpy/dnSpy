// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.Utils
{
	[TestFixture]
	public class TreeTraversalTests
	{
		sealed class Node
		{
			public int Data;
			public List<Node> Children = new List<Node>();
		}
		
		Node tree = new Node {
			Data = 1,
			Children = {
				new Node { Data = 2 },
				new Node { Data = 3,
					Children = {
						new Node { Data = 4 },
						new Node { Data = 5 }
					} },
				new Node { Data = 6, Children = null }
			}
		};
		
		[Test]
		public void PreOrder()
		{
			Assert.AreEqual(new int[] { 1, 2, 3, 4, 5, 6 },
			                TreeTraversal.PreOrder(tree, n => n.Children).Select(n => n.Data).ToArray());
		}
		
		[Test]
		public void PostOrder()
		{
			Assert.AreEqual(new int[] { 2, 4, 5, 3, 6, 1 },
			                TreeTraversal.PostOrder(tree, n => n.Children).Select(n => n.Data).ToArray());
		}
	}
}

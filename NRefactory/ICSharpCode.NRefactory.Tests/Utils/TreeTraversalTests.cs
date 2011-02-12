// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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

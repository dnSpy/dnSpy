// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using NUnit.Framework;
using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.VB.Dom;
using ICSharpCode.NRefactory.VB.Visitors;

namespace ICSharpCode.NRefactory.VB.Tests.Dom
{
	/// <summary>
	/// Ensures that all nodes have the Parent property correctly set.
	/// </summary>
	public class CheckParentVisitor : NodeTrackingDomVisitor
	{
		Stack<INode> nodeStack = new Stack<INode>();
		
		public CheckParentVisitor()
		{
			nodeStack.Push(null);
		}
		
		protected override void BeginVisit(INode node)
		{
			nodeStack.Push(node);
		}
		
		protected override void EndVisit(INode node)
		{
			Assert.AreSame(node, nodeStack.Pop(), "nodeStack was corrupted!");
			Assert.AreSame(nodeStack.Peek(), node.Parent, "node " + node + " is missing parent: " + nodeStack.Peek());
		}
	}
}

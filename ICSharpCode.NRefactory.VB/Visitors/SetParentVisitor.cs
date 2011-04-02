// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.VB.Ast;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.VB.Visitors
{
	/// <summary>
	/// Sets the parent property on all nodes in the tree.
	/// </summary>
	public class SetParentVisitor : NodeTrackingAstVisitor
	{
		Stack<INode> nodeStack = new Stack<INode>();
		
		public SetParentVisitor()
		{
			nodeStack.Push(null);
		}
		
		protected override void BeginVisit(INode node)
		{
			node.Parent = nodeStack.Peek();
			nodeStack.Push(node);
		}
		
		protected override void EndVisit(INode node)
		{
			nodeStack.Pop();
		}
	}
}

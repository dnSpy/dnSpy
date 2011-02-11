// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Visitors
{
	/// <summary>
	/// Sets StartLocation and EndLocation (region) of every node to union of regions of its children.
	/// Parsers don't do this by default:
	/// e.g. "a.Foo()" is InvocationExpression, its region includes only the "()" and it has a child MemberReferenceExpression, with region ".Foo".
	/// </summary>
	public class SetRegionInclusionVisitor : NodeTrackingAstVisitor
	{
		Stack<INode> parentNodes = new Stack<INode>();
		
		/// <summary>
		/// Sets StartLocation and EndLocation (region) of every node to union of regions of its children.
		/// Parsers don't do this by default:
		/// e.g. "a.Foo()" is InvocationExpression, its region includes only the "()" and it has a child MemberReferenceExpression, with region ".Foo".
		/// </summary>
		public SetRegionInclusionVisitor()
		{
			parentNodes.Push(null);
		}
		
		protected override void BeginVisit(INode node)
		{
			base.BeginVisit(node);
			
			// Only push nodes on the stack which have valid position information.
			if (node != null &&
			    node.StartLocation.X >= 1 && node.StartLocation.Y >= 1 &&
			    node.EndLocation.X >= 1 && node.EndLocation.Y >= 1) {
				
				if (node is PropertyDeclaration) {
					// PropertyDeclaration has correctly set BodyStart and BodyEnd by the parser,
					// but it has no subnode "body", just 2 children GetRegion and SetRegion which don't span
					// the whole (BodyStart, BodyEnd) region => we have to handle PropertyDeclaration as a special case.
					node.EndLocation = ((PropertyDeclaration)node).BodyEnd;
				}
				
				this.parentNodes.Push(node);
			}
		}
		
		protected override void EndVisit(INode node)
		{
			base.EndVisit(node);
			
			// Only remove those nodes which have actually been pushed before.
			if (this.parentNodes.Count > 0 && INode.ReferenceEquals(this.parentNodes.Peek(), node)) {
				// remove this node
				this.parentNodes.Pop();
				// fix region of parent
				var parent = this.parentNodes.Peek();
				if (parent == null)
					return;
				if (node.StartLocation < parent.StartLocation)
					parent.StartLocation = node.StartLocation;
				if (node.EndLocation > parent.EndLocation)
					parent.EndLocation = node.EndLocation;
			}
			
			// Block statement as a special case - we want block contents without the '{' and '}'
			if (node is BlockStatement) {
				var firstSatement = node.Children.FirstOrDefault();
				if (firstSatement != null) {
					node.StartLocation = firstSatement.StartLocation;
					node.EndLocation = node.Children.Last().EndLocation;
				}
			}
		}
	}
}

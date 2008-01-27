using System;

using Ast = ICSharpCode.NRefactory.Ast;
using Decompiler.ControlFlow;

namespace ICSharpCode.NRefactory.Ast
{
	public class MyGotoStatement: Ast.GotoStatement
	{
		NodeLabel nodeLabel;
		
		public NodeLabel NodeLabel {
			get { return nodeLabel; }
		}
		
		public MyGotoStatement(NodeLabel nodeLabel): base(nodeLabel.Label)
		{
			this.nodeLabel = nodeLabel;
			
			this.nodeLabel.ReferenceCount++;
		}
		
		public static Ast.Statement Create(Node contextNode, Node targetNode)
		{
			// Propagate target up to the top most scope
			while (targetNode.Parent != null && targetNode.Parent.HeadChild == targetNode) {
				targetNode = targetNode.Parent;
			}
			// If branches to the start of encapsulating loop
			if (contextNode.Parent is Loop && targetNode == contextNode.Parent) {
				return new Ast.ContinueStatement();
			}
			// If branches outside the encapsulating loop
			if (contextNode.Parent is Loop && targetNode == contextNode.Parent.NextNode) {
				return new Ast.BreakStatement();
			}
			return new Ast.MyGotoStatement(targetNode.Label);
		}
	}
}

using System;

using Ast = ICSharpCode.NRefactory.Ast;
using Decompiler.ControlFlow;

namespace ICSharpCode.NRefactory.Ast
{
	public class MyLabelStatement: Ast.LabelStatement
	{
		NodeLabel nodeLabel;
		
		public NodeLabel NodeLabel {
			get { return nodeLabel; }
		}
		
		public MyLabelStatement(NodeLabel nodeLabel): base(nodeLabel.Label)
		{
			this.nodeLabel = nodeLabel;
		}
	}
}

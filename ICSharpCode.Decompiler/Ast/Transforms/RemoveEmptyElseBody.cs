using System;

using Ast = ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace Decompiler.Transforms.Ast
{
	public class RemoveEmptyElseBody: AbstractAstTransformer
	{
		public override object VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			for(int i = 0; i < blockStatement.Children.Count; i++) {
				if (blockStatement.Children[i] is Statement &&
				    ((Statement)blockStatement.Children[i]).IsNull)
				{
					blockStatement.Children.RemoveAt(i);
					i--;
				}
			}
			return base.VisitBlockStatement(blockStatement, data);
		}
		
		public override object VisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			base.VisitIfElseStatement(ifElseStatement, data);
			if (ifElseStatement.FalseStatement.Count == 1 &&
			    ifElseStatement.FalseStatement[0] is BlockStatement &&
			    ifElseStatement.FalseStatement[0].Children.Count == 0)
			{
				ifElseStatement.FalseStatement.Clear();
			}
			return null;
		}
	}
}

using System;

using Ast = ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace Decompiler.Transforms.Ast
{
	public class RemoveEmptyElseBody: AbstractAstTransformer
	{
		public override object VisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			base.VisitIfElseStatement(ifElseStatement, data);
			if (ifElseStatement.FalseStatement.Count == 1 &&
			    ifElseStatement.FalseStatement[0] is MyBlockStatement &&
			    ifElseStatement.FalseStatement[0].Children.Count == 0)
			{
				ifElseStatement.FalseStatement.Clear();
			}
			return null;
		}
	}
}

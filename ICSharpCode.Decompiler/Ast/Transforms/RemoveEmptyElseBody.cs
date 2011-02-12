using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;

namespace Decompiler.Transforms.Ast
{
	public class RemoveEmptyElseBody: DepthFirstAstVisitor<object, object>
	{
		public override object VisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			base.VisitIfElseStatement(ifElseStatement, data);
			BlockStatement block = ifElseStatement.FalseStatement as BlockStatement;
			if (block != null && !block.Statements.Any()) {
				ifElseStatement.FalseStatement = null;
			}
			return null;
		}
	}
}

using System;
using System.Collections.Generic;

using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace Decompiler.Transforms.Ast
{
	public class RemoveParenthesis: AbstractAstTransformer
	{
		Expression Deparenthesize(Expression expr)
		{
			if (expr is ParenthesizedExpression) {
				return Deparenthesize(((ParenthesizedExpression)expr).Expression);
			} else {
				return expr;
			}
		}
		
		public override object VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, object data)
		{
			// The following do not need to be parenthesized
			if (parenthesizedExpression.Expression is IdentifierExpression ||
			    parenthesizedExpression.Expression is PrimitiveExpression ||
			    parenthesizedExpression.Expression is ParenthesizedExpression) {
				ReplaceCurrentNode(parenthesizedExpression.Expression);
				return null;
			}
			return base.VisitParenthesizedExpression(parenthesizedExpression, data);
		}
		
		public override object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			return base.VisitBinaryOperatorExpression(binaryOperatorExpression, data);
		}
		
		public override object VisitExpressionStatement(ExpressionStatement expressionStatement, object data)
		{
			expressionStatement.Expression = Deparenthesize(expressionStatement.Expression);
			return base.VisitExpressionStatement(expressionStatement, data);
		}
		
		public override object VisitForStatement(ForStatement forStatement, object data)
		{
			forStatement.Condition = Deparenthesize(forStatement.Condition);
			return base.VisitForStatement(forStatement, data);
		}
		
		public override object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			return base.VisitInvocationExpression(invocationExpression, data);
		}
	}
}

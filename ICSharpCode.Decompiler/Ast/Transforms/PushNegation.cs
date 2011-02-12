using System;
using System.Collections.Generic;

using ICSharpCode.NRefactory.CSharp;

namespace Decompiler.Transforms.Ast
{
	public class PushNegation: DepthFirstAstVisitor<object, object>
	{
		public override object VisitUnaryOperatorExpression(UnaryOperatorExpression unary, object data)
		{
			// Remove double negation
			// !!a
			if (unary.Operator == UnaryOperatorType.Not &&
			    unary.Expression is UnaryOperatorExpression &&
			    (unary.Expression as UnaryOperatorExpression).Operator == UnaryOperatorType.Not)
			{
				AstNode newNode = (unary.Expression as UnaryOperatorExpression).Expression;
				unary.ReplaceWith(newNode);
				return newNode.AcceptVisitor(this, data);
			}
			
			// Push through binary operation
			// !((a) op (b))
			BinaryOperatorExpression binaryOp = unary.Expression as BinaryOperatorExpression;
			if (unary.Operator == UnaryOperatorType.Not && binaryOp != null) {
				bool sucessful = true;
				switch (binaryOp.Operator) {
					case BinaryOperatorType.Equality:           binaryOp.Operator = BinaryOperatorType.InEquality; break;
					case BinaryOperatorType.InEquality:         binaryOp.Operator = BinaryOperatorType.Equality; break;
					// TODO: these are invalid for floats (stupid NaN)
					case BinaryOperatorType.GreaterThan:        binaryOp.Operator = BinaryOperatorType.LessThanOrEqual; break;
					case BinaryOperatorType.GreaterThanOrEqual: binaryOp.Operator = BinaryOperatorType.LessThan; break;
					case BinaryOperatorType.LessThanOrEqual:    binaryOp.Operator = BinaryOperatorType.GreaterThan; break;
					case BinaryOperatorType.LessThan:           binaryOp.Operator = BinaryOperatorType.GreaterThanOrEqual; break;
					default: sucessful = false; break;
				}
				if (sucessful) {
					unary.ReplaceWith(binaryOp);
					return binaryOp.AcceptVisitor(this, data);
				}
				
				sucessful = true;
				switch (binaryOp.Operator) {
					case BinaryOperatorType.ConditionalAnd: binaryOp.Operator = BinaryOperatorType.ConditionalOr; break;
					case BinaryOperatorType.ConditionalOr:  binaryOp.Operator = BinaryOperatorType.ConditionalAnd; break;
					default: sucessful = false; break;
				}
				if (sucessful) {
					binaryOp.Left.ReplaceWith(e => new UnaryOperatorExpression(UnaryOperatorType.Not, e));
					binaryOp.Right.ReplaceWith(e => new UnaryOperatorExpression(UnaryOperatorType.Not, e));
					unary.ReplaceWith(binaryOp);
					return binaryOp.AcceptVisitor(this, data);
				}
			}
			return base.VisitUnaryOperatorExpression(unary, data);
		}
	}
}

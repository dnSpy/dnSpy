using System;
using System.Collections.Generic;

using ICSharpCode.NRefactory.CSharp;

namespace Decompiler.Transforms
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
				bool successful = true;
				switch (binaryOp.Operator) {
					case BinaryOperatorType.Equality:
						binaryOp.Operator = BinaryOperatorType.InEquality;
						break;
					case BinaryOperatorType.InEquality:
						binaryOp.Operator = BinaryOperatorType.Equality;
						break;
					case BinaryOperatorType.GreaterThan: // TODO: these are invalid for floats (stupid NaN)
						binaryOp.Operator = BinaryOperatorType.LessThanOrEqual;
						break;
					case BinaryOperatorType.GreaterThanOrEqual:
						binaryOp.Operator = BinaryOperatorType.LessThan;
						break;
					case BinaryOperatorType.LessThanOrEqual:
						binaryOp.Operator = BinaryOperatorType.GreaterThan;
						break;
					case BinaryOperatorType.LessThan:
						binaryOp.Operator = BinaryOperatorType.GreaterThanOrEqual;
						break;
					default:
						successful = false;
						break;
				}
				if (successful) {
					unary.ReplaceWith(binaryOp);
					return binaryOp.AcceptVisitor(this, data);
				}
				
				successful = true;
				switch (binaryOp.Operator) {
					case BinaryOperatorType.ConditionalAnd:
						binaryOp.Operator = BinaryOperatorType.ConditionalOr;
						break;
					case BinaryOperatorType.ConditionalOr:
						binaryOp.Operator = BinaryOperatorType.ConditionalAnd;
						break;
					default:
						successful = false;
						break;
				}
				if (successful) {
					binaryOp.Left.ReplaceWith(e => new UnaryOperatorExpression(UnaryOperatorType.Not, e));
					binaryOp.Right.ReplaceWith(e => new UnaryOperatorExpression(UnaryOperatorType.Not, e));
					unary.ReplaceWith(binaryOp);
					return binaryOp.AcceptVisitor(this, data);
				}
			}
			return base.VisitUnaryOperatorExpression(unary, data);
		}
		
		public override object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			BinaryOperatorType op = binaryOperatorExpression.Operator;
			bool? rightOperand = null;
			if (binaryOperatorExpression.Right is PrimitiveExpression)
				rightOperand = ((PrimitiveExpression)binaryOperatorExpression.Right).Value as bool?;
			if (op == BinaryOperatorType.Equality && rightOperand == true || op == BinaryOperatorType.InEquality && rightOperand == false) {
				// 'b == true' or 'b != false' is useless
				binaryOperatorExpression.Left.AcceptVisitor(this, data);
				binaryOperatorExpression.ReplaceWith(binaryOperatorExpression.Left);
				return null;
			} else if (op == BinaryOperatorType.Equality && rightOperand == false || op == BinaryOperatorType.InEquality && rightOperand == true) {
				// 'b == false' or 'b != true' is a negation:
				Expression left = binaryOperatorExpression.Left;
				left.Remove();
				UnaryOperatorExpression uoe = new UnaryOperatorExpression(UnaryOperatorType.Not, left);
				binaryOperatorExpression.ReplaceWith(uoe);
				return uoe.AcceptVisitor(this, data);
			} else {
				return base.VisitBinaryOperatorExpression(binaryOperatorExpression, data);
			}
		}
	}
}

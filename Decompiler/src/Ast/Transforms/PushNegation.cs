using System;
using System.Collections.Generic;

using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace Decompiler.Transforms.Ast
{
	public class PushNegation: AbstractAstTransformer
	{
		public override object VisitUnaryOperatorExpression(UnaryOperatorExpression unary, object data)
		{
			// Remove double negation
			// !!a
			if (unary.Op == UnaryOperatorType.Not &&
			    unary.Expression is UnaryOperatorExpression &&
			    (unary.Expression as UnaryOperatorExpression).Op == UnaryOperatorType.Not) {
				Expression newParenth = new ParenthesizedExpression((unary.Expression as UnaryOperatorExpression).Expression);
				ReplaceCurrentNode(newParenth);
				return newParenth.AcceptVisitor(this, data);
			}
			
			// Basic assumtion is that we have something in form !(a)
			if (unary.Op == UnaryOperatorType.Not &&
			    unary.Expression is ParenthesizedExpression) {
				ParenthesizedExpression parenth = ((ParenthesizedExpression)unary.Expression);
				
				// Push through two parenthesis
				// !((a))
				if (parenth.Expression is ParenthesizedExpression) {
					parenth.Expression = new UnaryOperatorExpression(parenth.Expression, UnaryOperatorType.Not);
					ReplaceCurrentNode(parenth);
					return parenth.AcceptVisitor(this, data);
				}
				
				// Remove double negation
				// !(!a)
				if (parenth.Expression is UnaryOperatorExpression &&
				    (parenth.Expression as UnaryOperatorExpression).Op == UnaryOperatorType.Not) {
					parenth.Expression = (parenth.Expression as UnaryOperatorExpression).Expression;
					ReplaceCurrentNode(parenth);
					return parenth.AcceptVisitor(this, data);
				}
				
				// Push through binary operation
				// !((a) op (b))
				BinaryOperatorExpression binaryOp = parenth.Expression as BinaryOperatorExpression;
				if (binaryOp != null &&
				    binaryOp.Left is ParenthesizedExpression &&
				    binaryOp.Right is ParenthesizedExpression) {
					
					bool sucessful = true;
					switch(binaryOp.Op) {
						case BinaryOperatorType.Equality:           binaryOp.Op = BinaryOperatorType.InEquality; break;
						case BinaryOperatorType.InEquality:         binaryOp.Op = BinaryOperatorType.Equality; break;
						case BinaryOperatorType.GreaterThan:        binaryOp.Op = BinaryOperatorType.LessThanOrEqual; break;
						case BinaryOperatorType.GreaterThanOrEqual: binaryOp.Op = BinaryOperatorType.LessThan; break;
						case BinaryOperatorType.LessThanOrEqual:    binaryOp.Op = BinaryOperatorType.GreaterThan; break;
						case BinaryOperatorType.LessThan:           binaryOp.Op = BinaryOperatorType.GreaterThanOrEqual; break;
						default: sucessful = false; break;
					}
					if (sucessful) {
						ReplaceCurrentNode(parenth);
						return parenth.AcceptVisitor(this, data);
					}
					
					sucessful = true;
					switch(binaryOp.Op) {
						case BinaryOperatorType.BitwiseAnd: binaryOp.Op = BinaryOperatorType.BitwiseOr; break;
						case BinaryOperatorType.BitwiseOr:  binaryOp.Op = BinaryOperatorType.BitwiseAnd; break;
						case BinaryOperatorType.LogicalAnd: binaryOp.Op = BinaryOperatorType.LogicalOr; break;
						case BinaryOperatorType.LogicalOr:  binaryOp.Op = BinaryOperatorType.LogicalAnd; break;
						default: sucessful = false; break;
					}
					if (sucessful) {
						binaryOp.Left = new UnaryOperatorExpression(binaryOp.Left, UnaryOperatorType.Not);
						binaryOp.Right = new UnaryOperatorExpression(binaryOp.Right, UnaryOperatorType.Not);
						ReplaceCurrentNode(parenth);
						return parenth.AcceptVisitor(this, data);
					}
				}
			}
			
			return base.VisitUnaryOperatorExpression(unary, data);
		}
	}
}

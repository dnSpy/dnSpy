// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	public class PushNegation: DepthFirstAstVisitor<object, object>, IAstTransform
	{
		sealed class LiftedOperator { }
		/// <summary>
		/// Annotation for lifted operators that cannot be transformed by PushNegation
		/// </summary>
		public static readonly object LiftedOperatorAnnotation = new LiftedOperator();

		public override object VisitUnaryOperatorExpression(UnaryOperatorExpression unary, object data)
		{
			// lifted operators can't be transformed
			if (unary.Annotation<LiftedOperator>() != null || unary.Expression.Annotation<LiftedOperator>() != null)
				return base.VisitUnaryOperatorExpression(unary, data);

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
		
		readonly static AstNode asCastIsNullPattern = new BinaryOperatorExpression(
			new AnyNode("expr").ToExpression().CastAs(new AnyNode("type")),
			BinaryOperatorType.Equality,
			new NullReferenceExpression()
		);
		
		readonly static AstNode asCastIsNotNullPattern = new BinaryOperatorExpression(
			new AnyNode("expr").ToExpression().CastAs(new AnyNode("type")),
			BinaryOperatorType.InEquality,
			new NullReferenceExpression()
		);
		
		public override object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			// lifted operators can't be transformed
			if (binaryOperatorExpression.Annotation<LiftedOperator>() != null)
				return base.VisitBinaryOperatorExpression(binaryOperatorExpression, data);

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
				bool negate = false;
				Match m = asCastIsNotNullPattern.Match(binaryOperatorExpression);
				if (!m.Success) {
					m = asCastIsNullPattern.Match(binaryOperatorExpression);
					negate = true;
				}
				if (m.Success) {
					Expression expr = m.Get<Expression>("expr").Single().Detach().IsType(m.Get<AstType>("type").Single().Detach());
					if (negate)
						expr = new UnaryOperatorExpression(UnaryOperatorType.Not, expr);
					binaryOperatorExpression.ReplaceWith(expr);
					return expr.AcceptVisitor(this, data);
				} else {
					return base.VisitBinaryOperatorExpression(binaryOperatorExpression, data);
				}
			}
		}
		void IAstTransform.Run(AstNode node)
		{
			node.AcceptVisitor(this, null);
		}
	}
}

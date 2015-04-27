// 
// CSharpUtil.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp
{
	public static class CSharpUtil
	{
		/// <summary>
		/// Inverts a boolean condition. Note: The condition object can be frozen (from AST) it's cloned internally.
		/// </summary>
		/// <param name="condition">The condition to invert.</param>
		public static Expression InvertCondition(Expression condition)
		{
			return InvertConditionInternal(condition);
		}
		
		static Expression InvertConditionInternal(Expression condition)
		{
			if (condition is ParenthesizedExpression) {
				return new ParenthesizedExpression(InvertCondition(((ParenthesizedExpression)condition).Expression));
			}
			
			if (condition is UnaryOperatorExpression) {
				var uOp = (UnaryOperatorExpression)condition;
				if (uOp.Operator == UnaryOperatorType.Not) {
					if (!(uOp.Parent is Expression))
						return GetInnerMostExpression(uOp.Expression).Clone();
					return uOp.Expression.Clone();
				}
				return new UnaryOperatorExpression(UnaryOperatorType.Not, uOp.Clone());
			}
			
			if (condition is BinaryOperatorExpression) {
				var bOp = (BinaryOperatorExpression)condition;

				if ((bOp.Operator == BinaryOperatorType.ConditionalAnd) || (bOp.Operator == BinaryOperatorType.ConditionalOr)) {
					return new BinaryOperatorExpression(InvertCondition(bOp.Left), NegateConditionOperator(bOp.Operator), InvertCondition(bOp.Right));
				} else if ((bOp.Operator == BinaryOperatorType.Equality) || (bOp.Operator == BinaryOperatorType.InEquality) || (bOp.Operator == BinaryOperatorType.GreaterThan)
					|| (bOp.Operator == BinaryOperatorType.GreaterThanOrEqual) || (bOp.Operator == BinaryOperatorType.LessThan) || 
					(bOp.Operator == BinaryOperatorType.LessThanOrEqual)) {
					return new BinaryOperatorExpression(bOp.Left.Clone(), NegateRelationalOperator(bOp.Operator), bOp.Right.Clone());
				} else {
					var negatedOp = NegateRelationalOperator(bOp.Operator);
					if (negatedOp == BinaryOperatorType.Any)
						return new UnaryOperatorExpression(UnaryOperatorType.Not, new ParenthesizedExpression(condition.Clone()));
					bOp = (BinaryOperatorExpression)bOp.Clone();
					bOp.Operator = negatedOp;
					return bOp;
				}
			}
			if (condition is ConditionalExpression) {
				var cEx = condition.Clone() as ConditionalExpression;
				cEx.Condition = InvertCondition(cEx.Condition);
				return cEx;
			}
			if (condition is PrimitiveExpression) {
				var pex = condition as PrimitiveExpression;
				if (pex.Value is bool) {
					return new PrimitiveExpression(!((bool)pex.Value)); 
				}
			}
			
			return new UnaryOperatorExpression(UnaryOperatorType.Not, AddParensForUnaryExpressionIfRequired(condition.Clone()));
		}

		/// <summary>
		/// When negating an expression this is required, otherwise you would end up with
		/// a or b -> !a or b
		/// </summary>
		internal static Expression AddParensForUnaryExpressionIfRequired(Expression expression)
		{
			if ((expression is BinaryOperatorExpression) ||
			    (expression is AssignmentExpression) ||
			    (expression is CastExpression) ||
			    (expression is AsExpression) ||
			    (expression is IsExpression) ||
			    (expression is LambdaExpression) ||
			    (expression is ConditionalExpression)) {
				return new ParenthesizedExpression(expression);
			}

			return expression;
		}

		/// <summary>
		/// Get negation of the specified relational operator
		/// </summary>
		/// <returns>
		/// negation of the specified relational operator, or BinaryOperatorType.Any if it's not a relational operator
		/// </returns>
		public static BinaryOperatorType NegateRelationalOperator(BinaryOperatorType op)
		{
			switch (op) {
				case BinaryOperatorType.GreaterThan:
					return BinaryOperatorType.LessThanOrEqual;
				case BinaryOperatorType.GreaterThanOrEqual:
					return BinaryOperatorType.LessThan;
				case BinaryOperatorType.Equality:
					return BinaryOperatorType.InEquality;
				case BinaryOperatorType.InEquality:
					return BinaryOperatorType.Equality;
				case BinaryOperatorType.LessThan:
					return BinaryOperatorType.GreaterThanOrEqual;
				case BinaryOperatorType.LessThanOrEqual:
					return BinaryOperatorType.GreaterThan;
				case BinaryOperatorType.ConditionalOr:
					return BinaryOperatorType.ConditionalAnd;
				case BinaryOperatorType.ConditionalAnd:
					return BinaryOperatorType.ConditionalOr;
			}
			return BinaryOperatorType.Any;
		}

		/// <summary>
		/// Returns true, if the specified operator is a relational operator
		/// </summary>
		public static bool IsRelationalOperator(BinaryOperatorType op)
		{
			return NegateRelationalOperator(op) != BinaryOperatorType.Any;
		}

		/// <summary>
		/// Get negation of the condition operator
		/// </summary>
		/// <returns>
		/// negation of the specified condition operator, or BinaryOperatorType.Any if it's not a condition operator
		/// </returns>
		public static BinaryOperatorType NegateConditionOperator(BinaryOperatorType op)
		{
			switch (op) {
				case BinaryOperatorType.ConditionalOr:
					return BinaryOperatorType.ConditionalAnd;
				case BinaryOperatorType.ConditionalAnd:
					return BinaryOperatorType.ConditionalOr;
			}
			return BinaryOperatorType.Any;
		}

		public static bool AreConditionsEqual(Expression cond1, Expression cond2)
		{
			if (cond1 == null || cond2 == null)
				return false;
			return GetInnerMostExpression(cond1).IsMatch(GetInnerMostExpression(cond2));
		}

		public static Expression GetInnerMostExpression(Expression target)
		{
			while (target is ParenthesizedExpression)
				target = ((ParenthesizedExpression)target).Expression;
			return target;
		}
	}
}


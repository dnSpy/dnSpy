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

namespace ICSharpCode.NRefactory.CSharp
{
	public static class CSharpUtil
	{
		public static Expression InvertCondition (Expression condition)
		{
			return InvertConditionInternal (condition.Clone ());
		}
		
		static Expression InvertConditionInternal (Expression condition)
		{
			if (condition is ParenthesizedExpression) {
				((ParenthesizedExpression)condition).Expression = InvertCondition (((ParenthesizedExpression)condition).Expression);
				return condition;
			}
			
			if (condition is UnaryOperatorExpression) {
				var uOp = (UnaryOperatorExpression)condition;
				if (uOp.Operator == UnaryOperatorType.Not)
					return uOp.Expression;
				return new UnaryOperatorExpression (UnaryOperatorType.Not, uOp);
			}
			
			if (condition is BinaryOperatorExpression) {
				var bOp = (BinaryOperatorExpression)condition;
				var negatedOp = NegateRelationalOperator (bOp.Operator);
				if (negatedOp == BinaryOperatorType.Any)
					return new UnaryOperatorExpression (UnaryOperatorType.Not, new ParenthesizedExpression (condition));
				bOp.Operator = negatedOp;
				return bOp;
			}
			if (condition is ConditionalExpression) {
				var cEx = condition as ConditionalExpression;
				cEx.Condition = InvertCondition (cEx.Condition);
				return cEx;
			}
			if (condition is PrimitiveExpression) {
				var pex = condition as PrimitiveExpression;
				if (pex.Value is bool) {
					return new PrimitiveExpression (!((bool)pex.Value)); 
				}
			}
			
			return new UnaryOperatorExpression (UnaryOperatorType.Not, condition);
		}

		/// <summary>
		/// Get negation of the specified relational operator
		/// </summary>
		/// <returns>
		/// negation of the specified relational operator, or BinaryOperatorType.Any if it's not a relational operator
		/// </returns>
		public static BinaryOperatorType NegateRelationalOperator (BinaryOperatorType op)
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
			}
			return BinaryOperatorType.Any;
		}
	}
}


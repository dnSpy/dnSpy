// 
// BinaryOperatorExpression.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Linq.Expressions;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Left Operator Right
	/// </summary>
	public class BinaryOperatorExpression : Expression
	{
		public readonly static Role<Expression> LeftRole = new Role<Expression>("Left", Expression.Null);
		public readonly static Role<CSharpTokenNode> OperatorRole = new Role<CSharpTokenNode>("Operator", CSharpTokenNode.Null);
		public readonly static Role<Expression> RightRole = new Role<Expression>("Right", Expression.Null);
		
		public BinaryOperatorExpression()
		{
		}
		
		public BinaryOperatorExpression(Expression left, BinaryOperatorType op, Expression right)
		{
			this.Left = left;
			this.Operator = op;
			this.Right = right;
		}
		
		public BinaryOperatorType Operator {
			get;
			set;
		}
		
		public Expression Left {
			get { return GetChildByRole (LeftRole); }
			set { SetChildByRole(LeftRole, value); }
		}
		
		public CSharpTokenNode OperatorToken {
			get { return GetChildByRole (OperatorRole); }
		}
		
		public Expression Right {
			get { return GetChildByRole (RightRole); }
			set { SetChildByRole(RightRole, value); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data = default(T))
		{
			return visitor.VisitBinaryOperatorExpression (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			BinaryOperatorExpression o = other as BinaryOperatorExpression;
			return o != null && (this.Operator == BinaryOperatorType.Any || this.Operator == o.Operator)
				&& this.Left.DoMatch(o.Left, match) && this.Right.DoMatch(o.Right, match);
		}
		
		public static string GetOperatorSymbol(BinaryOperatorType op)
		{
			switch (op) {
				case BinaryOperatorType.BitwiseAnd:
					return "&";
				case BinaryOperatorType.BitwiseOr:
					return "|";
				case BinaryOperatorType.ConditionalAnd:
					return "&&";
				case BinaryOperatorType.ConditionalOr:
					return "||";
				case BinaryOperatorType.ExclusiveOr:
					return "^";
				case BinaryOperatorType.GreaterThan:
					return ">";
				case BinaryOperatorType.GreaterThanOrEqual:
					return ">=";
				case BinaryOperatorType.Equality:
					return "==";
				case BinaryOperatorType.InEquality:
					return "!=";
				case BinaryOperatorType.LessThan:
					return "<";
				case BinaryOperatorType.LessThanOrEqual:
					return "<=";
				case BinaryOperatorType.Add:
					return "+";
				case BinaryOperatorType.Subtract:
					return "-";
				case BinaryOperatorType.Multiply:
					return "*";
				case BinaryOperatorType.Divide:
					return "/";
				case BinaryOperatorType.Modulus:
					return "%";
				case BinaryOperatorType.ShiftLeft:
					return "<<";
				case BinaryOperatorType.ShiftRight:
					return ">>";
				case BinaryOperatorType.NullCoalescing:
					return "??";
				default:
					throw new NotSupportedException("Invalid value for BinaryOperatorType");
			}
		}
		
		public static ExpressionType GetLinqNodeType(BinaryOperatorType op, bool checkForOverflow)
		{
			switch (op) {
				case BinaryOperatorType.BitwiseAnd:
					return ExpressionType.And;
				case BinaryOperatorType.BitwiseOr:
					return ExpressionType.Or;
				case BinaryOperatorType.ConditionalAnd:
					return ExpressionType.AndAlso;
				case BinaryOperatorType.ConditionalOr:
					return ExpressionType.OrElse;
				case BinaryOperatorType.ExclusiveOr:
					return ExpressionType.ExclusiveOr;
				case BinaryOperatorType.GreaterThan:
					return ExpressionType.GreaterThan;
				case BinaryOperatorType.GreaterThanOrEqual:
					return ExpressionType.GreaterThanOrEqual;
				case BinaryOperatorType.Equality:
					return ExpressionType.Equal;
				case BinaryOperatorType.InEquality:
					return ExpressionType.NotEqual;
				case BinaryOperatorType.LessThan:
					return ExpressionType.LessThan;
				case BinaryOperatorType.LessThanOrEqual:
					return ExpressionType.LessThanOrEqual;
				case BinaryOperatorType.Add:
					return checkForOverflow ? ExpressionType.AddChecked : ExpressionType.Add;
				case BinaryOperatorType.Subtract:
					return checkForOverflow ? ExpressionType.SubtractChecked : ExpressionType.Subtract;
				case BinaryOperatorType.Multiply:
					return checkForOverflow ? ExpressionType.MultiplyChecked : ExpressionType.Multiply;
				case BinaryOperatorType.Divide:
					return ExpressionType.Divide;
				case BinaryOperatorType.Modulus:
					return ExpressionType.Modulo;
				case BinaryOperatorType.ShiftLeft:
					return ExpressionType.LeftShift;
				case BinaryOperatorType.ShiftRight:
					return ExpressionType.RightShift;
				case BinaryOperatorType.NullCoalescing:
					return ExpressionType.Coalesce;
				default:
					throw new NotSupportedException("Invalid value for BinaryOperatorType");
			}
		}
	}
	
	public enum BinaryOperatorType
	{
		/// <summary>
		/// Any binary operator (used in pattern matching)
		/// </summary>
		Any,
		
		// We avoid 'logical or' on purpose, because it's not clear if that refers to the bitwise
		// or to the short-circuiting (conditional) operator:
		// MCS and old NRefactory used bitwise='|', logical='||'
		// but the C# spec uses logical='|', conditional='||'
		/// <summary>left &amp; right</summary>
		BitwiseAnd,
		/// <summary>left | right</summary>
		BitwiseOr,
		/// <summary>left &amp;&amp; right</summary>
		ConditionalAnd,
		/// <summary>left || right</summary>
		ConditionalOr,
		/// <summary>left ^ right</summary>
		ExclusiveOr,
		
		/// <summary>left &gt; right</summary>
		GreaterThan,
		/// <summary>left &gt;= right</summary>
		GreaterThanOrEqual,
		/// <summary>left == right</summary>
		Equality,
		/// <summary>left != right</summary>
		InEquality,
		/// <summary>left &lt; right</summary>
		LessThan,
		/// <summary>left &lt;= right</summary>
		LessThanOrEqual,
		
		/// <summary>left + right</summary>
		Add,
		/// <summary>left - right</summary>
		Subtract,
		/// <summary>left * right</summary>
		Multiply,
		/// <summary>left / right</summary>
		Divide,
		/// <summary>left % right</summary>
		Modulus,
		
		/// <summary>left &lt;&lt; right</summary>
		ShiftLeft,
		/// <summary>left &gt;&gt; right</summary>
		ShiftRight,
		
		/// <summary>left ?? right</summary>
		NullCoalescing
	}
}

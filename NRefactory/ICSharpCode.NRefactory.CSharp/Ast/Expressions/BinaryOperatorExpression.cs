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
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Left Operator Right
	/// </summary>
	public class BinaryOperatorExpression : Expression
	{
		public readonly static TokenRole BitwiseAndRole = new TokenRole ("&");
		public readonly static TokenRole BitwiseOrRole = new TokenRole ("|");
		public readonly static TokenRole ConditionalAndRole = new TokenRole ("&&");
		public readonly static TokenRole ConditionalOrRole = new TokenRole ("||");
		public readonly static TokenRole ExclusiveOrRole = new TokenRole ("^");
		public readonly static TokenRole GreaterThanRole = new TokenRole (">");
		public readonly static TokenRole GreaterThanOrEqualRole = new TokenRole (">=");
		public readonly static TokenRole EqualityRole = new TokenRole ("==");
		public readonly static TokenRole InEqualityRole = new TokenRole ("!=");
		public readonly static TokenRole LessThanRole = new TokenRole ("<");
		public readonly static TokenRole LessThanOrEqualRole = new TokenRole ("<=");
		public readonly static TokenRole AddRole = new TokenRole ("+");
		public readonly static TokenRole SubtractRole = new TokenRole ("-");
		public readonly static TokenRole MultiplyRole = new TokenRole ("*");
		public readonly static TokenRole DivideRole = new TokenRole ("/");
		public readonly static TokenRole ModulusRole = new TokenRole ("%");
		public readonly static TokenRole ShiftLeftRole = new TokenRole ("<<");
		public readonly static TokenRole ShiftRightRole = new TokenRole (">>");
		public readonly static TokenRole NullCoalescingRole = new TokenRole ("??");
		
		public readonly static Role<Expression> LeftRole = new Role<Expression>("Left", Expression.Null);
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
			get { return GetChildByRole (GetOperatorRole (Operator)); }
		}
		
		public Expression Right {
			get { return GetChildByRole (RightRole); }
			set { SetChildByRole (RightRole, value); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitBinaryOperatorExpression (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitBinaryOperatorExpression (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitBinaryOperatorExpression (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			BinaryOperatorExpression o = other as BinaryOperatorExpression;
			return o != null && (this.Operator == BinaryOperatorType.Any || this.Operator == o.Operator)
				&& this.Left.DoMatch(o.Left, match) && this.Right.DoMatch(o.Right, match);
		}
		
		public static TokenRole GetOperatorRole (BinaryOperatorType op)
		{
			switch (op) {
				case BinaryOperatorType.BitwiseAnd:
					return BitwiseAndRole;
				case BinaryOperatorType.BitwiseOr:
					return BitwiseOrRole;
				case BinaryOperatorType.ConditionalAnd:
					return ConditionalAndRole;
				case BinaryOperatorType.ConditionalOr:
					return ConditionalOrRole;
				case BinaryOperatorType.ExclusiveOr:
					return ExclusiveOrRole;
				case BinaryOperatorType.GreaterThan:
					return GreaterThanRole;
				case BinaryOperatorType.GreaterThanOrEqual:
					return GreaterThanOrEqualRole;
				case BinaryOperatorType.Equality:
					return EqualityRole;
				case BinaryOperatorType.InEquality:
					return InEqualityRole;
				case BinaryOperatorType.LessThan:
					return LessThanRole;
				case BinaryOperatorType.LessThanOrEqual:
					return LessThanOrEqualRole;
				case BinaryOperatorType.Add:
					return AddRole;
				case BinaryOperatorType.Subtract:
					return SubtractRole;
				case BinaryOperatorType.Multiply:
					return MultiplyRole;
				case BinaryOperatorType.Divide:
					return DivideRole;
				case BinaryOperatorType.Modulus:
					return ModulusRole;
				case BinaryOperatorType.ShiftLeft:
					return ShiftLeftRole;
				case BinaryOperatorType.ShiftRight:
					return ShiftRightRole;
				case BinaryOperatorType.NullCoalescing:
					return NullCoalescingRole;
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
		#region Builder methods
		public override MemberReferenceExpression Member(string memberName)
		{
			return new MemberReferenceExpression { Target = this, MemberName = memberName };
		}

		public override IndexerExpression Indexer(IEnumerable<Expression> arguments)
		{
			IndexerExpression expr = new IndexerExpression();
			expr.Target = new ParenthesizedExpression(this);
			expr.Arguments.AddRange(arguments);
			return expr;
		}

		public override IndexerExpression Indexer(params Expression[] arguments)
		{
			IndexerExpression expr = new IndexerExpression();
			expr.Target = new ParenthesizedExpression(this);
			expr.Arguments.AddRange(arguments);
			return expr;
		}

		public override InvocationExpression Invoke(string methodName, IEnumerable<AstType> typeArguments, IEnumerable<Expression> arguments)
		{
			InvocationExpression ie = new InvocationExpression();
			MemberReferenceExpression mre = new MemberReferenceExpression();
			mre.Target = new ParenthesizedExpression(this);
			mre.MemberName = methodName;
			mre.TypeArguments.AddRange(typeArguments);
			ie.Target = mre;
			ie.Arguments.AddRange(arguments);
			return ie;
		}

		public override InvocationExpression Invoke(IEnumerable<Expression> arguments)
		{
			InvocationExpression ie = new InvocationExpression();
			ie.Target = new ParenthesizedExpression(this);
			ie.Arguments.AddRange(arguments);
			return ie;
		}

		public override InvocationExpression Invoke(params Expression[] arguments)
		{
			InvocationExpression ie = new InvocationExpression();
			ie.Target = new ParenthesizedExpression(this);
			ie.Arguments.AddRange(arguments);
			return ie;
		}

		public override CastExpression CastTo(AstType type)
		{
			return new CastExpression { Type = type,  Expression = new ParenthesizedExpression(this) };
		}

		public override AsExpression CastAs(AstType type)
		{
			return new AsExpression { Type = type,  Expression = new ParenthesizedExpression(this) };
		}

		public override IsExpression IsType(AstType type)
		{
			return new IsExpression { Type = type,  Expression = new ParenthesizedExpression(this) };
		}
		#endregion
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

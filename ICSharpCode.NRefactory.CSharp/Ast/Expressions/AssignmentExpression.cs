// 
// AssignmentExpression.cs
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
	/// Left Operator= Right
	/// </summary>
	public class AssignmentExpression : Expression
	{
		// reuse roles from BinaryOperatorExpression
		public readonly static Role<Expression> LeftRole = BinaryOperatorExpression.LeftRole;
		public readonly static Role<Expression> RightRole = BinaryOperatorExpression.RightRole;
		
		public readonly static TokenRole AssignRole = new TokenRole ("=");
		public readonly static TokenRole AddRole = new TokenRole ("+=");
		public readonly static TokenRole SubtractRole = new TokenRole ("-=");
		public readonly static TokenRole MultiplyRole = new TokenRole ("*=");
		public readonly static TokenRole DivideRole = new TokenRole ("/=");
		public readonly static TokenRole ModulusRole = new TokenRole ("%=");
		public readonly static TokenRole ShiftLeftRole = new TokenRole ("<<=");
		public readonly static TokenRole ShiftRightRole = new TokenRole (">>=");
		public readonly static TokenRole BitwiseAndRole = new TokenRole ("&=");
		public readonly static TokenRole BitwiseOrRole = new TokenRole ("|=");
		public readonly static TokenRole ExclusiveOrRole = new TokenRole ("^=");
		
		public AssignmentExpression()
		{
		}
		
		public AssignmentExpression(Expression left, Expression right)
		{
			this.Left = left;
			this.Right = right;
		}
		
		public AssignmentExpression(Expression left, AssignmentOperatorType op, Expression right)
		{
			this.Left = left;
			this.Operator = op;
			this.Right = right;
		}
		
		public AssignmentOperatorType Operator {
			get;
			set;
		}
		
		public Expression Left {
			get { return GetChildByRole (LeftRole); }
			set { SetChildByRole(LeftRole, value); }
		}
		
		public CSharpTokenNode OperatorToken {
			get { return GetChildByRole (GetOperatorRole(Operator)); }
		}
		
		public Expression Right {
			get { return GetChildByRole (RightRole); }
			set { SetChildByRole(RightRole, value); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitAssignmentExpression (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitAssignmentExpression (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitAssignmentExpression (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			AssignmentExpression o = other as AssignmentExpression;
			return o != null && (this.Operator == AssignmentOperatorType.Any || this.Operator == o.Operator)
				&& this.Left.DoMatch(o.Left, match) && this.Right.DoMatch(o.Right, match);
		}
		
		public static TokenRole GetOperatorRole(AssignmentOperatorType op)
		{
			switch (op) {
				case AssignmentOperatorType.Assign:
					return AssignRole;
				case AssignmentOperatorType.Add:
					return AddRole;
				case AssignmentOperatorType.Subtract:
					return SubtractRole;
				case AssignmentOperatorType.Multiply:
					return MultiplyRole;
				case AssignmentOperatorType.Divide:
					return DivideRole;
				case AssignmentOperatorType.Modulus:
					return ModulusRole;
				case AssignmentOperatorType.ShiftLeft:
					return ShiftLeftRole;
				case AssignmentOperatorType.ShiftRight:
					return ShiftRightRole;
				case AssignmentOperatorType.BitwiseAnd:
					return BitwiseAndRole;
				case AssignmentOperatorType.BitwiseOr:
					return BitwiseOrRole;
				case AssignmentOperatorType.ExclusiveOr:
					return ExclusiveOrRole;
				default:
					throw new NotSupportedException("Invalid value for AssignmentOperatorType");
			}
		}
		
		/// <summary>
		/// Gets the binary operator for the specified compound assignment operator.
		/// Returns null if 'op' is not a compound assignment.
		/// </summary>
		public static BinaryOperatorType? GetCorrespondingBinaryOperator(AssignmentOperatorType op)
		{
			switch (op) {
				case AssignmentOperatorType.Assign:
					return null;
				case AssignmentOperatorType.Add:
					return BinaryOperatorType.Add;
				case AssignmentOperatorType.Subtract:
					return BinaryOperatorType.Subtract;
				case AssignmentOperatorType.Multiply:
					return BinaryOperatorType.Multiply;
				case AssignmentOperatorType.Divide:
					return BinaryOperatorType.Divide;
				case AssignmentOperatorType.Modulus:
					return BinaryOperatorType.Modulus;
				case AssignmentOperatorType.ShiftLeft:
					return BinaryOperatorType.ShiftLeft;
				case AssignmentOperatorType.ShiftRight:
					return BinaryOperatorType.ShiftRight;
				case AssignmentOperatorType.BitwiseAnd:
					return BinaryOperatorType.BitwiseAnd;
				case AssignmentOperatorType.BitwiseOr:
					return BinaryOperatorType.BitwiseOr;
				case AssignmentOperatorType.ExclusiveOr:
					return BinaryOperatorType.ExclusiveOr;
				default:
					throw new NotSupportedException("Invalid value for AssignmentOperatorType");
			}
		}
		
		public static ExpressionType GetLinqNodeType(AssignmentOperatorType op, bool checkForOverflow)
		{
			switch (op) {
				case AssignmentOperatorType.Assign:
					return ExpressionType.Assign;
				case AssignmentOperatorType.Add:
					return checkForOverflow ? ExpressionType.AddAssignChecked : ExpressionType.AddAssign;
				case AssignmentOperatorType.Subtract:
					return checkForOverflow ? ExpressionType.SubtractAssignChecked : ExpressionType.SubtractAssign;
				case AssignmentOperatorType.Multiply:
					return checkForOverflow ? ExpressionType.MultiplyAssignChecked : ExpressionType.MultiplyAssign;
				case AssignmentOperatorType.Divide:
					return ExpressionType.DivideAssign;
				case AssignmentOperatorType.Modulus:
					return ExpressionType.ModuloAssign;
				case AssignmentOperatorType.ShiftLeft:
					return ExpressionType.LeftShiftAssign;
				case AssignmentOperatorType.ShiftRight:
					return ExpressionType.RightShiftAssign;
				case AssignmentOperatorType.BitwiseAnd:
					return ExpressionType.AndAssign;
				case AssignmentOperatorType.BitwiseOr:
					return ExpressionType.OrAssign;
				case AssignmentOperatorType.ExclusiveOr:
					return ExpressionType.ExclusiveOrAssign;
				default:
					throw new NotSupportedException("Invalid value for AssignmentOperatorType");
			}
		}
	}
	
	public enum AssignmentOperatorType
	{
		/// <summary>left = right</summary>
		Assign,
		
		/// <summary>left += right</summary>
		Add,
		/// <summary>left -= right</summary>
		Subtract,
		/// <summary>left *= right</summary>
		Multiply,
		/// <summary>left /= right</summary>
		Divide,
		/// <summary>left %= right</summary>
		Modulus,
		
		/// <summary>left <<= right</summary>
		ShiftLeft,
		/// <summary>left >>= right</summary>
		ShiftRight,
		
		/// <summary>left &= right</summary>
		BitwiseAnd,
		/// <summary>left |= right</summary>
		BitwiseOr,
		/// <summary>left ^= right</summary>
		ExclusiveOr,
		
		/// <summary>Any operator (for pattern matching)</summary>
		Any
	}
}

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

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Left Operator= Right
	/// </summary>
	public class AssignmentExpression : Expression
	{
		// reuse roles from BinaryOperatorExpression
		public readonly static Role<Expression> LeftRole = BinaryOperatorExpression.LeftRole;
		public readonly static Role<CSharpTokenNode> OperatorRole = BinaryOperatorExpression.OperatorRole;
		public readonly static Role<Expression> RightRole = BinaryOperatorExpression.RightRole;
		
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
			get { return GetChildByRole (OperatorRole); }
		}
		
		public Expression Right {
			get { return GetChildByRole (RightRole); }
			set { SetChildByRole(RightRole, value); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data = default(T))
		{
			return visitor.VisitAssignmentExpression (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			AssignmentExpression o = other as AssignmentExpression;
			return o != null && (this.Operator == AssignmentOperatorType.Any || this.Operator == o.Operator)
				&& this.Left.DoMatch(o.Left, match) && this.Right.DoMatch(o.Right, match);
		}
		
		public static string GetOperatorSymbol(AssignmentOperatorType op)
		{
			switch (op) {
				case AssignmentOperatorType.Assign:
					return "=";
				case AssignmentOperatorType.Add:
					return "+=";
				case AssignmentOperatorType.Subtract:
					return "-=";
				case AssignmentOperatorType.Multiply:
					return "*=";
				case AssignmentOperatorType.Divide:
					return "/=";
				case AssignmentOperatorType.Modulus:
					return "%=";
				case AssignmentOperatorType.ShiftLeft:
					return "<<=";
				case AssignmentOperatorType.ShiftRight:
					return ">>=";
				case AssignmentOperatorType.BitwiseAnd:
					return "&=";
				case AssignmentOperatorType.BitwiseOr:
					return "|=";
				case AssignmentOperatorType.ExclusiveOr:
					return "^=";
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

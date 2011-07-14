// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// Operator Expression
	/// </summary>
	public class UnaryOperatorExpression : Expression
	{
		public readonly static Role<VBTokenNode> OperatorRole = BinaryOperatorExpression.OperatorRole;
		
		public UnaryOperatorExpression()
		{
		}
		
		public UnaryOperatorExpression(UnaryOperatorType op, Expression expression)
		{
			this.Operator = op;
			this.Expression = expression;
		}
		
		public UnaryOperatorType Operator {
			get;
			set;
		}
		
		public VBTokenNode OperatorToken {
			get { return GetChildByRole (OperatorRole); }
		}
		
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitUnaryOperatorExpression(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			UnaryOperatorExpression o = other as UnaryOperatorExpression;
			return o != null && this.Operator == o.Operator && this.Expression.DoMatch(o.Expression, match);
		}
		
		public static string GetOperatorSymbol(UnaryOperatorType op)
		{
			switch (op) {
				case UnaryOperatorType.Not:
					return "Not";
				case UnaryOperatorType.Minus:
					return "-";
				case UnaryOperatorType.Plus:
					return "+";
				default:
					throw new NotSupportedException("Invalid value for UnaryOperatorType");
			}
		}
	}
	
	public enum UnaryOperatorType
	{
		/// <summary>Logical/Bitwise not (Not a)</summary>
		Not,
		/// <summary>Unary minus (-a)</summary>
		Minus,
		/// <summary>Unary plus (+a)</summary>
		Plus,
		/// <summary>AddressOf</summary>
		AddressOf,
		/// <summary>Await</summary>
		Await
	}
	
}

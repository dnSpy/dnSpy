// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class BinaryOperatorExpression : Expression
	{
		public readonly static Role<Expression> LeftExpressionRole = new Role<Expression>("Left");
		public readonly static Role<VBTokenNode> OperatorRole = new Role<VBTokenNode>("Operator");
		public readonly static Role<Expression> RightExpressionRole = new Role<Expression>("Right");
		
		public BinaryOperatorExpression(Expression left, BinaryOperatorType type, Expression right)
		{
			AddChild(left, LeftExpressionRole);
			AddChild(right, RightExpressionRole);
			Operator = type;
		}
		
		public Expression Left {
			get { return GetChildByRole(LeftExpressionRole); }
			set { SetChildByRole(LeftExpressionRole, value); }
		}
		
		public BinaryOperatorType Operator { get; set; }
		
		public Expression Right {
			get { return GetChildByRole(RightExpressionRole); }
			set { SetChildByRole(RightExpressionRole, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitBinaryOperatorExpression(this, data);
		}
	}
	
		public enum BinaryOperatorType
	{
		None,
		
		/// <summary>'&amp;' in C#, 'And' in VB.</summary>
		BitwiseAnd,
		/// <summary>'|' in C#, 'Or' in VB.</summary>
		BitwiseOr,
		/// <summary>'&amp;&amp;' in C#, 'AndAlso' in VB.</summary>
		LogicalAnd,
		/// <summary>'||' in C#, 'OrElse' in VB.</summary>
		LogicalOr,
		/// <summary>'^' in C#, 'Xor' in VB.</summary>
		ExclusiveOr,
		
		/// <summary>&gt;</summary>
		GreaterThan,
		/// <summary>&gt;=</summary>
		GreaterThanOrEqual,
		/// <summary>'==' in C#, '=' in VB.</summary>
		Equality,
		/// <summary>'!=' in C#, '&lt;&gt;' in VB.</summary>
		InEquality,
		/// <summary>&lt;</summary>
		LessThan,
		/// <summary>&lt;=</summary>
		LessThanOrEqual,
		
		/// <summary>+</summary>
		Add,
		/// <summary>-</summary>
		Subtract,
		/// <summary>*</summary>
		Multiply,
		/// <summary>/</summary>
		Divide,
		/// <summary>'%' in C#, 'Mod' in VB.</summary>
		Modulus,
		/// <summary>VB-only: \</summary>
		DivideInteger,
		/// <summary>VB-only: ^</summary>
		Power,
		/// <summary>VB-only: &amp;</summary>
		Concat,
		
		/// <summary>C#: &lt;&lt;</summary>
		ShiftLeft,
		/// <summary>C#: &gt;&gt;</summary>
		ShiftRight,
		/// <summary>VB-only: Is</summary>
		ReferenceEquality,
		/// <summary>VB-only: IsNot</summary>
		ReferenceInequality,
		
		/// <summary>VB-only: Like</summary>
		Like,
		/// <summary>
		/// 	C#: ??
		/// 	VB: IF(x, y)
		/// </summary>
		NullCoalescing,
		
		/// <summary>VB-only: !</summary>
		DictionaryAccess
	}
}

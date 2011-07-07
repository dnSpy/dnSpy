// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public abstract class Expression : AstNode
	{
		#region Null
		public new static readonly Expression Null = new NullExpression ();
		
		sealed class NullExpression : Expression
		{
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
			{
				return default (S);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return other == null || other.IsNull;
			}
		}
		#endregion
	}
	
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
	
	public class AssignmentExpression : Expression
	{
		public readonly static Role<Expression> LeftExpressionRole = BinaryOperatorExpression.LeftExpressionRole;
		public readonly static Role<VBTokenNode> OperatorRole = BinaryOperatorExpression.OperatorRole;
		public readonly static Role<Expression> RightExpressionRole = BinaryOperatorExpression.RightExpressionRole;
		
		public AssignmentExpression(Expression left, AssignmentOperatorType type, Expression right)
		{
			AddChild(left, LeftExpressionRole);
			AddChild(right, RightExpressionRole);
			Operator = type;
		}
		
		public Expression Left {
			get { return GetChildByRole(LeftExpressionRole); }
			set { SetChildByRole(LeftExpressionRole, value); }
		}
		
		public AssignmentOperatorType Operator { get; set; }
		
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
			return visitor.VisitAssignmentExpression(this, data);
		}
	}
	
	/// <summary>
	/// Target(Arguments)
	/// </summary>
	public class InvocationExpression : Expression
	{
		public Expression Target {
			get { return GetChildByRole (Roles.TargetExpression); }
			set { SetChildByRole(Roles.TargetExpression, value); }
		}
		
		public AstNodeCollection<Expression> Arguments {
			get { return GetChildrenByRole<Expression>(Roles.Argument); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitInvocationExpression(this, data);
		}
		
		public InvocationExpression ()
		{
		}
		
		public InvocationExpression (Expression target, IEnumerable<Expression> arguments)
		{
			AddChild (target, Roles.TargetExpression);
			if (arguments != null) {
				foreach (var arg in arguments) {
					AddChild (arg, Roles.Argument);
				}
			}
		}
		
		public InvocationExpression (Expression target, params Expression[] arguments) : this (target, (IEnumerable<Expression>)arguments)
		{
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			InvocationExpression o = other as InvocationExpression;
			return o != null && this.Target.DoMatch(o.Target, match) && this.Arguments.DoMatch(o.Arguments, match);
		}
	}
	
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
		Plus
	}
	
	/// <summary>
	/// Represents a named argument passed to a method or attribute.
	/// </summary>
	public class NamedArgumentExpression : Expression
	{
		public Identifier Identifier {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public VBTokenNode AssignToken {
			get { return GetChildByRole (Roles.Assign); }
		}
		
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitNamedArgumentExpression(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			NamedArgumentExpression o = other as NamedArgumentExpression;
			return o != null && this.Identifier.DoMatch(o.Identifier, match) && this.Expression.DoMatch(o.Expression, match);
		}
	}
	
	/// <summary>
	/// Identifier As Type = Expression
	/// </summary>
	public class VariableInitializer : AstNode
	{
		public VariableIdentifier Identifier {
			get { return GetChildByRole(VariableIdentifier.VariableIdentifierRole); }
			set { SetChildByRole(VariableIdentifier.VariableIdentifierRole, value); }
		}
		
		public AstType Type {
			get { return GetChildByRole(Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}
		
		public VBTokenNode AssignToken {
			get { return GetChildByRole (Roles.Assign); }
		}
		
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitVariableInitializer(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			VariableInitializer o = other as VariableInitializer;
			return o != null && this.Identifier.DoMatch(o.Identifier, match) && this.Expression.DoMatch(o.Expression, match);
		}
	}
	
	/// <summary>
	/// [ Key ] .Identifier = Expression
	/// </summary>
	public class FieldInitializerExpression : Expression
	{
		public bool IsKey { get; set; }
		
		public VBTokenNode KeyToken {
			get { return GetChildByRole (Roles.Keyword); }
		}
		
		public VBTokenNode DotToken {
			get { return GetChildByRole (Roles.Dot); }
		}
		
		public Identifier Identifier {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public VBTokenNode AssignToken {
			get { return GetChildByRole (Roles.Assign); }
		}
		
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitFieldInitializerExpression(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			FieldInitializerExpression o = other as FieldInitializerExpression;
			return o != null && this.IsKey == o.IsKey && this.Identifier.DoMatch(o.Identifier, match) && this.Expression.DoMatch(o.Expression, match);
		}
	}
	
	public class ConditionalExpression : Expression
	{
		public readonly static Role<Expression> ConditionExpressionRole = new Role<Expression>("ConditionExpressionRole", Expression.Null);
		public readonly static Role<Expression> TrueExpressionRole = new Role<Expression>("TrueExpressionRole", Expression.Null);
		public readonly static Role<Expression> FalseExpressionRole = new Role<Expression>("FalseExpressionRole", Expression.Null);
		
		public VBTokenNode IfToken {
			get { return GetChildByRole (Roles.Keyword); }
		}
		
		public Expression ConditionExpression {
			get { return GetChildByRole (ConditionExpressionRole); }
			set { SetChildByRole (ConditionExpressionRole, value); }
		}
		
		public Expression TrueExpression {
			get { return GetChildByRole (TrueExpressionRole); }
			set { SetChildByRole (TrueExpressionRole, value); }
		}
		
		public Expression FalseExpression {
			get { return GetChildByRole (FalseExpressionRole); }
			set { SetChildByRole (FalseExpressionRole, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitConditionalExpression(this, data);
		}
	}
}

// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class SelectStatement : Statement
	{
		public Expression Expression {
			get { return GetChildByRole(Roles.Expression); }
			set { SetChildByRole(Roles.Expression, value); }
		}
		
		public AstNodeCollection<CaseStatement> Cases {
			get { return GetChildrenByRole(CaseStatement.CaseStatementRole); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSelectStatement(this, data);
		}
	}
	
	public class CaseStatement : Statement
	{
		public static readonly Role<CaseStatement> CaseStatementRole = new Role<CaseStatement>("CaseStatement");
		
		public AstNodeCollection<CaseClause> Clauses {
			get { return GetChildrenByRole(CaseClause.CaseClauseRole); }
		}
		
		public BlockStatement Body {
			get { return GetChildByRole(Roles.Body); }
			set { SetChildByRole(Roles.Body, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitCaseStatement(this, data);
		}
	}
	
	public abstract class CaseClause : AstNode
	{
		#region Null
		public new static readonly CaseClause Null = new NullCaseClause();
		
		sealed class NullCaseClause : CaseClause
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
		
		public static readonly Role<CaseClause> CaseClauseRole = new Role<CaseClause>("CaseClause", CaseClause.Null);
		
		public Expression Expression {
			get { return GetChildByRole(Roles.Expression); }
			set { SetChildByRole(Roles.Expression, value); }
		}
	}

	public class SimpleCaseClause : CaseClause
	{
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSimpleCaseClause(this, data);
		}
	}

	public class RangeCaseClause : CaseClause
	{
		public static readonly Role<Expression> ToExpressionRole = ForStatement.ToExpressionRole;
		
		public Expression ToExpression {
			get { return GetChildByRole(ToExpressionRole); }
			set { SetChildByRole(ToExpressionRole, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitRangeCaseClause(this, data);
		}
	}

	public class ComparisonCaseClause : CaseClause
	{
		public static readonly Role<VBTokenNode> OperatorRole = BinaryOperatorExpression.OperatorRole;
		
		public ComparisonOperator Operator { get; set; }
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitComparisonCaseClause(this, data);
		}
	}

	public enum ComparisonOperator
	{
		Equality = BinaryOperatorType.Equality,
		InEquality = BinaryOperatorType.InEquality,
		LessThan = BinaryOperatorType.LessThan,
		GreaterThan = BinaryOperatorType.GreaterThan,
		LessThanOrEqual = BinaryOperatorType.LessThanOrEqual,
		GreaterThanOrEqual = BinaryOperatorType.GreaterThanOrEqual
	}
}

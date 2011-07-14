// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class QueryExpression : Expression
	{
		public AstNodeCollection<QueryOperator> QueryOperators {
			get { return GetChildrenByRole(QueryOperator.QueryOperatorRole); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpression(this, data);
		}
	}
	
	public abstract class QueryOperator : AstNode
	{
		#region Null
		public new static readonly QueryOperator Null = new NullQueryOperator();
		
		sealed class NullQueryOperator : QueryOperator
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
		
		public static readonly Role<QueryOperator> QueryOperatorRole = new Role<QueryOperator>("QueryOperator", QueryOperator.Null);
	}
	
	public class FromQueryOperator : QueryOperator
	{
		public AstNodeCollection<CollectionRangeVariableDeclaration> Variables {
			get { return GetChildrenByRole (CollectionRangeVariableDeclaration.CollectionRangeVariableDeclarationRole); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitFromQueryOperator(this, data);
		}
	}
	
	public class AggregateQueryOperator : QueryOperator
	{
		public CollectionRangeVariableDeclaration Variable {
			get { return GetChildByRole(CollectionRangeVariableDeclaration.CollectionRangeVariableDeclarationRole); }
			set { SetChildByRole(CollectionRangeVariableDeclaration.CollectionRangeVariableDeclarationRole, value); }
		}
		
		public AstNodeCollection<QueryOperator> SubQueryOperators {
			get { return GetChildrenByRole(QueryOperatorRole); }
		}
		
		public AstNodeCollection<VariableInitializer> IntoExpressions {
			get { return GetChildrenByRole(VariableInitializer.VariableInitializerRole); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitAggregateQueryOperator(this, data);
		}
	}
	
	public class SelectQueryOperator : QueryOperator
	{
		public AstNodeCollection<VariableInitializer> Variables {
			get { return GetChildrenByRole(VariableInitializer.VariableInitializerRole); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSelectQueryOperator(this, data);
		}
	}
	
	public class DistinctQueryOperator : QueryOperator
	{
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitDistinctQueryOperator(this, data);
		}
	}
	
	public class WhereQueryOperator : QueryOperator
	{
		public Expression Condition {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitWhereQueryOperator(this, data);
		}
	}
	
	public class OrderExpression : AstNode
	{
		public static readonly Role<OrderExpression> OrderExpressionRole = new Role<OrderExpression>("OrderExpression");
		
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public QueryOrderingDirection Direction { get; set; }
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitOrderExpression(this, data);
		}
	}
	
	public class OrderByQueryOperator : QueryOperator
	{
		public AstNodeCollection<OrderExpression> Expressions {
			get { return GetChildrenByRole(OrderExpression.OrderExpressionRole); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitOrderByQueryOperator(this, data);
		}
	}
	
	public class PartitionQueryOperator : QueryOperator
	{
		public PartitionKind Kind { get; set; }
		
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitPartitionQueryOperator(this, data);
		}
	}
	
	public class LetQueryOperator : QueryOperator
	{
		public AstNodeCollection<VariableInitializer> Variables {
			get { return GetChildrenByRole(VariableInitializer.VariableInitializerRole); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitLetQueryOperator(this, data);
		}
	}
	
	public class GroupByQueryOperator : QueryOperator
	{
		public static readonly Role<VariableInitializer> GroupExpressionRole = new Role<VariableInitializer>("GroupExpression");
		public static readonly Role<VariableInitializer> ByExpressionRole = new Role<VariableInitializer>("ByExpression");
		public static readonly Role<VariableInitializer> IntoExpressionRole = new Role<VariableInitializer>("IntoExpression");
		
		public AstNodeCollection<VariableInitializer> GroupExpressions {
			get { return GetChildrenByRole(GroupExpressionRole); }
		}
		
		public AstNodeCollection<VariableInitializer> ByExpressions {
			get { return GetChildrenByRole(ByExpressionRole); }
		}
		
		public AstNodeCollection<VariableInitializer> IntoExpressions {
			get { return GetChildrenByRole(IntoExpressionRole); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitGroupByQueryOperator(this, data);
		}
	}
	
	public class JoinQueryOperator : QueryOperator
	{
		#region Null
		public new static readonly JoinQueryOperator Null = new NullJoinQueryOperator();
		
		sealed class NullJoinQueryOperator : JoinQueryOperator
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
		
		public static readonly Role<JoinQueryOperator> JoinQueryOperatorRole = new Role<JoinQueryOperator>("JoinQueryOperator", JoinQueryOperator.Null);
		
		public CollectionRangeVariableDeclaration JoinVariable {
			get { return GetChildByRole(CollectionRangeVariableDeclaration.CollectionRangeVariableDeclarationRole); }
			set { SetChildByRole(CollectionRangeVariableDeclaration.CollectionRangeVariableDeclarationRole, value); }
		}
		
		public JoinQueryOperator SubJoinQuery {
			get { return GetChildByRole(JoinQueryOperatorRole); }
			set {  SetChildByRole(JoinQueryOperatorRole, value); }
		}
		
		public AstNodeCollection<JoinCondition> JoinConditions {
			get { return GetChildrenByRole(JoinCondition.JoinConditionRole); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitJoinQueryOperator(this, data);
		}
	}
	
	public class JoinCondition : AstNode
	{
		public static readonly Role<JoinCondition> JoinConditionRole = new Role<JoinCondition>("JoinCondition");
		
		public static readonly Role<Expression> LeftExpressionRole = BinaryOperatorExpression.LeftExpressionRole;
		public static readonly Role<Expression> RightExpressionRole = BinaryOperatorExpression.RightExpressionRole;
		
		public Expression Left {
			get { return GetChildByRole (LeftExpressionRole); }
			set { SetChildByRole (LeftExpressionRole, value); }
		}
		
		public Expression Right {
			get { return GetChildByRole (RightExpressionRole); }
			set { SetChildByRole (RightExpressionRole, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitJoinCondition(this, data);
		}
	}
	
	public class GroupJoinQueryOperator : JoinQueryOperator
	{
		public static readonly Role<VariableInitializer> IntoExpressionRole = GroupByQueryOperator.IntoExpressionRole;
		
		public AstNodeCollection<VariableInitializer> IntoExpressions {
			get { return GetChildrenByRole(IntoExpressionRole); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitGroupJoinQueryOperator(this, data);
		}
	}
}

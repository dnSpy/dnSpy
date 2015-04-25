// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
namespace ICSharpCode.NRefactory.CSharp
{
	public class QueryExpression : Expression
	{
		public static readonly Role<QueryClause> ClauseRole = new Role<QueryClause>("Clause");
		
		#region Null
		public new static readonly QueryExpression Null = new NullQueryExpression ();
		
		sealed class NullQueryExpression : QueryExpression
		{
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public override void AcceptVisitor (IAstVisitor visitor)
			{
				visitor.VisitNullNode(this);
			}
			
			public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
			{
				return visitor.VisitNullNode(this);
			}
			
			public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
			{
				return visitor.VisitNullNode(this, data);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return other == null || other.IsNull;
			}
		}
		#endregion
		
		public AstNodeCollection<QueryClause> Clauses {
			get { return GetChildrenByRole(ClauseRole); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitQueryExpression (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitQueryExpression (this);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpression (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			QueryExpression o = other as QueryExpression;
			return o != null && !o.IsNull && this.Clauses.DoMatch(o.Clauses, match);
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
	
	public abstract class QueryClause : AstNode
	{
		public override NodeType NodeType {
			get { return NodeType.QueryClause; }
		}
	}
	
	/// <summary>
	/// Represents a query continuation.
	/// "(from .. select ..) into Identifier" or "(from .. group .. by ..) into Identifier"
	/// Note that "join .. into .." is not a query continuation!
	/// 
	/// This is always the first(!!) clause in a query expression.
	/// The tree for "from a in b select c into d select e" looks like this:
	/// new QueryExpression {
	/// 	new QueryContinuationClause {
	/// 		PrecedingQuery = new QueryExpression {
	/// 			new QueryFromClause(a in b),
	/// 			new QuerySelectClause(c)
	/// 		},
	/// 		Identifier = d
	/// 	},
	/// 	new QuerySelectClause(e)
	/// }
	/// </summary>
	public class QueryContinuationClause : QueryClause
	{
		public static readonly Role<QueryExpression> PrecedingQueryRole = new Role<QueryExpression>("PrecedingQuery", QueryExpression.Null);
		public static readonly TokenRole IntoKeywordRole = new TokenRole ("into");
		
		public QueryExpression PrecedingQuery {
			get { return GetChildByRole(PrecedingQueryRole); }
			set { SetChildByRole(PrecedingQueryRole, value); }
		}
		
		public CSharpTokenNode IntoKeyword {
			get { return GetChildByRole (IntoKeywordRole); }
		}

		public string Identifier {
			get {
				return GetChildByRole (Roles.Identifier).Name;
			}
			set {
				SetChildByRole(Roles.Identifier, CSharp.Identifier.Create (value));
			}
		}
		
		public Identifier IdentifierToken {
			get { return GetChildByRole (Roles.Identifier); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitQueryContinuationClause (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitQueryContinuationClause (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryContinuationClause (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			QueryContinuationClause o = other as QueryContinuationClause;
			return o != null && MatchString(this.Identifier, o.Identifier) && this.PrecedingQuery.DoMatch(o.PrecedingQuery, match);
		}
	}
	
	public class QueryFromClause : QueryClause
	{
		public static readonly TokenRole FromKeywordRole =  new TokenRole ("from");
		public static readonly TokenRole InKeywordRole =  new TokenRole ("in");
		
		public CSharpTokenNode FromKeyword {
			get { return GetChildByRole (FromKeywordRole); }
		}

		public AstType Type {
			get { return GetChildByRole (Roles.Type); }
			set { SetChildByRole (Roles.Type, value); }
		}
		
		public string Identifier {
			get {
				return GetChildByRole (Roles.Identifier).Name;
			}
			set {
				SetChildByRole(Roles.Identifier, CSharp.Identifier.Create (value));
			}
		}
		
		public Identifier IdentifierToken {
			get { return GetChildByRole(Roles.Identifier); }
		}
		
		public CSharpTokenNode InKeyword {
			get { return GetChildByRole (InKeywordRole); }
		}

		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitQueryFromClause (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitQueryFromClause (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryFromClause (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			QueryFromClause o = other as QueryFromClause;
			return o != null && this.Type.DoMatch(o.Type, match) && MatchString(this.Identifier, o.Identifier)
				&& this.Expression.DoMatch(o.Expression, match);
		}
	}
	
	public class QueryLetClause : QueryClause
	{
		public readonly static TokenRole LetKeywordRole = new TokenRole ("let");
		
		public CSharpTokenNode LetKeyword {
			get { return GetChildByRole(LetKeywordRole); }
		}
		
		public string Identifier {
			get {
				return GetChildByRole(Roles.Identifier).Name;
			}
			set {
				SetChildByRole(Roles.Identifier, CSharp.Identifier.Create (value));
			}
		}
		
		public Identifier IdentifierToken {
			get { return GetChildByRole(Roles.Identifier); }
		}
		
		public CSharpTokenNode AssignToken {
			get { return GetChildByRole(Roles.Assign); }
		}
		
		public Expression Expression {
			get { return GetChildByRole(Roles.Expression); }
			set { SetChildByRole(Roles.Expression, value); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitQueryLetClause (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitQueryLetClause (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryLetClause (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			QueryLetClause o = other as QueryLetClause;
			return o != null && MatchString(this.Identifier, o.Identifier) && this.Expression.DoMatch(o.Expression, match);
		}
	}
	
	
	public class QueryWhereClause : QueryClause
	{
		public readonly static TokenRole WhereKeywordRole = new TokenRole ("where");

		public CSharpTokenNode WhereKeyword {
			get { return GetChildByRole (WhereKeywordRole); }
		}
		
		public Expression Condition {
			get { return GetChildByRole (Roles.Condition); }
			set { SetChildByRole (Roles.Condition, value); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitQueryWhereClause (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitQueryWhereClause (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryWhereClause (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			QueryWhereClause o = other as QueryWhereClause;
			return o != null && this.Condition.DoMatch(o.Condition, match);
		}
	}
	
	/// <summary>
	/// Represents a join or group join clause.
	/// </summary>
	public class QueryJoinClause : QueryClause
	{
		public static readonly TokenRole JoinKeywordRole = new TokenRole ("join");
		public static readonly Role<AstType> TypeRole = Roles.Type;
		public static readonly Role<Identifier> JoinIdentifierRole = Roles.Identifier;
		public static readonly TokenRole InKeywordRole =  new TokenRole ("in");
		public static readonly Role<Expression> InExpressionRole = Roles.Expression;
		public static readonly TokenRole OnKeywordRole =  new TokenRole ("on");
		public static readonly Role<Expression> OnExpressionRole = new Role<Expression>("OnExpression", Expression.Null);
		public static readonly TokenRole EqualsKeywordRole =  new TokenRole ("equals");
		public static readonly Role<Expression> EqualsExpressionRole = new Role<Expression>("EqualsExpression", Expression.Null);
		public static readonly TokenRole IntoKeywordRole =  new TokenRole ("into");
		public static readonly Role<Identifier> IntoIdentifierRole = new Role<Identifier>("IntoIdentifier", Identifier.Null);
		
		public bool IsGroupJoin {
			get { return !string.IsNullOrEmpty(this.IntoIdentifier); }
		}
		
		public CSharpTokenNode JoinKeyword {
			get { return GetChildByRole (JoinKeywordRole); }
		}
		
		public AstType Type {
			get { return GetChildByRole (TypeRole); }
			set { SetChildByRole (TypeRole, value); }
		}
		
		public string JoinIdentifier {
			get {
				return GetChildByRole(JoinIdentifierRole).Name;
			}
			set {
				SetChildByRole(JoinIdentifierRole, Identifier.Create (value));
			}
		}
		
		public Identifier JoinIdentifierToken {
			get { return GetChildByRole(JoinIdentifierRole); }
		}
		
		public CSharpTokenNode InKeyword {
			get { return GetChildByRole (InKeywordRole); }
		}
		
		public Expression InExpression {
			get { return GetChildByRole (InExpressionRole); }
			set { SetChildByRole (InExpressionRole, value); }
		}
		
		public CSharpTokenNode OnKeyword {
			get { return GetChildByRole (OnKeywordRole); }
		}
		
		public Expression OnExpression {
			get { return GetChildByRole (OnExpressionRole); }
			set { SetChildByRole (OnExpressionRole, value); }
		}
		
		public CSharpTokenNode EqualsKeyword {
			get { return GetChildByRole (EqualsKeywordRole); }
		}
		
		public Expression EqualsExpression {
			get { return GetChildByRole (EqualsExpressionRole); }
			set { SetChildByRole (EqualsExpressionRole, value); }
		}
		
		public CSharpTokenNode IntoKeyword {
			get { return GetChildByRole (IntoKeywordRole); }
		}
		
		public string IntoIdentifier {
			get {
				return GetChildByRole (IntoIdentifierRole).Name;
			}
			set {
				SetChildByRole(IntoIdentifierRole, Identifier.Create (value));
			}
		}
		
		public Identifier IntoIdentifierToken {
			get { return GetChildByRole(IntoIdentifierRole); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitQueryJoinClause (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitQueryJoinClause (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryJoinClause (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			QueryJoinClause o = other as QueryJoinClause;
			return o != null && this.IsGroupJoin == o.IsGroupJoin
				&& this.Type.DoMatch(o.Type, match) && MatchString(this.JoinIdentifier, o.JoinIdentifier)
				&& this.InExpression.DoMatch(o.InExpression, match) && this.OnExpression.DoMatch(o.OnExpression, match)
				&& this.EqualsExpression.DoMatch(o.EqualsExpression, match)
				&& MatchString(this.IntoIdentifier, o.IntoIdentifier);
		}
	}
	
	public class QueryOrderClause : QueryClause
	{
		public static readonly TokenRole OrderbyKeywordRole = new TokenRole ("orderby");
		public static readonly Role<QueryOrdering> OrderingRole = new Role<QueryOrdering>("Ordering");
		
		public CSharpTokenNode OrderbyToken {
			get { return GetChildByRole (OrderbyKeywordRole); }
		}
		
		public AstNodeCollection<QueryOrdering> Orderings {
			get { return GetChildrenByRole (OrderingRole); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitQueryOrderClause (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitQueryOrderClause (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryOrderClause (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			QueryOrderClause o = other as QueryOrderClause;
			return o != null && this.Orderings.DoMatch(o.Orderings, match);
		}
	}
	
	public class QueryOrdering : AstNode
	{
		public readonly static TokenRole AscendingKeywordRole = new TokenRole ("ascending");
		public readonly static TokenRole DescendingKeywordRole = new TokenRole ("descending");

		public override NodeType NodeType {
			get { return NodeType.Unknown; }
		}
		
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public QueryOrderingDirection Direction {
			get;
			set;
		}
		
		public CSharpTokenNode DirectionToken {
			get { return Direction == QueryOrderingDirection.Ascending ? GetChildByRole (AscendingKeywordRole) : GetChildByRole (DescendingKeywordRole); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitQueryOrdering (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitQueryOrdering (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryOrdering (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			QueryOrdering o = other as QueryOrdering;
			return o != null && this.Direction == o.Direction && this.Expression.DoMatch(o.Expression, match);
		}
	}
	
	public enum QueryOrderingDirection
	{
		None,
		Ascending,
		Descending
	}
	
	public class QuerySelectClause : QueryClause
	{
		public readonly static TokenRole SelectKeywordRole = new TokenRole ("select");

		public CSharpTokenNode SelectKeyword {
			get { return GetChildByRole (SelectKeywordRole); }
		}
		
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitQuerySelectClause (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitQuerySelectClause (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQuerySelectClause (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			QuerySelectClause o = other as QuerySelectClause;
			return o != null && this.Expression.DoMatch(o.Expression, match);
		}
	}
	
	public class QueryGroupClause : QueryClause
	{
		public static readonly TokenRole GroupKeywordRole = new TokenRole ("group");
		public static readonly Role<Expression> ProjectionRole = new Role<Expression>("Projection", Expression.Null);
		public static readonly TokenRole ByKeywordRole = new TokenRole ("by");
		public static readonly Role<Expression> KeyRole = new Role<Expression>("Key", Expression.Null);
		
		public CSharpTokenNode GroupKeyword {
			get { return GetChildByRole (GroupKeywordRole); }
		}
		
		public Expression Projection {
			get { return GetChildByRole (ProjectionRole); }
			set { SetChildByRole (ProjectionRole, value); }
		}
		
		public CSharpTokenNode ByKeyword {
			get { return GetChildByRole (ByKeywordRole); }
		}
		
		public Expression Key {
			get { return GetChildByRole (KeyRole); }
			set { SetChildByRole (KeyRole, value); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitQueryGroupClause (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitQueryGroupClause (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryGroupClause (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			QueryGroupClause o = other as QueryGroupClause;
			return o != null && this.Projection.DoMatch(o.Projection, match) && this.Key.DoMatch(o.Key, match);
		}
	}
}
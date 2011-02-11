// 
// QueryExpression.cs
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

namespace ICSharpCode.NRefactory.CSharp
{
	/* TODO: how do we represent clauses? is QueryExpressionFromClause an expression,
	 * or do we introduce a parent QueryExpression?
	public class QueryExpressionFromClause : AstNode
	{
		public const int FromKeywordRole = 100;
		public const int InKeywordRole = 101;
		
		public AstType Type {
			get { return GetChildByRole (Roles.Type); }
			set { SetChildByRole (Roles.Type, value); }
		}
		
		public string Identifier {
			get {
				return QueryIdentifier.Name;
			}
		}
		
		public Identifier QueryIdentifier {
			get {
				return (Identifier)GetChildByRole (Roles.Identifier) ?? ICSharpCode.NRefactory.CSharp.Identifier.Null;
			}
		}
		
		public AstNode Expression {
			get { return GetChildByRole (Roles.Expression) ?? AstNode.Null; }
		}
		
		public override S AcceptVisitor<T, S> (AstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpressionFromClause (this, data);
		}
	}
	
	public class QueryExpressionJoinClause : QueryExpressionFromClause
	{
		public const int OnExpressionRole     = 100;
		public const int EqualsExpressionRole = 101;
		public const int IntoIdentifierRole   = 102;
		
		public const int JoinKeywordRole     = 110;
		public new const int InKeywordRole       = 111;
		public const int OnKeywordRole       = 112;
		public const int EqualsKeywordRole   = 113;
		public const int IntoKeywordRole     = 114;
		
		public CSharpTokenNode JoinKeyword {
			get { return (CSharpTokenNode)GetChildByRole (JoinKeywordRole); }
		}
		public CSharpTokenNode InKeyword {
			get { return (CSharpTokenNode)GetChildByRole (InKeywordRole); }
		}
		public CSharpTokenNode OnKeyword {
			get { return (CSharpTokenNode)GetChildByRole (OnKeywordRole); }
		}
		public CSharpTokenNode EqualsKeyword {
			get { return (CSharpTokenNode)GetChildByRole (EqualsKeywordRole); }
		}
		public CSharpTokenNode IntoKeyword {
			get { return (CSharpTokenNode)GetChildByRole (IntoKeywordRole); }
		}
		
		
		public AstNode OnExpression {
			get {
				return GetChildByRole (OnExpressionRole);
			}
		}
		
		public AstNode EqualsExpression {
			get {
				return GetChildByRole (EqualsExpressionRole);
			}
		}
		
		public string IntoIdentifier {
			get {
				return IntoIdentifierIdentifier.Name;
			}
		}
		
		public Identifier IntoIdentifierIdentifier {
			get {
				return (Identifier)GetChildByRole (IntoIdentifierRole);
			}
		}
		
		public AstNode InExpression {
			get {
				return GetChildByRole (Roles.Expression);
			}
		}
		
		public override S AcceptVisitor<T, S> (AstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpressionJoinClause (this, data);
		}
	}
	
	public class QueryExpressionGroupClause : AstNode
	{
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public const int ProjectionExpressionRole = 100;
		public const int GroupByExpressionRole    = 101;
		
		public const int GroupKeywordRole    = 102;
		public const int ByKeywordRole    = 103;
		
		public CSharpTokenNode GroupKeyword {
			get { return (CSharpTokenNode)GetChildByRole (GroupKeywordRole); }
		}
		
		public CSharpTokenNode ByKeyword {
			get { return (CSharpTokenNode)GetChildByRole (ByKeywordRole); }
		}
		
		public AstNode Projection {
			get {
				return GetChildByRole (ProjectionExpressionRole);
			}
		}
		
		public AstNode GroupBy {
			get {
				return GetChildByRole (GroupByExpressionRole);
			}
		}
		
		public override S AcceptVisitor<T, S> (AstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpressionGroupClause (this, data);
		}
	}
	
	public class QueryExpressionLetClause : AstNode 
	{
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public string Identifier {
			get {
				return QueryIdentifier.Name;
			}
		}
		
		public Identifier QueryIdentifier {
			get {
				return (Identifier)GetChildByRole (Roles.Identifier);
			}
		}
		
		public AstNode Expression {
			get {
				return GetChildByRole (Roles.Expression);
			}
		}
		
		public CSharpTokenNode LetKeyword {
			get { return (CSharpTokenNode)GetChildByRole (Roles.Keyword); }
		}
		
		public AstNode Assign {
			get {
				return GetChildByRole (Roles.Assign);
			}
		}
		
		public override S AcceptVisitor<T, S> (AstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpressionLetClause (this, data);
		}
	}
	
	public class QueryExpressionOrderClause : AstNode
	{
		public const int OrderingRole = 100;
		
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public bool OrderAscending {
			get;
			set;
		}
		
		public AstNode Expression {
			get {
				return GetChildByRole (Roles.Expression);
			}
		}
		
		public CSharpTokenNode Keyword {
			get { return (CSharpTokenNode)GetChildByRole (Roles.Keyword); }
		}
		
		public override S AcceptVisitor<T, S> (AstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpressionOrderClause (this, data);
		}
	}
	
	public class QueryExpressionOrdering : AstNode
	{
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public QueryExpressionOrderingDirection Direction {
			get;
			set;
		}
		
		public AstNode Criteria {
			get {
				return GetChildByRole (Roles.Expression);
			}
		}
		
		public override S AcceptVisitor<T, S> (AstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpressionOrdering (this, data);
		}
	}
	
	public enum QueryExpressionOrderingDirection
	{
		Unknown,
		Ascending,
		Descending
	}
	
	public class QueryExpressionSelectClause : AstNode 
	{
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public CSharpTokenNode SelectKeyword {
			get { return (CSharpTokenNode)GetChildByRole (Roles.Keyword); }
		}
		
		public AstNode Projection {
			get {
				return GetChildByRole (Roles.Expression);
			}
		}
		
		public override S AcceptVisitor<T, S> (AstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpressionSelectClause (this, data);
		}
	}
	
	public class QueryExpressionWhereClause : AstNode 
	{
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public CSharpTokenNode WhereKeyword {
			get { return (CSharpTokenNode)GetChildByRole (Roles.Keyword); }
		}
		
		public AstNode Condition {
			get {
				return GetChildByRole (Roles.Condition);
			}
		}
		
		public override S AcceptVisitor<T, S> (AstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpressionWhereClause (this, data);
		}
		
	}
	*/
}
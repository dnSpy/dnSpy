// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

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
		
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			throw new NotImplementedException();
		}
	}
}

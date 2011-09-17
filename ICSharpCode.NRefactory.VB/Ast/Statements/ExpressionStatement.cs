// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// Expression
	/// </summary>
	// TODO this does not directly reflect the VB grammar!
	public class ExpressionStatement : Statement
	{
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitExpressionStatement(this, data);
		}
		
		public ExpressionStatement()
		{
		}
		
		public ExpressionStatement(Expression expression)
		{
			this.Expression = expression;
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ExpressionStatement o = other as ExpressionStatement;
			return o != null && this.Expression.DoMatch(o.Expression, match);
		}
	}
}

// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.VB.Ast
{
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

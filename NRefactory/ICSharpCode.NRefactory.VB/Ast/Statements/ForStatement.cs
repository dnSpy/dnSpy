// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class ForStatement : Statement
	{
		public static readonly Role<AstNode> VariableRole = new Role<AstNode>("Variable", AstNode.Null);
		public static readonly Role<Expression> ToExpressionRole = new Role<Expression>("ToExpression", Expression.Null);
		public static readonly Role<Expression> StepExpressionRole = new Role<Expression>("StepExpression", Expression.Null);
		
		public AstNode Variable {
			get { return GetChildByRole(VariableRole); }
			set { SetChildByRole(VariableRole, value); }
		}
		
		public Expression ToExpression {
			get { return GetChildByRole(ToExpressionRole); }
			set { SetChildByRole(ToExpressionRole, value); }
		}
		
		public Expression StepExpression {
			get { return GetChildByRole(StepExpressionRole); }
			set { SetChildByRole(StepExpressionRole, value); }
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
			return visitor.VisitForStatement(this, data);
		}
	}
}

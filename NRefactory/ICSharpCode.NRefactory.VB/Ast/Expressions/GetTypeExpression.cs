// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class GetTypeExpression : Expression
	{
		public GetTypeExpression()
		{
		}
		
		public AstType Type {
			get { return GetChildByRole(Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var expr = other as GetTypeExpression;
			return expr != null &&
				Type.DoMatch(expr.Type, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitGetTypeExpression(this, data);
		}
	}
}

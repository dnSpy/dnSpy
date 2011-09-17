// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class MemberAccessExpression : Expression
	{
		public MemberAccessExpression()
		{
		}
		
		public Expression Target {
			get { return GetChildByRole(Roles.Expression); }
			set { SetChildByRole(Roles.Expression, value); }
		}
		
		public Identifier MemberName {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public AstNodeCollection<AstType> TypeArguments {
			get { return GetChildrenByRole(Roles.TypeArgument); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var expr = other as MemberAccessExpression;
			return expr != null &&
				Target.DoMatch(expr.Target, match) &&
				MemberName.DoMatch(expr.MemberName, match) &&
				TypeArguments.DoMatch(expr.TypeArguments, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitMemberAccessExpression(this, data);
		}
	}
}

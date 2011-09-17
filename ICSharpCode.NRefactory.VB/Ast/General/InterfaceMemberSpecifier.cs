// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class InterfaceMemberSpecifier : AstNode
	{
		public static readonly Role<InterfaceMemberSpecifier> InterfaceMemberSpecifierRole = new Role<InterfaceMemberSpecifier>("InterfaceMemberSpecifier");
		
		public InterfaceMemberSpecifier(Expression target, Identifier member)
		{
			Target = target;
			Member = member;
		}
		
		public InterfaceMemberSpecifier(AstType target, string member)
		{
			Target = new TypeReferenceExpression(target);
			Member = new Identifier(member, TextLocation.Empty);
		}
		
		public Expression Target {
			get { return GetChildByRole(Roles.Expression); }
			set { SetChildByRole(Roles.Expression, value); }
		}
		
		public Identifier Member {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var expr = other as InterfaceMemberSpecifier;
			return expr != null &&
				Target.DoMatch(expr.Target, match) &&
				Member.DoMatch(expr.Member, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitInterfaceMemberSpecifier(this, data);
		}
	}
}

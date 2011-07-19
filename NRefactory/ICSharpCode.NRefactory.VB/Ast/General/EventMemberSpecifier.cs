// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class EventMemberSpecifier : AstNode
	{
		public static readonly Role<EventMemberSpecifier> EventMemberSpecifierRole = new Role<EventMemberSpecifier>("EventMemberSpecifier");
		
		public EventMemberSpecifier()
		{
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
			var expr = other as EventMemberSpecifier;
			return expr != null &&
				Target.DoMatch(expr.Target, match) &&
				Member.DoMatch(expr.Member, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitEventMemberSpecifier(this, data);
		}
	}
}

// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class AnonymousObjectCreationExpression : Expression
	{
		public AstNodeCollection<Expression> Initializer {
			get { return GetChildrenByRole(Roles.Expression); }
		}
		
		public AnonymousObjectCreationExpression ()
		{
		}
		
		public AnonymousObjectCreationExpression (IEnumerable<Expression> initializer)
		{
			foreach (var ini in initializer) {
				AddChild (ini, Roles.Expression);
			}
		}
		
		public AnonymousObjectCreationExpression (params Expression[] initializer) : this ((IEnumerable<Expression>)initializer)
		{
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitAnonymousObjectCreationExpression(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			var o = other as AnonymousObjectCreationExpression;
			return o != null && this.Initializer.DoMatch(o.Initializer, match);
		}
	}
}

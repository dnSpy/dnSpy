// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// Target(Arguments)
	/// </summary>
	public class InvocationExpression : Expression
	{
		public Expression Target {
			get { return GetChildByRole (Roles.TargetExpression); }
			set { SetChildByRole(Roles.TargetExpression, value); }
		}
		
		public AstNodeCollection<Expression> Arguments {
			get { return GetChildrenByRole<Expression>(Roles.Argument); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitInvocationExpression(this, data);
		}
		
		public InvocationExpression ()
		{
		}
		
		public InvocationExpression (Expression target, IEnumerable<Expression> arguments)
		{
			AddChild (target, Roles.TargetExpression);
			if (arguments != null) {
				foreach (var arg in arguments) {
					AddChild (arg, Roles.Argument);
				}
			}
		}
		
		public InvocationExpression (Expression target, params Expression[] arguments) : this (target, (IEnumerable<Expression>)arguments)
		{
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			InvocationExpression o = other as InvocationExpression;
			return o != null && this.Target.DoMatch(o.Target, match) && this.Arguments.DoMatch(o.Arguments, match);
		}
	}
}

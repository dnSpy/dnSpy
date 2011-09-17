// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// New Type(Arguments) { Initializer }
	/// </summary>
	public class ObjectCreationExpression : Expression
	{
		public readonly static Role<ArrayInitializerExpression> InitializerRole = ArrayInitializerExpression.InitializerRole;
		
		public AstType Type {
			get { return GetChildByRole (Roles.Type); }
			set { SetChildByRole (Roles.Type, value); }
		}
		
		public AstNodeCollection<Expression> Arguments {
			get { return GetChildrenByRole (Roles.Argument); }
		}
		
		public ArrayInitializerExpression Initializer {
			get { return GetChildByRole (InitializerRole); }
			set { SetChildByRole (InitializerRole, value); }
		}
		
		public ObjectCreationExpression()
		{
		}
		
		public ObjectCreationExpression (AstType type, IEnumerable<Expression> arguments = null)
		{
			AddChild (type, Roles.Type);
			if (arguments != null) {
				foreach (var arg in arguments) {
					AddChild (arg, Roles.Argument);
				}
			}
		}
		
		public ObjectCreationExpression (AstType type, params Expression[] arguments) : this (type, (IEnumerable<Expression>)arguments)
		{
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitObjectCreationExpression(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ObjectCreationExpression o = other as ObjectCreationExpression;
			return o != null && this.Type.DoMatch(o.Type, match) && this.Arguments.DoMatch(o.Arguments, match) && this.Initializer.DoMatch(o.Initializer, match);
		}
	}
}
